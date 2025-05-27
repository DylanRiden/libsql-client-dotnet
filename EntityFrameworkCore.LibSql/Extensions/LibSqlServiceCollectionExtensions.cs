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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using EntityFrameworkCore.LibSql.Infrastructure;
using EntityFrameworkCore.LibSql.Storage;
using EntityFrameworkCore.LibSql.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     LibSQL specific extension methods for <see cref="IServiceCollection" />.
/// </summary>
public static class LibSqlServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the given Entity Framework <see cref="DbContext" /> as a service in the <see cref="IServiceCollection" />
    ///     and configures it to connect to a LibSQL database.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is a shortcut for configuring a <see cref="DbContext" /> to use LibSQL. It does not support all options.
    ///         Use <see cref="O:EntityFrameworkServiceCollectionExtensions.AddDbContext" /> and related methods for full control of
    ///         this process.
    ///     </para>
    ///     <para>
    ///         Use this method when using dependency injection in your application, such as with ASP.NET Core.
    ///         For applications that don't use dependency injection, consider creating <see cref="DbContext" />
    ///         instances directly with its constructor. The <see cref="DbContext.OnConfiguring" /> method can then be
    ///         overridden to configure the LibSQL provider and connection string.
    ///     </para>
    ///     <para>
    ///         To configure the <see cref="DbContextOptions{TContext}" /> for the context, either override the
    ///         <see cref="DbContext.OnConfiguring" /> method in your derived context, or supply
    ///         an optional action to configure the <see cref="DbContextOptions" /> for the context.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-di">Using DbContext with dependency injection</see> for more information and examples.
    ///     </para>
    ///     <para>
    ///         See <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see>, and
    ///         <see href="https://github.com/tursodatabase/libsql-client-dotnet">Accessing LibSQL databases with EF Core</see> for more information and examples.
    ///     </para>
    /// </remarks>
    /// <typeparam name="TContext">The type of context to be registered.</typeparam>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="connectionString">The connection string of the database to connect to.</param>
    /// <param name="libSqlOptionsAction">An optional action to allow additional LibSQL specific configuration.</param>
    /// <param name="optionsAction">An optional action to configure the <see cref="DbContextOptions" /> for the context.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
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
    ///     <para>
    ///         Adds the services required by the LibSQL database provider for Entity Framework
    ///         to an <see cref="IServiceCollection" />.
    ///     </para>
    ///     <para>
    ///         Warning: Do not call this method accidentally. It is much more likely you need
    ///         to call <see cref="AddLibSql{TContext}" />.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     Calling this method is no longer necessary when building most applications, including those that
    ///     use dependency injection in ASP.NET or elsewhere.
    ///     It is only needed when building the internal service provider for use with
    ///     the <see cref="DbContextOptionsBuilder.UseInternalServiceProvider" /> method.
    ///     This is not recommend other than for some advanced scenarios.
    /// </remarks>
    /// <param name="serviceCollection">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <returns>
    ///     The same service collection so that multiple calls can be chained.
    /// </returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection AddEntityFrameworkLibSql(this IServiceCollection serviceCollection)
    {
        // First add core EF services
        new EntityFrameworkServicesBuilder(serviceCollection).TryAddCoreServices();

        var builder = new EntityFrameworkRelationalServicesBuilder(serviceCollection)
            .TryAdd<IDatabaseProvider, DatabaseProvider<LibSqlOptionsExtension>>()
            .TryAdd<IExecutionStrategyFactory, LibSqlExecutionStrategyFactory>()
            .TryAdd<IQueryableMethodTranslatingExpressionVisitorFactory, LibSqlQueryableMethodTranslatingExpressionVisitorFactory>()
            .TryAdd<IRelationalConnection, LibSqlConnection>()
            .TryAdd<ISqlGenerationHelper, LibSqlSqlGenerationHelper>()
            .TryAdd<IRelationalTypeMappingSource, LibSqlTypeMappingSource>()
            .TryAdd<IQuerySqlGeneratorFactory, LibSqlQuerySqlGeneratorFactory>()
            .TryAdd<IRelationalTransactionFactory, LibSqlTransactionFactory>()
            .TryAdd<IModificationCommandBatchFactory, LibSqlModificationCommandBatchFactory>()
            .TryAdd<IRelationalDatabaseCreator, LibSqlDatabaseCreator>()
            .TryAdd<IHistoryRepository, LibSqlHistoryRepository>()
            .TryAdd<IMigrationsSqlGenerator, LibSqlMigrationsSqlGenerator>()
            .TryAdd<IRelationalAnnotationProvider, LibSqlAnnotationProvider>()
            .TryAdd<IMethodCallTranslatorProvider, LibSqlMethodCallTranslatorProvider>()
            .TryAdd<IMemberTranslatorProvider, LibSqlMemberTranslatorProvider>()
            .TryAdd<IRelationalParameterBasedSqlProcessorFactory, LibSqlParameterBasedSqlProcessorFactory>()
            .TryAddProviderSpecificServices(b =>
            {
                // Only register services that are truly provider-specific and not already handled by the main builder
                b.TryAdd(typeof(IRelationalCommand), typeof(LibSqlCommand), ServiceLifetime.Singleton);
                b.TryAdd(typeof(IRelationalCommandBuilder),
                    p => new LibSqlCommandBuilder(
                        (IRelationalTypeMappingSource)p.GetRequiredService(typeof(IRelationalTypeMappingSource))),
                    ServiceLifetime.Singleton);
            });

        return serviceCollection;
    }
}