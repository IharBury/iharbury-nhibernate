using System.Collections.Generic;
using System.Linq.Expressions;

namespace IharBury.NHibernate
{
    internal sealed class CollectionFetchCollector : ExpressionVisitor
    {
        private readonly List<MethodCallExpression> collectionFetches = new List<MethodCallExpression>();

        public List<MethodCallExpression> CollectFrom(Expression expression)
        {
            collectionFetches.Clear();
            Visit(expression);
            return collectionFetches;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.IsCollectionFetch())
                collectionFetches.Add(node);

            return base.VisitMethodCall(node);
        }
    }
}
