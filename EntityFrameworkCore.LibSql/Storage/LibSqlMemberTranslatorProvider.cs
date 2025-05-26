using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlMemberTranslatorProvider : RelationalMemberTranslatorProvider
{
    public LibSqlMemberTranslatorProvider(RelationalMemberTranslatorProviderDependencies dependencies)
        : base(dependencies)
    {
    }
}