using Microsoft.EntityFrameworkCore;

namespace Demo.EFCoreLibSql;

public class TestMemorySharing
{
    public static async Task RunTest()
    {
        Console.WriteLine("=== TESTING MEMORY DATABASE SHARING ===");
        
        try
        {
            Console.WriteLine("Step 1: Creating first context...");
            using (var context1 = new SimpleContext())
            {
                Console.WriteLine("Step 2: Calling EnsureCreatedAsync...");
                await context1.Database.EnsureCreatedAsync();
                Console.WriteLine("Step 3: Database created successfully");
                
                Console.WriteLine("Step 4: Adding entity...");
                var entity = new SimpleEntity { Name = "Shared Entity" };
                context1.Entities.Add(entity);
                
                Console.WriteLine("Step 5: Calling SaveChangesAsync...");
                await context1.SaveChangesAsync();
                Console.WriteLine("Step 6: Entity saved successfully");
                
                var count1 = context1.Entities.Count();
                Console.WriteLine($"Step 7: Context 1 count: {count1}");
            }
            
            Console.WriteLine("Step 8: Creating second context...");
            using (var context2 = new SimpleContext())
            {
                Console.WriteLine("Step 9: Querying entities in context 2...");
                var count2 = context2.Entities.Count();
                Console.WriteLine($"Step 10: Context 2 count: {count2}");
                
                if (count2 > 0)
                {
                    Console.WriteLine("✅ SUCCESS: Memory database sharing is working correctly!");
                }
                else
                {
                    Console.WriteLine("❌ FAILED: Memory databases are not being shared");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Test failed with error: {ex.Message}");
            Console.WriteLine($"Exception type: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                Console.WriteLine($"Inner exception type: {ex.InnerException.GetType().Name}");
            }
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("Test completed.");
    }
}
