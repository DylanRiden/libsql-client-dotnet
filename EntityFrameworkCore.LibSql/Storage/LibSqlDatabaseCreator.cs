using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlDatabaseCreator : RelationalDatabaseCreator
{
    public LibSqlDatabaseCreator(RelationalDatabaseCreatorDependencies dependencies)
        : base(dependencies)
    {
    }

    public override bool Exists()
    {
        // For in-memory databases, they always "exist" once connected
        // For file databases, check if file exists
        return true;
    }

    public override Task<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public override void Create()
    {
        // The database file/connection is created automatically
        // We don't need to do anything special here for LibSQL
    }

    public override Task CreateAsync(CancellationToken cancellationToken = default)
    {
        // The database file/connection is created automatically
        return Task.CompletedTask;
    }

    public override void Delete()
    {
        // For in-memory databases, this is a no-op
        // For file databases, you might want to delete the file
    }

    public override Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public override bool HasTables()
    {
        try
        {
            return CheckForTablesInternal(Dependencies.Connection);
        }
        catch
        {
            return false;
        }
    }

    public override async Task<bool> HasTablesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await CheckForTablesInternalAsync(Dependencies.Connection, cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    private bool CheckForTablesInternal(IRelationalConnection connection)
    {
        var wasOpened = connection.DbConnection.State == System.Data.ConnectionState.Open;
        if (!wasOpened)
        {
            connection.Open();
        }

        try
        {
            using var command = connection.DbConnection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";
            var result = command.ExecuteScalar();
            return Convert.ToInt32(result) > 0;
        }
        finally
        {
            if (!wasOpened)
            {
                connection.Close();
            }
        }
    }

    private async Task<bool> CheckForTablesInternalAsync(IRelationalConnection connection, CancellationToken cancellationToken)
    {
        var wasOpened = connection.DbConnection.State == System.Data.ConnectionState.Open;
        if (!wasOpened)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            using var command = connection.DbConnection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(result) > 0;
        }
        finally
        {
            if (!wasOpened)
            {
                await connection.CloseAsync();
            }
        }
    }
}