// Author: Ian Cooper
// Date: 23 November 2025
// Notes: C# port of the Python TDD kata for Conway's Game of Life

namespace Conway.Core;

/// <summary>
/// Reader interface for reading seed files
/// </summary>
public interface IReader
{
    (int generation, (int rows, int cols) size, char[,] cells) ReadSeedFile();
}

/// <summary>
/// Reads a seed file for Conway's Game of Life
/// File format:
/// Generation {number}
/// {rows} {cols}
/// {grid of . and * characters}
/// </summary>
public class SeedReader : IReader
{
    private readonly string _fileName;

    public SeedReader(string fileName)
    {
        _fileName = fileName;
    }

    public (int generation, (int rows, int cols) size, char[,] cells) ReadSeedFile()
    {
        var seed = File.ReadAllText(_fileName);
        var lines = seed.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Parse generation (e.g., "Generation 0")
        var generation = int.Parse(lines[0].Substring(11));

        // Parse size (e.g., "3 3")
        var sizeParts = lines[1].Split(' ');
        var rows = int.Parse(sizeParts[0]);
        var cols = int.Parse(sizeParts[1]);
        var size = (rows, cols);

        // Parse cells
        var cells = new char[rows, cols];
        for (int r = 0; r < rows && r + 2 < lines.Length; r++)
        {
            var row = lines[r + 2];
            for (int c = 0; c < cols && c < row.Length; c++)
            {
                cells[r, c] = row[c];
            }
        }

        return (generation, size, cells);
    }
}
