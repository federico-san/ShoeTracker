using ShoeTracker.Models;

namespace ShoeTracker.Services;

///<summary>
/// Business logic to manage runs and shoes.
/// keeping this logic separate from Program.cs (which controls only the user interaction)
/// is a basic SoC pattern found in basically ALL enterprise projects.
/// </summary>

public class TrackerService
{
    //in Pyhton would be used a list; here List<T> is generics
    //meaning, the list knows at compile-time that contains only Shoe objects, with no need
    //to check types at runtime.
    private readonly List<Shoe> _shoes = new();
    private readonly List<Run> _runs = new();

    public IReadOnlyList<Shoe> Shoes => _shoes;
    public IReadOnlyList<Run> Runs => _runs;
    
    public Shoe AddShoe(string brand, string model, int dropMm, int lifespan = 700)
    {
        var shoe = new Shoe
        {
            Brand = brand,
            Model = model,
            DropMm = dropMm,
            PurchaseDate = DateOnly.FromDateTime(DateTime.Now),
            LifespanKm = lifespan
        };
        _shoes.Add(shoe);
        return shoe;
    }

    public Run? LogRun(Guid shoeId, double distanceKm, RunType type, DateOnly? date = null, TimeSpan? duration = null)
    {
        //FirstOrDefault (LINQ) prints null if does not find anything, instead of throwing an exception
        var shoe = _shoes.FirstOrDefault(s => s.Id == shoeId);
        if (shoe is null) return null;

        var run = new Run
        {
            ShoeId = shoeId,
            DistanceKm = distanceKm,
            Type = type,
            Duration = duration
        };
        _runs.Add(run);
        shoe.TotalKm += distanceKm; //updates shoe mileage

        return run;
    }

    ///<summary>
    ///LINQ: Where filters, then Sum sums (duh). Similar to filter + reduce in JS
    /// or list comprehension + sum() in Python.
    /// </summary>
    public double GetKmForShoe(Guid shoeId) =>
        _runs.Where(r => r.ShoeId == shoeId).Sum(r => r.DistanceKm);

    public double GetKmLastDays(Guid shoeId, int days)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.Now.AddDays(-days));
        return _runs
            .Where(r => r.ShoeId == shoeId && r.Date >= cutoff)
            .Sum(r => r.DistanceKm);
    }

    public List<Shoe> ShowShoesToRetire() => _shoes.Where(s => s.ShoeReplace).ToList();

    ///<summary>
    /// GroupBy groups run by month. Same concept of groupby() in Pandas.
    /// In C# it's native.
    ///</summary>
    public Dictionary<string, double> GetKmByMonth(Guid shoeId)
    {
        return _runs
            .Where(r => r.ShoeId == shoeId)
            .GroupBy(r => $"{r.Date.Year}-{r.Date.Month:D2}")
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.DistanceKm));
    }

    public List<Run> GetRunsForShoe(Guid shoeId) =>
        _runs.Where(r => r.ShoeId == shoeId).OrderByDescending(r => r.Date).ToList();
}