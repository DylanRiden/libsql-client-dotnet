using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFrameworkCore.LibSql.Extensions;

public static class LibSqlServiceCollectionExtensions
{
    public static IServiceCollection AddEntityFrameworkLibSql(this IServiceCollection serviceCollection)
    {
        var builder = new EntityFrameworkServicesBuilder(serviceCollection)
            .TryAdd<IDatabaseProvider, DatabaseProvider<LibSqlOptionsExtension>>()
            .TryAdd<IRelationalConnection, LibSqlConnection>()
            .TryAdd<IRelationalCommand, LibSqlCommand>()
            .TryAdd<IRelationalCommandBuilder, LibSqlCommandBuilder>()
            .TryAdd<ISqlGenerationHelper, LibSqlSqlGenerationHelper>()
            .TryAdd<IRelationalTypeMappingSource, LibSqlTypeMappingSource>()
            .TryAdd<IQuerySqlGeneratorFactory, LibSqlQuerySqlGeneratorFactory>()
            .TryAdd<IRelationalTransactionFactory, LibSqlTransactionFactory>()
            .TryAdd<IExecutionStrategyFactory, LibSqlExecutionStrategyFactory>()
            .TryAdd<IModificationCommandBatchFactory, LibSqlModificationCommandBatchFactory>()
            .TryAdd<IRelationalDatabaseCreator, LibSqlDatabaseCreator>()
            .TryAdd<IHistoryRepository, LibSqlHistoryRepository>()
            .TryAdd<IMigrationsSqlGenerator, LibSqlMigrationsSqlGenerator>()
            .TryAdd<IRelationalAnnotationProvider, LibSqlAnnotationProvider>()
            .TryAdd<IMethodCallTranslatorProvider, LibSqlMethodCallTranslatorProvider>()
            .TryAdd<IMemberTranslatorProvider, LibSqlMemberTranslatorProvider>()
            .TryAdd<IQueryableMethodTranslatingExpressionVisitorFactory, LibSqlQueryableMethodTranslatingExpressionVisitorFactory>()
            .TryAdd<IRelationalParameterBasedSqlProcessorFactory, LibSqlParameterBasedSqlProcessorFactory>();

        builder.TryAddCoreServices();

        return serviceCollection;
    }
}