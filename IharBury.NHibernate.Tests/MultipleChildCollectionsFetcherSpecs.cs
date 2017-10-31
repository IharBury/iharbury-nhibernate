using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;
using Xunit;

namespace IharBury.NHibernate.Tests
{
    public sealed class MultipleChildCollectionsFetcherSpecs
    {
        [Fact]
        public void WhenTheQueryHasTwoFetchManyItShouldSplitTheQueryIntoTwoQueriesInTheSameBatch()
        {
            var item = new A();
            var queryProvider = new FakeQueryProvider<A>(item);
            var query = queryProvider.RootQueryable.FetchMany(a => a.BCollection).FetchMany(a => a.CCollection);
            var result = query.WithFastFetches().ToList();
            Assert.Collection(result, firstItem => Assert.Same(item, firstItem));
            Assert.Equal(1, queryProvider.ExecutedQueryBatches.Count);
            var batch = queryProvider.ExecutedQueryBatches[0];
            Assert.Equal(2, batch.ExecutedQueries.Count);
            AssertHasExpressionBody(
                queryProvider.RootQueryable.FetchMany(a => a.BCollection),
                batch.ExecutedQueries);
            AssertHasExpressionBody(
                queryProvider.RootQueryable.FetchMany(a => a.CCollection),
                batch.ExecutedQueries);
        }

        [Fact]
        public void WhenTheQueryHasOneFetchManyItShouldNotModifyTheQuery()
        {
            var item = new A();
            var queryProvider = new FakeQueryProvider<A>(item);
            var query = queryProvider.RootQueryable.FetchMany(a => a.BCollection);
            var result = query.WithFastFetches().ToList();
            Assert.Collection(result, firstItem => Assert.Same(item, firstItem));
            Assert.Equal(1, queryProvider.ExecutedQueryBatches.Count);
            Assert.Equal(1, queryProvider.ExecutedQueryBatches[0].ExecutedQueries.Count);
        }

        [Fact]
        public void WhenTheQueryHasNoFetchManyItShouldNotModifyTheQuery()
        {
            var item = new A();
            var queryProvider = new FakeQueryProvider<A>(item);
            var query = queryProvider.RootQueryable;
            var result = query.WithFastFetches().ToList();
            Assert.Collection(result, firstItem => Assert.Same(item, firstItem));
            Assert.Equal(1, queryProvider.ExecutedQueryBatches.Count);
            Assert.Equal(1, queryProvider.ExecutedQueryBatches[0].ExecutedQueries.Count);
        }

        [Fact]
        public void WhenTheQueryHasTwoFetchesForCollectionPropertiesItShouldSplitTheQueryIntoTwoQueriesInTheSameBatch()
        {
            var item = new A();
            var queryProvider = new FakeQueryProvider<A>(item);
            var query = queryProvider.RootQueryable.Fetch(a => a.BCollection).Fetch(a => a.CCollection);
            var result = query.WithFastFetches().ToList();
            Assert.Collection(result, firstItem => Assert.Same(item, firstItem));
            Assert.Equal(1, queryProvider.ExecutedQueryBatches.Count);
            var batch = queryProvider.ExecutedQueryBatches[0];
            Assert.Equal(2, batch.ExecutedQueries.Count);
        }

        [Fact]
        public void WhenTheQueryHasTwoFetchesForNonCollectionPropertiesItShouldNotModifyTheQuery()
        {
            var item = new A();
            var queryProvider = new FakeQueryProvider<A>(item);
            var query = queryProvider.RootQueryable.Fetch(a => a.X).Fetch(a => a.Y);
            var result = query.WithFastFetches().ToList();
            Assert.Collection(result, firstItem => Assert.Same(item, firstItem));
            Assert.Equal(1, queryProvider.ExecutedQueryBatches.Count);
            Assert.Equal(1, queryProvider.ExecutedQueryBatches[0].ExecutedQueries.Count);
        }

