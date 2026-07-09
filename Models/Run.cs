namespace ShoeTracker.Models;

/// <summary>
/// Enum for type of training completed. in C# enums are "heavier" and type-safe
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
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid ShoeId { get; set; }
    public DateOnly Date { get; set; }
    public double DistanceKm { get; set; }
    public RunType Type { get; set; }
    public TimeSpan? Duration { get; set; } // "?" -> nullable. Run can have no time registered.

    public override string ToString()
    {
        var durationText = Duration is not null ? $", {Duration:hh\\:mm\\:ss}" : "";
        return $"{Date:dd/MM/yy} - {Type} - {DistanceKm:F1}km{durationText}";
    }
}