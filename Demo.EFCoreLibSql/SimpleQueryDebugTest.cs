using Microsoft.EntityFrameworkCore;

namespace Demo.EFCoreLibSql;

public class SimpleQueryDebugTest
{
    public static async Task RunSimpleQueryTest()
    {
        Console.WriteLine("=== Simple Query Debug Test ===\n");

        var connectionString = Environment.GetEnvironmentVariable("LIBSQL_HTTP_URL") ??
                              Environment.GetEnvironmentVariable("TURSO_DATABASE_URL") ??
                              Environment.GetEnvironmentVariable("LIBSQL_URL");

        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("❌ No connection string available for debug test");
            return;
        }

        try
        {
            using var context = new HttpTestContext(connectionString);

            Console.WriteLine("Step 1: Ensure database is created...");
            await context.Database.EnsureCreatedAsync();
            Console.WriteLine("✓ Database ready\n");

            Console.WriteLine("Step 2: Try simple count query...");
            try
            {
                var count = await context.Users.CountAsync();
                Console.WriteLine($"✓ Count query successful: {count} users");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Count query failed: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("\nStep 3: Try simple select all query...");
            try
            {
                var allUsers = await context.Users.ToListAsync();
                Console.WriteLine($"✓ Select all query successful: {allUsers.Count} users");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Select all query failed: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("\nStep 4: Try adding a single user...");
            try
            {
                var testUser = new HttpTestUser 
                { 
                    Name = "Debug Test User", 
                    Email = "debug@test.com",
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(testUser);
                var result = await context.SaveChangesAsync();
                Console.WriteLine($"✓ Add user successful: {result} changes saved, ID = {testUser.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Add user failed: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
                }
            }

            Console.WriteLine("\nStep 5: Try simple WHERE query...");
            try
            {
                var user = await context.Users.FirstOrDefaultAsync(u => u.Name == "Debug Test User");
                if (user != null)
                {
                    Console.WriteLine($"✓ WHERE query successful: Found {user.Name}");
                }
                else
                {
                    Console.WriteLine("✓ WHERE query successful: No matching user found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ WHERE query failed: {ex.Message}");
                Console.WriteLine($"Exception type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
                }
                
                // Print the full stack trace for the problematic query
                Console.WriteLine($"Full stack trace:");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\n🎉 Simple query debug test completed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Debug test failed: {ex.Message}");
            Console.WriteLine($"Exception type: {ex.GetType().Name}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}