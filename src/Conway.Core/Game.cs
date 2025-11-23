// Author: Ian Cooper
// Date: 23 November 2025
// Notes: C# port of the Python TDD kata for Conway's Game of Life

using System.Diagnostics;

namespace Conway.Core;

/// <summary>
/// Game orchestrates the Game of Life simulation
/// This is the use case layer in Clean Architecture
/// </summary>
public class Game
{
    private readonly IReader _reader;
    private readonly IWriter _writer;

    public Game(IReader reader, IWriter writer)
    {
        _reader = reader;
        _writer = writer;
    }

    public void Play(int runs = 1)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("Game.Play");
        activity?.SetTag("game.runs", runs);

        var values = _reader.ReadSeedFile();
        var board = new Board(values.generation, values.size, values.cells);

        activity?.SetTag("game.initial_generation", values.generation);
        activity?.SetTag("game.board_size", $"{values.size.rows}x{values.size.cols}");

        WriteBoard(board, "initial");

        for (int i = 0; i < runs; i++)
        {
            board = board.Tick();
            WriteBoard(board, $"generation_{i + 1}");
        }

        activity?.SetTag("game.final_generation", board.GetGeneration());
    }

    private void WriteBoard(Board board, string stage)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("Game.WriteBoard");
        activity?.SetTag("board.generation", board.GetGeneration());
        activity?.SetTag("board.stage", stage);

        _writer.WriteBoard(board);
    }
}

/// <summary>
/// Extension methods for Board to access internal state for telemetry
/// </summary>
internal static class BoardTelemetryExtensions
{
    public static int GetGeneration(this Board board)
    {
        var boardType = board.GetType();
        var generationField = boardType.GetField("_generation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (int)generationField!.GetValue(board)!;
    }
}
