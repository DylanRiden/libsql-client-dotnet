using Microsoft.EntityFrameworkCore.Migrations;
using System.Threading;
using System.Threading.Tasks;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlHistoryRepository : HistoryRepository
{
    public LibSqlHistoryRepository(HistoryRepositoryDependencies dependencies)
        : base(dependencies)
    {
    }

    protected override string ExistsSql =>
        "SELECT COUNT(*) FROM sqlite_master WHERE name = '__EFMigrationsHistory' AND type = 'table';";

    public override string GetCreateScript() =>
        """
        CREATE TABLE "__EFMigrationsHistory" (
            "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
            "ProductVersion" TEXT NOT NULL
        );
        """;

    public override string GetInsertScript(HistoryRow row) =>
        $"""
         INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
         VALUES ('{row.MigrationId}', '{row.ProductVersion}');
         """;

    public override string GetDeleteScript(string migrationId) =>
        $"""DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = '{migrationId}';""";
    
    public override string GetCreateIfNotExistsScript() => GetCreateScript();
    public override string GetBeginIfNotExistsScript(string migrationId) => "";
    public override string GetBeginIfExistsScript(string migrationId) => "";
    public override string GetEndIfScript() => "";
    protected override bool InterpretExistsResult(object? value) => (long?)value > 0;

    public override LockReleaseBehavior LockReleaseBehavior => LockReleaseBehavior.Connection;

    public override IMigrationsDatabaseLock AcquireDatabaseLock()
    {
        return new NoopMigrationsDatabaseLock(this);
    }

    public override Task<IMigrationsDatabaseLock> AcquireDatabaseLockAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IMigrationsDatabaseLock>(new NoopMigrationsDatabaseLock(this));
    }

    private sealed class NoopMigrationsDatabaseLock : IMigrationsDatabaseLock
    {
        public NoopMigrationsDatabaseLock(IHistoryRepository historyRepository)
        {
            HistoryRepository = historyRepository;
        }

        public IHistoryRepository HistoryRepository { get; }
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public void ReacquireIfNeeded(bool throwOnFailure, bool? useTransaction) { }
        public Task ReacquireIfNeededAsync(bool throwOnFailure, bool? useTransaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}