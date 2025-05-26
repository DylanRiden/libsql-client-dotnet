using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class ByteArrayTypeMapping : RelationalTypeMapping
{
    public ByteArrayTypeMapping(string storeType)
        : base(storeType, typeof(byte[]))
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new ByteArrayTypeMapping(StoreType);
}