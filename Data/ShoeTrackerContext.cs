using Microsoft.EntityFrameworkCore;
using ShoeTracker.Models;

namespace ShoeTracker.Data;

/// <summary>
/// DbContext object: represents the session towards the database.
/// Central point through which every query or change passes.
/// Conceptually, the role previously played by TrackerService, but with tracking change
/// and SQL generation provided by the framework.
/// </summary>
public class ShoeTrackerContext : DbContext
{
    //DbSet<T> object: identifies a table, typed and queryable through LINQ.
    //Set<T> (instead of a private field + new()) is the recommended pattern by Microsoft
    //for the newest EFCore versions.
    public DbSet<Shoe> Shoes => Set<Shoe>();
    public DbSet<Run> Runs => Set<Run>();

    private readonly string _dbPath = Path.Combine(AppContext.BaseDirectory, "shoetracker.db");

    //Constructor without parameters. Necessary both for normal use of the app
    //and because the dotnet ef tool (used for migrations) must be able to instantiate the context on its own,
    //without a dependency injection container.
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source={_dbPath}");

    //OnModelCreating: this is where the explicit configuration (Fluent API) lives for
    //everything that EF Core's conventions don't cover on their own.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //Same logic for the JSON in TrackerService.
        //Enum gets saved as readable text instead as int
        modelBuilder.Entity<Run>()
            .Property(r => r.Type)
            .HasConversion<string>();
    }
}