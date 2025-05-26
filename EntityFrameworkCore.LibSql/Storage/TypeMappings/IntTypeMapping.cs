using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class IntTypeMapping : RelationalTypeMapping
{
    public IntTypeMapping(string storeType)
        : base(storeType, typeof(int))
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new IntTypeMapping(StoreType);
}