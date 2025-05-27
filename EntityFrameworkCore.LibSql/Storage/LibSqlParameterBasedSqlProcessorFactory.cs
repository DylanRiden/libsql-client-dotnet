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
        return new LibSqlParameterBasedSqlProcessor(_dependencies, useRelationalNulls);
    }
}

public class LibSqlParameterBasedSqlProcessor : RelationalParameterBasedSqlProcessor
{
    public LibSqlParameterBasedSqlProcessor(
        RelationalParameterBasedSqlProcessorDependencies dependencies,
        bool useRelationalNulls)
        : base(dependencies, useRelationalNulls)
    {
    }
    // No custom logic needed unless you want to override base behavior
}