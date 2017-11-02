using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate.Linq;

namespace IharBury.NHibernate
{
    /// <summary>
    /// Contains <see cref="IQueryable{T}"/> extension methods to fetches multiple related collections
    /// while avoiding Cartesian product database queries
    /// which can return exponentially huge record count. 
    /// </summary>
    public static class MultipleRelatedCollectionsFetcher
    {
        /// <summary>
        /// Fetches multiple related collections
        /// while avoiding Cartesian product database queries
        /// which can return exponentially huge record count.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being loaded from the database.</typeparam>
        /// <param name="query">The query which can fetch multiple related collections.</param>
        /// <returns>An interface that allows to configure batching and execute the query.</returns>
        /// <remarks>
        /// The query should not use eager loading configured in the mapping.
        /// Such eager loading may be executed several times or it may not be executed at all.
        /// </remarks>
        public static IAvoidingCartesianProducts<TEntity> AvoidingCartesianProducts<TEntity>(this IQueryable<TEntity> query)
        {
            return new AvoidingCartesianProductsImplementation<TEntity>(query ?? throw new ArgumentNullException(nameof(query)));
        }

        /// <summary>
        /// Allows to filter, configure batching and to execute the query.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being loaded from the database.</typeparam>
        public interface IAvoidingCartesianProducts<TEntity>
        {
            /// <summary>
            /// Fetches all the related collections in a single database query batch but in different queries.
            /// Preserves the order of entities (if there is any).
            /// The query must have a limited number of parameters so small that
            /// even multiplied by the number of fetched related collections it is small enough so
            /// the database supports all the parameters in a single query batch.
            /// </summary>
            /// <returns>The list of the loaded entities.</returns>
            List<TEntity> ToList();

            /// <summary>
            /// Configures query execution in a number of query batches large enough so that
            /// the resulting query batch parameter count is supported by the database.
            /// Additional filtering of the query based on a potentially large collection of values
            /// is required to produce the batches.
            /// Each batch is then filtered by a subset of the collection and all the results are aggregated.
            /// </summary>
            /// <param name="getMaxSqlQueryParameterCount">
            /// A delegate that determines the maximum SQL query parameter count supported by the database.
            /// </param>
            /// <returns>An interface that allows to configure additional filtering and execute the query.</returns>
            /// <remarks>
            /// The <paramref name="getMaxSqlQueryParameterCount"/> parameter is database specific and will likely be the same
            /// for the entire application using it. It's best to define an extension method <c>InBatches</c> in the application
            /// which supplies this parameter.
            /// </remarks>
            IInBatches<TEntity> InBatches(GetMaxSqlQueryParameterCount getMaxSqlQueryParameterCount);
        }

        private sealed class AvoidingCartesianProductsImplementation<TEntity> : IAvoidingCartesianProducts<TEntity>
        {
            private readonly IQueryable<TEntity> query;

            public AvoidingCartesianProductsImplementation(IQueryable<TEntity> query)
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

            public IInBatches<TEntity> InBatches(GetMaxSqlQueryParameterCount getMaxSqlQueryParameterCount)
            {
                if (getMaxSqlQueryParameterCount == null)
                    throw new ArgumentNullException(nameof(getMaxSqlQueryParameterCount));

                return new InBatchesImplementation<TEntity>(query, getMaxSqlQueryParameterCount);
            }
        }

        /// <summary>
        /// Allows to configure additional filtering and execute the query.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being loaded from the database.</typeparam>
        public interface IInBatches<TEntity>
        {
            /// <summary>
            /// Configures filtering by a potentially large collection of values
            /// which is split into batches.
            /// </summary>
            /// <typeparam name="TProperty">The type of the property values for filtering in batches.</typeparam>
            /// <param name="propertyValues">The property values for filtering in batches.</param>
            /// <returns>An interface that allows to configure additional filtering and execute the query.</returns>
            IFilteredBy<TEntity, TProperty> FilteredBy<TProperty>(IEnumerable<TProperty> propertyValues);
        }

