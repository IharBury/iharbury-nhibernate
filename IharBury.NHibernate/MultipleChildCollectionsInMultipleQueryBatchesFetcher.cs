using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Persister.Entity;

namespace IharBury.NHibernate
{
    /// <summary>
    /// Contains <see cref="ISession"/> extension methods to fetches multiple child collections in a multiple database query batches.
    /// </summary>
    public static class MultipleChildCollectionsInMultipleQueryBatchesFetcher
    {
        /// <summary>
        /// Loads a collection of entities from the database filtered by a collection of values of a property.
        /// Fetches multiple child collections in a multiple database query batches
        /// while avoiding Cartesian product database queries
        /// which can return exponentially huge record count. 
        /// </summary>
        /// <param name="session">The NHibernate session.</param>
        /// <param name="getQueryParameterCountLimit">A delegate that determines the maximum query parameter count supported by the database.</param>
        /// <typeparam name="T">The type of the entity being loaded from the database.</typeparam>
        /// <returns>An interface that allows to filter, configure fetches and execute the query.</returns>
        public static IFilter<T> FetchMultipleChildCollectionsInMultipleQueryBatches<T>(this ISession session, GetQueryParameterCountLimit getQueryParameterCountLimit)
        {
            return new Filter<T>(session, getQueryParameterCountLimit);
        }

        /// <summary>
        /// Allows to filter, configure fetching of child entities and child collections, and to execute the query.
        /// </summary>
        /// <typeparam name="T">The type of the entity being loaded from the database.</typeparam>
        public interface IFilter<T>
        {
            /// <summary>
            /// Filters the loaded collection of entities by a collection of values of a property.
            /// </summary>
            /// <param name="property">The property expression.</param>
            /// <param name="propertyValues">The values of the filtered property.</param>
            /// <typeparam name="TProperty">The type of the property to filter by.</typeparam>
            /// <returns>An interface that allows to configure fetches and execute the query.</returns>
            IFetch<T> FilterBy<TProperty>(Expression<Func<T, TProperty>> property, IEnumerable<TProperty> propertyValues);
        }

        private sealed class Filter<T> : IFilter<T>
        {
            private readonly ISession session;
            private readonly GetQueryParameterCountLimit getQueryParameterCountLimit;

            public Filter(ISession session, GetQueryParameterCountLimit getQueryParameterCountLimit)
            {
                this.session = session;
                this.getQueryParameterCountLimit = getQueryParameterCountLimit;
            }

            public IFetch<T> FilterBy<TProperty>(Expression<Func<T, TProperty>> property, IEnumerable<TProperty> propertyValues)
            {
                return new Fetch<T, TProperty>(session, propertyValues.ToList(), FilterBy, getQueryParameterCountLimit);

                IQueryable<T> FilterBy(IQueryable<T> query, IEnumerable<TProperty> propertyValueBatch)
                {
                    Expression<Func<TProperty, bool>> templateExpression =
                        propertyValue => propertyValueBatch.Contains(propertyValue);

                    var item = Expression.Parameter(typeof(T), "item");
                    var propertyExpression = Expression.Invoke(property, item);
                    var filterExpressionBody = Expression.Invoke(templateExpression, propertyExpression);
                    var filterExpression = Expression.Lambda<Func<T, bool>>(filterExpressionBody, item);
                    return query.Where(filterExpression);
                }
            }
        }

        /// <summary>
        /// Allows to configure fetching of child entities and child collections, and to execute the query.
        /// </summary>
        /// <typeparam name="T">The type of the entity being loaded from the database.</typeparam>
        public interface IFetch<T>
        {
            /// <summary>
            /// Fetches a child collection.
            /// </summary>
            /// <param name="children">An expression selecting the child collection.</param>
            /// <param name="then">An optional delegate that can fetch descendants.</param>
            /// <typeparam name="TChild">The type of the entities stored in the child collection.</typeparam>
            /// <returns>An interface that allows to configure additional fetches and execute the query.</returns>
            IFetch<T> FetchMany<TChild>(
                Expression<Func<T, IEnumerable<TChild>>> children,
                Action<IDescendantFetch<T, TChild>> then = null);

            /// <summary>
            /// Executes the query.
            /// </summary>
            /// <returns>The loaded entities.</returns>
            List<T> ToList();
        }

        private sealed class Fetch<T, TProperty> : IFetch<T>
        {
            private readonly ISession session;
            private readonly IList<TProperty> propertyValues;
            private readonly Func<IQueryable<T>, IEnumerable<TProperty>, IQueryable<T>> filterBy;
            private readonly GetQueryParameterCountLimit getQueryParameterCountLimit;
            private readonly IQueryable<T> queryWithoutFetching;
            private readonly List<IQueryable<T>> fetches = new List<IQueryable<T>>();

