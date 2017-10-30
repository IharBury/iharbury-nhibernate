using System;
using System.Linq.Expressions;

namespace IharBury.NHibernate.Tests
{
    internal sealed class ConstantExpressionReplacer : ExpressionVisitor
    {
        private object valueToReplace;
        private object replacementValue;

        public Expression Replace(Expression expression, object valueToReplace, object replacementValue)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            this.valueToReplace = valueToReplace ?? throw new ArgumentNullException(nameof(valueToReplace));
            this.replacementValue = replacementValue ?? throw new ArgumentNullException(nameof(replacementValue));
            return Visit(expression);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            return node.Value == valueToReplace
                ? Expression.Constant(replacementValue)
                : base.VisitConstant(node);
        }
    }
}
