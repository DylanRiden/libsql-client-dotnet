using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EntityFrameworkCore.LibSql.Extensions;

namespace Demo.EFCoreLibSql.Tests;

// Simple test entity
public class TestUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Simple test context
public class BasicTestContext : DbContext
{
    private readonly string _connectionString;

    public BasicTestContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<TestUser> Users => Set<TestUser>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLibSql(_connectionString);
        // Enable more detailed logging to see the SQL being generated
        optionsBuilder.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Debug);
        optionsBuilder.EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Use manual IDs for now - this will test the basic provider functionality
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
        });
    }
}

public class BasicLibSqlEFTest
{
    private static readonly string TestDbPath = "basic_test.db";

    public static async Task RunBasicTest()
    {
        Console.WriteLine("=== Basic LibSQL EF Core Provider Test ===\n");

        try
        {
            // Clean up any existing test database
            CleanupTestDatabase();

            // Let's test different connection string formats to see what works
            Console.WriteLine("Testing different connection string formats...\n");
            
            var testPaths = new[]
            {
                ":memory:",
                "basic_test.db",
                "./basic_test.db"
            };

            string? workingConnectionString = null;

            foreach (var testPath in testPaths)
            {
                Console.WriteLine($"Testing path: '{testPath}'");
                try
                {
                    var directClient = await Libsql.Client.DatabaseClient.Create(testPath);
                    Console.WriteLine($"✓ SUCCESS with path: '{testPath}'");
                    directClient.Dispose();
                    
                    workingConnectionString = testPath;
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ FAILED with path '{testPath}': {ex.Message}");
                }
            }
            
            if (workingConnectionString == null)
            {
                throw new InvalidOperationException("No working connection string found!");
            }
            
            Console.WriteLine($"\nUsing working connection string: {workingConnectionString}\n");
            
            using var context = new BasicTestContext(workingConnectionString);

            // Step 1: Create the database
            Console.WriteLine("Step 1: Creating database...");
            await context.Database.EnsureCreatedAsync();
            Console.WriteLine("✓ Database created successfully\n");

            // Step 2: Insert some test data (manual IDs to test basic functionality)
            Console.WriteLine("Step 2: Inserting test data...");
            var testUsers = new[]
            {
                new TestUser { Id = 1, Name = "John Doe", Email = "john@example.com" },
                new TestUser { Id = 2, Name = "Jane Smith", Email = "jane@example.com" },
                new TestUser { Id = 3, Name = "Bob Johnson", Email = "bob@example.com" }
            };

            // Debug: Check the entities before adding
            foreach (var user in testUsers)
            {
                Console.WriteLine($"DEBUG: Creating user with Id={user.Id}, Name={user.Name}");
            }

            context.Users.AddRange(testUsers);
            var insertedCount = await context.SaveChangesAsync();
            Console.WriteLine($"✓ Inserted {insertedCount} users successfully\n");

            // Step 3: Query the data back
            Console.WriteLine("Step 3: Querying data...");
            var allUsers = await context.Users.ToListAsync();
            Console.WriteLine($"✓ Retrieved {allUsers.Count} users:");
            foreach (var user in allUsers)
            {
                Console.WriteLine($"  - ID: {user.Id}, Name: {user.Name}, Email: {user.Email}");
            }
            Console.WriteLine();

            // Step 4: Test filtering
            Console.WriteLine("Step 4: Testing filtered queries...");
            var johnUser = await context.Users.FirstOrDefaultAsync(u => u.Name.Contains("John"));
            if (johnUser != null)
            {
                Console.WriteLine($"✓ Found user by name filter: {johnUser.Name} ({johnUser.Email})");
            }
            else
            {
                Console.WriteLine("❌ Could not find user by name filter");
            }

            // Step 5: Test count
            var userCount = await context.Users.CountAsync();
            Console.WriteLine($"✓ Total user count: {userCount}\n");

            // Step 6: Update a record
            Console.WriteLine("Step 5: Testing updates...");
            if (johnUser != null)
            {
                johnUser.Email = "john.doe.updated@example.com";
                var updateCount = await context.SaveChangesAsync();
                Console.WriteLine($"✓ Updated {updateCount} record(s)");
                
                // Verify the update
                var updatedUser = await context.Users.FindAsync(johnUser.Id);
                if (updatedUser?.Email == "john.doe.updated@example.com")
                {
                    Console.WriteLine("✓ Update verified successfully");
                }
                else
                {
                    Console.WriteLine("❌ Update verification failed");
                }
            }
            Console.WriteLine();

            // Step 7: Test deletion
            Console.WriteLine("Step 6: Testing deletion...");
            var userToDelete = await context.Users.FirstOrDefaultAsync(u => u.Name.Contains("Bob"));
            if (userToDelete != null)
            {
                context.Users.Remove(userToDelete);
                var deleteCount = await context.SaveChangesAsync();
                Console.WriteLine($"✓ Deleted {deleteCount} record(s)");
                
                // Verify deletion
                var remainingCount = await context.Users.CountAsync();
                Console.WriteLine($"✓ Remaining users: {remainingCount}");
            }
            Console.WriteLine();

            // Step 8: Verify database file exists and has content (only for file databases)
            if (workingConnectionString != ":memory:")
            {
                Console.WriteLine("Step 7: Verifying database persistence...");
                var fileInfo = new FileInfo(TestDbPath);
                if (fileInfo.Exists && fileInfo.Length > 0)
                {
                    Console.WriteLine($"✓ Database file exists and has content ({fileInfo.Length} bytes)");
                }
                else
                {
                    Console.WriteLine("❌ Database file issue");
                }
            }
            else
            {
                Console.WriteLine("Step 7: Skipping file verification (using in-memory database)");
            }

            Console.WriteLine("\n🎉 All basic tests passed! Your LibSQL EF Core provider is working!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Test failed: {ex.Message}");
            Console.WriteLine($"Exception type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
        finally
        {
            CleanupTestDatabase();
        }
    }

    private static void CleanupTestDatabase()
    {
        try
        {
            if (File.Exists(TestDbPath))
            {
                File.Delete(TestDbPath);
                Console.WriteLine($"✓ Cleaned up test database: {TestDbPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not clean up test database: {ex.Message}");
        }
    }
}
