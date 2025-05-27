using Microsoft.EntityFrameworkCore.Update;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlModificationCommandBatchFactory : IModificationCommandBatchFactory
{
    private readonly ModificationCommandBatchFactoryDependencies _dependencies;

    public LibSqlModificationCommandBatchFactory(ModificationCommandBatchFactoryDependencies dependencies)
    {
        _dependencies = dependencies;
    }

    public ModificationCommandBatch Create() 
    {
        return new SingularModificationCommandBatch(_dependencies);
    }
}