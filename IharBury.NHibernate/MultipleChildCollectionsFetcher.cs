using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate.Linq;
using NHibernate;
using NHibernate.Type;
using System.Collections;

namespace IharBury.NHibernate
{
    /// <summary>
    /// Contains <see cref="IQueryable{T}"/> extension methods to fetches multiple child collections
    /// while avoiding Cartesian product database queries
    /// which can return exponentially huge record count. 
    /// </summary>
    public static class MultipleChildCollectionsFetcher
    {
        /// <summary>
        /// Fetches multiple child collections
        /// while avoiding Cartesian product database queries
        /// which can return exponentially huge record count.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being loaded from the database.</typeparam>
        /// <param name="query">The query which can fetch multiple child collections.</param>
        /// <returns>An interface that allows to configure batching and execute the query.</returns>
        public static IWithFastFetches<TEntity> WithFastFetches<TEntity>(this IQueryable<TEntity> query)
        {
            return new WithFastFetchesImplementation<TEntity>(query ?? throw new ArgumentNullException(nameof(query)));
        }

        /// <summary>
        /// Allows to filter, configure batching and to execute the query.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being loaded from the database.</typeparam>
        public interface IWithFastFetches<TEntity>
        {
            /// <summary>
            /// Fetches all the child collections in a single database query batch but in different queries.
            /// Preserves the order of entities (if there is any).
            /// The query must have a limited number of parameters so small that
            /// even multiplied by the number of fetched child collections it is small enough so
            /// the database supports all the parameters in a single query batch.
            /// </summary>
            /// <returns>The list of the loaded entities.</returns>
            List<TEntity> ToList();

            /// <summary>
            /// Configures additional filtering of the query based on a potentially large collection of values.
            /// Configures query execution in a number of query batches large enough so that
            /// the resulting query batch parameter count is supported by the database.
            /// Each batch is filtered by a subset of the given values and all the results are aggregated.
            /// </summary>
            /// <typeparam name="TProperty">The type of the property values for filtering in batches.</typeparam>
            /// <param name="propertyValues">The property values for filtering in batches.</param>
            /// <param name="session">The NHibernate session.</param>
            /// <param name="getQueryParameterCountLimit">
            /// A delegate that determines the maximum query parameter count supported by the database.
            /// </param>
            /// <returns>An interface that allows to configure additional filtering and execute the query.</returns>
            IInBatches<TEntity, TProperty> InBatches<TProperty>(
                IEnumerable<TProperty> propertyValues,
                ISession session,
                GetQueryParameterCountLimit getQueryParameterCountLimit);
        }

        private sealed class WithFastFetchesImplementation<TEntity> : IWithFastFetches<TEntity>
        {
            private readonly IQueryable<TEntity> query;

            public WithFastFetchesImplementation(IQueryable<TEntity> query)
            {
                this.query = query;
            }

            public List<TEntity> ToList()
            {
                var collectionFetchCollector = new CollectionFetchCollector();
                var collectionFetchRemover = new CollectionFetchRemover();

                var collectionFetches = collectionFetchCollector.CollectFrom(query.Expression);
                if (collectionFetches.Count < 2)
                    return query.ToList();

                var futures = collectionFetches
                    .Select(collectionFetch => collectionFetchRemover.RemoveExceptFrom(query, collectionFetch).ToFuture())
                    .ToList();
                return futures[0].ToList();
            }

            public IInBatches<TEntity, TProperty> InBatches<TProperty>(
                IEnumerable<TProperty> propertyValues,
                ISession session,
                GetQueryParameterCountLimit getQueryParameterCountLimit)
            {
                if (propertyValues == null)
                    throw new ArgumentNullException(nameof(propertyValues));
                if (session == null)
                    throw new ArgumentNullException(nameof(session));
                if (getQueryParameterCountLimit == null)
                    throw new ArgumentNullException(nameof(getQueryParameterCountLimit));

                return new InBatchesImplementation<TEntity, TProperty>(
                    query,
                    propertyValues.Distinct().ToList(),
                    session,
                    getQueryParameterCountLimit);
            }
        }

