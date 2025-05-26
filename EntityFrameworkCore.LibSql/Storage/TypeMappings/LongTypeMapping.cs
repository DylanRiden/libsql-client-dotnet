using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class LongTypeMapping : RelationalTypeMapping
{
    public LongTypeMapping(string storeType)
        : base(storeType, typeof(long))
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new LongTypeMapping(StoreType);
}