using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class DoubleTypeMapping : RelationalTypeMapping
{
    public DoubleTypeMapping(string storeType)
        : base(storeType, typeof(double))
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new DoubleTypeMapping(StoreType);
}