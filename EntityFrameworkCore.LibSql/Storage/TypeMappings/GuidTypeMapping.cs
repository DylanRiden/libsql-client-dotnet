using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class GuidTypeMapping : RelationalTypeMapping
{
    public GuidTypeMapping(string storeType)
        : base(storeType, typeof(Guid))
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new GuidTypeMapping(StoreType);
}