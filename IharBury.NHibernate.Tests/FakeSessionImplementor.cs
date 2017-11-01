using NHibernate.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.AdoNet;
using NHibernate.Cache;
using NHibernate.Collection;
using NHibernate.Engine.Query.Sql;
using NHibernate.Event;
using NHibernate.Hql;
using NHibernate.Impl;
using NHibernate.Loader.Custom;
using NHibernate.Persister.Entity;
using NHibernate.Transaction;
using NHibernate.Type;
using System.Collections;
using System.Data;

namespace IharBury.NHibernate.Tests
{
    internal sealed class FakeSessionImplementor : ISessionImplementor
    {
        public long Timestamp => throw new NotImplementedException();

        public ISessionFactoryImplementor Factory { get; } = new FakeSessionFactoryImpementor();

        public IBatcher Batcher => throw new NotImplementedException();

        public IDictionary<string, IFilter> EnabledFilters { get; } = new Dictionary<string, IFilter>();

        public IInterceptor Interceptor => throw new NotImplementedException();

        public EventListeners Listeners => throw new NotImplementedException();

        public int DontFlushFromFind => throw new NotImplementedException();

        public ConnectionManager ConnectionManager => throw new NotImplementedException();

        public bool IsEventSource => throw new NotImplementedException();

        public IPersistenceContext PersistenceContext => throw new NotImplementedException();

        public CacheMode CacheMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool IsOpen => throw new NotImplementedException();

        public bool IsConnected => throw new NotImplementedException();

        public FlushMode FlushMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string FetchProfile { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IDbConnection Connection => throw new NotImplementedException();

        public bool IsClosed => throw new NotImplementedException();

        public bool TransactionInProgress => throw new NotImplementedException();

        public EntityMode EntityMode => throw new NotImplementedException();

        public FutureCriteriaBatch FutureCriteriaBatch => throw new NotImplementedException();

        public FutureQueryBatch FutureQueryBatch => throw new NotImplementedException();

        public Guid SessionId => throw new NotImplementedException();

        public ITransactionContext TransactionContext { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void AfterTransactionBegin(ITransaction tx)
        {
            throw new NotImplementedException();
        }

        public void AfterTransactionCompletion(bool successful, ITransaction tx)
        {
            throw new NotImplementedException();
        }

        public void BeforeTransactionCompletion(ITransaction tx)
        {
            throw new NotImplementedException();
        }

        public string BestGuessEntityName(object entity)
        {
            throw new NotImplementedException();
        }

        public void CloseSessionFromDistributedTransaction()
        {
            throw new NotImplementedException();
        }

        public IQuery CreateQuery(IQueryExpression queryExpression)
        {
            throw new NotImplementedException();
        }

        public IEnumerable Enumerable(string query, QueryParameters parameters)
        {
            throw new NotImplementedException();
        }

        public IEnumerable Enumerable(IQueryExpression query, QueryParameters parameters)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> Enumerable<T>(string query, QueryParameters queryParameters)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> Enumerable<T>(IQueryExpression query, QueryParameters queryParameters)
        {
            throw new NotImplementedException();
        }

        public IEnumerable EnumerableFilter(object collection, string filter, QueryParameters parameters)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> EnumerableFilter<T>(object collection, string filter, QueryParameters parameters)
        {
            throw new NotImplementedException();
        }

        public int ExecuteNativeUpdate(NativeSQLQuerySpecification specification, QueryParameters queryParameters)
        {
            throw new NotImplementedException();
        }

        public int ExecuteUpdate(string query, QueryParameters queryParameters)
        {
            throw new NotImplementedException();
        }

        public int ExecuteUpdate(IQueryExpression query, QueryParameters queryParameters)
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public CacheKey GenerateCacheKey(object id, IType type, string entityOrRoleName)
        {
            throw new NotImplementedException();
        }

        public EntityKey GenerateEntityKey(object id, IEntityPersister persister)
        {
            throw new NotImplementedException();
        }

        public object GetContextEntityIdentifier(object obj)
        {
            throw new NotImplementedException();
        }

        public IEntityPersister GetEntityPersister(string entityName, object obj)
        {
            throw new NotImplementedException();
        }

        public object GetEntityUsingInterceptor(EntityKey key)
        {
            throw new NotImplementedException();
        }

        public IType GetFilterParameterType(string filterParameterName)
        {
            throw new NotImplementedException();
        }

        public object GetFilterParameterValue(string filterParameterName)
        {
            throw new NotImplementedException();
        }

        public IQuery GetNamedQuery(string queryName)
        {
            throw new NotImplementedException();
        }

        public IQuery GetNamedSQLQuery(string name)
        {
            throw new NotImplementedException();
        }

        public IQueryTranslator[] GetQueries(string query, bool scalar)
        {
            throw new NotImplementedException();
        }

        public IQueryTranslator[] GetQueries(IQueryExpression query, bool scalar)
        {
            throw new NotImplementedException();
        }

        public string GuessEntityName(object entity)
        {
            throw new NotImplementedException();
        }

        public object ImmediateLoad(string entityName, object id)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void InitializeCollection(IPersistentCollection collection, bool writing)
        {
            throw new NotImplementedException();
        }

        public object Instantiate(string entityName, object id)
        {
            throw new NotImplementedException();
        }

        public object InternalLoad(string entityName, object id, bool eager, bool isNullable)
        {
            throw new NotImplementedException();
        }

        public IList List(string query, QueryParameters parameters)
        {
            throw new NotImplementedException();
        }

        public IList List(IQueryExpression queryExpression, QueryParameters parameters)
        {
            throw new NotImplementedException();
        }

        public void List(string query, QueryParameters parameters, IList results)
        {
            throw new NotImplementedException();
        }

        public void List(IQueryExpression queryExpression, QueryParameters queryParameters, IList results)
        {
            throw new NotImplementedException();
        }

        public IList<T> List<T>(string query, QueryParameters queryParameters)
        {
            throw new NotImplementedException();
        }

        public IList<T> List<T>(IQueryExpression queryExpression, QueryParameters queryParameters)
        {
            throw new NotImplementedException();
        }

        public IList<T> List<T>(CriteriaImpl criteria)
        {
            throw new NotImplementedException();
        }

        public void List(CriteriaImpl criteria, IList results)
        {
            throw new NotImplementedException();
        }

        public IList List(CriteriaImpl criteria)
        {
            throw new NotImplementedException();
        }

        public IList List(NativeSQLQuerySpecification spec, QueryParameters queryParameters)
        {
            throw new NotImplementedException();
        }

        public void List(NativeSQLQuerySpecification spec, QueryParameters queryParameters, IList results)
        {
            throw new NotImplementedException();
        }

        public IList<T> List<T>(NativeSQLQuerySpecification spec, QueryParameters queryParameters)
        {
            throw new NotImplementedException();
        }

        public void ListCustomQuery(ICustomQuery customQuery, QueryParameters queryParameters, IList results)
        {
            throw new NotImplementedException();
        }

        public IList<T> ListCustomQuery<T>(ICustomQuery customQuery, QueryParameters queryParameters)
        {
            throw new NotImplementedException();
        }

        public IList ListFilter(object collection, string filter, QueryParameters parameters)
        {
            throw new NotImplementedException();
        }

        public IList<T> ListFilter<T>(object collection, string filter, QueryParameters parameters)
        {
            throw new NotImplementedException();
        }
    }
}
