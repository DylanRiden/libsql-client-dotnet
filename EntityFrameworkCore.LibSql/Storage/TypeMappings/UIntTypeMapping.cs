using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class UIntTypeMapping : RelationalTypeMapping
{
    public UIntTypeMapping(string storeType) 
        : base(storeType, typeof(uint)) 
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new UIntTypeMapping(StoreType);
}