using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class DateTimeTypeMapping : RelationalTypeMapping
{
    public DateTimeTypeMapping(string storeType)
        : base(storeType, typeof(DateTime))
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new DateTimeTypeMapping(StoreType);
}