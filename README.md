# SimpleSunriseSunset

A small, cross?platform .NET console app that lets you interactively pick a city and a date and then shows approximate sunrise and sunset times.

It reads a CSV of world cities (name, country, latitude, longitude), offers fast type?ahead search with an optional country filter, and renders a compact result table in the terminal.


## How it works
- On startup the app looks for a file named `world_cities.csv` either next to the executable (output folder) or in the current working directory.
- The file is parsed with a lightweight CSV reader that supports quoted fields and commas inside quotes.
- You can optionally filter by country to reduce the city list.
- Type to search and select a city; then choose a date (defaults to today).
- Sunrise/sunset are computed using a simple NOAA?style approximation implemented in `NOAASunCalculator`.
- Results are printed using Spectre.Console as a small table.

Notes on accuracy:
- Times are approximate and do not account for local time zones, UTC offsets, or daylight saving time.
- High?latitude locations (near/inside polar circles) and edge cases (polar day/night, refraction, elevation, etc.) are not handled.


## CSV input
The app expects a header row and these columns (case?insensitive):
- city
- city_ascii (optional; used as a fallback/ASCII name)
- lat
- lng
- country

Only rows with valid numeric `lat`/`lng` and a non?empty city name are considered. Extra columns are ignored.

Place `world_cities.csv` either:
- in the same directory as the built executable, or
- in the directory you run the app from.

If you add the file to the project, it is already configured to copy to the output directory on build:
```
<ItemGroup>
  <None Update="world_cities.csv">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```


## Packages used
- Spectre.Console (0.49.1) — rich console UI: title/banners, prompts, tables, spinners.


## Requirements
- .NET 9 SDK
- Any supported OS (Windows, macOS, Linux)


## Build and run
From the repository root:

Build:
```
dotnet build
```

Run (debug):
```
dotnet run --project SimpleSunriseSunset
```

Alternatively, from the project folder:
```
dotnet run
```

Ensure `world_cities.csv` is available as described above before running.


## Publish (optional)
The project is configured for Native AOT publishing. To produce a self?contained native binary, specify a runtime identifier (change `win-x64` to your target RID, e.g., `linux-x64`, `osx-x64`, `osx-arm64`):
```
dotnet publish -c Release -r win-x64
```
The CSV file must be placed alongside the published executable at runtime unless you included it in the project to copy to output.


## Project details
- Target framework: .NET 9
- Language features: C# 13
- Key files:
  - `Program.cs` — CLI flow, CSV parsing, prompts, and rendering
  - `NOAASunCalculator.cs` — sunrise/sunset approximation


## What the app does (at a glance)
- Loads cities from `world_cities.csv` with a spinner
- Lets you filter by country (optional) and search by city name
- Prompts for a date (defaults to today)
- Calculates and displays sunrise and sunset times for the selected city/date
- Prints a short accuracy note and exits