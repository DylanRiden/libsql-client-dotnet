using System.Data;
using System.Data.Common;

namespace EntityFrameworkCore.LibSql.Storage;

public class HttpLibSqlTransaction : DbTransaction
{
    private readonly HttpLibSqlDbConnection _connection;
    private readonly IsolationLevel _isolationLevel;
    private bool _disposed;

    public HttpLibSqlTransaction(HttpLibSqlDbConnection connection, IsolationLevel isolationLevel)
    {
        _connection = connection;
        _isolationLevel = isolationLevel;
    }

    public override IsolationLevel IsolationLevel => _isolationLevel;
    protected override DbConnection DbConnection => _connection;

    public override void Commit()
    {
        // HTTP LibSQL might not support explicit transactions
        // For now, this is a no-op
        Console.WriteLine("DEBUG HTTP LibSqlTransaction: Commit called (no-op)");
    }

    public override void Rollback()
    {
        // HTTP LibSQL might not support explicit transactions
        // For now, this is a no-op
        Console.WriteLine("DEBUG HTTP LibSqlTransaction: Rollback called (no-op)");
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                // Don't rollback on dispose for HTTP-based client
                Console.WriteLine("DEBUG HTTP LibSqlTransaction: Disposed");
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