namespace ShoeTracker.Models;

/// <summary>
/// Enum (enumeration). Unlike strings or integers, an enum creates a "heavier" typed type.
/// The compiler will prevent the user from assigning an unexpected value, eliminating entire classes of typo-related bugs.
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
/// Entity which models a single registered run, linked to a pair of shoes with ShoeId (foreign key, same as RDBMS)
/// </summary>

public class Run
{
    public Guid Id { get; init; } = Guid.NewGuid(); // Guid (Globally unique identifier) > 128-bit integer. 
                                                    // init > modifier which allows assignment ONLY when the object is created. After that, the property becomes immutable (read-only).
    public required Guid ShoeId { get; set; } // required > forces anyone instantiating this class to provide a ShoeId value, preventing the creation of orphaned Run objects.
    public DateOnly Date { get; set; }
    public double DistanceKm { get; set; }
    public RunType Type { get; set; }
    public TimeSpan? Duration { get; set; } // "?" > Nullable Value Type. Duration can contain a valid Timespan or a NULL value (Run can have no time registered).

    /// <summary>
    /// Polymorphic override inherited from System.Object.
    /// It's called implicitly whenever an attempt is made to print the object.
    /// </summary>
    public override string ToString()
    {
        var durationText = Duration is not null ? $", {Duration:hh\\:mm\\:ss}" : ""; // is not null > Pattern Matching. More safe instead of "!= null".
        return $"{Date:dd/MM/yyyy} - {Type} - {DistanceKm:F2}km{durationText}"; // $"..." > String Interpolation. Similar to a Python f-string.
    }
}