using System.Data.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlConnection : RelationalConnection, IDisposable
{
    private DbConnection? _cachedConnection;
    private static DbConnection? _sharedMemoryConnection;
    private static readonly object _lock = new object();

    public LibSqlConnection(RelationalConnectionDependencies dependencies)
        : base(dependencies)
    {
    }

    protected override DbConnection CreateDbConnection()
    {
        // For :memory: databases, use a single shared DbConnection instance across all contexts
        if (ConnectionString == ":memory:")
        {
            lock (_lock)
            {
                return _sharedMemoryConnection ??= new LibSqlDbConnection(ConnectionString ?? throw new InvalidOperationException("Connection string is required"));
            }
        }
        
        // For file databases, create new instances as usual
        return new LibSqlDbConnection(ConnectionString ?? throw new InvalidOperationException("Connection string is required"));
    }

    public new void Dispose()
    {
        // Don't dispose the shared memory connection, only dispose individual file connections
        if (ConnectionString != ":memory:")
        {
            _cachedConnection?.Dispose();
        }
        _cachedConnection = null;
        base.Dispose();
    }
}