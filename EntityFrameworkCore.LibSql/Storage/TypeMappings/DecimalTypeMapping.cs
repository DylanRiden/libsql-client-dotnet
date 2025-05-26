using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class DecimalTypeMapping : RelationalTypeMapping
{
    public DecimalTypeMapping(string storeType) 
        : base(storeType, typeof(decimal)) 
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new DecimalTypeMapping(StoreType);
}