using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class SByteTypeMapping : RelationalTypeMapping
{
    public SByteTypeMapping(string storeType) 
        : base(storeType, typeof(sbyte)) 
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new SByteTypeMapping(StoreType);
}