        [Fact]
        public void WhenTheQueryHasTwoFetchManyWithThenFetchManyItShouldSplitTheQueryIntoTwoQueriesInTheSameBatch()
        {
            var item = new A();
            var queryProvider = new FakeQueryProvider<A>(item);
            var query = queryProvider.RootQueryable
                .FetchMany(a => a.BCollection).ThenFetchMany(b => b.DCollection)
                .FetchMany(a => a.CCollection).ThenFetchMany(c => c.ECollection);
            var result = query.WithFastFetches().ToList();
            Assert.Collection(result, firstItem => Assert.Same(item, firstItem));
            Assert.Equal(1, queryProvider.ExecutedQueryBatches.Count);
            var batch = queryProvider.ExecutedQueryBatches[0];
            Assert.Equal(2, batch.ExecutedQueries.Count);
            AssertHasExpressionBody(
                queryProvider.RootQueryable.FetchMany(a => a.BCollection).ThenFetchMany(b => b.DCollection),
                batch.ExecutedQueries);
            AssertHasExpressionBody(
                queryProvider.RootQueryable.FetchMany(a => a.CCollection).ThenFetchMany(c => c.ECollection),
                batch.ExecutedQueries);
        }

        [Fact]
        public void WhenTheQueryHasTwoFetchManyWithThenFetchItShouldSplitTheQueryIntoTwoQueriesInTheSameBatch()
        {
            var item = new A();
            var queryProvider = new FakeQueryProvider<A>(item);
            var query = queryProvider.RootQueryable
                .FetchMany(a => a.BCollection).ThenFetch(b => b.DCollection)
                .FetchMany(a => a.CCollection).ThenFetch(c => c.ECollection);
            var result = query.WithFastFetches().ToList();
            Assert.Collection(result, firstItem => Assert.Same(item, firstItem));
            Assert.Equal(1, queryProvider.ExecutedQueryBatches.Count);
            var batch = queryProvider.ExecutedQueryBatches[0];
            Assert.Equal(2, batch.ExecutedQueries.Count);
            AssertHasExpressionBody(
                queryProvider.RootQueryable.FetchMany(a => a.BCollection).ThenFetch(b => b.DCollection),
                batch.ExecutedQueries);
            AssertHasExpressionBody(
                queryProvider.RootQueryable.FetchMany(a => a.CCollection).ThenFetch(c => c.ECollection),
                batch.ExecutedQueries);
        }

        [Fact]
        public void WhenTheQueryHasWhereItShouldFilterAllFutures()
        {
            var matchingItem = new A { X = 1 };
            var nonMatchingItem = new A { X = 2 };
            var queryProvider = new FakeQueryProvider<A>(matchingItem, nonMatchingItem);
            var query = queryProvider.RootQueryable
                .FetchMany(a => a.BCollection)
                .FetchMany(a => a.CCollection)
                .Where(a => a.X == 1);
            var result = query.WithFastFetches().ToList();
            Assert.Collection(result, item => Assert.Same(matchingItem, item));
            Assert.Equal(1, queryProvider.ExecutedQueryBatches.Count);
            var batch = queryProvider.ExecutedQueryBatches[0];
            Assert.Equal(2, batch.ExecutedQueries.Count);
            AssertHasExpressionBody(
                queryProvider.RootQueryable.FetchMany(a => a.BCollection).Where(a => a.X == 1),
                batch.ExecutedQueries);
            AssertHasExpressionBody(
                queryProvider.RootQueryable.FetchMany(a => a.CCollection).Where(a => a.X == 1),
                batch.ExecutedQueries);
        }

        [Fact]
        public void WhenTheQueryHasTooManyCollectionParametersItShouldSplitThemInBatches()
        {
            WithSqLiteInMemory(
                new[]
                {
                    new A { X = 1 },
                    new A { Y = 1 },
                    new A { BCollection = { new B() { U = 1 } } },
                    new A { X = 2 },
                    new A { X = 10 }
                },
                session =>
                {
                    session.EnableFilter("aFilter")
                        .SetParameter("f1", 10)
                        .SetParameter("f2", 11)
                        .SetParameter("f3", 12);
                    var filteredX = new[] { 1 }.Concat(Enumerable.Range(3, 999));
                    var result = session.Query<A>()
                        .FetchMany(a => a.BCollection).ThenFetchMany(b => b.DCollection)
                        .FetchMany(a => a.CCollection).ThenFetchMany(c => c.ECollection)
                        .Where(a => !a.Z)
                        .Where(a => !a.BCollection.Any(b => b.U == 2017))
                        .Where(a => !new[] { -1, -2, -3, -4 }.Contains(a.X))
                        .WithFastFetches()
                        .InBatches(filteredX, session, (_, __) => 999)
                        .WhereBatchItemEquals(a => a.X)
                        .OrBatchItemEquals(a => a.Y)
                        .OrBatchItemEqualsAny(a => a.BCollection, b => b.U)
                        .ToUnorderedDistinctList();
                    Assert.Equal(3, result.Count);
                    Assert.Contains(result, item => item.X == 1);
                    Assert.Contains(result, item => item.Y == 1);
                    Assert.Contains(result, item => item.BCollection.Any(b => b.U == 1));
                });
        }

