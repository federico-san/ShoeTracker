namespace ShoeTrackers.Models;

/// <summary>
/// Features a pair of running shoes.
/// In C# data types must be declared explicitly.
/// "required" (C# 11+) is used to ensure that a property or field must be initialized when an object is created.
/// </summary>

public class Shoe
{
    //Guid is a unique 128-bit identifier, convenient for not having to
    //worry about generating incremental IDs by hand.
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Brand { get; set; }
    public required string Model { get; set; }

    // Drop -> difference in mm from heel to forefoot (e.g. Hyperion 3 has 8mm drop)
    public int DropMm { get; set; }

    public DateOnly PurchaseDate { get; set; }

    //after how many kms the pair should be replaced (usually 600-800km)
    public int Lifespan { get; set; } = 650;

    // "init" vs "set": TotalKm is incremental (every run increases its value), needs a normal setter
    public double TotalKm { get; set; } = 0;

    //computed property: gets recalculated every time you read it. Similar to @property in python.
    public double RemainingKm => Math.Max(0, Lifespan - TotalKm);

    public bool ShoeReplace => TotalKm >= Lifespan;

    public override string ToString() =>
        $"{Brand} {Model} (drop {DropMm}mm) - {TotalKm:F2}km / {Lifespan}km";
}