namespace ShoeTracker.Models;

///<summary>
/// Container used only for JSON deserialize. Groups shoes and runs
/// in a single file, instead of creating two separate ones.
/// No logic, only data. 
/// </summary>

public class TrackerData
{
    public List<Shoe> Shoes { get; set; } = new();
    public List<Run> Runs { get; set; } = new();
}