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
        => DelimitIdentifier(name);
    
    private static string EscapeSqlLiteral(string literal)
        => literal.Replace("'", "''");
}