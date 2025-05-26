using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class FloatTypeMapping : RelationalTypeMapping
{
    public FloatTypeMapping(string storeType) 
        : base(storeType, typeof(float)) 
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new FloatTypeMapping(StoreType);
}