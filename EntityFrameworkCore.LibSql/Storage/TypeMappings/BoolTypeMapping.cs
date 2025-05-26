using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class BoolTypeMapping : RelationalTypeMapping
{
    public BoolTypeMapping(string storeType)
        : base(storeType, typeof(bool))
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new BoolTypeMapping(StoreType);
}