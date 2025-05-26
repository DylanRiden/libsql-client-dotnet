using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class TimeSpanTypeMapping : RelationalTypeMapping
{
    public TimeSpanTypeMapping(string storeType) 
        : base(storeType, typeof(TimeSpan)) 
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new TimeSpanTypeMapping(StoreType);
}