using System.Data;
using System.Data.Common;
using EntityFrameworkCore.LibSql.Http;

namespace EntityFrameworkCore.LibSql.Storage;

public class HttpLibSqlDbConnection : DbConnection
{
    private readonly string _connectionString;
    private HttpLibSqlClient? _client;
    private ConnectionState _state = ConnectionState.Closed;

    public HttpLibSqlDbConnection(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    internal HttpLibSqlClient? Client => _client;
    
    public override string ConnectionString 
    { 
        get => _connectionString; 
        set => throw new NotSupportedException("Cannot change connection string after construction"); 
    }
    
    public override string ServerVersion => "libSQL HTTP";
    
    public override string? Database => null;
    
    public override string? DataSource => _connectionString;
    
    public override ConnectionState State => _state;

    public override void Open()
    {
        if (_state == ConnectionState.Open)
            return;

        try
        {
            // Parse connection string - expect format like: "libsql://your-db.turso.io?authToken=your-token"
            var (url, authToken) = ParseConnectionString(_connectionString);
            _client = new HttpLibSqlClient(url, authToken);
                
            _state = ConnectionState.Open;
            OnStateChange(new StateChangeEventArgs(ConnectionState.Closed, ConnectionState.Open));
            Console.WriteLine($"DEBUG HTTP LibSqlDbConnection: Successfully opened connection to {url}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR HTTP LibSqlDbConnection.Open: {ex}");
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
            var (url, authToken) = ParseConnectionString(_connectionString);
            _client = new HttpLibSqlClient(url, authToken);
                
            _state = ConnectionState.Open;
            OnStateChange(new StateChangeEventArgs(ConnectionState.Closed, ConnectionState.Open));
            Console.WriteLine($"DEBUG HTTP LibSqlDbConnection: Successfully opened connection async to {url}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR HTTP LibSqlDbConnection.OpenAsync: {ex}");
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
            _client?.Dispose();
            _client = null;
        }
        finally
        {
            var previousState = _state;
            _state = ConnectionState.Closed;
            OnStateChange(new StateChangeEventArgs(previousState, ConnectionState.Closed));
            Console.WriteLine($"DEBUG HTTP LibSqlDbConnection: Connection closed");
        }
    }

    protected override DbCommand CreateDbCommand()
    {
        if (_client == null)
            throw new InvalidOperationException("Connection is not open");
            
        Console.WriteLine($"DEBUG HTTP LibSqlDbConnection: Creating DbCommand");
        return new HttpLibSqlDbCommand(_client)
        {
            Connection = this
        };
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        Console.WriteLine($"DEBUG HTTP LibSqlDbConnection: Beginning transaction with isolation level {isolationLevel}");
        // For now, return a no-op transaction - HTTP LibSQL might not support transactions
        return new HttpLibSqlTransaction(this, isolationLevel);
    }

    public override void ChangeDatabase(string databaseName)
    {
        throw new NotSupportedException("LibSQL does not support changing databases");
    }

    private (string url, string authToken) ParseConnectionString(string connectionString)
    {
        // Parse connection strings like:
        // "libsql://your-db.turso.io?authToken=your-token"
        // or "url=libsql://your-db.turso.io;authToken=your-token"
        
        if (connectionString.StartsWith("libsql://"))
        {
            var uri = new Uri(connectionString);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var authToken = query["authToken"];
            
            if (string.IsNullOrEmpty(authToken))
            {
                throw new ArgumentException("AuthToken is required in connection string");
            }
            
            var baseUrl = $"{uri.Scheme}://{uri.Host}";
            if (uri.Port != -1 && uri.Port != 80 && uri.Port != 443)
            {
                baseUrl += $":{uri.Port}";
            }
            
            return (baseUrl, authToken);
        }
        else
        {
            // Parse key-value pairs
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            string? url = null;
            string? authToken = null;
            
            foreach (var part in parts)
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToLowerInvariant();
                    var value = keyValue[1].Trim();
                    
                    switch (key)
                    {
                        case "url":
                            url = value;
                            break;
                        case "authtoken":
                            authToken = value;
                            break;
                    }
                }
            }
            
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(authToken))
            {
                throw new ArgumentException("Both URL and AuthToken are required in connection string");
            }
            
            return (url, authToken);
        }
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