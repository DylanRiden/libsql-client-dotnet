using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlMigrationsSqlGenerator : MigrationsSqlGenerator
{
    public LibSqlMigrationsSqlGenerator(
        MigrationsSqlGeneratorDependencies dependencies,
        IRelationalAnnotationProvider migrationsAnnotations)
        : base(dependencies, migrationsAnnotations)
    {
    }

    private void CreateTableColumns(
        CreateTableOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        for (var i = 0; i < operation.Columns.Count; i++)
        {
            var column = operation.Columns[i];
            ColumnDefinition(column, model, builder);

            if (i != operation.Columns.Count - 1)
            {
                builder.AppendLine(",");
            }
        }
    }

    private void CreateTableConstraints(
        CreateTableOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        if (operation.PrimaryKey != null)
        {
            builder.AppendLine(",");
            PrimaryKeyConstraint(operation.PrimaryKey, model, builder);
        }

        foreach (var foreignKey in operation.ForeignKeys)
        {
            builder.AppendLine(",");
            ForeignKeyConstraint(foreignKey, model, builder);
        }
    }

    protected virtual void ColumnDefinition(
        AddColumnOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        builder
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name))
            .Append(" ")
            .Append(operation.ColumnType ?? GetColumnType(operation.ClrType));

        if (!operation.IsNullable)
        {
            builder.Append(" NOT NULL");
        }

        if (operation.DefaultValue != null)
        {
            builder
                .Append(" DEFAULT ")
                .Append(Dependencies.SqlGenerationHelper.GenerateLiteral(operation.DefaultValue));
        }
        else if (!string.IsNullOrEmpty(operation.DefaultValueSql))
        {
            builder
                .Append(" DEFAULT ")
                .Append(operation.DefaultValueSql);
        }
    }

    private string GetColumnType(Type clrType)
    {
        return clrType switch
        {
            _ when clrType == typeof(string) => "TEXT",
            _ when clrType == typeof(int) || clrType == typeof(long) || clrType == typeof(bool) => "INTEGER",
            _ when clrType == typeof(double) || clrType == typeof(float) || clrType == typeof(decimal) => "REAL",
            _ when clrType == typeof(byte[]) => "BLOB",
            _ when clrType == typeof(DateTime) || clrType == typeof(DateTimeOffset) => "TEXT",
            _ when clrType == typeof(Guid) => "TEXT",
            _ => "TEXT"
        };
    }
}