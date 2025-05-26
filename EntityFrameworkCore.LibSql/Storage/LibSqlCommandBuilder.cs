using System.Text;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlCommandBuilder : IRelationalCommandBuilder
{
    private readonly StringBuilder _commandTextBuilder = new();
    private readonly List<IRelationalParameter> _parameters = new();

    public IReadOnlyList<IRelationalParameter> Parameters => _parameters;

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
        return AddParameter(new RelationalParameter(invariantName, name, new RelationalTypeMapping("TEXT", typeof(string)), nullable: true));
    }

    public override string ToString() => _commandTextBuilder.ToString();
}