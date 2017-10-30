using System.Collections.Generic;
using System.Linq.Expressions;

namespace IharBury.NHibernate.Tests
{
    internal sealed class FakeBatch
    {
        public IList<Expression> ExecutedQueries { get; } = new List<Expression>();
    }
}
