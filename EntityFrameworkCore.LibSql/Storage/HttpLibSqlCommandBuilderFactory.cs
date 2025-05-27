using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage;

public class HttpLibSqlCommandBuilderFactory : IRelationalCommandBuilderFactory
{
    private readonly IRelationalTypeMappingSource _typeMappingSource;

    public HttpLibSqlCommandBuilderFactory(IRelationalTypeMappingSource typeMappingSource)
    {
        _typeMappingSource = typeMappingSource;
    }

    public IRelationalCommandBuilder Create()
    {
        return new HttpLibSqlCommandBuilder(_typeMappingSource);
    }
}