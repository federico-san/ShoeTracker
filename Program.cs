using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ShoeTracker.Data;
using ShoeTracker.Models;
using ShoeTracker.Services;

/// <summary>
/// "await using": Asynchronously dispose of the context at the end of the program (DbContext implements IAsyncDisposable).
/// Using "await" here at the top of the top-level statements is what causes the compiler to generate an asynchronous Main for the entire file,
/// without having to write it explicitly.
/// </summary>
await using var context = new ShoeTrackerContext();


/// <summary>
/// Automatically applies migrations that haven't yet been applied to the database.
/// Handy for a learning project; in a real-world production app, this step is often handled separately
/// (e.g., by a deployment pipeline) rather than at every application startup.
/// </summary>
await context.Database.MigrateAsync();

var tracker = new TrackerService(context);

bool running = true;

while (running)
{
    //main loop of the basic console interface
    //keeps going until 'running' turns false
    Console.WriteLine();
    Console.WriteLine("=== Shoe Tracker ===");
    Console.WriteLine();
    Console.WriteLine("1. Shoes list");
    Console.WriteLine("2. Add/Delete shoes");
    Console.WriteLine("3. Km/Month (for each pair)");
    Console.WriteLine("4. Shoes to retire");
    Console.WriteLine("5. Runs list");
    Console.WriteLine("6. Register run");
    Console.WriteLine("7. Edit run");
    Console.WriteLine("8. Exit");
    Console.WriteLine();
    Console.WriteLine("====================");
    Console.WriteLine();
    Console.Write("Choose: ");

    var choice = Console.ReadLine();

    switch (choice)
    {
        case "1":
            await ListShoes(tracker);
            break;
        case "2":
            await AddShoeInteractive(tracker);
            break;
        case "3":
            await ShowKmMonth(tracker);
            break;
        case "4":
            await ShowShoesToRetire(tracker);
            break;
        case "5":
            await ListRuns(tracker);
            break;
        case "6":
            await LogRunInteractive(tracker);
            break;
        case "7":
            await EditRunInteractive(tracker);
            break;
        case "8":
            running = false; //kills the loop
            break;
        default:
            Console.WriteLine("Invalid choice.");
            break;
    }
}

// LOCAL FUNCTIONS
//These functions are only visible within Program.cs / Main method.
//They allow to split a very long file into reusable logical blocks.
//Now "static async Task" instead of "static void". Rest of the pattern is same as before.

/// <summary>
/// Culture-robust KM parsing.
/// Use of 'out' parameters. C# does not natively support Python-style return tuples (return a, b) without specific syntax, so the TryParse methods
/// return a boolean (success/failure) and "populate" the 'out distance' variable.
/// CultureInfo.InvariantCulture normalizes the input ensuring that floats are treated the same regardless of the operating system wherever the app is running on.
/// </summary>
static bool TryParseDistance(string? input, out double distance)
{
    var normalized = (input ?? string.Empty).Trim().Replace(',','.');
    return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out distance);
}

//Explicit date parsing in the dd/mm/yyyy format, independent from system culture; flexible on some formatting details.
static bool TryParseDate(string? input, out DateOnly date)
{
    string[] formats = { "d/M/yyyy", "d/M/yy" };
    return DateOnly.TryParseExact(
        (input ?? string.Empty).Trim(),
        formats,
        CultureInfo.InvariantCulture,
        DateTimeStyles.None,
        out date);
}

static void PrintShoes(List<Shoe> shoes)
{
    Console.WriteLine();
    if (shoes.Count == 0)
    {
        Console.WriteLine("No shoes registered.");
        return;
    }

    //Console.WriteLine with index for numeric lists
    for (int i=0; i < shoes.Count; i++)
    {
        var shoe = shoes[i];
        //Ternary Operator: if Shoereplace == True assigns the string, else leave empty.
        var flag = shoe.ShoeReplace ? "!!! NEEDS REPLACEMENT !!!" : "";
        Console.WriteLine($"{i + 1}. {shoe}{flag}");
    }
    Console.WriteLine();
}

