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
    
    // Fixed: Make these nullable to avoid compiler warnings
    public override string? Database => null;
    
    public override string? DataSource => _connectionString;
    
    public override ConnectionState State => _state;

    public override void Open()
    {
        if (_state == ConnectionState.Open)
            return;

        try
        {
            if (_connectionString.StartsWith(":memory:", StringComparison.OrdinalIgnoreCase))
            {
                // For memory databases, use shared clients
                _client = _sharedClients.GetOrAdd(_connectionString, _ => 
                    DatabaseClient.Create(_connectionString).GetAwaiter().GetResult());
            }
            else
            {
                // For file databases, create individual clients
                _client = DatabaseClient.Create(_connectionString).GetAwaiter().GetResult();
            }
                
            _state = ConnectionState.Open;
            OnStateChange(new StateChangeEventArgs(ConnectionState.Closed, ConnectionState.Open));
            Console.WriteLine($"DEBUG LibSqlDbConnection: Successfully opened connection");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR LibSqlDbConnection.Open: {ex}");
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
                // For memory databases, use shared clients
                if (_sharedClients.TryGetValue(_connectionString, out var existingClient))
                {
                    _client = existingClient;
                }
                else
                {
                    var newClient = await DatabaseClient.Create(_connectionString);
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
                _client = await DatabaseClient.Create(_connectionString);
            }
                
            _state = ConnectionState.Open;
            OnStateChange(new StateChangeEventArgs(ConnectionState.Closed, ConnectionState.Open));
            Console.WriteLine($"DEBUG LibSqlDbConnection: Successfully opened connection async");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR LibSqlDbConnection.OpenAsync: {ex}");
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
            if (!_connectionString.StartsWith(":memory:", StringComparison.OrdinalIgnoreCase) && _client != null)
            {
                _client.Dispose();
                _client = null;
            }
            // For :memory: databases, keep the _client reference to maintain sharing
        }
        finally
        {
            var previousState = _state;
            _state = ConnectionState.Closed;
            OnStateChange(new StateChangeEventArgs(previousState, ConnectionState.Closed));
            Console.WriteLine($"DEBUG LibSqlDbConnection: Connection closed");
        }
    }

    protected override DbCommand CreateDbCommand()
    {
        if (_client == null)
            throw new InvalidOperationException("Connection is not open");
            
        Console.WriteLine($"DEBUG LibSqlDbConnection: Creating DbCommand");
        return new LibSqlDbCommand(_client)
        {
            Connection = this
        };
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        Console.WriteLine($"DEBUG LibSqlDbConnection: Beginning transaction with isolation level {isolationLevel}");
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
}