        /// <summary>
        /// Allows to configure additional filtering and execute the query.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being loaded from the database.</typeparam>
        /// <typeparam name="TProperty">The type of the property values for filtering in batches.</typeparam>
        public interface IInBatches<TEntity, TProperty>
        {
            /// <summary>
            /// Filters entities which have a property equal to one of the given property values.
            /// </summary>
            /// <param name="property">The property expression.</param>
            /// <returns>An interface that allows to configure additional filtering and execute the query.</returns>
            IWhere<TEntity, TProperty> WhereBatchItemEquals(Expression<Func<TEntity, TProperty>> property);

            /// <summary>
            /// Filters entities which have a child collection where at least on item has a property
            /// equal to one of the given property values.
            /// </summary>
            /// <param name="items">The child collection expression.</param>
            /// <param name="itemProperty">The child entity property expression.</param>
            /// <returns>An interface that allows to configure additional filtering and execute the query.</returns>
            IWhere<TEntity, TProperty> WhereBatchItemEqualsAny<TItem>(
                Expression<Func<TEntity, IEnumerable<TItem>>> items,
                Expression<Func<TItem, TProperty>> itemProperty);
        }

        private sealed class InBatchesImplementation<TEntity, TProperty> : IInBatches<TEntity, TProperty>
        {
            private readonly IQueryable<TEntity> query;
            private readonly List<TProperty> propertyValues;
            private readonly ISession session;
            private readonly GetQueryParameterCountLimit getQueryParameterCountLimit;

            public InBatchesImplementation(
                IQueryable<TEntity> query,
                List<TProperty> propertyValues,
                ISession session,
                GetQueryParameterCountLimit getQueryParameterCountLimit)
            {
                this.query = query;
                this.propertyValues = propertyValues;
                this.session = session;
                this.getQueryParameterCountLimit = getQueryParameterCountLimit;
            }

            public IWhere<TEntity, TProperty> WhereBatchItemEquals(Expression<Func<TEntity, TProperty>> property)
            {
                if (property == null)
                    throw new ArgumentNullException(nameof(property));

                return new WhereImplementation<TEntity, TProperty>(
                    query,
                    propertyValues,
                    session,
                    getQueryParameterCountLimit,
                    new List<Func<ParameterExpression, TProperty[], Expression>>
                    {
                        CreateWhereBatchItemEqualsBuilder(property)
                    });
            }

            public IWhere<TEntity, TProperty> WhereBatchItemEqualsAny<TItem>(
                Expression<Func<TEntity, IEnumerable<TItem>>> items,
                Expression<Func<TItem, TProperty>> itemProperty)
            {
                if (items == null)
                    throw new ArgumentNullException(nameof(items));
                if (itemProperty == null)
                    throw new ArgumentNullException(nameof(itemProperty));

                return new WhereImplementation<TEntity, TProperty>(
                    query,
                    propertyValues,
                    session,
                    getQueryParameterCountLimit,
                    new List<Func<ParameterExpression, TProperty[], Expression>>
                    {
                        CreateWhereBatchItemEqualsAnyBuilder(items, itemProperty)
                    });
            }
        }

        /// <summary>
        /// Allows to configure additional filtering and execute the query.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being loaded from the database.</typeparam>
        /// <typeparam name="TProperty">The type of the property values for filtering in batches.</typeparam>
        public interface IWhere<TEntity, TProperty>
        {
            /// <summary>
            /// Filters entities which are filtered by the previous methods
            /// or otherwise have a property equal to one of the given property values.
            /// </summary>
            /// <param name="property">The property expression.</param>
            /// <returns>An interface that allows to configure additional filtering and execute the query.</returns>
            IWhere<TEntity, TProperty> OrBatchItemEquals(Expression<Func<TEntity, TProperty>> property);

            /// <summary>
            /// Filters entities which are filtered by the previous methods
            /// or otherwise have a child collection where at least on item has a property
            /// equal to one of the given property values.
            /// </summary>
            /// <param name="items">The child collection expression.</param>
            /// <param name="itemProperty">The child entity property expression.</param>
            /// <returns>An interface that allows to configure additional filtering and execute the query.</returns>
            IWhere<TEntity, TProperty> OrBatchItemEqualsAny<TItem>(
                Expression<Func<TEntity, IEnumerable<TItem>>> items,
                Expression<Func<TItem, TProperty>> itemProperty);

