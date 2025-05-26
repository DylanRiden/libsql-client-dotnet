using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class UShortTypeMapping : RelationalTypeMapping
{
    public UShortTypeMapping(string storeType) 
        : base(storeType, typeof(ushort)) 
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new UShortTypeMapping(StoreType);
}