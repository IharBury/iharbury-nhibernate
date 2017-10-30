using NHibernate.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Cache;
using NHibernate.Cfg;
using NHibernate.Connection;
using NHibernate.Context;
using NHibernate.Dialect;
using NHibernate.Dialect.Function;
using NHibernate.Engine.Query;
using NHibernate.Exceptions;
using NHibernate.Id;
using NHibernate.Metadata;
using NHibernate.Persister.Collection;
using NHibernate.Persister.Entity;
using NHibernate.Proxy;
using NHibernate.Stat;
using NHibernate.Transaction;
using NHibernate.Type;
using System.Data;

namespace IharBury.NHibernate.Tests
{
    internal sealed class FakeSessionFactoryImpementor : ISessionFactoryImplementor
    {
        public Dialect Dialect => throw new NotImplementedException();

        public IInterceptor Interceptor => throw new NotImplementedException();

        public QueryPlanCache QueryPlanCache => throw new NotImplementedException();

        public IConnectionProvider ConnectionProvider => throw new NotImplementedException();

        public ITransactionFactory TransactionFactory => throw new NotImplementedException();

        public UpdateTimestampsCache UpdateTimestampsCache => throw new NotImplementedException();

        public IStatisticsImplementor StatisticsImplementor => throw new NotImplementedException();

        public ISQLExceptionConverter SQLExceptionConverter => throw new NotImplementedException();

        public Settings Settings => throw new NotImplementedException();

        public IEntityNotFoundDelegate EntityNotFoundDelegate => throw new NotImplementedException();

        public SQLFunctionRegistry SQLFunctionRegistry => throw new NotImplementedException();

        public IQueryCache QueryCache => throw new NotImplementedException();

        public ICurrentSessionContext CurrentSessionContext => throw new NotImplementedException();

        public IStatistics Statistics => throw new NotImplementedException();

        public bool IsClosed => throw new NotImplementedException();

        public ICollection<string> DefinedFilterNames => throw new NotImplementedException();

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Evict(Type persistentClass)
        {
            throw new NotImplementedException();
        }

        public void Evict(Type persistentClass, object id)
        {
            throw new NotImplementedException();
        }

        public void EvictCollection(string roleName)
        {
            throw new NotImplementedException();
        }

        public void EvictCollection(string roleName, object id)
        {
            throw new NotImplementedException();
        }

        public void EvictEntity(string entityName)
        {
            throw new NotImplementedException();
        }

        public void EvictEntity(string entityName, object id)
        {
            throw new NotImplementedException();
        }

        public void EvictQueries()
        {
            throw new NotImplementedException();
        }

        public void EvictQueries(string cacheRegion)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, IClassMetadata> GetAllClassMetadata()
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, ICollectionMetadata> GetAllCollectionMetadata()
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, ICache> GetAllSecondLevelCacheRegions()
        {
            throw new NotImplementedException();
        }

        public IClassMetadata GetClassMetadata(Type persistentClass)
        {
            throw new NotImplementedException();
        }

        public IClassMetadata GetClassMetadata(string entityName)
        {
            throw new NotImplementedException();
        }

        public ICollectionMetadata GetCollectionMetadata(string roleName)
        {
            throw new NotImplementedException();
        }

        public ICollectionPersister GetCollectionPersister(string role)
        {
            throw new NotImplementedException();
        }

        public ISet<string> GetCollectionRolesByEntityParticipant(string entityName)
        {
            throw new NotImplementedException();
        }

        public ISession GetCurrentSession()
        {
            throw new NotImplementedException();
        }

        public IEntityPersister GetEntityPersister(string entityName)
        {
            throw new NotImplementedException();
        }

        public FilterDefinition GetFilterDefinition(string filterName)
        {
            throw new NotImplementedException();
        }

        public IIdentifierGenerator GetIdentifierGenerator(string rootEntityName)
        {
            throw new NotImplementedException();
        }

        public string GetIdentifierPropertyName(string className)
        {
            throw new NotImplementedException();
        }

        public IType GetIdentifierType(string className)
        {
            throw new NotImplementedException();
        }

        public string[] GetImplementors(string entityOrClassName)
        {
            throw new NotImplementedException();
        }

        public string GetImportedClassName(string name)
        {
            throw new NotImplementedException();
        }

        public NamedQueryDefinition GetNamedQuery(string queryName)
        {
            throw new NotImplementedException();
        }

        public NamedSQLQueryDefinition GetNamedSQLQuery(string queryName)
        {
            throw new NotImplementedException();
        }

        public IQueryCache GetQueryCache(string regionName)
        {
            throw new NotImplementedException();
        }

        public IType GetReferencedPropertyType(string className, string propertyName)
        {
            throw new NotImplementedException();
        }

        public ResultSetMappingDefinition GetResultSetMapping(string resultSetRef)
        {
            throw new NotImplementedException();
        }

        public string[] GetReturnAliases(string queryString)
        {
            throw new NotImplementedException();
        }

        public IType[] GetReturnTypes(string queryString)
        {
            throw new NotImplementedException();
        }

        public ICache GetSecondLevelCacheRegion(string regionName)
        {
            throw new NotImplementedException();
        }

        public bool HasNonIdentifierPropertyNamedId(string className)
        {
            throw new NotImplementedException();
        }

        public ISession OpenSession(IDbConnection connection, bool flushBeforeCompletionEnabled, bool autoCloseSessionEnabled, ConnectionReleaseMode connectionReleaseMode)
        {
            throw new NotImplementedException();
        }

        public ISession OpenSession(IDbConnection conn)
        {
            throw new NotImplementedException();
        }

        public ISession OpenSession(IInterceptor sessionLocalInterceptor)
        {
            throw new NotImplementedException();
        }

        public ISession OpenSession(IDbConnection conn, IInterceptor sessionLocalInterceptor)
        {
            throw new NotImplementedException();
        }

        public ISession OpenSession()
        {
            throw new NotImplementedException();
        }

        public IStatelessSession OpenStatelessSession()
        {
            throw new NotImplementedException();
        }

        public IStatelessSession OpenStatelessSession(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        public IEntityPersister TryGetEntityPersister(string entityName)
        {
            throw new NotImplementedException();
        }

        public string TryGetGuessEntityName(Type implementor)
        {
            throw new NotImplementedException();
        }
    }
}
