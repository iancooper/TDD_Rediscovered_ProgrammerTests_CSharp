// Author: Ian Cooper
// Date: 24 November 2025
// Notes: Core game engine for Conway's Game of Life - refactored from Game class

using Conway.Core;
using System.Diagnostics;

namespace Conway.API;

/// <summary>
/// GameEngine orchestrates the Game of Life simulation
/// This is the use case layer in Clean Architecture, refactored for API use
/// </summary>
public class GameEngine
{
    public Board RunGenerations(Board initialBoard, int runs = 1)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("GameEngine.RunGenerations");
        activity?.SetTag("game.runs", runs);
        activity?.SetTag("game.initial_generation", initialBoard.GetGeneration());
        activity?.SetTag("game.board_size", $"{initialBoard.GetSize().rows}x{initialBoard.GetSize().cols}");

        var board = initialBoard;

        for (int i = 0; i < runs; i++)
        {
            board = board.Tick();
        }

        activity?.SetTag("game.final_generation", board.GetGeneration());

        return board;
    }
}

/// <summary>
/// Extension methods for Board to access internal state for telemetry and API use
/// </summary>
public static class BoardExtensions
{
    public static int GetGeneration(this Board board)
    {
        var boardType = board.GetType();
        var generationField = boardType.GetField("_generation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (int)generationField!.GetValue(board)!;
    }

    public static (int rows, int cols) GetSize(this Board board)
    {
        var boardType = board.GetType();
        var sizeField = boardType.GetField("_size",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return ((int rows, int cols))sizeField!.GetValue(board)!;
    }

    public static char[,] GetCells(this Board board)
    {
        var size = board.GetSize();
        var cells = new char[size.rows, size.cols];

        // Parse from ToString() output
        var lines = board.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Skip generation and size lines (first 2 lines)
        for (int r = 0; r < size.rows && r + 2 < lines.Length; r++)
        {
            var line = lines[r + 2];
            for (int c = 0; c < size.cols && c < line.Length; c++)
            {
                cells[r, c] = line[c];
            }
        }

        return cells;
    }
}
