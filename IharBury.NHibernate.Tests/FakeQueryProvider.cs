using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Type;

namespace IharBury.NHibernate.Tests
{
    internal sealed class FakeQueryProvider<T> : INhQueryProvider
    {
        private readonly List<T> items;
        private readonly FetchRemover fetchRemover = new FetchRemover();
        private readonly ConstantExpressionReplacer constantExpressionReplacer = new ConstantExpressionReplacer();
        private FakeBatch currentBatch = new FakeBatch();

        public FakeQueryProvider(params T[] items)
        {
            this.items = items.ToList();
            RootQueryable = new FakeQuery<T>(this);
        }

        public IQueryable<T> RootQueryable { get; }
        public IList<FakeBatch> ExecutedQueryBatches { get; } = new List<FakeBatch>();

        public IQueryable CreateQuery(Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            return new FakeQuery<T>(this, expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return (IQueryable<TElement>) CreateQuery(expression);
        }

        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        public TResult Execute<TResult>(Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            currentBatch.ExecutedQueries.Add(expression);
            ExecuteCurrentBatch();
            return (TResult)ExecuteInternal(expression);
        }

        private object ExecuteInternal(Expression expression)
        {
            var expressionWithoutFetches = fetchRemover.Remove(expression);

            var expressionWithData =
                constantExpressionReplacer.Replace(expressionWithoutFetches, RootQueryable, items.AsQueryable());

            return Expression.Lambda<Func<IQueryable<T>>>(expressionWithData).Compile().Invoke();
        }

        public object ExecuteFuture(Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            currentBatch.ExecutedQueries.Add(expression);
            return new Future((IEnumerable<T>)ExecuteInternal(expression), currentBatch, this);
        }

        public void SetResultTransformerAndAdditionalCriteria(
            IQuery query,
            NhLinqExpression nhExpression,
            IDictionary<string, Tuple<object, IType>> parameters)
        {
            throw new NotImplementedException();
        }

        private void ExecuteCurrentBatch()
        {
            ExecutedQueryBatches.Add(currentBatch);
            currentBatch = new FakeBatch();
        }

        private sealed class Future : IEnumerable<T>
        {
            private readonly FakeBatch batch;
            private readonly FakeQueryProvider<T> provider;
            private readonly List<T> items;

            public Future(IEnumerable<T> items, FakeBatch batch, FakeQueryProvider<T> provider)
            {
                this.batch = batch;
                this.provider = provider;
                this.items = items.ToList();
            }

            public IEnumerator<T> GetEnumerator()
            {
                if (provider.currentBatch == batch)
                    provider.ExecuteCurrentBatch();
                return items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
