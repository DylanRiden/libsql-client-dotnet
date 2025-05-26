using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

// Type mappings for LibSQL-specific data types
public class StringTypeMapping : RelationalTypeMapping
{
    public StringTypeMapping(string storeType, Type? clrType = null)
        : base(storeType, clrType ?? typeof(string))
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new StringTypeMapping(StoreType, typeof(string));
}