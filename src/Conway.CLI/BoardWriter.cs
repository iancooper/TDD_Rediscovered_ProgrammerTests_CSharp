// Author: Ian Cooper
// Date: 24 November 2025
// Notes: Moved from Conway.Core to Conway.CLI - now a CLI adapter concern

namespace Conway.CLI;

/// <summary>
/// Writes a board string representation to the console
/// </summary>
public class BoardWriter
{
    public void WriteBoard(string boardString)
    {
        Console.WriteLine();
        Console.WriteLine(boardString);
        Console.WriteLine();
    }
}
