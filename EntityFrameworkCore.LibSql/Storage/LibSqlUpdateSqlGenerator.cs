using System.Text;
using Microsoft.EntityFrameworkCore.Update;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlUpdateSqlGenerator : UpdateSqlGenerator
{
    public LibSqlUpdateSqlGenerator(UpdateSqlGeneratorDependencies dependencies)
        : base(dependencies)
    {
    }

    // Only override methods that actually exist in the base class
    // Most of the base functionality should work fine for SQLite/LibSQL
}