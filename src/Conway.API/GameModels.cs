// Author: Ian Cooper
// Date: 24 November 2025
// Notes: DTOs for Conway Game of Life API

namespace Conway.API;

/// <summary>
/// Request model for running Conway's Game of Life
/// </summary>
public record GameRequest
{
    /// <summary>
    /// The starting generation number
    /// </summary>
    public int Generation { get; init; }

    /// <summary>
    /// The size of the board (rows, cols)
    /// </summary>
    public SizeDto Size { get; init; } = new();

    /// <summary>
    /// The initial cell configuration (. for dead, * for alive)
    /// </summary>
    public char[,] Cells { get; init; } = new char[0, 0];

    /// <summary>
    /// Number of generations to run
    /// </summary>
    public int Runs { get; init; } = 1;
}

/// <summary>
/// Size DTO for JSON serialization
/// </summary>
public record SizeDto
{
    public int Rows { get; init; }
    public int Cols { get; init; }
}

/// <summary>
/// Response model for Conway's Game of Life
/// </summary>
public record GameResponse
{
    /// <summary>
    /// The final generation number
    /// </summary>
    public int Generation { get; init; }

    /// <summary>
    /// The size of the board
    /// </summary>
    public SizeDto Size { get; init; } = new();

    /// <summary>
    /// The final cell configuration
    /// </summary>
    public char[,] Cells { get; init; } = new char[0, 0];

    /// <summary>
    /// String representation of the board
    /// </summary>
    public string BoardString { get; init; } = string.Empty;
}
