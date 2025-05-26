using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlQuerySqlGeneratorFactory : IQuerySqlGeneratorFactory
{
    private readonly QuerySqlGeneratorDependencies _dependencies;

    public LibSqlQuerySqlGeneratorFactory(QuerySqlGeneratorDependencies dependencies)
    {
        _dependencies = dependencies;
    }

    public QuerySqlGenerator Create() => new LibSqlQuerySqlGenerator(_dependencies);
}