        private void WithSqLiteInMemory(object[] entities, Action<ISession> action)
        {
            Configuration configuration = null;

            var sessionFactory = Fluently.Configure()
                .Database(SQLiteConfiguration.Standard.InMemory())
                .Mappings(mappingConfiguration => mappingConfiguration.FluentMappings.AddFromAssemblyOf<MultipleChildCollectionsFetcherSpecs>())
                .ExposeConfiguration(exposedConfiguration => configuration = exposedConfiguration)
                .BuildSessionFactory();

            using (var session = sessionFactory.OpenSession())
            {
                new SchemaExport(configuration).Execute(true, true, false, session.Connection, null);

                foreach (var entity in entities)
                    session.Save(entity);
                session.Flush();
                session.Clear();

                action(session);
            }
        }

        private static void AssertHasExpressionBody(IQueryable<A> query, IEnumerable<Expression> expressionList)
        {
            Assert.Contains(query.Expression.ToString(), expressionList.Select(expression => expression.ToString()));
        }

        private class A
        {
            public virtual int Id { get; set; }
            public virtual int X { get; set; }
            public virtual int Y { get; set; }
            public virtual bool Z { get; set; }
            public virtual ISet<B> BCollection { get; set; } = new HashSet<B>();
            public virtual ISet<C> CCollection { get; set; } = new HashSet<C>();
        }

        private class AClassMap : ClassMap<A>
        {
            public AClassMap()
            {
                Id(a => a.Id).GeneratedBy.Identity();
                Map(a => a.X);
                Map(a => a.Y);
                Map(a => a.Z);
                HasMany(a => a.BCollection).AsSet().Cascade.All();
                HasMany(a => a.CCollection).AsSet();
                ApplyFilter<AFilter>();
            }
        }

        private class B
        {
            public virtual int Id { get; set; }
            public virtual int U { get; set; }
            public virtual ISet<D> DCollection { get; set; } = new HashSet<D>();
        }

        private class BClassMap : ClassMap<B>
        {
            public BClassMap()
            {
                Id(b => b.Id).GeneratedBy.Identity();
                Map(b => b.U);
                HasMany(b => b.DCollection).AsSet();
            }
        }

        private class C
        {
            public virtual int Id { get; set; }
            public virtual ISet<E> ECollection { get; set; } = new HashSet<E>();
        }

        private class CClassMap : ClassMap<C>
        {
            public CClassMap()
            {
                Id(c => c.Id).GeneratedBy.Identity();
                HasMany(c => c.ECollection).AsSet();
            }
        }

        private class D
        {
            public virtual int Id { get; set; }
        }

        private class DClassMap : ClassMap<D>
        {
            public DClassMap()
            {
                Id(d => d.Id).GeneratedBy.Identity();
            }
        }

        private class E
        {
            public virtual int Id { get; set; }
        }

        private class EClassMap : ClassMap<E>
        {
            public EClassMap()
            {
                Id(e => e.Id).GeneratedBy.Identity();
            }
        }

        private class AFilter : FilterDefinition
        {
            public AFilter()
            {
                WithName("aFilter").WithCondition("(X <> :f1) and (X <> :f2) and (X <> :f3)")
                    .AddParameter("f1", NHibernateUtil.Int32)
                    .AddParameter("f2", NHibernateUtil.Int32)
                    .AddParameter("f3", NHibernateUtil.Int32);
            }
        }
    }
}
