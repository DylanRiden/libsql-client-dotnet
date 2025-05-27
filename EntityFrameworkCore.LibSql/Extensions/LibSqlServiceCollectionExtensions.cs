// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Microsoft.EntityFrameworkCore.Sqlite.Migrations.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using EntityFrameworkCore.LibSql.Infrastructure;
using EntityFrameworkCore.LibSql.Storage;
using EntityFrameworkCore.LibSql.Extensions;
using Microsoft.EntityFrameworkCore.Sqlite.Storage.Internal;
using Microsoft.EntityFrameworkCore.Sqlite.Update.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     LibSQL-specific logging definitions.
/// </summary>
public class LibSqlLoggingDefinitions : RelationalLoggingDefinitions
{
    // This can be empty for now - it just needs to exist so the DI container can resolve LoggingDefinitions
}

/// <summary>
///     LibSQL specific extension methods for <see cref="IServiceCollection" />.
/// </summary>
public static class LibSqlServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the given Entity Framework <see cref="DbContext" /> as a service in the <see cref="IServiceCollection" />
    ///     and configures it to connect to a LibSQL database via HTTP.
    /// </summary>
    public static IServiceCollection AddLibSql<TContext>(
        this IServiceCollection serviceCollection,
        string? connectionString,
        Action<LibSqlDbContextOptionsBuilder>? libSqlOptionsAction = null,
        Action<DbContextOptionsBuilder>? optionsAction = null)
        where TContext : DbContext
        => serviceCollection.AddDbContext<TContext>(
            (_, options) =>
            {
                optionsAction?.Invoke(options);
                options.UseLibSql(connectionString!, libSqlOptionsAction);
            });

    /// <summary>
    ///     Adds the services required by the HTTP LibSQL database provider for Entity Framework
    ///     to an <see cref="IServiceCollection" />.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection AddEntityFrameworkLibSql(this IServiceCollection serviceCollection)
    {
        var builder = new EntityFrameworkRelationalServicesBuilder(serviceCollection)
            // Core provider identification
            .TryAdd<LoggingDefinitions, LibSqlLoggingDefinitions>()
            .TryAdd<IDatabaseProvider, DatabaseProvider<LibSqlOptionsExtension>>()
        
            // **CRITICAL: Add the missing SQL generation helper**
            .TryAdd<ISqlGenerationHelper, LibSqlSqlGenerationHelper>()
        
            // Command building and execution - HTTP ONLY
            .TryAdd<IRelationalConnection, HttpLibSqlConnection>()
            .TryAdd<IRelationalDatabaseCreator, LibSqlDatabaseCreator>()
            .TryAdd<IRelationalCommandBuilderFactory, HttpLibSqlCommandBuilderFactory>()
        
            // SQL generation and migrations - using SQLite generator for compatibility
            .TryAdd<IMigrationsSqlGenerator, SqliteMigrationsSqlGenerator>()
            .TryAdd<IHistoryRepository, LibSqlHistoryRepository>()
        
            // Type mappings and annotations - using more SQLite services for compatibility
            .TryAdd<IRelationalTypeMappingSource, LibSqlTypeMappingSource>()
            .TryAdd<IRelationalAnnotationProvider, LibSqlAnnotationProvider>()
        
            // Query processing
            .TryAdd<IQuerySqlGeneratorFactory, LibSqlQuerySqlGeneratorFactory>()
            .TryAdd<IRelationalParameterBasedSqlProcessorFactory, LibSqlParameterBasedSqlProcessorFactory>()
            .TryAdd<IQueryableMethodTranslatingExpressionVisitorFactory, LibSqlQueryableMethodTranslatingExpressionVisitorFactory>()
            .TryAdd<ISqlExpressionFactory, LibSqlSqlExpressionFactory>()
        
            // Update operations - using SQLite batch factory for compatibility
            .TryAdd<IUpdateSqlGenerator, LibSqlUpdateSqlGenerator>()
            .TryAdd<IModificationCommandBatchFactory, SqliteModificationCommandBatchFactory>()
        
            // Transaction handling
            .TryAdd<IRelationalTransactionFactory, LibSqlTransactionFactory>()
        
            // Method and member translators for LINQ
            .TryAdd<IMethodCallTranslatorProvider, LibSqlMethodCallTranslatorProvider>()
            .TryAdd<IMemberTranslatorProvider, LibSqlMemberTranslatorProvider>()
        
            // Execution strategy
            .TryAdd<IExecutionStrategyFactory, LibSqlExecutionStrategyFactory>();

        // Let EF Core handle everything else with defaults
        builder.TryAddCoreServices();
        return serviceCollection;
    }
}