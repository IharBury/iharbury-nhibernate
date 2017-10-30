using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IharBury.NHibernate
{
    internal sealed class CollectionFetchRemover : ExpressionVisitor
    {
        private List<MethodCallExpression> excludedCollectionFetches;

        public Expression RemoveExceptFrom(Expression expression, params MethodCallExpression[] excludedCollectionFetches)
        {
            if (excludedCollectionFetches == null)
                throw new ArgumentNullException(nameof(excludedCollectionFetches));

            this.excludedCollectionFetches = excludedCollectionFetches.ToList();
            return Visit(expression);
        }

        public IQueryable<T> RemoveExceptFrom<T>(IQueryable<T> query, params MethodCallExpression[] excludedCollectionFetches)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));
            if (excludedCollectionFetches == null)
                throw new ArgumentNullException(nameof(excludedCollectionFetches));

            return query.Provider.CreateQuery<T>(RemoveExceptFrom(query.Expression, excludedCollectionFetches));
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            return node.IsCollectionFetch() || node.IsContinuationOfCollectionFetch()
                ? VisitCollectionFetchChain(node).Expression
                : base.VisitMethodCall(node);
        }

        private CollectionFetchChainVisitResult VisitCollectionFetchChain(MethodCallExpression node)
        {
            if (node.IsCollectionFetch())
            {
                if (excludedCollectionFetches.Contains(node))
                {
                    return new CollectionFetchChainVisitResult
                    {
                        Expression = base.VisitMethodCall(node),
                        IsCollectionFetchRemoved = false
                    };
                }

                return new CollectionFetchChainVisitResult
                {
                    Expression = Visit(node.Arguments[0]),
                    IsCollectionFetchRemoved = true
                };
            }

            if (node.IsContinuationOfCollectionFetch())
            {
                var child = node.Arguments[0];
                if (!(child is MethodCallExpression childMethodCall))
                    throw new InvalidOperationException($"Unexpected expression in a collection fetch chain: {child}.");

                var visitedChild = VisitCollectionFetchChain(childMethodCall);

                if (visitedChild.IsCollectionFetchRemoved)
                {
                    return new CollectionFetchChainVisitResult
                    {
                        Expression = visitedChild.Expression,
                        IsCollectionFetchRemoved = true
                    };
                }

                return new CollectionFetchChainVisitResult
                {
                    Expression = node.Update(null, new[] { visitedChild.Expression, node.Arguments[1] }),
                    IsCollectionFetchRemoved = false
                };
            }

            throw new InvalidOperationException($"Unexpected expression in a collection fetch chain: {node}.");
        }

        private struct CollectionFetchChainVisitResult
        {
            public Expression Expression { get; set; }
            public bool IsCollectionFetchRemoved { get; set; }
        }
    }
}
