using System.Data.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlConnection : RelationalConnection
{
    public LibSqlConnection(RelationalConnectionDependencies dependencies)
        : base(dependencies)
    {
    }

    protected override DbConnection CreateDbConnection()
    {
        return new LibSqlDbConnection(ConnectionString ?? throw new InvalidOperationException("Connection string is required"));
    }
}