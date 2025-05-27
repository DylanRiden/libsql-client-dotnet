using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EntityFrameworkCore.LibSql.Extensions;

namespace Demo.EFCoreLibSql;

// Ultra-simple model
public class SimpleEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Ultra-simple context
public class SimpleContext : DbContext
{
    public DbSet<SimpleEntity> Entities => Set<SimpleEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLibSql("../test-ef.db");
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SimpleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired();
        });
    }
}

class Program
{
    static async Task Main()
    {
        try
        {
            // Test the direct LibSQL client first
            await SimpleFileTest.RunTest();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}