// Author: Ian Cooper
// Date: 24 November 2025
// Notes: API DTOs for CLI to communicate with Conway.API

namespace Conway.CLI;

/// <summary>
/// Request model for running Conway's Game of Life
/// </summary>
public record GameRequest
{
    public int Generation { get; init; }
    public SizeDto Size { get; init; } = new();
    public char[,] Cells { get; init; } = new char[0, 0];
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
    public int Generation { get; init; }
    public SizeDto Size { get; init; } = new();
    public char[,] Cells { get; init; } = new char[0, 0];
    public string BoardString { get; init; } = string.Empty;
}