            /// <summary>
            /// Executes the query. Does not preserve the order of items. Removes duplicate entities.
            /// </summary>
            /// <returns>The list of the loaded entities.</returns>
            List<TEntity> ToUnorderedDistinctList();
        }

        private sealed class WhereImplementation<TEntity, TProperty> : IWhere<TEntity, TProperty>
        {
            private readonly IQueryable<TEntity> query;
            private readonly List<TProperty> propertyValues;
            private readonly ISession session;
            private readonly GetQueryParameterCountLimit getQueryParameterCountLimit;
            private readonly List<Func<ParameterExpression, TProperty[], Expression>> batchFilterBuilders;

            public WhereImplementation(
                IQueryable<TEntity> query,
                List<TProperty> propertyValues,
                ISession session,
                GetQueryParameterCountLimit getQueryParameterCountLimit,
                List<Func<ParameterExpression, TProperty[], Expression>> batchFilterBuilders)
            {
                this.query = query;
                this.propertyValues = propertyValues;
                this.session = session;
                this.getQueryParameterCountLimit = getQueryParameterCountLimit;
                this.batchFilterBuilders = batchFilterBuilders;
            }

            public IWhere<TEntity, TProperty> OrBatchItemEquals(Expression<Func<TEntity, TProperty>> property)
            {
                if (property == null)
                    throw new ArgumentNullException(nameof(property));

                return new WhereImplementation<TEntity, TProperty>(
                    query,
                    propertyValues,
                    session,
                    getQueryParameterCountLimit,
                    batchFilterBuilders.Concat(new[] { CreateWhereBatchItemEqualsBuilder(property) }).ToList());
            }

            public IWhere<TEntity, TProperty> OrBatchItemEqualsAny<TItem>(
                Expression<Func<TEntity, IEnumerable<TItem>>> items,
                Expression<Func<TItem, TProperty>> itemProperty)
            {
                if (items == null)
                    throw new ArgumentNullException(nameof(items));
                if (itemProperty == null)
                    throw new ArgumentNullException(nameof(itemProperty));

                return new WhereImplementation<TEntity, TProperty>(
                    query,
                    propertyValues,
                    session,
                    getQueryParameterCountLimit,
                    batchFilterBuilders.Concat(new[] { CreateWhereBatchItemEqualsAnyBuilder(items, itemProperty) }).ToList());
            }

            public List<TEntity> ToUnorderedDistinctList()
            {
                var collectionFetchCollector = new CollectionFetchCollector();
                var collectionFetchRemover = new CollectionFetchRemover();
                var nonBatchParameterCount = GetQueryParameterCount();

                var collectionFetches = collectionFetchCollector.CollectFrom(query.Expression);
                var queryCount = Math.Max(collectionFetches.Count, 1);
                var parameterCountLimit = getQueryParameterCountLimit(session, queryCount);
                var batchSize = (parameterCountLimit - nonBatchParameterCount) / batchFilterBuilders.Count;
                if (batchSize < 1)
                    throw new InvalidOperationException("The query has too many parameters.");

                var result = new HashSet<TEntity>();

                var singleCollectionFetchQueries = collectionFetches
                    .Select(collectionFetch => collectionFetchRemover.RemoveExceptFrom(query, collectionFetch))
                    .ToList();

                foreach (var batch in propertyValues.InBatchesOf(batchSize))
                {
                    IEnumerable<TEntity> batchResult;

                    if (collectionFetches.Count < 2)
                    {
                        batchResult = BuildBatchQuery(query, batch);
                    }
                    else
                    {
                        var futures = singleCollectionFetchQueries
                            .Select(singleCollectionFetchQuery => BuildBatchQuery(singleCollectionFetchQuery, batch).ToFuture())
                            .ToList();
                        batchResult = futures[0];
                    }

                    foreach (var entity in batchResult)
                        result.Add(entity);
                }

                return result.ToList();
            }

