using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ShoeTracker.Data;
using ShoeTracker.Models;

namespace ShoeTracker.Services;

/// <summary>
/// Business logic to manage runs and shoes. Now the database gets queried via ShoeTrackerContext instead of two in-memory List<T>
/// every method has become async, because every operation here touches disk (or network, with a real database) and is therefore I/O-bound
/// </summary>
public class TrackerService
{
    private readonly ShoeTrackerContext _context;
    public TrackerService(ShoeTrackerContext context)
    {
        _context = context;
    }

    public async Task<bool> HasAnyShoesAsync() =>
        await _context.Shoes.AnyAsync();
    
    public async Task<Shoe> AddShoeAsync(string brand, string model, int dropMm, int lifespan = 700)
    {
        var shoe = new Shoe
        {
            Brand = brand,
            Model = model,
            DropMm = dropMm,
            PurchaseDate = DateOnly.FromDateTime(DateTime.Now), //shoe gets added with current date
            LifespanKm = lifespan
        };
        _context.Shoes.Add(shoe);
        await _context.SaveChangesAsync();
        return shoe;
    }

    public async Task<Run?> LogRunAsync(Guid shoeId, double distanceKm, RunType type, DateOnly? date = null, TimeSpan? duration = null)
    {
        //LINQ (Language Integrated Query): declarative paradigm for manipulating collections.
        //Lambda expressions (e.g. s => s.Id == shoeId) define predicates
        var shoe = await _context.Shoes.FirstOrDefaultAsync(s => s.Id == shoeId);
        if (shoe is null) return null;

        var run = new Run
        {
            ShoeId = shoeId,
            DistanceKm = distanceKm,
            Type = type,
            Date = date ?? DateOnly.FromDateTime(DateTime.Now), // '??' > Null-coalescing Operator. If date is null, executes and returns the right side
                                                                // of the expression (today's date). If date has a value, returns the other one.
            Duration = duration
        };
        _context.Runs.Add(run);

        //"shoe" comes from a tracked query (FirstOrDefaultAsync): EF Core is already watching this instance.
        //No explicit "Update" is needed, change the property is sufficient; tracking will detect this automatically,
        //and SaveChangesAsync() will generate the correct UPDATE and the INSERT for the new run, in the same transaction
        shoe.TotalKm += distanceKm;

        await _context.SaveChangesAsync();
        return run;
    }

    public async Task<List<Shoe>> GetShoesAsync() =>
        await _context.Shoes.ToListAsync();

    public async Task<List<Run>> GetRunsAsync() =>
        await _context.Runs.ToListAsync();

    public async Task<double> GetKmForShoeAsync(Guid shoeId) =>
        await _context.Runs.Where(r => r.ShoeId == shoeId).SumAsync(r => r.DistanceKm);

    //calculates kms of one shoe in the last N days
    public async Task<double> GetKmLastDaysAsync(Guid shoeId, int days)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.Now.AddDays(-days));
        return await _context.Runs
            .Where(r => r.ShoeId == shoeId && r.Date >= cutoff)
            .SumAsync(r => r.DistanceKm);
    }

    //Note: The filter uses "s.TotalKm >= s.LifespanKm" (mapped properties), NOT "s.ShoeReplace" (the computed [NotMapped] property)
    //EF Core must translate this expression into an SQL WHERE clause, and can only do so with properties that fit to real columns.
    //ShoeReplace doesn't have one, so using it inside a Where() against the DbSet would fail at runtime.
    //Practical example of "not all C# can be translated to SQL".
    public async Task<List<Shoe>> ShowShoesToRetireAsync() =>
        await _context.Shoes.Where(s => s.TotalKm >= s.LifespanKm).ToListAsync();

    public async Task<Dictionary<string, double>> GetKmByMonthAsync(Guid shoeId)
    {
        //materialize the relevant runs first (ToListAsync executes a
        //real SQL query with WHERE on ShoeId), THEN group on the C# side with
        //LINQ to Objects.
        //The reason is GroupBy with custom key formatting (":D2") is not guaranteed to be
        //translatable into SQL by all providers.
        var runs = await _context.Runs
            .Where(r => r.ShoeId == shoeId)
            .ToListAsync();

        return runs
            .GroupBy(r => $"{r.Date.Year}-{r.Date.Month:D2}") //LINQ grouping
            .OrderBy(g => g.Key)                              //order by key (year-month)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.DistanceKm)); //sends to dictionary
    }

    public async Task<List<Run>> GetRunsForShoeAsync(Guid shoeId) =>
        await _context.Runs
            .Where(r => r.ShoeId == shoeId)
            .OrderByDescending(r => r.Date)
            .ToListAsync();

    /// <summary>
    /// Edits an existing run. Every parameter is Nullable: if passed
    /// (non-null) is applied, else the field remains the same.
    /// Returns false if the run does not exist.
    /// </summary> 
    public async Task<bool> EditRunAsync(Guid runId, Guid? newShoeId = null, double? newDistanceKm = null, RunType? newType = null, DateOnly? newDate = null)
    {
        var run = await _context.Runs.FirstOrDefaultAsync(r => r.Id == runId);
        if (run is null) return false;

        //Need to remember the original shoes BEFORE editing the run,
        //either the reference to the shoe to recalculate gets lost
        var oldShoeId = run.ShoeId;

        if (newShoeId is not null) run.ShoeId = newShoeId.Value;
        if (newDistanceKm is not null) run.DistanceKm = newDistanceKm.Value;
        if (newType is not null) run.Type = newType.Value;
        if (newDate is not null) run.Date = newDate.Value;

        await RecalculateShoeTotalAsync(oldShoeId);
        if (run.ShoeId != oldShoeId)
        {
            await RecalculateShoeTotalAsync(run.ShoeId);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    //private method to recalculate total kms of one shoe from the source data of the runs
    private async Task RecalculateShoeTotalAsync(Guid shoeId)
    {
        var shoe = await _context.Shoes.FirstOrDefaultAsync(s => s.Id == shoeId);
        if (shoe is null) return;

        //SumAsync makes a true SUM() database-side, we don't dump all the runs into memory just to sum them together
        shoe.TotalKm = await _context.Runs
            .Where(r => r.ShoeId == shoeId)
            .SumAsync(r => r.DistanceKm);
    }

    /// <summary>
    /// One-time migration from the old JSON backup (Level 2) to the SQLite database. Call only when the database is still empty;
    /// it does not perform any duplicate checks, and is not intended to be rerun multiple times on the same data.
    /// </summary>
    public async Task<int> ImportFromJsonAsync(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return 0;

        var options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };

        var json = await File.ReadAllTextAsync(jsonPath);
        var data = JsonSerializer.Deserialize<TrackerData>(json, options);
        if (data is null) return 0;

        _context.Shoes.AddRange(data.Shoes);
        _context.Runs.AddRange(data.Runs);
        await _context.SaveChangesAsync();

        return data.Shoes.Count + data.Runs.Count;
    }
}