using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.Storage;

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
        return new LibSqlModificationCommandBatch(_dependencies);
    }
}

public class LibSqlModificationCommandBatch : SingularModificationCommandBatch
{
    public LibSqlModificationCommandBatch(ModificationCommandBatchFactoryDependencies dependencies)
        : base(dependencies)
    {
    }

    public override void Execute(IRelationalConnection connection)
    {
        Console.WriteLine($"DEBUG ModificationCommandBatch: Execute called");
        Console.WriteLine($"DEBUG ModificationCommandBatch: ModificationCommands count: {ModificationCommands.Count}");
        
        foreach (var cmd in ModificationCommands)
        {
            Console.WriteLine($"DEBUG ModificationCommandBatch: Command table: {cmd.TableName}");
            Console.WriteLine($"DEBUG ModificationCommandBatch: Command type: {cmd.EntityState}");
            Console.WriteLine($"DEBUG ModificationCommandBatch: Key columns:");
            
            foreach (var col in cmd.ColumnModifications.Where(c => c.IsKey))
            {
                Console.WriteLine($"  Key column {col.ColumnName}: {col.Value}");
            }
        }
        
        base.Execute(connection);
    }

    public override async Task ExecuteAsync(IRelationalConnection connection, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"DEBUG ModificationCommandBatch: ExecuteAsync called");
        Console.WriteLine($"DEBUG ModificationCommandBatch: ModificationCommands count: {ModificationCommands.Count}");
        
        foreach (var cmd in ModificationCommands)
        {
            Console.WriteLine($"DEBUG ModificationCommandBatch: Command table: {cmd.TableName}");
            Console.WriteLine($"DEBUG ModificationCommandBatch: Command type: {cmd.EntityState}");
            Console.WriteLine($"DEBUG ModificationCommandBatch: Key columns:");
            
            foreach (var col in cmd.ColumnModifications.Where(c => c.IsKey))
            {
                Console.WriteLine($"  Key column {col.ColumnName}: {col.Value} (Type: {col.Value?.GetType().Name})");
            }
        }
        
        await base.ExecuteAsync(connection, cancellationToken);
    }
}