static async Task ListShoes(TrackerService tracker)
{
    var shoes = await tracker.GetShoesAsync();
    PrintShoes(shoes);
}

static async Task AddShoeInteractive(TrackerService tracker)
{
    Console.WriteLine();
    Console.WriteLine("1. Add shoe");
    Console.WriteLine("2. Delete shoe");
    Console.WriteLine("Choice: ");
    var action = Console.ReadLine();

    switch(action)
    {
        case "1":
            await AddShoe(tracker);
            break;
        case "2":
            await DeleteShoe(tracker);
            break;
        default:
            Console.WriteLine("Invalid choice.");
            break;
    }
}

static async Task AddShoe(TrackerService tracker)
{
    Console.Write("Brand: ");
    var brand = Console.ReadLine() ?? "Unknown";

    Console.Write("Model: ");
    var model = Console.ReadLine() ?? "Unknown";

    Console.Write("Drop (mm): ");
    //int.TryParse does not crash the application if the user enters text instead of numbers.
    //For text, 'drop' defaults to 0.
    int.TryParse(Console.ReadLine(), out int drop);

    Console.Write("Recommended mileage in Km (default 700): ");
    var lifespanInput = Console.ReadLine();
    int lifespan = string.IsNullOrWhiteSpace(lifespanInput) ? 700 : int.Parse(lifespanInput); //IsNullOrWhiteSpace method checks for null, empty string, or a string consisting only of spaces

    var shoe = await tracker.AddShoeAsync(brand, model, drop, lifespan);
    Console.WriteLine($"Added: {shoe}");
    Console.WriteLine();
}

