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
    Console.WriteLine();
    Console.WriteLine("1. Shoes List");
    Console.WriteLine("2. Add New Shoes");
    Console.WriteLine("3. Km/Month (for each pair)");
    Console.WriteLine("4. Shoes To Retire");
    Console.WriteLine("5. Runs List");
    Console.WriteLine("6. Register Run");
    Console.WriteLine("7. Edit Run");
    Console.WriteLine("0. Exit");
    Console.WriteLine();
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
            AddShoeInteractive(tracker);
            tracker.SaveToFile(dataFilePath);
            break;
        case "3":
            ShowKmMonth(tracker);
            break;
        case "4":
            ShowShoesToRetire(tracker);
            break;
        case "5":
            ListRuns(tracker);
            break;
        case "6":
            LogRunInteractive(tracker);
            tracker.SaveToFile(dataFilePath);
            break;
        case "7":
            EditRunInteractive(tracker);
            tracker.SaveToFile(dataFilePath);
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
/// need to normalize the comma to a dot and force InvariantCulture, so the
/// behavior is identical wherever the app runs.
///</summary>
static bool TryParseDistance(string? input, out double distance)
{
    var normalized = (input ?? string.Empty).Trim().Replace(',','.');
    return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out distance);
}

///Explicit date parsing in the dd/mm/yyyy format, independent from system culture similar to above.
static bool TryParseDate(string? input, out DateOnly date)
{
    return DateOnly.TryParseExact(
        (input ?? string.Empty).Trim(),
        "dd/MM/yyyy",
        CultureInfo.InvariantCulture,
        DateTimeStyles.None,
        out date);
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

static void ListRuns(TrackerService tracker)
{
    if (tracker.Runs.Count == 0)
    {
        Console.WriteLine("No run registered.");
        return;
    }

    //same sorting of EditRun
    var sortedRuns = tracker.Runs.OrderByDescending(r => r.Date).ToList();

    Console.WriteLine();
    Console.WriteLine("=== Registered Runs ===");
    foreach (var r in sortedRuns)
    {
        var shoe = tracker.Shoes.FirstOrDefault(s => s.Id == r.ShoeId);
        var shoeLabel = shoe is not null ? $"{shoe.Brand} {shoe.Model}": "unknown shoe";
        Console.WriteLine($"{r} ({shoeLabel})");
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

    Console.Write("Run date (dd/mm/yyyy, ENTER for today): ");
    var dateInput = Console.ReadLine();
    DateOnly runDate;
    if (string.IsNullOrWhiteSpace(dateInput))
    {
        runDate = DateOnly.FromDateTime(DateTime.Now);
    }
    else if (!TryParseDate(dateInput, out runDate))
    {
        Console.WriteLine("Invalid date (use dd/mm/yyyy), using today.");
        runDate = DateOnly.FromDateTime(DateTime.Now);
    }

    Console.Write("Run type (Easy/Recovery/LongRun/Tempo/Intervals/Race}): ");
    var typeInput = Console.ReadLine();
    if (!Enum.TryParse<RunType>(typeInput, ignoreCase: true, out var type))
    {
        Console.WriteLine("Wrong type, using Easy instead.");
        type = RunType.Easy;
    }

    var run = tracker.LogRun(shoe.Id, distance, type, runDate);
    Console.WriteLine(run is not null ? $"Registered run: {run}" : "Error while registering.");
    Console.WriteLine();
}

static void EditRunInteractive(TrackerService tracker)
{
    if (tracker.Runs.Count == 0)
    {
        Console.WriteLine("No run registered.");
        return;
    }

    //Order runs by date (newest on top)
    //use this list to resolve index chosen by the user as well
    //otherwise they wouldn't match
    var sortedRuns = tracker.Runs.OrderByDescending(r => r.Date).ToList();

    //Summary of ALL runs (unfiltered by shoe) with progressive index and
    //shoe name for easy spotting
    Console.WriteLine();
    Console.WriteLine("=== Registered Runs ===");
    for (int i=0; i < tracker.Runs.Count; i++)
    {
        var r = sortedRuns[i];
        var shoe = tracker.Shoes.FirstOrDefault(s => s.Id == r.ShoeId);
        var shoeLabel = shoe is not null ? $"{shoe.Brand} {shoe.Model}" : "Unknown Shoe.";
        Console.WriteLine($"{i + 1}. {r} ({shoeLabel})");
    }

    Console.Write("Run number to edit: ");
    if (!int.TryParse(Console.ReadLine(), out int runIndex) || runIndex < 1 || runIndex > sortedRuns.Count)
    {
        Console.WriteLine("Invalid number.");
        return;
    }

    var runToEdit = sortedRuns[runIndex - 1];
    Console.WriteLine();
    Console.WriteLine("Leave empty field (only ENTER) to avoid changes.");

    // === Shoe ===
    Guid? newShoeId = null;
    ListShoes(tracker);
    Console.Write("New shoe number: ");
    var shoeInput = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(shoeInput))
    {
        if (int.TryParse(shoeInput, out int shoeIndex) && shoeIndex >= 1 && shoeIndex <= tracker.Shoes.Count)
        {
            newShoeId = tracker.Shoes[shoeIndex - 1].Id;
        }
        else
        {
            Console.WriteLine("Unvalid shoe number, keeping the current one.");
        }
    }

    // === Distance ===
    double? newDistance = null;
    ListShoes(tracker);
    Console.Write($"New distance (km, current {runToEdit.DistanceKm:F2}): ");
    var distanceInput = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(distanceInput))
    {
        if (TryParseDistance(distanceInput, out double parsedDistance))
        {
            newDistance = parsedDistance;
        }
        else
        {
            Console.WriteLine("Unvalid distance, keeping the current one.");
        }
    }

    // === Type ===
    RunType? newType = null;
    Console.Write($"New Type (current {runToEdit.Type}): ");
    var editTypeInput = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(editTypeInput))
    {
        if (Enum.TryParse<RunType>(editTypeInput, ignoreCase: true, out var parsedType))
        {
            newType = parsedType;
        }
        else
        {
            Console.WriteLine("Invalid Type, keeping the current one.");
        }
    }

    // === Date ===
    DateOnly? newDate = null;
    Console.Write($"new date (dd/mm/yyyy, current {runToEdit.Date:dd/MM/yyyy}): ");
    var editDateinput = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(editDateinput))
    {
        if (TryParseDate(editDateinput, out var parsedDate))
        {
            newDate = parsedDate;
        }
        else
        {
            Console.WriteLine("Unvalid date, keeping the current one.");
        }
    }

    bool updated = tracker.EditRun(runToEdit.Id, newShoeId, newDistance, newType, newDate);
    Console.WriteLine(updated ? "Updated run." : "Error while updating.");
}