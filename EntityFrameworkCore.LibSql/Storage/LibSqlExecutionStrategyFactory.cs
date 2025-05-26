using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlExecutionStrategyFactory : IExecutionStrategyFactory
{
    private readonly ExecutionStrategyDependencies _dependencies;

    public LibSqlExecutionStrategyFactory(ExecutionStrategyDependencies dependencies)
    {
        _dependencies = dependencies;
    }

    public IExecutionStrategy Create() => new NonRetryingExecutionStrategy(_dependencies);
}