using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class ULongTypeMapping : RelationalTypeMapping
{
    public ULongTypeMapping(string storeType) 
        : base(storeType, typeof(ulong)) 
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new ULongTypeMapping(StoreType);
}