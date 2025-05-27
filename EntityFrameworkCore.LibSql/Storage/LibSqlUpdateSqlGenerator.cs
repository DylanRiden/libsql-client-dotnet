using System.Text;
using Microsoft.EntityFrameworkCore.Update;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlUpdateSqlGenerator : UpdateSqlGenerator
{
    public LibSqlUpdateSqlGenerator(UpdateSqlGeneratorDependencies dependencies)
        : base(dependencies)
    {
    }

    public override ResultSetMapping AppendInsertOperation(
        StringBuilder commandStringBuilder,
        IReadOnlyModificationCommand command,
        int commandPosition)
    {
        Console.WriteLine($"DEBUG UpdateSqlGenerator: AppendInsertOperation called for table {command.TableName}");
        Console.WriteLine($"DEBUG UpdateSqlGenerator: Command has {command.ColumnModifications.Count} column modifications");
        Console.WriteLine($"DEBUG UpdateSqlGenerator: Command EntityState: {command.EntityState}");
        
        foreach (var col in command.ColumnModifications)
        {
            Console.WriteLine($"DEBUG UpdateSqlGenerator: Column {col.ColumnName}:");
            Console.WriteLine($"  Value: {col.Value}");
            Console.WriteLine($"  OriginalValue: {col.OriginalValue}");
            Console.WriteLine($"  IsKey: {col.IsKey}");
            Console.WriteLine($"  IsRead: {col.IsRead}");
            Console.WriteLine($"  IsWrite: {col.IsWrite}");
            Console.WriteLine($"  UseCurrentValue: {col.UseCurrentValue}");
            Console.WriteLine($"  UseOriginalValue: {col.UseOriginalValue}");
        }

        Console.WriteLine($"DEBUG UpdateSqlGenerator: About to call base.AppendInsertOperation");
        var result = base.AppendInsertOperation(commandStringBuilder, command, commandPosition);
        Console.WriteLine($"DEBUG UpdateSqlGenerator: Generated SQL so far: {commandStringBuilder}");
        return result;
    }

    protected override void AppendValuesHeader(StringBuilder commandStringBuilder, IReadOnlyList<IColumnModification> operations)
    {
        Console.WriteLine($"DEBUG UpdateSqlGenerator: AppendValuesHeader with {operations.Count} operations");
        base.AppendValuesHeader(commandStringBuilder, operations);
    }

    protected override void AppendValues(StringBuilder commandStringBuilder, string name, string? schema, IReadOnlyList<IColumnModification> operations)
    {
        Console.WriteLine($"DEBUG UpdateSqlGenerator: AppendValues for table {name}");
        foreach (var op in operations)
        {
            Console.WriteLine($"DEBUG UpdateSqlGenerator: Value operation - {op.ColumnName}: {op.Value} (IsKey: {op.IsKey})");
        }
        base.AppendValues(commandStringBuilder, name, schema, operations);
    }
}