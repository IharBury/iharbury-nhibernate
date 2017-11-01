using System;
using System.Linq.Expressions;
using NHibernate.Linq;

namespace IharBury.NHibernate.Tests
{
    internal sealed class FetchRemover : ExpressionVisitor
    {
        public Expression Remove(Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            return Visit(expression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (IsFetch(node))
                return Visit(node.Arguments[0]);

            return base.VisitMethodCall(node);
        }

        private static bool IsFetch(MethodCallExpression node)
        {
            return node.Method.DeclaringType == typeof(EagerFetchingExtensionMethods);
        }
    }
}
