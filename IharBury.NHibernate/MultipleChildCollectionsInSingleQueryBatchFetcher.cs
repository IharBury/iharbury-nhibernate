using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate;
using NHibernate.Linq;

namespace IharBury.NHibernate
{
    /// <summary>
    /// Contains <see cref="ISession"/> extension methods to fetches multiple child collections in a single database query batch.
    /// </summary>
    public static class MultipleChildCollectionsInSingleQueryBatchFetcher
    {
        /// <summary>
        /// Fetches multiple child collections in a single database query batch
        /// while avoiding Cartesian product database queries
        /// which can return exponentially huge record count. 
        /// </summary>
        /// <param name="session">The NHibernate session.</param>
        /// <param name="queryWithoutFetching">
        /// The query to be executed which does not yet configure eager loading
        /// (does not contain <see cref="EagerFetchingExtensionMethods.Fetch{TOriginating,TRelated}"/>,
        /// <see cref="EagerFetchingExtensionMethods.FetchMany{TOriginating,TRelated}"/> and other similar methods).
        /// The query must have a limited number of parameters so small that
        /// even multiplied by the number of fetched relationships it is small enough so
        /// the database supports all the parameters in a single query batch.
        /// </param>
        /// <typeparam name="T">The type of the entity being loaded from the database.</typeparam>
        /// <returns>An interface that allows to configure fetches and execute the query.</returns>
        public static IFetch<T> FetchMultipleChildCollectionsInSingleQueryBatch<T>(
            this ISession session,
            IQueryable<T> queryWithoutFetching)
        {
            return new Fetch<T>(queryWithoutFetching);
        }

        /// <summary>
        /// Allows to configure fetching of child entities and child collections, and to execute the query.
        /// </summary>
        /// <typeparam name="T">The type of the entity being loaded from the database.</typeparam>
        public interface IFetch<T>
        {
            /// <summary>
            /// Fetches a child collection in the same database query batch.
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

        private sealed class Fetch<T> : IFetch<T>
        {
            private readonly IQueryable<T> queryWithoutFetching;
            private readonly List<IQueryable<T>> fetches = new List<IQueryable<T>>();

            public Fetch(IQueryable<T> queryWithoutFetching)
            {
                this.queryWithoutFetching = queryWithoutFetching;
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
                if (fetches.Any())
                {
                    // Fetch child collections in separate queries in the same batch to avoid Cartesian products.
                    // NHibernate uses the same projection object for all the quiries
                    // and just updates with fetched child collections.
                    var futures = fetches.Select(fetch => fetch.ToFuture()).ToList();
                    return futures.First().ToList();
                }

                return queryWithoutFetching.ToList();
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
            /// Fetches a child collection in the same database query batch.
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
