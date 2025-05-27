using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

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
        var reader = new LibSqlDataReader(command.ExecuteReader());
        var logger = parameterObject.Context?.GetService<IRelationalCommandDiagnosticsLogger>();
        var intReader = new RelationalDataReader();
        
        intReader.Initialize(parameterObject.Connection,
            command,
            reader,
            Guid.NewGuid(),
            logger);
        return intReader;
    }

    public async Task<RelationalDataReader> ExecuteReaderAsync(RelationalCommandParameterObject parameterObject, CancellationToken cancellationToken = default)
    {
        var command = CreateDbCommand(parameterObject);
        var reader = new LibSqlDataReader(await command.ExecuteReaderAsync(cancellationToken));
        var logger = parameterObject.Context?.GetService<IRelationalCommandDiagnosticsLogger>();
        var intReader = new RelationalDataReader();
        
        intReader.Initialize(parameterObject.Connection,
            command,
            reader,
            Guid.NewGuid(),
            logger);
        return intReader;
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

        Console.WriteLine($"DEBUG LibSqlCommand: Creating command with text: {_commandText}");
        Console.WriteLine($"DEBUG LibSqlCommand: Parameter count: {_parameters.Count}");

        // Improved parameter handling
        if (parameterObject.ParameterValues != null)
        {
            foreach (var parameter in _parameters)
            {
                var dbParam = command.CreateParameter();
                dbParam.ParameterName = parameter.InvariantName;
                
                // Get the parameter value, handling null properly
                if (parameterObject.ParameterValues.TryGetValue(parameter.InvariantName, out var value))
                {
                    dbParam.Value = value ?? DBNull.Value;
                }
                else
                {
                    dbParam.Value = DBNull.Value;
                }
                
                Console.WriteLine($"DEBUG LibSqlCommand: Parameter {dbParam.ParameterName} = {dbParam.Value} (Type: {dbParam.Value?.GetType().Name ?? "null"})");
                command.Parameters.Add(dbParam);
            }
        }

        return command;
    }

    public void PopulateFrom(IRelationalCommandTemplate template)
    {
        // Implementation for command template population
    }
}