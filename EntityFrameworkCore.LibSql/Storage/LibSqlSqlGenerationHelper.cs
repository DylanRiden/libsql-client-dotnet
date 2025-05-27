using System.Text;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlSqlGenerationHelper : RelationalSqlGenerationHelper
{
    public LibSqlSqlGenerationHelper(RelationalSqlGenerationHelperDependencies dependencies)
        : base(dependencies)
    {
    }

    public override string EscapeIdentifier(string identifier)
        => identifier.Replace("\"", "\"\"");

    public override void EscapeIdentifier(StringBuilder builder, string identifier)
    {
        builder.Append(identifier.Replace("\"", "\"\""));
    }

    public override string DelimitIdentifier(string identifier)
        => $"\"{EscapeIdentifier(identifier)}\"";

    public override string DelimitIdentifier(string name, string? schema)
        => DelimitIdentifier(name); // LibSQL doesn't support schemas

    public override string GenerateParameterName(string name)
        => $"@{name}";

    public override void GenerateParameterName(StringBuilder builder, string name)
        => builder.Append($"@{name}");

    public override string GenerateParameterNamePlaceholder(string name)
        => GenerateParameterName(name);

    public override void GenerateParameterNamePlaceholder(StringBuilder builder, string name)
        => GenerateParameterName(builder, name);

    // Override the statement terminator to ensure consistency
    public override string StatementTerminator => ";";

    // Ensure batch separator is appropriate for SQLite
    public override string BatchTerminator => string.Empty;
}