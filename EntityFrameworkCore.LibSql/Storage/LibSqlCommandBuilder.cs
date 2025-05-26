using System.Text;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlCommandBuilder : IRelationalCommandBuilder
{
    private readonly StringBuilder _commandTextBuilder = new();
    private readonly List<IRelationalParameter> _parameters = new();

    public IReadOnlyList<IRelationalParameter> Parameters => _parameters;
    
    // Provide a simple implementation that returns null - EF Core will handle this
    public IRelationalTypeMappingSource? TypeMappingSource => null;

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
        // Use a simple string type mapping
        var typeMapping = new RelationalTypeMapping("TEXT", typeof(string));
        return AddParameter(new RelationalParameter(invariantName, name, typeMapping, nullable: true));
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

// Simple RelationalTypeMapping for basic functionality
public class SimpleRelationalTypeMapping : RelationalTypeMapping
{
    public SimpleRelationalTypeMapping(string storeType, Type clrType) : base(storeType, clrType)
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
    {
        return new SimpleRelationalTypeMapping(StoreType, parameters.ClrType ?? ClrType);
    }
}