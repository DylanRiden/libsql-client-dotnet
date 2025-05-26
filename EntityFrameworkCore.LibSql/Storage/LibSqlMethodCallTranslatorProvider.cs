using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlMethodCallTranslatorProvider : RelationalMethodCallTranslatorProvider
{
    public LibSqlMethodCallTranslatorProvider(RelationalMethodCallTranslatorProviderDependencies dependencies)
        : base(dependencies)
    {
    }
}