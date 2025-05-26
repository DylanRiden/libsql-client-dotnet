using System.Data;
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

    public override bool IsMultipleActiveResultSetsEnabled => false; // LibSQL doesn't support MARS
}

public class LibSqlTransaction : DbTransaction
{
    private readonly LibSqlDbConnection _connection;
    private readonly IsolationLevel _isolationLevel;
    private bool _disposed;

    public LibSqlTransaction(LibSqlDbConnection connection, IsolationLevel isolationLevel)
    {
        _connection = connection;
        _isolationLevel = isolationLevel;
    }

    public override IsolationLevel IsolationLevel => _isolationLevel;
    protected override DbConnection DbConnection => _connection;

    public override void Commit()
    {
        // TODO: Implement actual transaction commit
        // This would use the LibSQL client's transaction API when available
    }

    public override void Rollback()
    {
        // TODO: Implement actual transaction rollback
        // This would use the LibSQL client's transaction API when available
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                Rollback();
            }
            catch
            {
                // Ignore errors during dispose
            }
            _disposed = true;
        }
        base.Dispose(disposing);
    }
}