using System.Globalization;
using ShoeTracker.Models;
using ShoeTracker.Services;

var tracker = new TrackerService();

//The file lives near the executable (AppContext.BaseDirectory), instead
//on the folder where the command gets launched. The path is always the same
//whether doing dotnet run from the main directory or launch the exe from bin/Debug/net8.0
var dataFilePath = Path.Combine(AppContext.BaseDirectory, "shoetracker-data.json");

bool loaded = tracker.LoadFromFile(dataFilePath);

if (!loaded)
{
    //Initially there's nothing saved. Populate it with the initial rotation.
    //In this way the file exists from the beginning.
    //initial seed with current shoe rotation
    var hyperion3 = tracker.AddShoe("Brooks", "Hyperion 3", dropMm: 8, lifespan: 500);
    var glizzymax2 = tracker.AddShoe("Brooks", "Glycerin Max 2", dropMm: 6, lifespan: 700);
    var skyflow = tracker.AddShoe("HOKA", "Skyflow", dropMm: 5, lifespan: 600);

    //some absolutely real runs to populate the tracker
    tracker.LogRun(glizzymax2.Id, 7.29, RunType.Easy, new DateOnly(2026, 07, 08));
    tracker.LogRun(hyperion3.Id, 5.64, RunType.Tempo, new DateOnly(2026, 06, 06));
    tracker.LogRun(glizzymax2.Id, 10.6, RunType.LongRun, new DateOnly(2026, 06, 14));
    tracker.LogRun(hyperion3.Id, 7.64, RunType.Easy, new DateOnly(2026, 06, 12));
    tracker.LogRun(skyflow.Id, 8.12, RunType.Recovery, new DateOnly(2026, 04, 12));
    tracker.LogRun(skyflow.Id, 7.47, RunType.Easy, new DateOnly(2026, 04, 09));

    tracker.SaveToFile(dataFilePath);
}
else
{
    Console.WriteLine($"Loaded data from {dataFilePath}");
}

bool running = true;

while (running)
{
    //basic console interface
    Console.WriteLine("=== Shoe Tracker ===");
    Console.WriteLine("1. Shoes List");
    Console.WriteLine("2. Register Run");
    Console.WriteLine("3. Add New Shoes");
    Console.WriteLine("4. Km/Month (for each pair)");
    Console.WriteLine("5. Shoes To Retire");
    Console.WriteLine("0. Exit");
    Console.WriteLine("====================");
    Console.WriteLine();
    Console.Write("Choose: ");

    var choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            ListShoes(tracker);
            break;
        case "2":
            LogRunInteractive(tracker);
            tracker.SaveToFile(dataFilePath);
            break;
        case "3":
            AddShoeInteractive(tracker);
            tracker.SaveToFile(dataFilePath);
            break;
        case "4":
            ShowKmMonth(tracker);
            break;
        case "5":
            ShowShoesToRetire(tracker);
            break;
        case "0":
            tracker.SaveToFile(dataFilePath);
            running = false;
            break;
        default:
            Console.WriteLine("Invalid choice.");
            break;
    }
}

//--- local functions: C# useful feature to keep Program.cs readable without
//creating different classes for each action ---

///<summary>
/// Culture-robust KM parsing. 
/// double.TryParse(string, out double) alone depends on the CurrentCulture:
/// on a machine with invariant/en-US culture, the comma is read as
/// a thousands separator, so "7.29" would become 729 instead of 7.29.
/// we want to normalize the comma to a dot and force InvariantCulture, so the
/// behavior is identical wherever the app runs.
///</summary>
static bool TryParseDistance(string? input, out double distance)
{
    var normalized = (input ?? string.Empty).Trim().Replace(',','.');
    return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out distance);
}

static void ListShoes(TrackerService tracker)
{
    Console.WriteLine();
    if (tracker.Shoes.Count == 0)
    {
        Console.WriteLine("No shoes registered.");
        return;
    }

    //Console.WriteLine with index for numeric lists
    for (int i=0; i < tracker.Shoes.Count; i++)
    {
        var shoe = tracker.Shoes[i];
        var flag = shoe.ShoeReplace ? "!!! NEEDS REPLACEMENT !!!" : "";
        Console.WriteLine($"{i + 1}. {shoe}{flag}");
    }
    Console.WriteLine();
}

static void LogRunInteractive(TrackerService tracker)
{
    ListShoes(tracker);
    if (tracker.Shoes.Count == 0) return;

    Console.Write("Shoes number: ");
    if (!int.TryParse(Console.ReadLine(), out int index) || index < 1 || index > tracker.Shoes.Count)
    {
        Console.Write("invalid number.");
        return;
    }

    var shoe = tracker.Shoes[index - 1];

    Console.Write("Distance (km, e.g. 8.14 or 8,14): ");
    if (!TryParseDistance(Console.ReadLine(), out double distance))
    {
        Console.WriteLine("Invalid distance.");
        return;
    }

    Console.Write("Run type (Easy/Recovery/LongRun/Tempo/Intervals/Race}): ");
    var typeInput = Console.ReadLine();
    if (!Enum.TryParse<RunType>(typeInput, ignoreCase: true, out var type))
    {
        Console.WriteLine("Wrong type, using Easy instead.");
        type = RunType.Easy;
    }

    var run = tracker.LogRun(shoe.Id, distance, type);
    Console.WriteLine(run is not null ? $"Registered run: {run}" : "Error while registering.");
    Console.WriteLine();
}

static void AddShoeInteractive(TrackerService tracker)
{
    Console.Write("Brand: ");
    var brand = Console.ReadLine() ?? "Unknown";

    Console.Write("Model: ");
    var model = Console.ReadLine() ?? "Unknown";

    Console.Write("Drop (mm): ");
    int.TryParse(Console.ReadLine(), out int drop);

    Console.Write("Recommended mileage in Km (default 700): ");
    var lifespanInput = Console.ReadLine();
    int lifespan = string.IsNullOrWhiteSpace(lifespanInput) ? 700 : int.Parse(lifespanInput);

    var shoe = tracker.AddShoe(brand, model, drop, lifespan);
    Console.WriteLine($"Added: {shoe}");
    Console.WriteLine();
}

static void ShowKmMonth(TrackerService tracker)
{
    ListShoes(tracker);
    if (tracker.Shoes.Count == 0) return;

    Console.Write("Shoes number: ");
    if (!int.TryParse(Console.ReadLine(), out int index) || index < 1 || index > tracker.Shoes.Count)
    {
        Console.WriteLine("Invalid number.");
        return;
    }

    var shoe = tracker.Shoes[index - 1];
    var byMonth = tracker.GetKmByMonth(shoe.Id);

    Console.WriteLine();
    Console.WriteLine($"Km per month -- {shoe.Brand} {shoe.Model}");
    foreach (var (month, km) in byMonth)
    {
        Console.WriteLine($"  {month}: {km:F2} km");
    }
    Console.WriteLine();
}

static void ShowShoesToRetire(TrackerService tracker)
{
    var toReplace = tracker.ShowShoesToRetire();
    Console.WriteLine();
    if (toReplace.Count == 0)
    {
        Console.WriteLine("No shoes have reached their max mileage yet.");
        Console.WriteLine();
        return;
    }

    foreach (var shoe in toReplace)
    {
        Console.WriteLine($"!!! WARNING {shoe} !!!");
    }
    Console.WriteLine();
}