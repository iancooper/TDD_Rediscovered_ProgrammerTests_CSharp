// Author: Ian Cooper
// Date: 23 November 2025
// Notes: CLI for Conway's Game of Life using Clean Architecture with OpenTelemetry

using Conway.Core;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Spectre.Console;

namespace Conway.CLI;

class Program
{
    static void Main(string[] args)
    {
        // Set up OpenTelemetry tracing
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(Telemetry.ServiceName, serviceVersion: Telemetry.ServiceVersion))
            .AddSource(Telemetry.ServiceName)
            .AddConsoleExporter()
            .Build();

        AnsiConsole.Write(
            new FigletText("Conway's Game of Life")
                .LeftJustified()
                .Color(Color.Green));

        AnsiConsole.MarkupLine("[dim]A C# implementation using TDD, Clean Architecture, and OpenTelemetry[/]");
        AnsiConsole.WriteLine();

        // Get seed file name
        var seedFile = AnsiConsole.Ask("Enter the [green]seed file[/] path:", "seed.txt");

        if (!File.Exists(seedFile))
        {
            AnsiConsole.MarkupLine($"[red]Error: File '{seedFile}' not found![/]");
            return;
        }

        // Get number of generations
        var generations = AnsiConsole.Ask("How many [green]generations[/] to run?", 3);

        // Ask if tracing should be enabled
        var enableTracing = AnsiConsole.Confirm("Enable [yellow]OpenTelemetry trace output[/]?", false);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Starting simulation...[/]");
        if (enableTracing)
        {
            AnsiConsole.MarkupLine("[dim]OpenTelemetry traces will be written to console[/]");
        }
        AnsiConsole.WriteLine();

        // Create the game with dependencies injected
        var reader = new SeedReader(seedFile);
        var writer = new SpectreConsoleWriter();
        var game = new Game(reader, writer);

        // Run the game
        game.Play(generations);

        // Force flush to ensure all telemetry is exported
        tracerProvider?.ForceFlush();

        AnsiConsole.MarkupLine("[green]Simulation complete![/]");

        if (enableTracing)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Traces exported above show the complete flow of the game[/]");
        }
    }
}

/// <summary>
/// Writes boards to the console using Spectre.Console for beautiful rendering
/// </summary>
class SpectreConsoleWriter : IWriter
{
    public void WriteBoard(Board board)
    {
        var boardString = board.ToString();
        var lines = boardString.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Display board with Spectre.Console styling
        var panel = new Panel(RenderBoard(lines))
        {
            Header = new PanelHeader(lines[0]),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Cyan1)
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
        Thread.Sleep(500);
    }

    private string RenderBoard(string[] lines)
    {
        // Skip the generation and size lines, just render the grid
        var gridLines = new List<string>();
        for (int i = 2; i < lines.Length; i++)
        {
            var line = "";
            foreach (var c in lines[i])
            {
                line += c == '*' ? "[green]█[/] " : "[dim]·[/] ";
            }
            gridLines.Add(line.TrimEnd());
        }

        return string.Join("\n", gridLines);
    }
}
