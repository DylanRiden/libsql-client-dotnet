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
        builder.Append('"');
        builder.Append(identifier.Replace("\"", "\"\""));
        builder.Append('"');
    }

    public override string DelimitIdentifier(string identifier)
        => $"\"{EscapeIdentifier(identifier)}\"";

    public override string DelimitIdentifier(string name, string? schema)
        => DelimitIdentifier(name); // LibSQL doesn't support schemas

    protected override string GenerateLiteralValue(byte[] value)
        => $"X'{Convert.ToHexString(value)}'";

    protected override string GenerateLiteralValue(bool value)
        => value ? "1" : "0";

    protected override string GenerateLiteralValue(DateTime value)
        => $"'{value:yyyy-MM-dd HH:mm:ss.FFFFFFF}'";

    protected override string GenerateLiteralValue(DateTimeOffset value)
        => $"'{value:yyyy-MM-dd HH:mm:ss.FFFFFFFzzz}'";

    protected override string GenerateLiteralValue(Guid value)
        => $"'{value}'";

    protected override string GenerateLiteralValue(string value)
        => $"'{EscapeSqlLiteral(value)}'";

    private static string EscapeSqlLiteral(string literal)
        => literal.Replace("'", "''");
}