            public Fetch(
                ISession session,
                IList<TProperty> propertyValues,
                Func<IQueryable<T>, IEnumerable<TProperty>, IQueryable<T>> filterBy,
                GetQueryParameterCountLimit getQueryParameterCountLimit)
            {
                this.session = session;
                this.propertyValues = propertyValues;
                this.filterBy = filterBy;
                this.getQueryParameterCountLimit = getQueryParameterCountLimit;
                queryWithoutFetching = session.Query<T>();
            }

            public IFetch<T> FetchMany<TChild>(
                Expression<Func<T, IEnumerable<TChild>>> children,
                Action<IDescendantFetch<T, TChild>> then = null)
            {
                var childFetch = queryWithoutFetching.FetchMany(children);

                if (then == null)
                {
                    fetches.Add(childFetch);
                }
                else
                {
                    var descendantFetch = new DescendantFetch<T, TChild>(childFetch);
                    then(descendantFetch);
                    fetches.AddRange(descendantFetch.GetFetches());
                }

                return this;
            }

            public List<T> ToList()
            {
                IList<IQueryable<T>> unfilteredQueries;

                if (fetches.Any())
                {
                    unfilteredQueries = fetches;
                }
                else
                {
                    unfilteredQueries = new[] { queryWithoutFetching };
                }

                return propertyValues
                    .InBatchesOf(GetBatchSize(unfilteredQueries.Count))
                    .SelectMany(LoadBatch)
                    .ToList();
            }

            private int GetBatchSize(int queryCount)
            {
                int queryParameterCountLimit = getQueryParameterCountLimit(session, queryCount);

                // When the queried entities form a class hierarchy of a base entity class and one or more derived classes,
                // NHibernate may add literals identifying the classes into the query and the database may treat them as
                // implicit query parameters.
                int entityHierarchySize = GetEntityPersister().EntityMetamodel.SubclassEntityNames.Count;

                int batchSize = entityHierarchySize <= 1 
                    ? queryParameterCountLimit
                    : queryParameterCountLimit - entityHierarchySize;

                if (batchSize <= 0)
                {
                    throw new InvalidOperationException("The query is too complex.");
                }

                return batchSize;
            }

            private AbstractEntityPersister GetEntityPersister()
            {
                if (!(session.SessionFactory.GetClassMetadata(typeof(T)) is AbstractEntityPersister persister))
                {
                    throw new InvalidOperationException(
                        "NHibernate ISessionFactory did not return 'AbstractEntityPersister' on GetClassMetadata().");
                }

                return persister;
            }

            private List<T> LoadBatch(IList<TProperty> propertyValueBatch)
            {
                // Fetch child collections in separate queries in the same batch to avoid Cartesian products.
                // NHibernate uses the same projection object for all the quiries
                // and just updates with fetched child collections.
                var futures = fetches.Select(fetch => filterBy(fetch, propertyValueBatch).ToFuture()).ToList();
                return futures.First().ToList();
            }
        }

        /// <summary>
        /// Allows to configure fetching of child entities (and child collections) of a descendant entity.
        /// </summary>
        /// <typeparam name="T">The type of the entity being loaded from the database.</typeparam>
        /// <typeparam name="TDescendant">
        /// The type of the descendant entity for which the child entities and child collections are being fetched.
        /// </typeparam>
        public interface IDescendantFetch<T, TDescendant>
        {
            /// <summary>
            /// Fetches a child collection.
            /// </summary>
            /// <param name="children">An expression selecting the child collection.</param>
            /// <param name="then">An optional delegate that can fetch descendants.</param>
            /// <typeparam name="TChild">The type of the entities stored in the child collection.</typeparam>
            /// <returns>An interface that allows to configure additional fetches.</returns>
            IDescendantFetch<T, TDescendant> FetchMany<TChild>(
                Expression<Func<TDescendant, IEnumerable<TChild>>> children,
                Action<IDescendantFetch<T, TChild>> then = null);
        }

        private sealed class DescendantFetch<T, TDescendant> : IDescendantFetch<T, TDescendant>
        {
            private readonly INhFetchRequest<T, TDescendant> fetchFromParent;
            private readonly List<IQueryable<T>> fetches = new List<IQueryable<T>>();

            public DescendantFetch(INhFetchRequest<T, TDescendant> fetchFromParent)
            {
                this.fetchFromParent = fetchFromParent;
            }

            public List<IQueryable<T>> GetFetches()
            {
                return fetches.Any() ? fetches.ToList() : new List<IQueryable<T>> { fetchFromParent };
            }

            public IDescendantFetch<T, TDescendant> FetchMany<TChild>(
                Expression<Func<TDescendant, IEnumerable<TChild>>> children,
                Action<IDescendantFetch<T, TChild>> then = null)
            {
                var childFetch = fetchFromParent.ThenFetchMany(children);

                if (then == null)
                {
                    fetches.Add(childFetch);
                }
                else
                {
                    var descendantFetch = new DescendantFetch<T, TChild>(childFetch);
                    then(descendantFetch);
                    fetches.AddRange(descendantFetch.GetFetches());
                }

                return this;
            }
        }
    }
}
