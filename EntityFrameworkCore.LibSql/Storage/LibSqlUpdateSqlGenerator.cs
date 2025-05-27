using System.Text;
using Microsoft.EntityFrameworkCore.Update;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlUpdateSqlGenerator : UpdateSqlGenerator
{
    public LibSqlUpdateSqlGenerator(UpdateSqlGeneratorDependencies dependencies)
        : base(dependencies)
    {
    }
}