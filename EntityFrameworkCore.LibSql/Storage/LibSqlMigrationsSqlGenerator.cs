using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlMigrationsSqlGenerator : MigrationsSqlGenerator
{
    public LibSqlMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies)
        : base(dependencies)
    {
    }

    // Add override keywords to fix warnings
    protected override void CreateTableColumns(
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

    protected override void CreateTableConstraints(
        CreateTableOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        foreach (var foreignKey in operation.ForeignKeys)
        {
            builder.AppendLine(",");
            ForeignKeyConstraint(foreignKey, model, builder);
        }
    
        foreach (var uniqueConstraint in operation.UniqueConstraints ?? Enumerable.Empty<AddUniqueConstraintOperation>())
        {
            builder.AppendLine(",");
            UniqueConstraint(uniqueConstraint, model, builder);
        }
    
        foreach (var checkConstraint in operation.CheckConstraints ?? Enumerable.Empty<AddCheckConstraintOperation>())
        {
            builder.AppendLine(",");
            CheckConstraint(checkConstraint, model, builder);
        }
    }

    protected override void ColumnDefinition(
        AddColumnOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        builder
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name))
            .Append(" ")
            .Append(operation.ColumnType ?? GetColumnType(operation.ClrType));

        // Check if this column is part of the primary key
        var createTableOperation = operation as AddColumnOperation;
        var isPrimaryKeyColumn = operation.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) && 
                                 operation.ClrType == typeof(int);
    
        if (isPrimaryKeyColumn)
        {
            builder.Append(" PRIMARY KEY");
        }
        else if (!operation.IsNullable)
        {
            builder.Append(" NOT NULL");
        }

        if (operation.DefaultValue != null)
        {
            builder
                .Append(" DEFAULT ")
                .Append(GenerateSqlLiteral(operation.DefaultValue));
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

    private string GenerateSqlLiteral(object value)
    {
        return value switch
        {
            null => "NULL",
            string s => $"'{s.Replace("'", "''")}'",
            bool b => b ? "1" : "0",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss.FFFFFFF}'",
            Guid g => $"'{g}'",
            _ => value.ToString() ?? "NULL"
        };
    }
}
