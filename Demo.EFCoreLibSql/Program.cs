using System;
using Demo.EFCoreLibSql.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EntityFrameworkCore.LibSql.Extensions;

namespace Demo.EFCoreLibSql;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("LibSQL HTTP-Only EF Core Provider Test Suite");
        Console.WriteLine("============================================\n");

        // Check for HTTP connection configuration
        var httpConnectionString = GetHttpConnectionString();
        
        try
        {
            Console.WriteLine($"Using HTTP connection: {SanitizeConnectionString(httpConnectionString)}\n");
            
            // Run HTTP-specific tests
            await HttpLibSqlEFTest.TestDirectHttpClient();
            Console.WriteLine("\n" + new string('=', 50) + "\n");
            
            // Run the simple debug test to isolate the LINQ issue
            await SimpleQueryDebugTest.RunSimpleQueryTest();
            Console.WriteLine("\n" + new string('=', 50) + "\n");
            
            await HttpLibSqlEFTest.RunHttpTest();
            
            Console.WriteLine("\n🎉 All HTTP LibSQL tests completed successfully!");
        }
        catch (ArgumentException ex) when (ex.Message.Contains("Invalid LibSQL HTTP connection string"))
        {
            Console.WriteLine($"\n❌ Connection String Error: {ex.Message}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Tests failed with error: {ex.Message}");
            Console.WriteLine($"Exception type: {ex.GetType().Name}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            
            Console.WriteLine($"\nFull stack trace:");
            Console.WriteLine(ex.StackTrace);
            
            Environment.Exit(1);
        }
    }
    
    private static string? GetHttpConnectionString()
    {
        return "libsql://test-dylanriden.aws-eu-west-1.turso.io?authToken=eyJhbGciOiJFZERTQSIsInR5cCI6IkpXVCJ9.eyJhIjoicnciLCJleHAiOjE3NDg0Njc2MTMsImlhdCI6MTc0ODM4MTIxMywiaWQiOiI4YzJjNGUxNi03NzUyLTRmODktOTljZC1hZjE0ZGU4YWE4ZjciLCJyaWQiOiJkOTQxYWI4My1iOGMwLTRlNTMtOWM4ZS04MDYzOTE1Yzk4ZDcifQ.xwg2XqpQYdHnAXRUrHSI0tiVVKveHq6h_pb3X_lRwJrLKPLpXTZhJlur2s38vs4qZMJA2iclNN3MKjZWQUEWCA";
    }
    
    private static string SanitizeConnectionString(string connectionString)
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
        
        return connectionString.Contains("authToken") ? "[HTTP CONNECTION WITH AUTH TOKEN]" : connectionString;
    }
}