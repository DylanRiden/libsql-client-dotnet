using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlValueGeneratorSelector : RelationalValueGeneratorSelector
{
    public LibSqlValueGeneratorSelector(ValueGeneratorSelectorDependencies dependencies)
        : base(dependencies)
    {
    }

    public override ValueGenerator Select(IProperty property, ITypeBase typeBase)
    {
        // For properties configured as ValueGeneratedNever, return a non-generating value generator
        if (property.ValueGenerated == ValueGenerated.Never)
        {
            return new NullValueGenerator();
        }

        // For integer primary keys that should auto-increment
        if (property.ClrType == typeof(int) && 
            property.ValueGenerated == ValueGenerated.OnAdd &&
            property.IsPrimaryKey())
        {
            return new LibSqlIntegerValueGenerator();
        }

        // Fall back to base implementation for other cases
        return base.Select(property, typeBase);
    }
}

/// <summary>
/// A value generator that doesn't generate values - for manually set keys
/// </summary>
public class NullValueGenerator : ValueGenerator
{
    public override bool GeneratesTemporaryValues => false;

    protected override object? NextValue(EntityEntry entry)
    {
        // Don't generate any value - the value should be set manually
        return null;
    }
}

/// <summary>
/// A value generator for integer primary keys in LibSQL
/// </summary>
public class LibSqlIntegerValueGenerator : ValueGenerator<int>
{
    public override bool GeneratesTemporaryValues => false;

    public override int Next(EntityEntry entry)
    {
        // For LibSQL/SQLite, we rely on AUTOINCREMENT
        // This shouldn't be called for ValueGeneratedNever properties
        throw new InvalidOperationException("LibSQL integer value generator should not be called for manually set IDs");
    }
}