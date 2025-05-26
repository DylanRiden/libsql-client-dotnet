using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage.TypeMappings;

public class CharTypeMapping : RelationalTypeMapping
{
    public CharTypeMapping(string storeType) 
        : base(storeType, typeof(char)) 
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters)
        => new CharTypeMapping(StoreType);
}