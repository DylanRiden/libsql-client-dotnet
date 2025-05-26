using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class ByteTypeMapping : RelationalTypeMapping
{
    public ByteTypeMapping(string storeType) 
        : base(storeType, typeof(byte)) 
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new ByteTypeMapping(StoreType);
}