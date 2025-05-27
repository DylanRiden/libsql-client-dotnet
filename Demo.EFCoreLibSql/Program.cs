using System;
using Demo.EFCoreLibSql.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EntityFrameworkCore.LibSql.Extensions;

namespace Demo.EFCoreLibSql;

class Program
{
    static async Task Main()
    {
        try
        {
            // Test the direct LibSQL client first
            await SimpleFileTest.RunTest();
            await BasicLibSqlEFTest.RunBasicTest();   
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}