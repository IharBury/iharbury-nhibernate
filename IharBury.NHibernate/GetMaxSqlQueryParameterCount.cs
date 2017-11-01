namespace IharBury.NHibernate
{
    /// <summary>
    /// Determines the maximum SQL query parameter count supported by the database.
    /// Supports scenarios where multiple SQL queries are combined into a SQL query batch.
    /// </summary>
    /// <param name="sqlBatchQueryCount">The SQL query count in the SQL query batch.</param>
    /// <returns>The maximum number of SQL query parameters supported by the database.</returns>
    public delegate int GetMaxSqlQueryParameterCount(int sqlBatchQueryCount);
}
