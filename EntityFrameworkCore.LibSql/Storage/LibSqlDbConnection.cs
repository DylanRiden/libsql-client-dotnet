using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Libsql.Client;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlDbConnection : DbConnection
{
    private readonly string _connectionString;
    private static readonly ConcurrentDictionary<string, IDatabaseClient> _sharedClients = new();
    private static readonly Lazy<Task<IDatabaseClient>> _memoryClientLazy = new(() => CreateMemoryClient(), true);
    private static IDatabaseClient? _sharedMemoryClient;
    private static readonly object _memoryClientLock = new();
    private IDatabaseClient? _client;
    private ConnectionState _state = ConnectionState.Closed;

    public LibSqlDbConnection(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    internal IDatabaseClient? Client => _client;
    
    public override string ConnectionString 
    { 
        get => _connectionString; 
        set => throw new NotSupportedException("Cannot change connection string after construction"); 
    }

    public override string Database => ExtractDatabaseFromConnectionString(_connectionString);
    public override string DataSource => ExtractDataSourceFromConnectionString(_connectionString);
    public override string ServerVersion => "libSQL";
    public override ConnectionState State => _state;

    public override void Open()
    {
        if (_state == ConnectionState.Open)
            return;

        try
        {
            if (_connectionString.StartsWith(":memory:", StringComparison.OrdinalIgnoreCase))
            {
                _client = _sharedClients.GetOrAdd(_connectionString, _ => 
                    CreateMemoryClient(_connectionString).GetAwaiter().GetResult());
            }
            else
            {
                // For file databases, create individual clients
                _client = _sharedClients.GetOrAdd(_connectionString, _ => CreateClient(_connectionString));
                Console.WriteLine($"DEBUG: Using file client instance: {_client.GetHashCode()}");
            }
                
            _state = ConnectionState.Open;
            OnStateChange(new StateChangeEventArgs(ConnectionState.Closed, ConnectionState.Open));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in Open: {ex}");
            _state = ConnectionState.Broken;
            throw;
        }
    }

    public override async Task OpenAsync(CancellationToken cancellationToken)
    {
        if (_state == ConnectionState.Open)
            return;

        try
        {
            if (_connectionString.StartsWith(":memory:", StringComparison.OrdinalIgnoreCase))
            {
                if (_sharedClients.TryGetValue(_connectionString, out var existingClient))
                {
                    _client = existingClient;
                }
                else
                {
                    var newClient = await CreateMemoryClient(_connectionString);
                    _client = _sharedClients.GetOrAdd(_connectionString, newClient);
                    if (!ReferenceEquals(_client, newClient))
                    {
                        newClient.Dispose();
                    }
                }
            }
            else
            {
                // For file databases, create individual clients
                _client = _sharedClients.GetOrAdd(_connectionString, _ => CreateClient(_connectionString));
                Console.WriteLine($"DEBUG: Using file client instance (async): {_client.GetHashCode()}");
            }
                
            _state = ConnectionState.Open;
            OnStateChange(new StateChangeEventArgs(ConnectionState.Closed, ConnectionState.Open));
            Console.WriteLine($"DEBUG: OpenAsync completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in OpenAsync: {ex}");
            _state = ConnectionState.Broken;
            throw;
        }
    }

    public override void Close()
    {
        if (_state == ConnectionState.Closed)
            return;

        try
        {
            // Don't dispose shared clients (for :memory: databases)
            // Only dispose non-shared clients (for file databases)
            if (_connectionString != ":memory:" && _client != null)
            {
                (_client as IDisposable)?.Dispose();
                _client = null; // Only null out non-shared clients
            }
            // For :memory: databases, keep the _client reference to maintain sharing
        }
        finally
        {
            var previousState = _state;
            _state = ConnectionState.Closed;
            OnStateChange(new StateChangeEventArgs(previousState, ConnectionState.Closed));
        }
    }

    protected override DbCommand CreateDbCommand()
    {
        return new LibSqlDbCommand(_client ?? throw new InvalidOperationException("Connection is not open"))
        {
            Connection = this
        };
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        return new LibSqlTransaction(this, isolationLevel);
    }

    public override void ChangeDatabase(string databaseName)
    {
        throw new NotSupportedException("LibSQL does not support changing databases");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Close();
        }
        base.Dispose(disposing);
    }

    private static IDatabaseClient CreateClient(string connectionString)
    {
        try
        {
            var options = ParseConnectionString(connectionString);
            // For file paths, ensure we have a proper URI format
            var url = options.Url;
            if (!url.StartsWith("file:") && !url.StartsWith("http") && !url.StartsWith("ws"))
            {
                // Convert local file path to proper URI format
                url = $"file:{Path.GetFullPath(url.Replace("file:", ""))}";
            }
            
            return DatabaseClient.Create(opts =>
            {
                opts.Url = url;
                if (!string.IsNullOrEmpty(options.AuthToken))
                    opts.AuthToken = options.AuthToken;
            }).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create database client for '{connectionString}'. Make sure the path is valid and accessible.", ex);
        }
    }

    private static async Task<IDatabaseClient> CreateMemoryClient(string connectionString = ":memory:")
    {
        try
        {
            return await DatabaseClient.Create(connectionString);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create in-memory database client: {ex.Message}", ex);
        }
    }

    private static ConnectionOptions ParseConnectionString(string connectionString)
    {
        if (connectionString == ":memory:")
        {
            return new ConnectionOptions { Url = ":memory:" };
        }

        if (connectionString.StartsWith("file://"))
        {
            return new ConnectionOptions { Url = connectionString };
        }

        var uri = new Uri(connectionString);
        var options = new ConnectionOptions { Url = connectionString };

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        if (query["authToken"] != null)
        {
            options.AuthToken = query["authToken"];
        }

        return options;
    }

    private static string ExtractDatabaseFromConnectionString(string connectionString)
    {
        try
        {
            if (connectionString == ":memory:")
                return ":memory:";
            
            var uri = new Uri(connectionString);
            return Path.GetFileName(uri.LocalPath) ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    private static string ExtractDataSourceFromConnectionString(string connectionString)
    {
        try
        {
            if (connectionString == ":memory:")
                return ":memory:";
            
            var uri = new Uri(connectionString);
            return uri.Host ?? "unknown";
        }
        catch
        {
            return connectionString;
        }
    }

    // Static method to cleanup shared clients (optional - for testing)
    public static void ClearSharedClients()
    {
        foreach (var client in _sharedClients.Values)
        {
            (client as IDisposable)?.Dispose();
        }
        _sharedClients.Clear();
    }

    private class ConnectionOptions
    {
        public string Url { get; set; } = string.Empty;
        public string? AuthToken { get; set; }
    }
}