            private int GetQueryParameterCount()
            {
                var sessionImplementation = session.GetSessionImplementation();
                var nHibernateLinqExpression =
                    new NhLinqExpression(query.Expression, sessionImplementation.Factory);
                var nHibernateQuery = sessionImplementation.CreateQuery(nHibernateLinqExpression);
                var nHibernateLinqExpressionParameters = nHibernateLinqExpression.ParameterValuesByName;
                var queryParameterCount = 0;

                foreach (var nHibernateQueryParameterName in nHibernateQuery.NamedParameters)
                {
                    var nHibernateQueryParameter = nHibernateLinqExpressionParameters[nHibernateQueryParameterName];
                    var nHibernateQueryParameterValue = nHibernateQueryParameter.Item1;
                    var nHibernateQueryParameterType = nHibernateQueryParameter.Item2?.ReturnedClass;

                    if (nHibernateQueryParameterValue == null)
                    {
                        if ((nHibernateQueryParameterType == null) ||
                                !typeof(IEnumerable).IsAssignableFrom(nHibernateQueryParameterType) ||
                                (nHibernateQueryParameter.Item2.ReturnedClass == typeof(string)))
                            queryParameterCount++;
                    }
                    else
                    {
                        if (nHibernateQueryParameterValue is string)
                        {
                            queryParameterCount++;
                        }
                        else if (nHibernateQueryParameterValue is ICollection collection)
                        {
                            queryParameterCount += collection.Count;
                        }
                        else if (nHibernateQueryParameterValue is IEnumerable enumerable)
                        {
                            queryParameterCount += enumerable.Cast<object>().Count();
                        }
                        else
                        {
                            queryParameterCount++;
                        }
                    }
                }

                return queryParameterCount;
            }

            private IQueryable<TEntity> BuildBatchQuery(IQueryable<TEntity> nonBatchQuery, TProperty[] batch)
            {
                var entityParameter = Expression.Parameter(typeof(TEntity), "entity");
                var batchFilters = batchFilterBuilders
                    .Select(batchFilterBuilder => batchFilterBuilder(entityParameter, batch))
                    .ToList();
                Expression combinedFilterExpression = null;

                foreach (var batchFilter in batchFilters)
                {
                    var filterExpression = batchFilter;
                    combinedFilterExpression = combinedFilterExpression == null
                        ? filterExpression
                        : Expression.OrElse(combinedFilterExpression, filterExpression);
                }

                return nonBatchQuery.Where(Expression.Lambda<Func<TEntity, bool>>(combinedFilterExpression, entityParameter));
            }
        }

        private static Func<ParameterExpression, TProperty[], Expression> CreateWhereBatchItemEqualsBuilder<TEntity, TProperty>(
            Expression<Func<TEntity, TProperty>> property)
        {
            return (entity, batch) =>
            {
                var entityPropertyExpression = Expression.Invoke(property, entity);
                Expression<Func<TProperty, bool>> templateExpression = propertyValue => batch.Contains(propertyValue);
                return Expression.Invoke(templateExpression, entityPropertyExpression);
            };
        }

        private static Func<ParameterExpression, TProperty[], Expression>
            CreateWhereBatchItemEqualsAnyBuilder<TEntity, TItem, TProperty>(
                Expression<Func<TEntity, IEnumerable<TItem>>> items,
                Expression<Func<TItem, TProperty>> itemProperty)
        {
            var whereBatchItemEqualsBuilder = CreateWhereBatchItemEqualsBuilder(itemProperty);

            return (entity, batch) =>
            {
                var itemCollectionExpression = Expression.Invoke(items, entity);
                var item = Expression.Parameter(typeof(TItem), "item");
                var whereBatchItemEquals = Expression.Lambda<Func<TItem, bool>>(whereBatchItemEqualsBuilder(item, batch), item);
                Expression<Func<IEnumerable<TItem>, Func<TItem, bool>, bool>> templateExpression =
                    (someItems, filter) => someItems.Any(filter);
                return Expression.Invoke(templateExpression, itemCollectionExpression, whereBatchItemEquals);
            };
        }
    }
}