        internal sealed class InBatchesImplementation<TEntity> : IInBatches<TEntity>
        {
            private readonly IQueryable<TEntity> query;
            private readonly GetMaxSqlQueryParameterCount getMaxSqlQueryParameterCount;

            public InBatchesImplementation(IQueryable<TEntity> query, GetMaxSqlQueryParameterCount getMaxSqlQueryParameterCount)
            {
                this.query = query;
                this.getMaxSqlQueryParameterCount = getMaxSqlQueryParameterCount;
            }

            public IFilteredBy<TEntity, TProperty> FilteredBy<TProperty>(IEnumerable<TProperty> propertyValues)
            {
                if (propertyValues == null)
                    throw new ArgumentNullException(nameof(propertyValues));

                return new FilteredByImplementation<TEntity, TProperty>(
                    query,
                    getMaxSqlQueryParameterCount,
                    propertyValues.Distinct().ToList());
            }
        }

        /// <summary>
        /// Allows to configure additional filtering and execute the query.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being loaded from the database.</typeparam>
        /// <typeparam name="TProperty">The type of the property values for filtering in batches.</typeparam>
        public interface IFilteredBy<TEntity, TProperty>
        {
            /// <summary>
            /// Filters entities which have a property equal to one of the given property values.
            /// </summary>
            /// <param name="property">The property expression.</param>
            /// <returns>An interface that allows to configure additional filtering and execute the query.</returns>
            IFilteredOn<TEntity, TProperty> FilteredOn(Expression<Func<TEntity, TProperty>> property);

            /// <summary>
            /// Filters entities which have a related collection where at least on item has a property
            /// equal to one of the given property values.
            /// </summary>
            /// <param name="relatedCollection">The related collection.</param>
            /// <param name="relatedEntityProperty">The related entity property expression.</param>
            /// <returns>An interface that allows to configure additional filtering and execute the query.</returns>
            IFilteredOn<TEntity, TProperty> FilteredOnAnyRelated<TRelated>(
                Expression<Func<TEntity, IEnumerable<TRelated>>> relatedCollection,
                Expression<Func<TRelated, TProperty>> relatedEntityProperty);
        }

        private sealed class FilteredByImplementation<TEntity, TProperty> : IFilteredBy<TEntity, TProperty>
        {
            private readonly IQueryable<TEntity> query;
            private readonly GetMaxSqlQueryParameterCount getMaxSqlQueryParameterCount;
            private readonly List<TProperty> propertyValues;

            public FilteredByImplementation(
                IQueryable<TEntity> query,
                GetMaxSqlQueryParameterCount getMaxSqlQueryParameterCount,
                List<TProperty> propertyValues)
            {
                this.query = query;
                this.getMaxSqlQueryParameterCount = getMaxSqlQueryParameterCount;
                this.propertyValues = propertyValues;
            }

            public IFilteredOn<TEntity, TProperty> FilteredOn(Expression<Func<TEntity, TProperty>> property)
            {
                if (property == null)
                    throw new ArgumentNullException(nameof(property));

                return new FilteredOnImplementation<TEntity, TProperty>(
                    query,
                    getMaxSqlQueryParameterCount,
                    propertyValues,
                    new List<Func<ParameterExpression, TProperty[], Expression>>
                    {
                        CreateFilterOnExpressionBuilder(property)
                    });
            }

            public IFilteredOn<TEntity, TProperty> FilteredOnAnyRelated<TRelated>(
                Expression<Func<TEntity, IEnumerable<TRelated>>> relatedCollection,
                Expression<Func<TRelated, TProperty>> relatedEntityProperty)
            {
                if (relatedCollection == null)
                    throw new ArgumentNullException(nameof(relatedCollection));
                if (relatedEntityProperty == null)
                    throw new ArgumentNullException(nameof(relatedEntityProperty));

                return new FilteredOnImplementation<TEntity, TProperty>(
                    query,
                    getMaxSqlQueryParameterCount,
                    propertyValues,
                    new List<Func<ParameterExpression, TProperty[], Expression>>
                    {
                        CreateFilterOnAnyExpressionBuilder(relatedCollection, relatedEntityProperty)
                    });
            }
        }

