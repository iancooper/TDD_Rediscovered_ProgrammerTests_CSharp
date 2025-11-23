// Author: Ian Cooper
// Date: 23 November 2025
// Notes: Tests for Game use case using OpenTelemetry traces to verify behavior

using Conway.Core;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using Xunit;

namespace Conway.Tests;

public class GameTests : IDisposable
{
    private readonly List<Activity> _exportedActivities = new();
    private readonly TracerProvider _tracerProvider;

    public GameTests()
    {
        // Set up OpenTelemetry with InMemory exporter to capture traces
        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(Telemetry.ServiceName))
            .AddSource(Telemetry.ServiceName)
            .AddInMemoryExporter(_exportedActivities)
            .Build()!;
    }

    public void Dispose()
    {
        _tracerProvider?.Dispose();
    }

    [Fact]
    public void GameOfLife()
    {
        // Run an iteration of game of life
        // We verify the flow through OpenTelemetry traces

        var reader = new FakeReader();
        var writer = new NoOpWriter(); // We don't need to track writes anymore

        var game = new Game(reader, writer);

        game.Play();

        // Force flush to ensure all activities are exported
        _tracerProvider.ForceFlush();

        // Verify we have the expected spans
        Assert.NotEmpty(_exportedActivities);

        // Should have: 1 Game.Play + 2 Game.WriteBoard (initial + generation 1) + 1 Board.Tick
        var gamePlaySpans = _exportedActivities.Where(a => a.DisplayName == "Game.Play").ToList();
        var writeBoardSpans = _exportedActivities.Where(a => a.DisplayName == "Game.WriteBoard").ToList();
        var boardTickSpans = _exportedActivities.Where(a => a.DisplayName == "Board.Tick").ToList();

        Assert.Single(gamePlaySpans);
        Assert.Equal(2, writeBoardSpans.Count);
        Assert.Single(boardTickSpans);

        // Verify Game.Play span attributes
        var gamePlaySpan = gamePlaySpans[0];
        Assert.Equal("1", gamePlaySpan.GetTagItem("game.runs")?.ToString());
        Assert.Equal("0", gamePlaySpan.GetTagItem("game.initial_generation")?.ToString());
        Assert.Equal("3x3", gamePlaySpan.GetTagItem("game.board_size")?.ToString());
        Assert.Equal("1", gamePlaySpan.GetTagItem("game.final_generation")?.ToString());

        // Verify WriteBoard spans
        var initialWrite = writeBoardSpans.FirstOrDefault(a => a.GetTagItem("board.stage")?.ToString() == "initial");
        var generation1Write = writeBoardSpans.FirstOrDefault(a => a.GetTagItem("board.stage")?.ToString() == "generation_1");

        Assert.NotNull(initialWrite);
        Assert.NotNull(generation1Write);

        Assert.Equal("0", initialWrite.GetTagItem("board.generation")?.ToString());
        Assert.Equal("1", generation1Write.GetTagItem("board.generation")?.ToString());

        // Verify Board.Tick span
        var tickSpan = boardTickSpans[0];
        Assert.Equal("0", tickSpan.GetTagItem("board.current_generation")?.ToString());
        Assert.Equal("1", tickSpan.GetTagItem("board.next_generation")?.ToString());
        Assert.Equal("3x3", tickSpan.GetTagItem("board.size")?.ToString());
        Assert.Equal("5", tickSpan.GetTagItem("board.live_cells_processed")?.ToString());
        Assert.Equal("4", tickSpan.GetTagItem("board.dead_cells_processed")?.ToString());
    }

    [Fact]
    public void GameOfLifeMultipleRuns()
    {
        // Test running multiple generations

        var reader = new FakeReader();
        var writer = new NoOpWriter();

        var game = new Game(reader, writer);

        game.Play(runs: 3);

        _tracerProvider.ForceFlush();

        // Should have: 1 Game.Play + 4 Game.WriteBoard (initial + 3 generations) + 3 Board.Tick
        var gamePlaySpans = _exportedActivities.Where(a => a.DisplayName == "Game.Play").ToList();
        var writeBoardSpans = _exportedActivities.Where(a => a.DisplayName == "Game.WriteBoard").ToList();
        var boardTickSpans = _exportedActivities.Where(a => a.DisplayName == "Board.Tick").ToList();

        Assert.Single(gamePlaySpans);
        Assert.Equal(4, writeBoardSpans.Count);
        Assert.Equal(3, boardTickSpans.Count);

        // Verify generation progression in Game.Play
        var gamePlaySpan = gamePlaySpans[0];
        Assert.Equal("3", gamePlaySpan.GetTagItem("game.runs")?.ToString());
        Assert.Equal("0", gamePlaySpan.GetTagItem("game.initial_generation")?.ToString());
        Assert.Equal("3", gamePlaySpan.GetTagItem("game.final_generation")?.ToString());

        // Verify WriteBoard stages
        var stages = writeBoardSpans.Select(a => a.GetTagItem("board.stage")?.ToString()).ToList();
        Assert.Contains("initial", stages);
        Assert.Contains("generation_1", stages);
        Assert.Contains("generation_2", stages);
        Assert.Contains("generation_3", stages);

        // Verify Board.Tick progression
        var tickGenerations = boardTickSpans
            .Select(a => a.GetTagItem("board.current_generation")?.ToString())
            .OrderBy(g => g)
            .ToList();

        Assert.Equal(new[] { "0", "1", "2" }, tickGenerations);
    }

    [Fact]
    public void GameTracesCorrectBoardTransformation()
    {
        // Specifically test that the board transformation is correctly traced
        // Using a simple pattern: single live cell (should die)

        var reader = new FakeReaderWithSingleCell();
        var writer = new NoOpWriter();

        var game = new Game(reader, writer);

        game.Play();

        _tracerProvider.ForceFlush();

        var boardTickSpans = _exportedActivities.Where(a => a.DisplayName == "Board.Tick").ToList();
        Assert.Single(boardTickSpans);

        var tickSpan = boardTickSpans[0];

        // Single cell in 3x3 grid: 1 live, 8 dead
        Assert.Equal("1", tickSpan.GetTagItem("board.live_cells_processed")?.ToString());
        Assert.Equal("8", tickSpan.GetTagItem("board.dead_cells_processed")?.ToString());

        // Verify generation transition
        Assert.Equal("0", tickSpan.GetTagItem("board.current_generation")?.ToString());
        Assert.Equal("1", tickSpan.GetTagItem("board.next_generation")?.ToString());
    }

    [Fact]
    public void GameTracesShowCorrectSpanHierarchy()
    {
        // Verify that spans have correct parent-child relationships

        var reader = new FakeReader();
        var writer = new NoOpWriter();

        var game = new Game(reader, writer);

        game.Play();

        _tracerProvider.ForceFlush();

        // Game.Play should be the root span
        var gamePlaySpan = _exportedActivities.FirstOrDefault(a => a.DisplayName == "Game.Play");
        Assert.NotNull(gamePlaySpan);

        // WriteBoard and Board.Tick should be children of Game.Play
        var writeBoardSpans = _exportedActivities.Where(a => a.DisplayName == "Game.WriteBoard").ToList();
        var boardTickSpans = _exportedActivities.Where(a => a.DisplayName == "Board.Tick").ToList();

        foreach (var writeSpan in writeBoardSpans)
        {
            Assert.Equal(gamePlaySpan.TraceId, writeSpan.TraceId);
            Assert.Equal(gamePlaySpan.SpanId, writeSpan.ParentSpanId);
        }

        foreach (var tickSpan in boardTickSpans)
        {
            Assert.Equal(gamePlaySpan.TraceId, tickSpan.TraceId);
            Assert.Equal(gamePlaySpan.SpanId, tickSpan.ParentSpanId);
        }
    }
}

/// <summary>
/// Fake reader that returns a known board configuration
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
/// No-op writer - we don't need to track writes anymore, traces tell us everything
/// </summary>
internal class NoOpWriter : IWriter
{
    public void WriteBoard(Board board)
    {
        // No-op - telemetry captures the writes
    }
}
