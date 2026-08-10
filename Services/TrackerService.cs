using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ShoeTracker.Data;
using ShoeTracker.Models;

namespace ShoeTracker.Services;

/// <summary>
/// Business logic to manage runs and shoes.
/// keeping this logic separate from Program.cs (which controls only the user interaction)
/// is a basic SoC pattern found in basically ALL enterprise projects
/// </summary>

public class TrackerService
{
    //List<T> is a Generics collection. It guarantees compile-time type safety.
    //'readonly' protects the list reference (can't do _shoes = new List...), but it does NOT prevent
    //adding or removing elements from the list itself
    private readonly List<Shoe> _shoes = new();
    private readonly List<Run> _runs = new();

    //Shared options between Save and Load: readable indentation + enum saved as text
    //instead of number thanks to JsonStringEnumConverter.
    //The goal is to leave the file readable even if is opened with a text editor
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() } //saves Enums as strings instead of numbers
    };

    //IReadOnlyList<T> is an interface that exposes only the read methods.
    //Crucial encapsulation technique: the outside can read the lists,
    //but only the TrackerService can modify them (via LogRun, AddShoe, etc.)
    public IReadOnlyList<Shoe> Shoes => _shoes;
    public IReadOnlyList<Run> Runs => _runs;
    
    public Shoe AddShoe(string brand, string model, int dropMm, int lifespan = 700)
    {
        var shoe = new Shoe
        {
            Brand = brand,
            Model = model,
            DropMm = dropMm,
            PurchaseDate = DateOnly.FromDateTime(DateTime.Now), //shoe gets added with current date
            LifespanKm = lifespan
        };
        _shoes.Add(shoe);
        return shoe;
    }

    /// <summary>
    /// Saves a new run linked to an existing shoe. Returns null if the shoe is not found inside the list
    /// </summary>
    public Run? LogRun(Guid shoeId, double distanceKm, RunType type, DateOnly? date = null, TimeSpan? duration = null)
    {
        //LINQ (Language Integrated Query): declarative paradigm for manipulating collections.
        //Lambda expressions (s => s.Id == shoeId) define predicates
        var shoe = _shoes.FirstOrDefault(s => s.Id == shoeId);
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
        _runs.Add(run);
        shoe.TotalKm += distanceKm; //updates shoe mileage

        return run;
    }

    /// <summary>
    /// LINQ: Where filters, then Sum sums. Similar to filter + reduce in JS
    /// or list comprehension + sum() in Python.
    /// Calculates total kms of a shoe using LINQ.
    /// </summary>
    public double GetKmForShoe(Guid shoeId) =>
        _runs.Where(r => r.ShoeId == shoeId).Sum(r => r.DistanceKm);

    //calculates kms of one shoe in the last N days
    public double GetKmLastDays(Guid shoeId, int days)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.Now.AddDays(-days));
        return _runs
            .Where(r => r.ShoeId == shoeId && r.Date >= cutoff)
            .Sum(r => r.DistanceKm);
    }

    public List<Shoe> ShowShoesToRetire() => _shoes.Where(s => s.ShoeReplace).ToList();

    /// <summary>
    /// GroupBy groups run by month. Same concept of groupby() in Pandas.
    /// In C# it's native.
    /// </summary>
    public Dictionary<string, double> GetKmByMonth(Guid shoeId)
    {
        return _runs
            .Where(r => r.ShoeId == shoeId)
            .GroupBy(r => $"{r.Date.Year}-{r.Date.Month:D2}") //LINQ grouping
            .OrderBy(g => g.Key)                              //order by key (year-month)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.DistanceKm)); //sends to dictionary
    }

    public List<Run> GetRunsForShoe(Guid shoeId) =>
        _runs.Where(r => r.ShoeId == shoeId).OrderByDescending(r => r.Date).ToList();

    /// <summary>
    /// Edits an existing run. Every parameter is Nullable: if passed
    /// (non-null) is applied, else the field remains the same.
    /// Returns false if the run does not exist.
    /// </summary> 
    public bool EditRun(Guid runId, Guid? newShoeId = null, double? newDistanceKm = null, RunType? newType = null, DateOnly? newDate = null)
    {
        var run = _runs.FirstOrDefault(r => r.Id == runId);
        if (run is null) return false;

        //Need to remember the original shoes BEFORE editing the run,
        //either the reference to the shoe to recalculate gets lost
        var oldShoeId = run.ShoeId;

        if (newShoeId is not null) run.ShoeId = newShoeId.Value;
        if (newDistanceKm is not null) run.DistanceKm = newDistanceKm.Value;
        if (newType is not null) run.Type = newType.Value;
        if (newDate is not null) run.Date = newDate.Value;

        //TotalKm is a cache updated manually (see LogRun)
        //not a value calculated in real-time like GetKmForShoe. Editing a run
        //can misalign it. Need to recalculate from 0 adding the real runs,
        //both for the old shoe (if changed) and the new.
        RecalculateShoeTotal(oldShoeId);
        if (run.ShoeId != oldShoeId)
        {
            RecalculateShoeTotal(run.ShoeId);
        }

        return true;
    }

    //private method to recalculate total kms of one shoe from the source data of the runs
    private void RecalculateShoeTotal(Guid shoeId)
    {
        var shoe = _shoes.FirstOrDefault(s => s.Id == shoeId);
        if (shoe is null) return;
        shoe.TotalKm = GetKmForShoe(shoeId);
    }

    //Saves shoes and runs in a readable JSON file.
    public void SaveToFile(string path)
    {
        var data = new TrackerData { Shoes = _shoes.ToList(), Runs = _runs.ToList() };
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Loads runs and running shoes, if they exist. Returns 'true' if data
    /// has actually loaded, 'false' if file is corrupted or not exists
    /// (in that case, Program.cs has to populate the file with initial data)
    /// </summary>
    public bool LoadFromFile(string path)
    {
        if (!File.Exists(path)) return false;

        try
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<TrackerData>(json, JsonOptions);
            if (data is null) return false;

            _shoes.Clear();
            _shoes.AddRange(data.Shoes);
            _runs.Clear();
            _runs.AddRange(data.Runs);
            return true;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Savefile seems corrupted ({ex.Message}). Starting again with empty data.");
            return false;
        }
    }
}