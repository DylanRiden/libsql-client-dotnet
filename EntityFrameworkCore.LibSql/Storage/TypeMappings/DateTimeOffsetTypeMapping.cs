using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class DateTimeOffsetTypeMapping : RelationalTypeMapping
{
    public DateTimeOffsetTypeMapping(string storeType) 
        : base(storeType, typeof(DateTimeOffset)) 
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new DateTimeOffsetTypeMapping(StoreType);
}