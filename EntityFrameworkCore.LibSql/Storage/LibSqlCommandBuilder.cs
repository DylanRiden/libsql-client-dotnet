using System.Text;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data.Common;
using System.Collections.Generic;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlCommandBuilder : IRelationalCommandBuilder
{
    private readonly StringBuilder _commandTextBuilder = new();
    private readonly List<IRelationalParameter> _parameters = new();
    private readonly IRelationalTypeMappingSource _typeMappingSource;

    public LibSqlCommandBuilder(IRelationalTypeMappingSource typeMappingSource)
    {
        _typeMappingSource = typeMappingSource;
    }

    public IReadOnlyList<IRelationalParameter> Parameters => _parameters;
    
    public IRelationalTypeMappingSource TypeMappingSource => _typeMappingSource;

    public IRelationalCommandBuilder Append(string value)
    {
        _commandTextBuilder.Append(value);
        return this;
    }

    public IRelationalCommandBuilder AppendLine()
    {
        _commandTextBuilder.AppendLine();
        return this;
    }

    public IRelationalCommandBuilder AppendLine(string value)
    {
        _commandTextBuilder.AppendLine(value);
        return this;
    }

    public IRelationalCommandBuilder AppendLines(string value)
    {
        _commandTextBuilder.AppendLine(value);
        return this;
    }

    public IRelationalCommandBuilder IncrementIndent() => this;
    public IRelationalCommandBuilder DecrementIndent() => this;

    public int CommandTextLength => _commandTextBuilder.Length;

    public IRelationalCommand Build() => new LibSqlCommand(_commandTextBuilder.ToString(), _parameters);

    public IRelationalCommandBuilder AddParameter(IRelationalParameter parameter)
    {
        _parameters.Add(parameter);
        return this;
    }

    public IRelationalCommandBuilder AddParameter(string invariantName, string name)
    {
        // Create a simple parameter without complex type mapping
        var parameter = new SimpleRelationalParameter(invariantName, name);
        return AddParameter(parameter);
    }

    public IRelationalCommandBuilder RemoveParameterAt(int index)
    {
        if (index >= 0 && index < _parameters.Count)
        {
            _parameters.RemoveAt(index);
        }
        return this;
    }

    public override string ToString() => _commandTextBuilder.ToString();
}

public class SimpleRelationalParameter : IRelationalParameter
{
    public SimpleRelationalParameter(string invariantName, string name)
    {
        InvariantName = invariantName;
        Name = name;
    }

    public string InvariantName { get; }
    public string Name { get; }

    public void AddDbParameter(DbCommand command, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = InvariantName;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    public void AddDbParameter(DbCommand command, IReadOnlyDictionary<string, object?>? parameterValues)
    {
        object? value = null;
        if (parameterValues != null && parameterValues.TryGetValue(InvariantName, out var v))
        {
            value = v;
        }
        AddDbParameter(command, value);
    }
}
