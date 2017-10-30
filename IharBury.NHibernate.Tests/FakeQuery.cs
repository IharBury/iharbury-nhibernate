using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using NHibernate.Linq;
using Remotion.Linq;

namespace IharBury.NHibernate.Tests
{
    internal sealed class FakeQuery<T> : QueryableBase<T>
    {
        public FakeQuery(INhQueryProvider provider)
            : base(provider)
        {
        }

        public FakeQuery(INhQueryProvider provider, Expression expression)
            : base(provider, expression)
        {
        }
    }
}
