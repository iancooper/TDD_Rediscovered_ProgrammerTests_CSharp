// Author: Ian Cooper
// Date: 23 November 2025
// Notes: C# port of the Python TDD kata for Conway's Game of Life

namespace Conway.Core;

/// <summary>
/// Writer interface for writing boards
/// </summary>
public interface IWriter
{
    void WriteBoard(Board board);
}

/// <summary>
/// Writes a board to the console
/// </summary>
public class BoardWriter : IWriter
{
    public void WriteBoard(Board board)
    {
        Console.WriteLine();
        Console.WriteLine(board.ToString());
        Console.WriteLine();
    }
}
