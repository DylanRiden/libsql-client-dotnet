using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EntityFrameworkCore.LibSql.Extensions;

namespace Demo.EFCoreLibSql;

public class SingleFileDbTest
{
    public static async Task RunTest()
    {
        // Define the database file path - use plain file path
        var dbPath = "test_database.db";
        
        Console.WriteLine("=== Single File Database Test ===");
        Console.WriteLine($"Database file: {dbPath}");
        
        // Clean up any existing database file
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
            Console.WriteLine("Cleaned up existing database file");
        }
        
        try
        {
            // Create and configure the context
            using var context = new FileDbContext(dbPath);
            var connection = context.Database.GetDbConnection();
            
            Console.WriteLine("Opening database connection...");
            await connection.OpenAsync();
            
            Console.WriteLine("Creating table...");
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS TestEntities (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL)";
                await cmd.ExecuteNonQueryAsync();
            }
            
            Console.WriteLine("Inserting test data...");
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO TestEntities (Name) VALUES ('Entity 1'), ('Entity 2'), ('Entity 3')";
                var insertedRows = await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"Inserted {insertedRows} rows");
            }
            
            Console.WriteLine("Querying data...");
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Name FROM TestEntities ORDER BY Id";
                using var reader = await cmd.ExecuteReaderAsync();
                
                Console.WriteLine("Retrieved entities:");
                while (await reader.ReadAsync())
                {
                    var id = reader.GetInt32(0);
                    var name = reader.GetString(1);
                    Console.WriteLine($"  ID: {id}, Name: {name}");
                }
            }
            
            Console.WriteLine("Getting row count...");
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM TestEntities";
                var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                Console.WriteLine($"Total entities in database: {count}");
            }
            
            await connection.CloseAsync();
            
            // Verify the file exists and has size
            var fileInfo = new FileInfo(dbPath);
            if (fileInfo.Exists)
            {
                Console.WriteLine($"Database file size: {fileInfo.Length} bytes");
                Console.WriteLine(" SUCCESS: Database file created and data saved!");
            }
            else
            {
                Console.WriteLine(" FAILED: Database file not found");
            }
            
            Console.WriteLine("=== Test completed successfully! ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }
}

// DbContext for file-based database
public class FileDbContext : DbContext
{
    private readonly string _dbPath;
    
    public FileDbContext(string dbPath)
    {
        _dbPath = dbPath;
    }
    
    public DbSet<SimpleEntity> Entities => Set<SimpleEntity>();
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLibSql(_dbPath);
    }
}
