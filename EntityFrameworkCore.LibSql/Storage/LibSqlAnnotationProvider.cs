using Microsoft.EntityFrameworkCore.Metadata;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlAnnotationProvider : RelationalAnnotationProvider
{
    public LibSqlAnnotationProvider(RelationalAnnotationProviderDependencies dependencies)
        : base(dependencies)
    {
    }
}