using EntityFrameworkCore.LibSql.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EntityFrameworkCore.LibSql.Extensions;

public static class LibSqlDbContextOptionsExtensions
{
    public static DbContextOptionsBuilder UseLibSql(
        this DbContextOptionsBuilder optionsBuilder,
        string connectionString,
        Action<LibSqlDbContextOptionsBuilder>? libSqlOptionsAction = null)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        var extension = GetOrCreateExtension(optionsBuilder).WithConnectionString(connectionString);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

        ConfigureWarnings(optionsBuilder);

        libSqlOptionsAction?.Invoke(new LibSqlDbContextOptionsBuilder(optionsBuilder));

        return optionsBuilder;
    }

    public static DbContextOptionsBuilder<TContext> UseLibSql<TContext>(
        this DbContextOptionsBuilder<TContext> optionsBuilder,
        string connectionString,
        Action<LibSqlDbContextOptionsBuilder>? libSqlOptionsAction = null)
        where TContext : DbContext
    {
        return (DbContextOptionsBuilder<TContext>)UseLibSql(
            (DbContextOptionsBuilder)optionsBuilder, connectionString, libSqlOptionsAction);
    }

    private static LibSqlOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.Options.FindExtension<LibSqlOptionsExtension>()
            ?? new LibSqlOptionsExtension();

    private static void ConfigureWarnings(DbContextOptionsBuilder optionsBuilder)
    {
        // Configure any specific warnings for LibSQL
        // Similar to how SQLite provider handles warnings
    }
}