using System;
using System.IO;
using System.Threading.Tasks;
using Libsql.Client;

namespace Demo.EFCoreLibSql;

public class SimpleFileTest
{
    public static async Task RunTest()
    {
        var dbPath = "../test-direct.db";
        
        Console.WriteLine("=== Simple File Database Test (Direct LibSQL Client) ===");
        Console.WriteLine($"Database file: {dbPath}");
        
        // Clean up any existing database file
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
            Console.WriteLine("Cleaned up existing database file");
        }
        
        try
        {
            Console.WriteLine("Creating LibSQL client...");
            var client = await DatabaseClient.Create(dbPath);
            
            Console.WriteLine("Creating table...");
            await client.Execute("CREATE TABLE IF NOT EXISTS users (id INTEGER PRIMARY KEY, name TEXT)");
            
            Console.WriteLine("Inserting data...");
            await client.Execute("INSERT INTO users (name) VALUES ('Alice')");
            await client.Execute("INSERT INTO users (name) VALUES ('Bob')");
            await client.Execute("INSERT INTO users (name) VALUES ('Charlie')");
            
            Console.WriteLine("Querying data...");
            var result = await client.Execute("SELECT id, name FROM users ORDER BY id");
            
            Console.WriteLine("Results:");
            foreach (var row in result.Rows)
            {
                var values = row.ToList();
                var id = values[0];
                var name = values[1];
                Console.WriteLine($"  ID: {id}, Name: {name}");
            }
            
            Console.WriteLine("Getting count...");
            var countResult = await client.Execute("SELECT COUNT(*) as count FROM users");
            var countRow = countResult.Rows.First().ToList();
            var count = countRow[0];
            Console.WriteLine($"Total users: {count}");
            
            Console.WriteLine("Closing client...");
            client.Dispose();
            
            // Verify the file exists and has size
            var fileInfo = new FileInfo(dbPath);
            if (fileInfo.Exists)
            {
                Console.WriteLine($"Database file size: {fileInfo.Length} bytes");
                Console.WriteLine("✅ SUCCESS: Database file created and data saved!");
            }
            else
            {
                Console.WriteLine("❌ FAILED: Database file not found");
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
