# ShoeTracker
mileage tracker for my running shoes, and to understand C# basics

Structure is as follows.

ShoeTracker/
├── ShoeTracker.csproj
├── Program.cs              ← interactive menu (entry point)
├── Models/
│   ├── Shoe.cs              ← Shoe class
│   └── Run.cs                ← Run class + RunType enum
└── Services/
    └── TrackerService.cs     ← business logic and LINQ query
