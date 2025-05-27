using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlSqlExpressionFactory : SqlExpressionFactory
{
    public LibSqlSqlExpressionFactory(SqlExpressionFactoryDependencies dependencies)
        : base(dependencies)
    {
    }

    // Use the base implementation - don't override anything unless necessary
    // The base SqlExpressionFactory should handle table references correctly
}