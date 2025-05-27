using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Collections.Generic;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlAnnotationProvider : RelationalAnnotationProvider
{
    public LibSqlAnnotationProvider(RelationalAnnotationProviderDependencies dependencies)
        : base(dependencies)
    {
    }

    public override IEnumerable<IAnnotation> For(IRelationalModel model, bool designTime)
    {
        Console.WriteLine($"DEBUG LibSqlAnnotationProvider.For(IRelationalModel): designTime={designTime}");
        return base.For(model, designTime);
    }

    public override IEnumerable<IAnnotation> For(ITable table, bool designTime)
    {
        Console.WriteLine($"DEBUG LibSqlAnnotationProvider.For(ITable): table={table.Name}, designTime={designTime}");
        return base.For(table, designTime);
    }

    public override IEnumerable<IAnnotation> For(IColumn column, bool designTime)
    {
        Console.WriteLine($"DEBUG LibSqlAnnotationProvider.For(IColumn): column={column.Name}, table={column.Table.Name}, type={column.StoreType}, designTime={designTime}");
        return base.For(column, designTime);
    }

    public override IEnumerable<IAnnotation> For(ITableIndex index, bool designTime)
    {
        Console.WriteLine($"DEBUG LibSqlAnnotationProvider.For(ITableIndex): index={index.Name}, designTime={designTime}");
        return base.For(index, designTime);
    }

    public override IEnumerable<IAnnotation> For(IUniqueConstraint constraint, bool designTime)
    {
        Console.WriteLine($"DEBUG LibSqlAnnotationProvider.For(IUniqueConstraint): constraint={constraint.Name}, designTime={designTime}");
        return base.For(constraint, designTime);
    }

    public override IEnumerable<IAnnotation> For(IForeignKeyConstraint foreignKey, bool designTime)
    {
        Console.WriteLine($"DEBUG LibSqlAnnotationProvider.For(IForeignKeyConstraint): foreignKey={foreignKey.Name}, designTime={designTime}");
        return base.For(foreignKey, designTime);
    }

    public override IEnumerable<IAnnotation> For(ICheckConstraint checkConstraint, bool designTime)
    {
        Console.WriteLine($"DEBUG LibSqlAnnotationProvider.For(ICheckConstraint): checkConstraint={checkConstraint.Name}, designTime={designTime}");
        return base.For(checkConstraint, designTime);
    }
}