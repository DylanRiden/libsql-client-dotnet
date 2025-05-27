using System.Data.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlConnection : RelationalConnection, IDisposable
{
    public LibSqlConnection(RelationalConnectionDependencies dependencies)
        : base(dependencies)
    {
    }

    protected override DbConnection CreateDbConnection()
    {
        var connectionString = ConnectionString ?? throw new InvalidOperationException("Connection string is required");
        
        // Determine if this is an HTTP connection or local file connection
        if (IsHttpConnection(connectionString))
        {
            Console.WriteLine("DEBUG: Creating HTTP LibSQL connection");
            return new HttpLibSqlDbConnection(connectionString);
        }
        else
        {
            Console.WriteLine("DEBUG: Creating local LibSQL connection");
            
            // For :memory: databases, use a single shared DbConnection instance across all contexts
            if (connectionString == ":memory:")
            {
                lock (_lock)
                {
                    return _sharedMemoryConnection ??= new LibSqlDbConnection(connectionString);
                }
            }
            
            // For file databases, create new instances as usual
            return new LibSqlDbConnection(connectionString);
        }
    }

    private static bool IsHttpConnection(string connectionString)
    {
        return connectionString.StartsWith("libsql://", StringComparison.OrdinalIgnoreCase) ||
               connectionString.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
               connectionString.Contains("authToken=", StringComparison.OrdinalIgnoreCase);
    }

    // Shared connection management for :memory: databases
    private static DbConnection? _sharedMemoryConnection;
    private static readonly object _lock = new object();

    public new void Dispose()
    {
        // Don't dispose the shared memory connection, only dispose individual file connections
        if (ConnectionString != ":memory:")
        {
            DbConnection?.Dispose();
        }
        base.Dispose();
    }
}