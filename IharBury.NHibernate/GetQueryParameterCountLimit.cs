using NHibernate;

namespace IharBury.NHibernate
{
    /// <summary>
    /// Determines the maximum query parameter count supported by the database.
    /// Supports scenarios where multiple queries are combined in the same query batch.
    /// </summary>
    /// <param name="session">The NHibernate session.</param>
    /// <param name="batchQueryCount">The query count in the query batch.</param>
    /// <returns>The maximum number of query parameters supported by the database.</returns>
    public delegate int GetQueryParameterCountLimit(ISession session, int batchQueryCount);
}
