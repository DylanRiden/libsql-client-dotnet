using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EntityFrameworkCore.LibSql.Extensions;

// Define the model
public class Blog
{
    public int BlogId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Post> Posts { get; set; } = new();
}

public class Post
{
    public int PostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int BlogId { get; set; }
    public Blog Blog { get; set; } = null!;
}

// Define the DbContext
public class BloggingContext : DbContext
{
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<Post> Posts => Set<Post>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Use in-memory for demo; replace with your LibSQL connection string as needed
        optionsBuilder
            .UseLibSql(":memory:")
            .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
            .EnableSensitiveDataLogging();
    }
}

class Program
{
    static void Main()
    {
        using var db = new BloggingContext();
        db.Database.EnsureCreated();

        // Add data
        if (!db.Blogs.Any())
        {
            var blog = new Blog { Name = "Test Blog", Posts = { new Post { Title = "Hello", Content = "First post!" } } };
            db.Blogs.Add(blog);
            db.SaveChanges();
        }

        // Query and print
        var blogs = db.Blogs.Include(b => b.Posts).ToList();
        foreach (var blog in blogs)
        {
            Console.WriteLine($"Blog: {blog.Name}");
            foreach (var post in blog.Posts)
            {
                Console.WriteLine($"  Post: {post.Title} - {post.Content}");
            }
        }
    }
}