        /// <summary>
        /// Allows to configure additional filtering and execute the query.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being loaded from the database.</typeparam>
        /// <typeparam name="TProperty">The type of the property values for filtering in batches.</typeparam>
        public interface IFilteredOn<TEntity, TProperty>
        {
            /// <summary>
            /// Filters entities which are filtered by the previous methods
            /// or otherwise have a property equal to one of the given property values.
            /// </summary>
            /// <param name="property">The property expression.</param>
            /// <returns>An interface that allows to configure additional filtering and execute the query.</returns>
            IFilteredOn<TEntity, TProperty> OrFilteredOn(Expression<Func<TEntity, TProperty>> property);

            /// <summary>
            /// Filters entities which are filtered by the previous methods
            /// or otherwise have a related collection where at least on item has a property
            /// equal to one of the given property values.
            /// </summary>
            /// <param name="relatedCollection">The related collection expression.</param>
            /// <param name="relatedEntityProperty">The related entity property expression.</param>
            /// <returns>An interface that allows to configure additional filtering and execute the query.</returns>
            IFilteredOn<TEntity, TProperty> OrFilteredOnAnyRelated<TRelated>(
                Expression<Func<TEntity, IEnumerable<TRelated>>> relatedCollection,
                Expression<Func<TRelated, TProperty>> relatedEntityProperty);

            /// <summary>
            /// Specifies the maximum SQL parameter count excluding the batch filter parameters.
            /// The higher the value, the less parameters are left available for the batch filter.
            /// So the value should be kept reasonably low.
            /// </summary>
            /// <param name="maxNonBatchSqlParameterCount">
            /// The maximum SQL parameter count excluding the batch filter parameters.
            /// </param>
            /// <returns>An interface that allows to execute the query.</returns>
            IWithMaxNonBatchSqlParameterCount<TEntity> WithMaxNonBatchSqlParameterCount(
                int maxNonBatchSqlParameterCount);
        }

        private sealed class FilteredOnImplementation<TEntity, TProperty> : IFilteredOn<TEntity, TProperty>
        {
            private readonly IQueryable<TEntity> query;
            private readonly List<TProperty> propertyValues;
            private readonly GetMaxSqlQueryParameterCount getMaxSqlQueryParameterCount;
            private readonly List<Func<ParameterExpression, TProperty[], Expression>> batchFilterBuilders;

            public FilteredOnImplementation(
                IQueryable<TEntity> query,
                GetMaxSqlQueryParameterCount getMaxSqlQueryParameterCount,
                List<TProperty> propertyValues,
                List<Func<ParameterExpression, TProperty[], Expression>> batchFilterBuilders)
            {
                this.query = query;
                this.propertyValues = propertyValues;
                this.getMaxSqlQueryParameterCount = getMaxSqlQueryParameterCount;
                this.batchFilterBuilders = batchFilterBuilders;
            }

            public IFilteredOn<TEntity, TProperty> OrFilteredOn(Expression<Func<TEntity, TProperty>> property)
            {
                if (property == null)
                    throw new ArgumentNullException(nameof(property));

                return new FilteredOnImplementation<TEntity, TProperty>(
                    query,
                    getMaxSqlQueryParameterCount,
                    propertyValues,
                    batchFilterBuilders.Concat(new[] { CreateFilterOnExpressionBuilder(property) }).ToList());
            }

            public IFilteredOn<TEntity, TProperty> OrFilteredOnAnyRelated<TRelated>(
                Expression<Func<TEntity, IEnumerable<TRelated>>> relatedCollection,
                Expression<Func<TRelated, TProperty>> relatedEntityProperty)
            {
                if (relatedCollection == null)
                    throw new ArgumentNullException(nameof(relatedCollection));
                if (relatedEntityProperty == null)
                    throw new ArgumentNullException(nameof(relatedEntityProperty));

                return new FilteredOnImplementation<TEntity, TProperty>(
                    query,
                    getMaxSqlQueryParameterCount,
                    propertyValues,
                    batchFilterBuilders
                        .Concat(new[] { CreateFilterOnAnyExpressionBuilder(relatedCollection, relatedEntityProperty) })
                        .ToList());
            }

