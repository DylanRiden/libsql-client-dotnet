using Microsoft.EntityFrameworkCore;

namespace Demo.EFCoreLibSql;

public class SimpleTest
{
    public static async Task RunTest()
    {
        Console.WriteLine("=== TESTING LIBSQL CONNECTION SHARING ===");
        
        try
        {
            // Test the fix directly at the DbConnection level
            Console.WriteLine("Step 1: Creating first connection...");
            using var context1 = new SimpleContext();
            var connection1 = context1.Database.GetDbConnection();
            
            Console.WriteLine("Step 2: Opening first connection...");
            await connection1.OpenAsync();
            
            Console.WriteLine("Step 3: Creating table...");
            using (var cmd = connection1.CreateCommand())
            {
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS TestTable (id INTEGER PRIMARY KEY, name TEXT)";
                await cmd.ExecuteNonQueryAsync();
            }
            
            Console.WriteLine("Step 4: Inserting data...");
            using (var cmd = connection1.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO TestTable (name) VALUES ('Test Data')";
                await cmd.ExecuteNonQueryAsync();
            }
            
            Console.WriteLine("Step 5: Closing first connection...");
            await connection1.CloseAsync();
            
            // Test if second connection can see the data
            Console.WriteLine("Step 6: Creating second connection...");
            using var context2 = new SimpleContext();
            var connection2 = context2.Database.GetDbConnection();
            
            Console.WriteLine("Step 7: Opening second connection...");
            await connection2.OpenAsync();
            
            Console.WriteLine("Step 8: Querying data from second connection...");
            using (var cmd = connection2.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM TestTable";
                var result = await cmd.ExecuteScalarAsync();
                var count = Convert.ToInt32(result);
                
                Console.WriteLine($"Step 9: Found {count} rows");
                
                if (count > 0)
                {
                    Console.WriteLine("✅ SUCCESS: LibSQL connections are sharing the same :memory: database!");
                }
                else
                {
                    Console.WriteLine("❌ FAILED: LibSQL connections are using separate databases");
                }
            }
            
            await connection2.CloseAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("Test completed.");
    }
}
