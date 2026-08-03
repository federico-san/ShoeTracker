namespace ShoeTracker.Models;

/// <summary>
/// Features a pair of running shoes.
/// In C# data types must be declared explicitly.
/// </summary>

public class Shoe
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Brand { get; set; }
    public required string Model { get; set; }

    // Drop -> difference in mm from heel to forefoot (e.g. Hyperion 3 has 8mm drop)
    public int DropMm { get; set; }

    public DateOnly PurchaseDate { get; set; }

    //after how many kms the pair should be replaced (usually 600-800km)
    public int LifespanKm { get; set; } = 650;

    // Unlike Id, TotalKm has a standard 'set' because its value must change over time, as new runs are recorded
    public double TotalKm { get; set; } = 0;

    //Computed Property: it doesn't allocate memory to store the value; the logic is re-executed every time the property is read. Similar to @property in Python
    public double RemainingKm => Math.Max(0, LifespanKm - TotalKm);

    //Boolean property calculated on-the-fly. Returns 'true' if TotalKm equals or passes the Lifespan
    public bool ShoeReplace => TotalKm >= LifespanKm;

    public override string ToString() =>
        $"{Brand} {Model} (drop {DropMm}mm) - {TotalKm:F2}km / {LifespanKm}km";
}