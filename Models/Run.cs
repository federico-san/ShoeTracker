namespace ShoeTracker.Models;

/// <summary>
/// Enum (type of values) for type of training completed. in C# enums are "heavier" and type-safe
/// respect to Python/JS. The compiler does not allow to write invalid values.
/// </summary>

public enum RunType
{
    Easy,
    Recovery,
    LongRun,
    Tempo,
    Intervals,
    Race
}

/// <summary>
/// A single registered run, linked to a pair of shoes with ShoeId (foreign key, same as RDBMS)
/// </summary>

public class Run
{
    public Guid Id { get; init; } = Guid.NewGuid(); // "init" for granting ID immutability after its creation.
    public required Guid ShoeId { get; set; } // modifier "required" (C# 11+) for initialize immediately when defined.
    public DateOnly Date { get; set; }
    public double DistanceKm { get; set; }
    public RunType Type { get; set; }
    public TimeSpan? Duration { get; set; } // "?" -> nullable. Run can have no time registered.
    
    /// <summary>
    /// Overrides ToString method of the base class System.Object
    /// for granting a properly formatted text representation of the run.
    /// </summary>
    public override string ToString()
    {
        // pattern matching with "is not null" and string format with interpolation
        var durationText = Duration is not null ? $", {Duration:hh\\:mm\\:ss}" : "";
        return $"{Date:dd/MM/yyyy} - {Type} - {DistanceKm:F2}km{durationText}";
    }
}