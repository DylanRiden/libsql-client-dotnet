using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class ShortTypeMapping : RelationalTypeMapping
{
    public ShortTypeMapping(string storeType) 
        : base(storeType, typeof(short)) 
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new ShortTypeMapping(StoreType);
}