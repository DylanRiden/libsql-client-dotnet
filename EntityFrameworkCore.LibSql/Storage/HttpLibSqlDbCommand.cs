using System.Data;
using System.Data.Common;
using EntityFrameworkCore.LibSql.Http;

namespace EntityFrameworkCore.LibSql.Storage;

public class HttpLibSqlDbCommand : DbCommand
{
    private readonly HttpLibSqlClient _client;
    private string _commandText = string.Empty;
    private readonly LibSqlParameterCollection _parameters;
    private HttpLibSqlDbConnection? _connection;

    public HttpLibSqlDbCommand(HttpLibSqlClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _parameters = new LibSqlParameterCollection();
    }

    public override string CommandText
    {
        get => _commandText;
        set => _commandText = value ?? string.Empty;
    }

    public override int CommandTimeout { get; set; } = 30;
    public override CommandType CommandType { get; set; } = CommandType.Text;
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    
    protected override DbConnection? DbConnection 
    { 
        get => _connection;
        set => _connection = (HttpLibSqlDbConnection?)value;
    }
    
    protected override DbTransaction? DbTransaction { get; set; }
    protected override DbParameterCollection DbParameterCollection => _parameters;

    public override void Cancel()
    {
        // HTTP requests can't be easily cancelled mid-flight
    }

    public override int ExecuteNonQuery() => ExecuteNonQueryAsync().GetAwaiter().GetResult();

    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"DEBUG HTTP LibSqlDbCommand: ExecuteNonQueryAsync - SQL: {_commandText}");
        Console.WriteLine($"DEBUG HTTP LibSqlDbCommand: Parameter count: {_parameters.Count}");
        
        // Debug parameters
        for (int i = 0; i < _parameters.Count; i++)
        {
            var param = _parameters[i];
            Console.WriteLine($"DEBUG HTTP LibSqlDbCommand: Parameter {i}: {param.ParameterName} = {param.Value} (Type: {param.Value?.GetType().Name ?? "null"})");
        }

        var result = await ExecuteInternal(cancellationToken);
        
        // Return the number of affected rows
        return result.RowsAffected;
    }

    public override object? ExecuteScalar() => ExecuteScalarAsync().GetAwaiter().GetResult();

    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        using var reader = await ExecuteDbDataReaderAsync(CommandBehavior.Default, cancellationToken);
        if (await reader.ReadAsync(cancellationToken) && reader.FieldCount > 0)
        {
            return reader.GetValue(0);
        }
        return null;
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
        ExecuteDbDataReaderAsync(behavior, default).GetAwaiter().GetResult();

    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"DEBUG HTTP LibSqlDbCommand: ExecuteDbDataReaderAsync - SQL: {_commandText}");
        var result = await ExecuteInternal(cancellationToken);
        return new HttpLibSqlDataReader(result);
    }

    public override void Prepare()
    {
        // HTTP-based client doesn't support prepared statements
    }

    protected override DbParameter CreateDbParameter()
    {
        return new LibSqlParameter();
    }

    private async Task<LibSqlResult> ExecuteInternal(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_commandText))
            throw new InvalidOperationException("Command text cannot be empty");

        try
        {
            Console.WriteLine($"DEBUG HTTP LibSqlDbCommand: Executing SQL: {_commandText}");
        
            // Convert parameters to the format expected by HTTP LibSQL
            var parameterValues = GetParameterValues();
        
            if (parameterValues.Length > 0)
            {
                Console.WriteLine($"DEBUG HTTP LibSqlDbCommand: Executing with {parameterValues.Length} parameters");
                for (int i = 0; i < parameterValues.Length; i++)
                {
                    Console.WriteLine($"DEBUG HTTP LibSqlDbCommand: Param[{i}] = {parameterValues[i]} ({parameterValues[i]?.GetType().Name ?? "null"})");
                }
            }
            else
            {
                Console.WriteLine($"DEBUG HTTP LibSqlDbCommand: Executing without parameters");
            }

            var result = await _client.ExecuteAsync(_commandText, parameterValues);
            
            Console.WriteLine($"DEBUG HTTP LibSqlDbCommand: Result - RowsAffected: {result.RowsAffected}, Columns: {result.Columns.Length}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR HTTP LibSqlDbCommand: Failed to execute - {ex.Message}");
            throw new InvalidOperationException($"Error executing HTTP LibSQL command: {ex.Message}", ex);
        }
    }
    
    private object[] GetParameterValues()
    {
        var values = new object[_parameters.Count];
        for (int i = 0; i < _parameters.Count; i++)
        {
            var param = _parameters[i];
            values[i] = ConvertParameterValue(param.Value);
        }
        return values;
    }

    private static object ConvertParameterValue(object? value)
    {
        return value switch
        {
            null => null!,
            DBNull => null!,
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss.fff"), // ISO format for HTTP
            bool b => b,
            _ => value
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _parameters.Clear();
        }
        base.Dispose(disposing);
    }
}