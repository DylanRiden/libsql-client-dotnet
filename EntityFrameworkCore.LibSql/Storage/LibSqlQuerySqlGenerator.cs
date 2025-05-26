using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlQuerySqlGenerator : QuerySqlGenerator
{
    public LibSqlQuerySqlGenerator(QuerySqlGeneratorDependencies dependencies)
        : base(dependencies)
    {
    }
}