using Microsoft.EntityFrameworkCore.Migrations;

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
}