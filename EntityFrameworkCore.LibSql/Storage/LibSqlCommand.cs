using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlCommand : IRelationalCommand
{
    private readonly string _commandText;
    private readonly IReadOnlyList<IRelationalParameter> _parameters;

    public LibSqlCommand(string commandText, IReadOnlyList<IRelationalParameter> parameters)
    {
        _commandText = commandText;
        _parameters = parameters;
    }

    public string CommandText => _commandText;
    public IReadOnlyList<IRelationalParameter> Parameters => _parameters;

    public int ExecuteNonQuery(RelationalCommandParameterObject parameterObject)
    {
        using var command = CreateDbCommand(parameterObject);
        return command.ExecuteNonQuery();
    }

    public async Task<int> ExecuteNonQueryAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken = default)
    {
        using var command = CreateDbCommand(parameterObject);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public object? ExecuteScalar(RelationalCommandParameterObject parameterObject)
    {
        using var command = CreateDbCommand(parameterObject);
        return command.ExecuteScalar();
    }

    public async Task<object?> ExecuteScalarAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken = default)
    {
        using var command = CreateDbCommand(parameterObject);
        return await command.ExecuteScalarAsync(cancellationToken);
    }

    public RelationalDataReader ExecuteReader(RelationalCommandParameterObject parameterObject)
    {
        var command = CreateDbCommand(parameterObject);
        var reader = command.ExecuteReader();
        return new RelationalDataReader(reader);
    }

    public async Task<RelationalDataReader> ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken = default)
    {
        var command = CreateDbCommand(parameterObject);
        var reader = await command.ExecuteReaderAsync(cancellationToken);
        return new RelationalDataReader(reader);
    }

    // Add missing method
    public DbCommand CreateDbCommand(RelationalCommandParameterObject parameterObject, Guid commandId, DbCommandMethod commandMethod)
    {
        return CreateDbCommand(parameterObject);
    }

    private LibSqlDbCommand CreateDbCommand(RelationalCommandParameterObject parameterObject)
    {
        var connection = (LibSqlDbConnection)parameterObject.Connection.DbConnection;
        var command = (LibSqlDbCommand)connection.CreateCommand();
        command.CommandText = _commandText;

        // Add parameters
        for (int i = 0; i < _parameters.Count; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = _parameters[i].InvariantName;
            parameter.Value = parameterObject.ParameterValues[_parameters[i].InvariantName];
            command.Parameters.Add(parameter);
        }

        return command;
    }

    public void PopulateFrom(IRelationalCommandTemplate template)
    {
        // Implementation for command template population
    }
}