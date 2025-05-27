using Microsoft.EntityFrameworkCore.Storage;
using System.Data.Common;

namespace EntityFrameworkCore.LibSql.Storage;

/// <summary>
/// EF Core connection wrapper for HTTP-only LibSQL connections
/// </summary>
public class HttpLibSqlConnection : RelationalConnection
{
    public HttpLibSqlConnection(RelationalConnectionDependencies dependencies)
        : base(dependencies)
    {
    }

    protected override DbConnection CreateDbConnection()
    {
        var connectionString = ConnectionString ?? throw new InvalidOperationException("Connection string is required");
        
        // Validate that this is a proper HTTP LibSQL connection string
        ValidateHttpConnectionString(connectionString);
        
        Console.WriteLine($"DEBUG: Creating HTTP LibSQL connection for: {SanitizeConnectionString(connectionString)}");
        return new HttpLibSqlDbConnection(connectionString);
    }
    
    private static void ValidateHttpConnectionString(string connectionString)
    {
        // Check for valid HTTP LibSQL connection string formats
        bool isValidHttpConnection = 
            connectionString.StartsWith("libsql://", StringComparison.OrdinalIgnoreCase) ||
            connectionString.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("authToken=", StringComparison.OrdinalIgnoreCase) ||
            (connectionString.Contains("url=") && connectionString.Contains("authToken="));

        if (!isValidHttpConnection)
        {
            throw new ArgumentException(
                $"Invalid LibSQL HTTP connection string. Expected format: " +
                $"'libsql://your-db.turso.io?authToken=your-token' or " +
                $"'url=https://your-db.turso.io;authToken=your-token'. " +
                $"This provider only supports HTTP connections to LibSQL/Turso databases. " +
                $"Local file and memory databases are not supported.",
                nameof(connectionString));
        }
    }
    
    private static string SanitizeConnectionString(string connectionString)
    {
        // Hide the auth token for logging security
        if (connectionString.Contains("authToken="))
        {
            return connectionString.Split('?')[0] + "?authToken=***";
        }
        return connectionString.Contains("authToken") ? "[HTTP CONNECTION WITH AUTH TOKEN]" : connectionString;
    }
}