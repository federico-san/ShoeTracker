using System.ComponentModel.DataAnnotations.Schema;

namespace ShoeTracker.Models;

/// <summary>
/// Features a pair of running shoes.
/// In C# data types must be declared explicitly.
/// </summary>

public class Shoe
{
    //Guid is a 128-bit identifier, and it works as primary key as well. EF Core recognizes it by convention,
    //thanks to the name "Id". No explicit configuration is necessary.
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string Brand { get; set; }
    public required string Model { get; set; }
    public int DropMm { get; set; }

    public DateOnly PurchaseDate { get; set; }

    //after how many kms the pair should be replaced (usually 600-800km)
    public int LifespanKm { get; set; } = 650;

    // Unlike Id, TotalKm has a standard 'set' because its value must change over time, as new runs are recorded
    public double TotalKm { get; set; } = 0;

    //[NotMapped]: Computed properties, no corresponding column exists in the database
    //so EF Core ignores them completely during mapping.
    //They can still be used in C# on an already loaded instance (e.g., for visualization)
    //but they CANNOT appear inside a .Where() filter passed to a DbSet:
    //EF Core would have no columns to translate into SQL, and the query would fail at runtime.
    [NotMapped]
    public double RemainingKm => Math.Max(0, LifespanKm - TotalKm);

    [NotMapped]
    public bool ShoeReplace => TotalKm >= LifespanKm;

    //Navigation property: "towards many" side of the Shoe(1) -> Run(N) relationship.
    //EF Core recognizes it by convention thanks to the presence of Run.ShoeId, which becomes the foreign key in the database.
    public List<Run> Runs { get; set; } = new();
    
    public override string ToString() =>
        $"{Brand} {Model} (drop {DropMm}mm) - {TotalKm:F2}km / {LifespanKm}km";
}