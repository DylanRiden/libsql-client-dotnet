using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlCommandBuilderFactory : IRelationalCommandBuilderFactory
{
    private readonly IRelationalTypeMappingSource _typeMappingSource;

    public LibSqlCommandBuilderFactory(IRelationalTypeMappingSource typeMappingSource)
    {
        _typeMappingSource = typeMappingSource;
    }

    public IRelationalCommandBuilder Create()
    {
        return new LibSqlCommandBuilder(_typeMappingSource);
    }
}