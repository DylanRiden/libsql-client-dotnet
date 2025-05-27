using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EntityFrameworkCore.LibSql.Extensions;

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
        optionsBuilder
            .UseLibSql("")
            .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug)))
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
    }
}

class Program
{
    static async Task Main()
    {
        try
        {
            using var db = new SimpleContext();
            
            Console.WriteLine("=== STEP-BY-STEP DATABASE CREATION ANALYSIS ===");
            
            // Step 1: Check if database "exists"
            Console.WriteLine("1. Checking if database exists...");
            bool canConnect = await db.Database.CanConnectAsync();
            Console.WriteLine($"   CanConnect: {canConnect}");
            
            // Step 2: Check if tables exist BEFORE EnsureCreated
            Console.WriteLine("2. Checking if tables exist before EnsureCreated...");
            var connection = db.Database.GetDbConnection();
            await connection.OpenAsync();
            bool hasTablesBefore = await CheckTablesExist(connection);
            Console.WriteLine($"   HasTables (before): {hasTablesBefore}");
            await connection.CloseAsync();
            
            // Step 3: Call EnsureCreated
            Console.WriteLine("3. Calling EnsureCreated...");
            bool created = await db.Database.EnsureCreatedAsync();
            Console.WriteLine($"   EnsureCreated returned: {created}");
            
            // Step 4: Check if tables exist AFTER EnsureCreated
            Console.WriteLine("4. Checking if tables exist after EnsureCreated...");
            await connection.OpenAsync();
            bool hasTablesAfter = await CheckTablesExist(connection);
            Console.WriteLine($"   HasTables (after): {hasTablesAfter}");
            
            // Step 5: Try to manually create the table using EF's SQL generator
            Console.WriteLine("5. Trying to generate and execute CREATE TABLE manually...");
            
            // Get the model and generate the creation script
            var model = db.Model;
            var entityType = model.FindEntityType(typeof(SimpleEntity));
            
            if (entityType != null)
            {
                Console.WriteLine($"   Entity found: {entityType.Name}");
                Console.WriteLine($"   Table: {entityType.GetTableName()}");
                
                // Try to create the table manually
                try
                {
                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS SimpleEntities (
                            Id INTEGER NOT NULL PRIMARY KEY,
                            Name TEXT NOT NULL
                        )";
                    
                    Console.WriteLine($"   Executing: {command.CommandText}");
                    var result = await command.ExecuteNonQueryAsync();
                    Console.WriteLine($"   Manual CREATE result: {result}");
                    
                    // Check again
                    bool hasTablesManual = await CheckTablesExist(connection);
                    Console.WriteLine($"   HasTables (after manual): {hasTablesManual}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   Manual CREATE failed: {ex.Message}");
                }
            }
            
            await connection.CloseAsync();
            
            Console.WriteLine("\n=== TESTING DIFFERENT CONNECTION INSTANCES ===");
            
            // Test if the issue is multiple database instances
            Console.WriteLine("6. Testing if each connection gets a separate in-memory database...");
            
            var client1 = Libsql.Client.DatabaseClient.Create(opts => opts.Url = ":memory:");
            var client2 = Libsql.Client.DatabaseClient.Create(opts => opts.Url = ":memory:");
            
            try
            {
                // Create table in client1
                await client1.Execute("CREATE TABLE Test1 (id INTEGER)");
                await client1.Execute("INSERT INTO Test1 VALUES (1)");
                
                // Try to read from client2
                var result1 = await client1.Execute("SELECT COUNT(*) FROM Test1");
                Console.WriteLine($"   Client1 can see its own table: {result1.Rows.FirstOrDefault()?.FirstOrDefault()}");
                
                try
                {
                    var result2 = await client2.Execute("SELECT COUNT(*) FROM Test1");
                    Console.WriteLine($"   Client2 can see client1's table: {result2.Rows.FirstOrDefault()?.FirstOrDefault()}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   Client2 CANNOT see client1's table: {ex.Message}");
                    Console.WriteLine("   >>> ISSUE FOUND: Each :memory: connection gets a separate database!");
                }
            }
            finally
            {
                //client1.Dispose();
                //client2.Dispose();
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
    
    static async Task<bool> CheckTablesExist(System.Data.Common.DbConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }
}