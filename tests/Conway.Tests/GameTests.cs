// Author: Ian Cooper
// Date: 23 November 2025
// Notes: Tests for Game use case following the Python test approach

using Conway.Core;
using Xunit;

namespace Conway.Tests;

public class GameTests
{
    [Fact]
    public void GameOfLife()
    {
        // Run an iteration of game of life
        // We are going to use fakes for the boundaries/adapters - this is fine

        var reader = new FakeReader();
        var writer = new FakeWriter();

        var game = new Game(reader, writer);

        game.Play();

        // Should have written 2 boards: initial (generation 0) and after one tick (generation 1)
        Assert.Equal(2, writer.BoardsWritten.Count);

        // Verify the initial board (generation 0) was written first
        var initialBoard = new Board(0, (3, 3), new[,]
        {
            { '.', '*', '.' },
            { '*', '*', '*' },
            { '.', '*', '.' }
        });
        Assert.Equal(initialBoard, writer.BoardsWritten[0]);

        // Verify the transformed board (generation 1) was written after one tick
        var expectedTransformedBoard = new Board(1, (3, 3), new[,]
        {
            { '*', '*', '*' },
            { '*', '.', '*' },
            { '*', '*', '*' }
        });
        Assert.Equal(expectedTransformedBoard, writer.BoardsWritten[1]);
    }

    [Fact]
    public void GameOfLifeMultipleRuns()
    {
        // Test running multiple generations

        var reader = new FakeReader();
        var writer = new FakeWriter();

        var game = new Game(reader, writer);

        game.Play(runs: 3);

        // Should have written 4 boards: initial + 3 generations
        Assert.Equal(4, writer.BoardsWritten.Count);

        // Verify generation sequence
        Assert.Equal(0, writer.BoardsWritten[0].GetGeneration());
        Assert.Equal(1, writer.BoardsWritten[1].GetGeneration());
        Assert.Equal(2, writer.BoardsWritten[2].GetGeneration());
        Assert.Equal(3, writer.BoardsWritten[3].GetGeneration());
    }

    [Fact]
    public void GameWritesCorrectTransformedBoard()
    {
        // Specifically test that the board transformation is correct
        // Using a simple pattern: single live cell (should die)

        var reader = new FakeReaderWithSingleCell();
        var writer = new FakeWriter();

        var game = new Game(reader, writer);

        game.Play();

        // Initial board has one live cell
        Assert.Equal(2, writer.BoardsWritten.Count);

        var initialBoard = writer.BoardsWritten[0];
        Assert.True(HasLiveCellAt(initialBoard, 1, 1));

        // After one tick, the single cell should die (underpopulation)
        var transformedBoard = writer.BoardsWritten[1];
        Assert.False(HasLiveCellAt(transformedBoard, 1, 1));
    }

    private bool HasLiveCellAt(Board board, int row, int col)
    {
        var boardString = board.ToString();
        var lines = boardString.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Skip generation (line 0) and size (line 1) lines
        if (row + 2 < lines.Length && col < lines[row + 2].Length)
        {
            return lines[row + 2][col] == '*';
        }

        return false;
    }
}

/// <summary>
/// Fake reader that returns a known board configuration
/// This matches the Python test pattern
/// </summary>
internal class FakeReader : IReader
{
    public (int generation, (int rows, int cols) size, char[,] cells) ReadSeedFile()
    {
        // Returns a cross pattern
        var cells = new char[3, 3]
        {
            { '.', '*', '.' },
            { '*', '*', '*' },
            { '.', '*', '.' }
        };

        return (0, (3, 3), cells);
    }
}

/// <summary>
/// Fake reader that returns a single cell
/// </summary>
internal class FakeReaderWithSingleCell : IReader
{
    public (int generation, (int rows, int cols) size, char[,] cells) ReadSeedFile()
    {
        var cells = new char[3, 3]
        {
            { '.', '.', '.' },
            { '.', '*', '.' },
            { '.', '.', '.' }
        };

        return (0, (3, 3), cells);
    }
}

/// <summary>
/// Fake writer that captures boards for verification
/// </summary>
internal class FakeWriter : IWriter
{
    public List<Board> BoardsWritten { get; } = new List<Board>();

    public void WriteBoard(Board board)
    {
        BoardsWritten.Add(board);
    }
}

/// <summary>
/// Extension methods to access internal Board state for testing
/// </summary>
internal static class BoardTestExtensions
{
    public static int GetGeneration(this Board board)
    {
        var boardType = board.GetType();
        var generationField = boardType.GetField("_generation",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (int)generationField!.GetValue(board)!;
    }
}
