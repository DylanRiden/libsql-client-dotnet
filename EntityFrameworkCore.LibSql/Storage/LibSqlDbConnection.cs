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
    
    public override string ServerVersion => "libSQL";
    
    public override string Database { get; }
    
    public override string DataSource { get; }
    
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
            return DatabaseClient.Create(connectionString).GetAwaiter().GetResult();
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
    
}