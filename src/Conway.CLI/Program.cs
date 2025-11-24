// Author: Ian Cooper
// Date: 24 November 2025
// Notes: CLI for Conway's Game of Life - now a thin wrapper around the API

using Conway.CLI;
using Spectre.Console;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

class Program
{
    static async Task Main(string[] args)
    {
        AnsiConsole.Write(
            new FigletText("Conway's Game of Life")
                .LeftJustified()
                .Color(Color.Green));

        AnsiConsole.MarkupLine("[dim]A C# implementation using TDD and Clean Architecture[/]");
        AnsiConsole.WriteLine();

        // Get API URL
        var apiUrl = AnsiConsole.Ask("Enter the [green]API URL[/]:", "https://localhost:5001");

        // Get seed file name
        var seedFile = AnsiConsole.Ask("Enter the [green]seed file[/] path:", "seed.txt");

        if (!File.Exists(seedFile))
        {
            AnsiConsole.MarkupLine($"[red]Error: File '{seedFile}' not found![/]");
            return;
        }

        // Get number of generations
        var generations = AnsiConsole.Ask("How many [green]generations[/] to run?", 3);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Reading seed file...[/]");

        // Read the seed file
        var reader = new SeedReader(seedFile);
        var (generation, size, cells) = reader.ReadSeedFile();

        AnsiConsole.MarkupLine($"[dim]Loaded board: {size.rows}x{size.cols}, generation {generation}[/]");
        AnsiConsole.WriteLine();

        // Display initial board
        DisplayBoard(cells, new SizeDto { Rows = size.rows, Cols = size.cols }, generation, "Initial State");

        // Create request
        var request = new GameRequest
        {
            Generation = generation,
            Size = new SizeDto { Rows = size.rows, Cols = size.cols },
            Cells = cells,
            Runs = generations
        };

        AnsiConsole.MarkupLine("[yellow]Sending request to API...[/]");

        // Call the API
        try
        {
            using var httpClient = new HttpClient
            {
                BaseAddress = new Uri(apiUrl)
            };

            // Configure JSON options to handle multi-dimensional arrays
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new MultiDimensionalArrayConverter() }
            };

            var response = await httpClient.PostAsJsonAsync("/api/game/run", request, options);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<GameResponse>(options);

                if (result != null)
                {
                    AnsiConsole.MarkupLine("[green]Simulation complete![/]");
                    AnsiConsole.WriteLine();

                    // Display final board
                    DisplayBoard(result.Cells, new SizeDto { Rows = result.Size.Rows, Cols = result.Size.Cols },
                        result.Generation, "Final State");
                }
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]API Error: {response.StatusCode}[/]");
                var errorContent = await response.Content.ReadAsStringAsync();
                AnsiConsole.MarkupLine($"[red]{errorContent}[/]");
            }
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Connection Error: {ex.Message}[/]");
            AnsiConsole.MarkupLine("[yellow]Make sure the API is running![/]");
        }
    }

    static void DisplayBoard(char[,] cells, SizeDto size, int generation, string title)
    {
        var gridLines = new List<string>();
        for (int r = 0; r < size.Rows; r++)
        {
            var line = "";
            for (int c = 0; c < size.Cols; c++)
            {
                line += cells[r, c] == '*' ? "[green]█[/] " : "[dim]·[/] ";
            }
            gridLines.Add(line.TrimEnd());
        }

        var panel = new Panel(string.Join("\n", gridLines))
        {
            Header = new PanelHeader($"{title} - Generation {generation}"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }
}

/// <summary>
/// Custom JSON converter for multi-dimensional arrays
/// </summary>
public class MultiDimensionalArrayConverter : JsonConverter<char[,]>
{
    public override char[,] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();

        var rows = new List<List<char>>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                break;

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var row = new List<char>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;

                    row.Add(reader.GetString()?[0] ?? '.');
                }
                rows.Add(row);
            }
        }

        if (rows.Count == 0)
            return new char[0, 0];

        var result = new char[rows.Count, rows[0].Count];
        for (int i = 0; i < rows.Count; i++)
        {
            for (int j = 0; j < rows[i].Count; j++)
            {
                result[i, j] = rows[i][j];
            }
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, char[,] value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        for (int i = 0; i < value.GetLength(0); i++)
        {
            writer.WriteStartArray();
            for (int j = 0; j < value.GetLength(1); j++)
            {
                writer.WriteStringValue(value[i, j].ToString());
            }
            writer.WriteEndArray();
        }

        writer.WriteEndArray();
    }
}
