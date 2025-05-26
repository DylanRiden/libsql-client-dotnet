using Microsoft.EntityFrameworkCore.Query;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlParameterBasedSqlProcessorFactory : IRelationalParameterBasedSqlProcessorFactory
{
    private readonly RelationalParameterBasedSqlProcessorDependencies _dependencies;

    public LibSqlParameterBasedSqlProcessorFactory(RelationalParameterBasedSqlProcessorDependencies dependencies)
    {
        _dependencies = dependencies;
    }

    public RelationalParameterBasedSqlProcessor Create(bool useRelationalNulls)
    {
        return new RelationalParameterBasedSqlProcessor(_dependencies, useRelationalNulls);
    }
}