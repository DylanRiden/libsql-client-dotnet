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

            // FIXED: For manual ID assignment, we need to explicitly configure this
            entity.Property(e => e.Id)
                .ValueGeneratedNever() // We're setting IDs manually
                .HasColumnType("INTEGER")
                .IsRequired();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("TEXT");

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("TEXT");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("TEXT");
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
    
    public static async Task TestAutoIncrementIds()
{
    Console.WriteLine("=== AUTO INCREMENT ID TEST ===\n");

    try
    {
        var connectionString = ":memory:";
        using var context = new AutoIncrementTestContext(connectionString);

        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("✓ Database created\n");

        // Create users without setting ID - let SQLite generate them
        var users = new[]
        {
            new AutoIncrementTestUser { Name = "Auto User 1", Email = "auto1@test.com" },
            new AutoIncrementTestUser { Name = "Auto User 2", Email = "auto2@test.com" },
            new AutoIncrementTestUser { Name = "Auto User 3", Email = "auto3@test.com" }
        };

        Console.WriteLine("Adding users with auto-generated IDs:");
        foreach (var user in users)
        {
            Console.WriteLine($"  Before: ID={user.Id}, Name={user.Name}");
        }

        context.Users.AddRange(users);
        
        // Check entity states
        foreach (var entry in context.ChangeTracker.Entries<AutoIncrementTestUser>())
        {
            Console.WriteLine($"Entity {entry.Entity.Name}: State={entry.State}, ID={entry.Entity.Id}");
        }

        Console.WriteLine("\nSaving changes...");
        var result = await context.SaveChangesAsync();
        Console.WriteLine($"✓ Saved {result} changes");

        Console.WriteLine("\nAfter save:");
        foreach (var user in users)
        {
            Console.WriteLine($"  After: ID={user.Id}, Name={user.Name}");
        }

        // Query back the data
        var savedUsers = await context.Users.ToListAsync();
        Console.WriteLine($"\nRetrieved {savedUsers.Count} users from database:");
        foreach (var user in savedUsers)
        {
            Console.WriteLine($"  ID={user.Id}, Name={user.Name}, Email={user.Email}");
        }

        Console.WriteLine("\n🎉 Auto-increment test passed!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Auto-increment test failed: {ex.Message}");
        Console.WriteLine($"Exception: {ex.GetType().Name}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}
    
    // Let's test if we can execute raw SQL to isolate the issue

    public static async Task TestRawSql()
    {
        Console.WriteLine("=== RAW SQL TEST ===\n");

        try
        {
            var connectionString = ":memory:";
            using var context = new AutoIdContext(connectionString);

            await context.Database.EnsureCreatedAsync();
            Console.WriteLine("✓ Database created\n");

            // Try executing raw SQL instead of using EF Core's update pipeline
            Console.WriteLine("Executing raw INSERT SQL...");
        
            var result = await context.Database.ExecuteSqlRawAsync(
                "INSERT INTO AutoIdUser (Name, Email) VALUES ('Raw Test', 'raw@test.com')");
        
            Console.WriteLine($"✓ Raw SQL executed, affected rows: {result}");

            // Try to query it back
            Console.WriteLine("Querying data back...");
            var users = await context.Users.ToListAsync();
        
            Console.WriteLine($"Found {users.Count} users:");
            foreach (var user in users)
            {
                Console.WriteLine($"  ID: {user.Id}, Name: {user.Name}, Email: {user.Email}");
            }

            Console.WriteLine("\n🎉 Raw SQL test passed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Raw SQL test failed: {ex.Message}");
            Console.WriteLine($"Exception type: {ex.GetType().Name}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
    
    public static async Task TestAutoGeneratedIds()
{
    Console.WriteLine("=== AUTO-GENERATED ID TEST ===\n");

    try
    {
        var connectionString = ":memory:";
        using var context = new AutoIdContext(connectionString);

        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("✓ Database created\n");

        // Create user without setting ID - let EF/LibSQL auto-generate it
        var user = new AutoIdUser 
        { 
            Name = "Auto User", 
            Email = "auto@test.com"
        };

        Console.WriteLine($"Before adding to context:");
        Console.WriteLine($"  Id: {user.Id} (should be 0)");
        Console.WriteLine($"  Name: {user.Name}");

        context.Users.Add(user);

        // Check entity state
        var entry = context.Entry(user);
        Console.WriteLine($"\nEntity state: {entry.State}");
        Console.WriteLine($"Id property metadata - ValueGenerated: {entry.Property(e => e.Id).Metadata.ValueGenerated}");

        Console.WriteLine("\nAttempting to save...");
        var result = await context.SaveChangesAsync();
        Console.WriteLine($"✓ Saved {result} changes");

        Console.WriteLine($"\nAfter save:");
        Console.WriteLine($"  Id: {user.Id} (should be auto-generated)");
        Console.WriteLine($"  Name: {user.Name}");

        // Query back to verify
        var savedUser = await context.Users.FirstOrDefaultAsync();
        if (savedUser != null)
        {
            Console.WriteLine($"\nRetrieved from DB:");
            Console.WriteLine($"  Id: {savedUser.Id}");
            Console.WriteLine($"  Name: {savedUser.Name}");
            Console.WriteLine($"  Email: {savedUser.Email}");
        }

        Console.WriteLine("\n🎉 Auto-generated ID test passed!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Auto-generated ID test failed: {ex.Message}");
        Console.WriteLine($"Exception type: {ex.GetType().Name}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}

    // Add this debug method to your BasicLibSqlEFTest class to understand what's happening

public static async Task DebugManualIds()
{
    Console.WriteLine("=== DEBUG: Manual ID Issue ===\n");

    try
    {
        var connectionString = ":memory:";
        using var context = new BasicTestContext(connectionString);

        await context.Database.EnsureCreatedAsync();
        Console.WriteLine("✓ Database created\n");

        // Create a single user with explicit debugging
        var user = new TestUser 
        { 
            Id = 1, 
            Name = "Test User", 
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };

        Console.WriteLine($"Before adding to context:");
        Console.WriteLine($"  Id: {user.Id}");
        Console.WriteLine($"  Name: {user.Name}");
        Console.WriteLine($"  Email: {user.Email}");

        context.Users.Add(user);

        // Check the entity state before saving
        var entry = context.Entry(user);
        Console.WriteLine($"\nEntity state: {entry.State}");
        Console.WriteLine($"Property states:");
        foreach (var prop in entry.Properties)
        {
            Console.WriteLine($"  {prop.Metadata.Name}: {prop.CurrentValue} (IsModified: {prop.IsModified})");
        }

        // Check if EF Core sees the key value
        var keyValues = entry.Metadata.FindPrimaryKey()?.Properties
            .Select(p => entry.Property(p.Name).CurrentValue)
            .ToArray();
        
        Console.WriteLine($"\nPrimary key values seen by EF Core: [{string.Join(", ", keyValues?.Select(v => v?.ToString() ?? "null") ?? new[] { "null" })}]");

        // Try to save
        Console.WriteLine($"\nAttempting to save...");
        var result = await context.SaveChangesAsync();
        Console.WriteLine($"✓ Saved {result} changes");

    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Debug failed: {ex.Message}");
        Console.WriteLine($"Exception type: {ex.GetType().Name}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
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

// Let's try a completely different approach - use auto-increment IDs first to get basic functionality working

public class AutoIncrementTestUser
{
    public int Id { get; set; }  // Will be auto-generated by SQLite
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class AutoIncrementTestContext : DbContext
{
    private readonly string _connectionString;

    public AutoIncrementTestContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<AutoIncrementTestUser> Users => Set<AutoIncrementTestUser>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLibSql(_connectionString);
        optionsBuilder.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Debug);
        optionsBuilder.EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AutoIncrementTestUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // Let SQLite handle ID generation automatically
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()  // This is the default, but let's be explicit
                .HasColumnType("INTEGER");
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasColumnType("TEXT");
                
            entity.Property(e => e.Email)
                .IsRequired()
                .HasColumnType("TEXT");
        });
    }
}

// Add this to your BasicLibSqlEFTest class

public class AutoIdUser
{
    public int Id { get; set; }  // Will be auto-generated
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class AutoIdContext : DbContext
{
    private readonly string _connectionString;

    public AutoIdContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<AutoIdUser> Users => Set<AutoIdUser>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLibSql(_connectionString);
        optionsBuilder.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Debug);
        optionsBuilder.EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AutoIdUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            // Let EF Core handle auto-generation (default behavior)
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Email).IsRequired();
        });
    }
}

