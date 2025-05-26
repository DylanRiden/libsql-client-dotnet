using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCore.LibSql.Infrastructure;

public class LibSqlDbContextOptionsBuilder
{
    public virtual DbContextOptionsBuilder OptionsBuilder { get; }

    public LibSqlDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
    {
        OptionsBuilder = optionsBuilder;
    }

    // Add LibSQL-specific configuration options here
    // For example: connection pooling, timeout settings, etc.
}