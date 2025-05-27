using EntityFrameworkCore.LibSql.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Demo.EFCoreLibSql;

// Test entity for HTTP LibSQL
public class HttpTestUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class HttpTestContext : DbContext
{
    private readonly string _connectionString;

    public HttpTestContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<HttpTestUser> Users => Set<HttpTestUser>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLibSql(_connectionString);
        // Enable detailed logging to see what's happening
        optionsBuilder.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Debug);
        optionsBuilder.EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HttpTestUser>(entity =>
        {
            entity.HasKey(e => e.Id);

            // For auto-increment, let EF Core handle it
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

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

public class HttpLibSqlEFTest
{
    public static async Task RunHttpTest()
    {
        Console.WriteLine("=== HTTP LibSQL EF Core Provider Test ===\n");

        // You'll need to replace these with your actual Turso database credentials
        var connectionString = GetTestConnectionString();
        
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("❌ No HTTP connection string configured!");
            Console.WriteLine("Please set one of the following:");
            Console.WriteLine("1. Environment variable: LIBSQL_HTTP_URL");
            Console.WriteLine("2. Environment variable: TURSO_DATABASE_URL");
            Console.WriteLine("3. Modify the GetTestConnectionString() method");
            Console.WriteLine("\nConnection string format:");
            Console.WriteLine("libsql://your-db.turso.io?authToken=your-token");
            Console.WriteLine("OR");
            Console.WriteLine("url=https://your-db.turso.io;authToken=your-token");
            return;
        }

        try
        {
            Console.WriteLine($"Using connection string: {SanitizeConnectionStringForLogging(connectionString)}\n");
            
            using var context = new HttpTestContext(connectionString);

            // Step 1: Create the database schema
            Console.WriteLine("Step 1: Creating database schema...");
            await context.Database.EnsureCreatedAsync();
            Console.WriteLine("✓ Database schema created successfully\n");

            // Step 2: Clear any existing test data
            Console.WriteLine("Step 2: Clearing existing test data...");
            var existingUsers = context.Users.Where(u => u.Name.StartsWith("HttpTest"));
            context.Users.RemoveRange(existingUsers);
            await context.SaveChangesAsync();
            Console.WriteLine("✓ Test data cleared\n");

            // Step 3: Insert test data with auto-generated IDs
            Console.WriteLine("Step 3: Inserting test data...");
            var testUsers = new[]
            {
                new HttpTestUser { Name = "HttpTest John", Email = "john@httptest.com" },
                new HttpTestUser { Name = "HttpTest Jane", Email = "jane@httptest.com" },
                new HttpTestUser { Name = "HttpTest Bob", Email = "bob@httptest.com" }
            };

            Console.WriteLine("Creating users with auto-generated IDs:");
            foreach (var user in testUsers)
            {
                Console.WriteLine($"  - {user.Name} ({user.Email}) - ID will be auto-generated");
            }

            context.Users.AddRange(testUsers);
            var insertedCount = await context.SaveChangesAsync();
            Console.WriteLine($"✓ Inserted {insertedCount} users successfully\n");

            // Check the generated IDs
            Console.WriteLine("Generated IDs:");
            foreach (var user in testUsers)
            {
                Console.WriteLine($"  - {user.Name}: ID = {user.Id}");
            }
            Console.WriteLine();

            // Step 4: Query the data back
            Console.WriteLine("Step 4: Querying data...");
            var allHttpTestUsers = await context.Users
                .Where(u => u.Name.StartsWith("HttpTest"))
                .ToListAsync();
            
            Console.WriteLine($"✓ Retrieved {allHttpTestUsers.Count} HTTP test users:");
            foreach (var user in allHttpTestUsers)
            {
                Console.WriteLine($"  - ID: {user.Id}, Name: {user.Name}, Email: {user.Email}, Created: {user.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            }
            Console.WriteLine();

            // Step 5: Test filtering and LINQ queries
            Console.WriteLine("Step 5: Testing filtered queries...");
            var johnUser = await context.Users
                .FirstOrDefaultAsync(u => u.Name.Contains("John") && u.Name.StartsWith("HttpTest"));
            
            if (johnUser != null)
            {
                Console.WriteLine($"✓ Found user by name filter: {johnUser.Name} (ID: {johnUser.Id}, Email: {johnUser.Email})");
            }
            else
            {
                Console.WriteLine("❌ Could not find user by name filter");
            }

            // Step 6: Test count operations
            var httpTestUserCount = await context.Users
                .CountAsync(u => u.Name.StartsWith("HttpTest"));
            Console.WriteLine($"✓ HTTP Test user count: {httpTestUserCount}\n");

            // Step 7: Test updates
            Console.WriteLine("Step 6: Testing updates...");
            if (johnUser != null)
            {
                var originalEmail = johnUser.Email;
                johnUser.Email = "john.updated@httptest.com";
                johnUser.CreatedAt = DateTime.UtcNow; // Update timestamp too
                
                var updateCount = await context.SaveChangesAsync();
                Console.WriteLine($"✓ Updated {updateCount} record(s)");
                
                // Verify the update by re-querying
                var updatedUser = await context.Users.FindAsync(johnUser.Id);
                if (updatedUser?.Email == "john.updated@httptest.com")
                {
                    Console.WriteLine($"✓ Update verified: {originalEmail} → {updatedUser.Email}");
                }
                else
                {
                    Console.WriteLine("❌ Update verification failed");
                }
            }
            Console.WriteLine();

            // Step 8: Test deletion
            Console.WriteLine("Step 7: Testing deletion...");
            var userToDelete = await context.Users
                .FirstOrDefaultAsync(u => u.Name.Contains("Bob") && u.Name.StartsWith("HttpTest"));
            
            if (userToDelete != null)
            {
                Console.WriteLine($"Deleting user: {userToDelete.Name} (ID: {userToDelete.Id})");
                context.Users.Remove(userToDelete);
                var deleteCount = await context.SaveChangesAsync();
                Console.WriteLine($"✓ Deleted {deleteCount} record(s)");
                
                // Verify deletion
                var remainingCount = await context.Users
                    .CountAsync(u => u.Name.StartsWith("HttpTest"));
                Console.WriteLine($"✓ Remaining HTTP test users: {remainingCount}");
            }
            Console.WriteLine();

            // Step 9: Test complex queries
            Console.WriteLine("Step 8: Testing complex queries...");
            var recentUsers = await context.Users
                .Where(u => u.Name.StartsWith("HttpTest") && u.CreatedAt > DateTime.UtcNow.AddMinutes(-5))
                .OrderBy(u => u.Name)
                .Select(u => new { u.Id, u.Name, u.Email })
                .ToListAsync();

            Console.WriteLine($"✓ Found {recentUsers.Count} recently created users:");
            foreach (var user in recentUsers)
            {
                Console.WriteLine($"  - {user.Name} (ID: {user.Id})");
            }

            Console.WriteLine("\n🎉 All HTTP LibSQL tests passed! Your HTTP provider is working correctly!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ HTTP Test failed: {ex.Message}");
            Console.WriteLine($"Exception type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public static async Task TestDirectHttpClient()
    {
        Console.WriteLine("=== Direct HTTP Client Test ===\n");

        var connectionString = GetTestConnectionString();
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("❌ No HTTP connection string configured!");
            return;
        }

        try
        {
            // Parse connection string to get URL and auth token
            var (url, authToken) = ParseConnectionString(connectionString);
            
            using var httpClient = new EntityFrameworkCore.LibSql.Http.HttpLibSqlClient(url, authToken);
            
            Console.WriteLine("Testing direct HTTP client...");
            
            // Test basic query
            var result = await httpClient.ExecuteAsync("SELECT 1 as test_value, 'Hello HTTP LibSQL' as message");
            
            Console.WriteLine($"✓ Query executed successfully");
            Console.WriteLine($"Columns: {string.Join(", ", result.Columns)}");
            Console.WriteLine($"Rows returned: {result.Rows.Count()}");
            
            foreach (var row in result.Rows)
            {
                Console.WriteLine($"Row: {string.Join(", ", row)}");
            }

            Console.WriteLine("\n🎉 Direct HTTP client test passed!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Direct HTTP client test failed: {ex.Message}");
            throw;
        }
    }

    private static string? GetTestConnectionString()
    {
        // Try multiple environment variables
        return "libsql://test-dylanriden.aws-eu-west-1.turso.io?authToken=eyJhbGciOiJFZERTQSIsInR5cCI6IkpXVCJ9.eyJhIjoicnciLCJleHAiOjE3NDg0Njc2MTMsImlhdCI6MTc0ODM4MTIxMywiaWQiOiI4YzJjNGUxNi03NzUyLTRmODktOTljZC1hZjE0ZGU4YWE4ZjciLCJyaWQiOiJkOTQxYWI4My1iOGMwLTRlNTMtOWM4ZS04MDYzOTE1Yzk4ZDcifQ.xwg2XqpQYdHnAXRUrHSI0tiVVKveHq6h_pb3X_lRwJrLKPLpXTZhJlur2s38vs4qZMJA2iclNN3MKjZWQUEWCA";
    }

    private static string SanitizeConnectionStringForLogging(string connectionString)
    {
        // Hide the auth token for security when logging
        if (connectionString.Contains("authToken="))
        {
            var parts = connectionString.Split('?');
            if (parts.Length > 1)
            {
                return $"{parts[0]}?authToken=***";
            }
        }
        
        return connectionString.Contains("authToken") ? "[CONNECTION STRING WITH AUTH TOKEN]" : connectionString;
    }

    private static (string url, string authToken) ParseConnectionString(string connectionString)
    {
        if (connectionString.StartsWith("libsql://"))
        {
            var uri = new Uri(connectionString);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var authToken = query["authToken"];
            
            if (string.IsNullOrEmpty(authToken))
            {
                throw new ArgumentException("AuthToken is required in connection string");
            }
            
            var baseUrl = $"{uri.Scheme}://{uri.Host}";
            if (uri.Port != -1 && uri.Port != 80 && uri.Port != 443)
            {
                baseUrl += $":{uri.Port}";
            }
            
            return (baseUrl, authToken);
        }
        else
        {
            // Parse key-value pairs
            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            string? url = null;
            string? authToken = null;
            
            foreach (var part in parts)
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToLowerInvariant();
                    var value = keyValue[1].Trim();
                    
                    switch (key)
                    {
                        case "url":
                            url = value;
                            break;
                        case "authtoken":
                            authToken = value;
                            break;
                    }
                }
            }
            
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(authToken))
            {
                throw new ArgumentException("Both URL and AuthToken are required in connection string");
            }
            
            return (url, authToken);
        }
    }
}