            public IWithMaxNonBatchSqlParameterCount<TEntity> WithMaxNonBatchSqlParameterCount(int maxNonBatchSqlParameterCount)
            {
                if (maxNonBatchSqlParameterCount < 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(maxNonBatchSqlParameterCount),
                        maxNonBatchSqlParameterCount,
                        "Must be non-negative.");

                return new WithMaxNonBatchSqlParameterCountImplementation<TEntity,TProperty>(
                    query,
                    getMaxSqlQueryParameterCount,
                    propertyValues,
                    batchFilterBuilders,
                    maxNonBatchSqlParameterCount);
            }
        }

        /// <summary>
        /// Allows to execute the query.
        /// </summary>
        /// <typeparam name="TEntity">The type of the entity being loaded from the database.</typeparam>
        public interface IWithMaxNonBatchSqlParameterCount<TEntity>
        {
            /// <summary>
            /// Executes the query. Does not preserve the order of items. Removes duplicate entities.
            /// </summary>
            /// <returns>The list of the loaded entities.</returns>
            List<TEntity> ToUnorderedDistinctList();
        }

        private sealed class WithMaxNonBatchSqlParameterCountImplementation<TEntity, TProperty> :
            IWithMaxNonBatchSqlParameterCount<TEntity>
        {
            private readonly IQueryable<TEntity> query;
            private readonly List<TProperty> propertyValues;
            private readonly GetMaxSqlQueryParameterCount getMaxSqlQueryParameterCount;
            private readonly List<Func<ParameterExpression, TProperty[], Expression>> batchFilterBuilders;
            private readonly int maxNonBatchSqlParameterCount;

            public WithMaxNonBatchSqlParameterCountImplementation(
                IQueryable<TEntity> query,
                GetMaxSqlQueryParameterCount getMaxSqlQueryParameterCount,
                List<TProperty> propertyValues,
                List<Func<ParameterExpression, TProperty[], Expression>> batchFilterBuilders,
                int maxNonBatchSqlParameterCount)
            {
                this.query = query;
                this.propertyValues = propertyValues;
                this.getMaxSqlQueryParameterCount = getMaxSqlQueryParameterCount;
                this.batchFilterBuilders = batchFilterBuilders;
                this.maxNonBatchSqlParameterCount = maxNonBatchSqlParameterCount;
            }

            public List<TEntity> ToUnorderedDistinctList()
            {
                var collectionFetchCollector = new CollectionFetchCollector();
                var collectionFetchRemover = new CollectionFetchRemover();

                var collectionFetches = collectionFetchCollector.CollectFrom(query.Expression);
                var queryCount = Math.Max(collectionFetches.Count, 1);
                var parameterCountLimit = getMaxSqlQueryParameterCount(queryCount);
                var batchSize = (parameterCountLimit - maxNonBatchSqlParameterCount) / batchFilterBuilders.Count;
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

        private static Func<ParameterExpression, TProperty[], Expression> CreateFilterOnExpressionBuilder<TEntity, TProperty>(
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
            CreateFilterOnAnyExpressionBuilder<TEntity, TRelated, TProperty>(
                Expression<Func<TEntity, IEnumerable<TRelated>>> relatedCollection,
                Expression<Func<TRelated, TProperty>> relatedEntityProperty)
        {
            var whereBatchItemEqualsBuilder = CreateFilterOnExpressionBuilder(relatedEntityProperty);

            return (entity, batch) =>
            {
                var itemCollectionExpression = Expression.Invoke(relatedCollection, entity);
                var related = Expression.Parameter(typeof(TRelated), "related");
                var whereBatchItemEquals = Expression.Lambda<Func<TRelated, bool>>(
                    whereBatchItemEqualsBuilder(related, batch),
                    related);
                Expression<Func<IEnumerable<TRelated>, Func<TRelated, bool>, bool>> templateExpression =
                    (someItems, filter) => someItems.Any(filter);
                return Expression.Invoke(templateExpression, itemCollectionExpression, whereBatchItemEquals);
            };
        }
    }
}
