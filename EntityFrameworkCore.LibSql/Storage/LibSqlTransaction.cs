using System.Data;
using System.Data.Common;

namespace EntityFrameworkCore.LibSql.Storage;

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