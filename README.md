# ShoeTracker
Mileage tracker for my running shoes, made to understand C# basics

Structure is as follows.

```text
ShoeTracker/
├── ShoeTracker.csproj
├── Program.cs               < interactive menu (entry point)
├── Models/
│   ├── Shoe.cs              < Shoe class
│   ├── Run.cs               < Run class + RunType enum
│   └── TrackerData.cs       < Container to save shoes + runs in a JSON file
└── Services/
    └── TrackerService.cs    < business logic + LINQ query
```
If there's a slight chance you'll use this repository, the program runs with the `dotnet run` command from CLI.
This small project will still be updated with new functionalities, as soon as I learn them.
