using EntityFrameworkCore.LibSql.Storage;

namespace Demo.EFCoreLibSql;

public class DirectConnectionTest
{
    public static async Task RunTest()
    {
        Console.WriteLine("=== TESTING DIRECT LIBSQL CONNECTION SHARING ===");
        
        // Use a simple test file in the current directory
        var currentDir = Directory.GetCurrentDirectory();
        Console.WriteLine($"Current directory: {currentDir}");
        
        var tempFile = Path.Combine(currentDir, "test.db");
        Console.WriteLine($"Database file path: {tempFile}");
        
        // Ensure the file doesn't exist before the test
        if (File.Exists(tempFile)) 
        {
            Console.WriteLine("Deleting existing database file...");
            File.Delete(tempFile);
        }
        
        var connectionString = $"file:{tempFile}";
        Console.WriteLine($"Using connection string: {connectionString}");
        
        try
        {
            Console.WriteLine($"Using temporary database file: {tempFile}");
            
            // First connection - create table and insert data
            Console.WriteLine("Step 1: Creating first connection...");
            var connection1 = new LibSqlDbConnection(connectionString);
            
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
            
            // Second connection - verify data
            Console.WriteLine("Step 6: Creating second connection...");
            var connection2 = new LibSqlDbConnection(connectionString);
            
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
                    Console.WriteLine("✅ SUCCESS: LibSQL connections are sharing the same database!");
                }
                else
                {
                    Console.WriteLine("❌ FAILED: No data found in the database");
                }
            }
            
            await connection2.CloseAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed: {ex.Message}");
            Console.WriteLine($"Exception type: {ex.GetType().FullName}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                Console.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
            }
            
            throw;
        }
        finally
        {
            // Clean up the temporary file
            try { if (File.Exists(tempFile)) File.Delete(tempFile); } 
            catch (Exception ex) { Console.WriteLine($"Warning: Failed to delete temp file: {ex.Message}"); }
        }
        
        Console.WriteLine("Test completed.");
    }
}