static async Task DeleteShoe(TrackerService tracker)
{
    var shoes = await tracker.GetShoesAsync();
    PrintShoes(shoes);
    if (shoes.Count == 0) return;

    Console.Write("Number of shoe to delete: ");
    if (!int.TryParse(Console.ReadLine(), out int index) || index < 1 || index > shoes.Count)
    {
        Console.WriteLine("Invalid number.");
        return;
    }

    var shoe = shoes[index -1];

    //Check how many runs are connected FIRST, so the user knows exactly what he is about to lose.
    //The database-level cascade delete doesn't ask for confirmation by itself, the UI does that.
    var runCount = await tracker.CountRunsAsync(shoe.Id);

    Console.WriteLine();
    if (runCount > 0)
    {
        var runWord = runCount == 1 ? "run" : "runs";
        Console.WriteLine($"WARNING: {shoe} has {runCount} {runWord} registered.");
        Console.WriteLine("Deleting the shoe will also delete ALL runs linked to it. This cannot be undone.");
    }
    else
    {
        Console.WriteLine($"You're about to delete: {shoe}");
    }

    Console.Write("Confirm? (Y/n): ");
    var confirm = Console.ReadLine();
    if (!string.Equals(confirm?.Trim(), "s", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Deletion aborted.");
        return;
    }

    bool deleted = await tracker.DeleteShoeAsync(shoe.Id);
    Console.WriteLine(deleted ? "Shoe deleted." : "Error during deletion.");
}

static async Task ShowKmMonth(TrackerService tracker)
{
    var shoes = await tracker.GetShoesAsync();
    PrintShoes(shoes);
    if (shoes.Count == 0) return;

    Console.Write("Shoes number: ");
    if (!int.TryParse(Console.ReadLine(), out int index) || index < 1 || index > shoes.Count)
    {
        Console.WriteLine("Invalid number.");
        return;
    }

    var shoe = shoes[index - 1];
    var byMonth = await tracker.GetKmByMonthAsync(shoe.Id);

    Console.WriteLine();
    Console.WriteLine($"Km per month -- {shoe.Brand} {shoe.Model}");
    foreach (var (month, km) in byMonth)
    {
        Console.WriteLine($"  {month}: {km:F2} km");
    }
    Console.WriteLine();
}

static async Task ShowShoesToRetire(TrackerService tracker)
{
    var toReplace = await tracker.ShowShoesToRetireAsync();
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

static async Task ListRuns(TrackerService tracker)
{
    var runs = await tracker.GetRunsAsync();

    if (runs.Count == 0)
    {
        Console.WriteLine("No run registered.");
        return;
    }

    var shoes = await tracker.GetShoesAsync();
    var sortedRuns = runs.OrderByDescending(r => r.Date).ToList();

    Console.WriteLine();
    Console.WriteLine("=== Registered Runs ===");
    Console.WriteLine();
    foreach (var r in sortedRuns)
    {
        var shoe = shoes.FirstOrDefault(s => s.Id == r.ShoeId);
        var shoeLabel = shoe is not null ? $"{shoe.Brand} {shoe.Model}": "unknown shoe.";
        Console.WriteLine($"{r} ({shoeLabel})");
    }

    Console.WriteLine();
}

static async Task LogRunInteractive(TrackerService tracker)
{
    var shoes = await tracker.GetShoesAsync();
    PrintShoes(shoes);
    if (shoes.Count == 0) return;

    Console.Write("Shoes number: ");
    if (!int.TryParse(Console.ReadLine(), out int index) || index < 1 || index > shoes.Count)
    {
        Console.Write("invalid number.");
        return;
    }

    var shoe = shoes[index - 1];

    Console.Write("Distance (km, e.g. 8.14 or 8,14): ");
    if (!TryParseDistance(Console.ReadLine(), out double distance))
    {
        Console.WriteLine("Invalid distance.");
        return;
    }

    Console.Write("Run date (e.g. 27/06/2026 or 27/6/26, ENTER for today): ");
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

    Console.Write("Run type (Easy/Recovery/LongRun/Tempo/Intervals/Race): ");
    var typeInput = Console.ReadLine();

    //Enum.TryParse attempts to convert the string into Enum.
    //'ignoreCase: true' allows the user to type "easy", "EASY", or "Easy"
    if (!Enum.TryParse<RunType>(typeInput, ignoreCase: true, out var type))
    {
        Console.WriteLine("Wrong type, using Easy instead.");
        type = RunType.Easy;
    }

    var run = await tracker.LogRunAsync(shoe.Id, distance, type, runDate);
    Console.WriteLine(run is not null ? $"Registered run: {run}" : "Error while registering.");
    Console.WriteLine();
}

static async Task EditRunInteractive(TrackerService tracker)
{
    var runs = await tracker.GetRunsAsync();
    if (runs.Count == 0)
    {
        Console.WriteLine("No run registered.");
        return;
    }

    var shoes = await tracker.GetShoesAsync();

    //Sorting for visualization, also used to resolve the user-selected index (shown and selected must match).
    var sortedRuns = runs.OrderByDescending(r => r.Date).ToList();

    //Summary of ALL runs (unfiltered by shoe) with progressive index and
    //shoe name for easy spotting
    Console.WriteLine();
    Console.WriteLine("=== Registered Runs ===");
    for (int i=0; i < sortedRuns.Count; i++)
    {
        var r = sortedRuns[i];
        var shoe = shoes.FirstOrDefault(s => s.Id == r.ShoeId);
        var shoeLabel = shoe is not null ? $"{shoe.Brand} {shoe.Model}" : "unknown shoe.";
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
    PrintShoes(shoes);
    Console.Write("New shoe number: ");
    var shoeInput = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(shoeInput))
    {
        if (int.TryParse(shoeInput, out int shoeIndex) && shoeIndex >= 1 && shoeIndex <= shoes.Count)
        {
            newShoeId = shoes[shoeIndex - 1].Id;
        }
        else
        {
            Console.WriteLine("Unvalid shoe number, keeping the current one.");
        }
    }

    // === Distance ===
    double? newDistance = null;
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
    Console.Write($"new date (e.g. 26/07/2026 or 26/7/26, current {runToEdit.Date:dd/MM/yyyy}): ");
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

    bool updated = await tracker.EditRunAsync(runToEdit.Id, newShoeId, newDistance, newType, newDate);
    Console.WriteLine(updated ? "Updated run." : "Error while updating.");
}