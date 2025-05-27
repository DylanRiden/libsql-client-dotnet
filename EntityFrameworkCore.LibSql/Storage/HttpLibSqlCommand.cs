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

public class HttpLibSqlCommand : IRelationalCommand
{
    private readonly string _commandText;
    private readonly IReadOnlyList<IRelationalParameter> _parameters;

    public HttpLibSqlCommand(string commandText, IReadOnlyList<IRelationalParameter> parameters)
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
        var reader = await command.ExecuteReaderAsync(cancellationToken);
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

    private HttpLibSqlDbCommand CreateDbCommand(RelationalCommandParameterObject parameterObject)
    {
        // Cast to HttpLibSqlDbConnection instead of LibSqlDbConnection
        var connection = (HttpLibSqlDbConnection)parameterObject.Connection.DbConnection;
        var command = (HttpLibSqlDbCommand)connection.CreateCommand();
        command.CommandText = _commandText;

        Console.WriteLine($"DEBUG HttpLibSqlCommand: Creating command with text: {_commandText}");
        Console.WriteLine($"DEBUG HttpLibSqlCommand: Parameter count: {_parameters.Count}");

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
                
                Console.WriteLine($"DEBUG HttpLibSqlCommand: Parameter {dbParam.ParameterName} = {dbParam.Value} (Type: {dbParam.Value?.GetType().Name ?? "null"})");
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