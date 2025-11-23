// Author: Ian Cooper
// Date: 23 November 2025
// Notes: C# port of the Python TDD kata for Conway's Game of Life

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
        var values = _reader.ReadSeedFile();
        var board = new Board(values.generation, values.size, values.cells);

        _writer.WriteBoard(board);

        for (int i = 0; i < runs; i++)
        {
            board = board.Tick();
            _writer.WriteBoard(board);
        }
    }
}
