using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlDatabaseCreator : RelationalDatabaseCreator
{
    public LibSqlDatabaseCreator(RelationalDatabaseCreatorDependencies dependencies)
        : base(dependencies)
    {
    }

    public override bool Exists() => true; // LibSQL databases are created on first connection
    public override Task<bool> ExistsAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    public override void Create() { } // No-op for LibSQL
    public override Task CreateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public override void Delete() { } // No-op for LibSQL
    public override Task DeleteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}