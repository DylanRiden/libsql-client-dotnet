using System.Data;
using System.Data.Common;
using Libsql.Client;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlDbConnection : DbConnection
{
    private readonly string _connectionString;
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
        try
        {
            var options = ParseConnectionString(_connectionString);
            _client = DatabaseClient.Create(opts =>
            {
                opts.Url = options.Url;
                if (!string.IsNullOrEmpty(options.AuthToken))
                    opts.AuthToken = options.AuthToken;
            });
                
            _state = ConnectionState.Open;
            OnStateChange(new StateChangeEventArgs(ConnectionState.Closed, ConnectionState.Open));
        }
        catch
        {
            _state = ConnectionState.Broken;
            throw;
        }
    }

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        if (_state == ConnectionState.Open)
            return Task.CompletedTask;

        try
        {
            var options = ParseConnectionString(_connectionString);
            _client = DatabaseClient.Create(opts =>
            {
                opts.Url = options.Url;
                if (!string.IsNullOrEmpty(options.AuthToken))
                    opts.AuthToken = options.AuthToken;
            });

            _state = ConnectionState.Open;
            OnStateChange(new StateChangeEventArgs(ConnectionState.Closed, ConnectionState.Open));
            return Task.CompletedTask;
        }
        catch
        {
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
            (_client as IDisposable)?.Dispose();
        }
        finally
        {
            _client = null;
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

    private class ConnectionOptions
    {
        public string Url { get; set; } = string.Empty;
        public string? AuthToken { get; set; }
    }
}
