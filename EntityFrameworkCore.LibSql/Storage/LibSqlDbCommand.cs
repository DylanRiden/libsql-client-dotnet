using System.Data;
using System.Data.Common;
using Libsql.Client;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlDbCommand : DbCommand
{
    private readonly IDatabaseClient _client;
    private string _commandText = string.Empty;
    private readonly LibSqlParameterCollection _parameters;

    public LibSqlDbCommand(IDatabaseClient client)
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
    protected override DbConnection? DbConnection { get; set; }
    protected override DbTransaction? DbTransaction { get; set; }
    protected override DbParameterCollection DbParameterCollection => _parameters;

    public override void Cancel()
    {
        // LibSQL doesn't support command cancellation
    }

    public override int ExecuteNonQuery() => ExecuteNonQueryAsync().GetAwaiter().GetResult();

    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        var result = await ExecuteInternal(cancellationToken);
        // Return affected rows from the result - adjust based on actual API
        return 1; // Placeholder - adjust based on actual result type
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
        var result = await ExecuteInternal(cancellationToken);
        return new LibSqlDataReader(result);
    }

    public override void Prepare()
    {
        // LibSQL doesn't expose prepared statements in the current client
        // This could be implemented when/if the client supports it
    }

    protected override DbParameter CreateDbParameter()
    {
        return new LibSqlParameter();
    }

    private async Task<object> ExecuteInternal(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_commandText))
            throw new InvalidOperationException("Command text cannot be empty");

        try
        {
            // Convert parameters to the format expected by LibSQL client
            var parameterValues = GetParameterValues();
            
            if (parameterValues.Length > 0)
            {
                return await _client.Execute(_commandText, parameterValues);
            }
            else
            {
                return await _client.Execute(_commandText);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error executing LibSQL command: {ex.Message}", ex);
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
            null => DBNull.Value,
            DBNull => DBNull.Value,
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
