using Spectre.Console;
using System.Globalization;

namespace SimpleSunriseSunset;

internal static class Program
{
    private static void Main()
    {
        AnsiConsole.Write(new FigletText("Sunrise / Sunset").Color(Color.Orange1));

        string? path = FindWorldCitiesCsv();
        if (path is null)
        {
            AnsiConsole.MarkupLine("[red]world_cities.csv not found.[/]");
            AnsiConsole.MarkupLine("Place the file next to the executable or in the current directory and run again.");
            return;
        }

        AnsiConsole.Write(new Rule($"Loading cities from [green]{Path.GetFileName(path)}[/]").Centered());

        // Load cities with a status spinner
        List<City> cities = [];
        AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots2)
            .SpinnerStyle(Style.Parse("green"))
            .Start("Parsing CSV...", _ => { cities = [.. LoadCities(path)]; });

        if (cities.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No cities could be loaded from CSV.[/]");
            return;
        }

        // Optional country filter to shrink the list
        List<string> countryChoices =
        [
            "All countries",
            .. cities.Select(c => c.Country)
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .Distinct()
                              .OrderBy(s => s),
        ];

        string filterByCountry = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Filter by country? (optional)")
                .PageSize(10)
                .EnableSearch()
                .MoreChoicesText("[grey](Move up and down to reveal more countries)[/]")
                .AddChoices(countryChoices)
        );

        IEnumerable<City> filtered = filterByCountry == "All countries"
            ? cities
            : cities.Where(c => string.Equals(c.Country, filterByCountry, StringComparison.OrdinalIgnoreCase));

        // Selection with type-ahead search
        City selected = AnsiConsole.Prompt(
            new SelectionPrompt<City>()
                .Title("Select a city (type to search)")
                .PageSize(15)
                .EnableSearch()
                .MoreChoicesText("[grey](Move up/down to see more, start typing to search)[/]")
                .UseConverter(c => c.Display)
                .AddChoices(filtered.OrderBy(c => c.Name))
        );

        DateTime date = AnsiConsole.Prompt(
            new TextPrompt<DateTime>("Enter date (leave empty for today)")
                .DefaultValue(DateTime.Today)
                .Validate(d => d.Year is > 1800 and < 9999
                    ? ValidationResult.Success()
                    : ValidationResult.Error("Invalid year"))
        );

        (DateTime sunrise, DateTime sunset) = NOAASunCalculator.Calculate(selected.Latitude, selected.Longitude, date);

        // Output results
        Table table = new Table().Border(TableBorder.Rounded).Title("[yellow]Sun Times[/]");
        table.AddColumn(new TableColumn("City").Centered());
        table.AddColumn(new TableColumn("Date").Centered());
        table.AddColumn(new TableColumn("Sunrise").Centered());
        table.AddColumn(new TableColumn("Sunset").Centered());

        string fmt = "yyyy-MM-dd HH:mm";
        table.AddRow(
            selected.Display,
            date.ToString("yyyy-MM-dd"),
            sunrise.ToString(fmt),
            sunset.ToString(fmt)
        );

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("\n[grey]Note: Times are approximate and do not account for time zone/UTC offsets.[/]");
    }

    private static string? FindWorldCitiesCsv()
    {
        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "world_cities.csv"),
            Path.Combine(Directory.GetCurrentDirectory(), "world_cities.csv")
        ];

        foreach (string? path in candidates)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    // Simple CSV splitter that handles quotes and commas within quotes
    private static List<string> SplitCsvLine(string line)
    {
        List<string> result = [];
        if (string.IsNullOrEmpty(line))
        {
            result.Add(string.Empty);
            return result;
        }

        bool inQuotes = false;
        System.Text.StringBuilder current = new();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++; // skip escaped quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result;
    }

    private static IEnumerable<City> LoadCities(string path)
    {
        using StreamReader reader = new(path);

        // Read header
        string? header = reader.ReadLine();
        if (header is null)
            yield break;

        List<string> headerCols = SplitCsvLine(header);
        int idxCity = headerCols.FindIndex(h => string.Equals(h, "city", StringComparison.OrdinalIgnoreCase));
        int idxCityAscii = headerCols.FindIndex(h => string.Equals(h, "city_ascii", StringComparison.OrdinalIgnoreCase));
        int idxLat = headerCols.FindIndex(h => string.Equals(h, "lat", StringComparison.OrdinalIgnoreCase));
        int idxLng = headerCols.FindIndex(h => string.Equals(h, "lng", StringComparison.OrdinalIgnoreCase));
        int idxCountry = headerCols.FindIndex(h => string.Equals(h, "country", StringComparison.OrdinalIgnoreCase));

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            List<string> cols = SplitCsvLine(line);

            string rawCity = idxCity >= 0 && idxCity < cols.Count ? cols[idxCity] : string.Empty;
            string cityAscii = idxCityAscii >= 0 && idxCityAscii < cols.Count ? cols[idxCityAscii] : string.Empty;
            string cityName = string.IsNullOrWhiteSpace(cityAscii) ? rawCity : cityAscii;
            string country = idxCountry >= 0 && idxCountry < cols.Count ? cols[idxCountry] : string.Empty;

            if (!(idxLat >= 0 && idxLat < cols.Count && idxLng >= 0 && idxLng < cols.Count))
                continue;

            if (!double.TryParse(cols[idxLat], NumberStyles.Float, CultureInfo.InvariantCulture, out double lat))
                continue;
            if (!double.TryParse(cols[idxLng], NumberStyles.Float, CultureInfo.InvariantCulture, out double lng))
                continue;

            if (string.IsNullOrWhiteSpace(cityName))
                continue;

            yield return new City(cityName, country, lat, lng);
        }
    }

    private sealed record City(string Name, string Country, double Latitude, double Longitude)
    {
        public string Display => $"{Name}"; // , ({Latitude:0.####}, {Longitude:0.####})";
    }
}
