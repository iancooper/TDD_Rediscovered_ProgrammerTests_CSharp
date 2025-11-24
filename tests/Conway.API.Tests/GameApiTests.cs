// Author: Ian Cooper
// Date: 24 November 2025
// Notes: Integration tests for Conway Game of Life API with telemetry verification

using Conway.API;
using Conway.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Conway.API.Tests;

/// <summary>
/// Custom WebApplicationFactory that configures InMemory telemetry exporter for testing
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public List<Activity> ExportedActivities { get; } = new();
    private TracerProvider? _tracerProvider;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing TracerProvider configuration and add our test one
            var openTelemetryBuilder = services.AddOpenTelemetry();
            openTelemetryBuilder.WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(Telemetry.ServiceName))
                    .AddSource(Telemetry.ServiceName)
                    .AddAspNetCoreInstrumentation()
                    .AddInMemoryExporter(ExportedActivities);
            });
        });
    }

    public void ForceFlush()
    {
        var tracerProvider = Services.GetService<TracerProvider>();
        tracerProvider?.ForceFlush();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tracerProvider?.Dispose();
        }
        base.Dispose(disposing);
    }
}

public class GameApiTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GameApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        // Clear activities from previous tests
        _factory.ExportedActivities.Clear();
    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    private List<Activity> GetExportedActivities() => _factory.ExportedActivities;

    [Fact]
    public async Task ApiRunsGameOfLifeSingleGeneration()
    {
        // Arrange - create a request with a cross pattern
        var request = new GameRequest
        {
            Generation = 0,
            Size = new SizeDto { Rows = 3, Cols = 3 },
            Cells = new char[3, 3]
            {
                { '.', '*', '.' },
                { '*', '*', '*' },
                { '.', '*', '.' }
            },
            Runs = 1
        };

        // Configure JSON options to handle multi-dimensional arrays
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new MultiDimensionalArrayConverter() }
        };

        // Act - POST to the API
        var response = await _client.PostAsJsonAsync("/api/game/run", request, options);

        // Assert - verify response
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"API returned {response.StatusCode}: {content}");

        var result = await response.Content.ReadFromJsonAsync<GameResponse>(options);
        Assert.NotNull(result);
        Assert.Equal(1, result.Generation);
        Assert.Equal(3, result.Size.Rows);
        Assert.Equal(3, result.Size.Cols);
    }

    [Fact]
    public async Task ApiRunsGameOfLifeMultipleGenerations()
    {
        // Arrange
        var request = new GameRequest
        {
            Generation = 0,
            Size = new SizeDto { Rows = 3, Cols = 3 },
            Cells = new char[3, 3]
            {
                { '.', '*', '.' },
                { '*', '*', '*' },
                { '.', '*', '.' }
            },
            Runs = 3
        };

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new MultiDimensionalArrayConverter() }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/game/run", request, options);

        // Assert
        Assert.True(response.IsSuccessStatusCode);

        var result = await response.Content.ReadFromJsonAsync<GameResponse>(options);
        Assert.NotNull(result);
        Assert.Equal(3, result.Generation);
    }

    [Fact]
    public async Task ApiCapturesTelemetryForGameExecution()
    {
        // Arrange
        var request = new GameRequest
        {
            Generation = 0,
            Size = new SizeDto { Rows = 3, Cols = 3 },
            Cells = new char[3, 3]
            {
                { '.', '*', '.' },
                { '*', '*', '*' },
                { '.', '*', '.' }
            },
            Runs = 2
        };

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new MultiDimensionalArrayConverter() }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/game/run", request, options);
        Assert.True(response.IsSuccessStatusCode);

        // Force flush to ensure all activities are exported
        _factory.ForceFlush();

        // Assert - verify telemetry spans were captured
        var exportedActivities = GetExportedActivities();
        Assert.NotEmpty(exportedActivities);

        // Should have: 1 GameEngine.RunGenerations + 2 Board.Tick (one per generation)
        var gameEngineSpans = exportedActivities.Where(a => a.DisplayName == "GameEngine.RunGenerations").ToList();
        var boardTickSpans = exportedActivities.Where(a => a.DisplayName == "Board.Tick").ToList();

        Assert.Single(gameEngineSpans);
        Assert.Equal(2, boardTickSpans.Count);

        // Verify GameEngine.RunGenerations span attributes
        var gameEngineSpan = gameEngineSpans[0];
        Assert.Equal("2", gameEngineSpan.GetTagItem("game.runs")?.ToString());
        Assert.Equal("0", gameEngineSpan.GetTagItem("game.initial_generation")?.ToString());
        Assert.Equal("3x3", gameEngineSpan.GetTagItem("game.board_size")?.ToString());
        Assert.Equal("2", gameEngineSpan.GetTagItem("game.final_generation")?.ToString());

        // Verify Board.Tick spans progression
        var tickGenerations = boardTickSpans
            .Select(a => a.GetTagItem("board.current_generation")?.ToString())
            .OrderBy(g => g)
            .ToList();

        Assert.Equal(new[] { "0", "1" }, tickGenerations);
    }

    [Fact]
    public async Task ApiTelemetryShowsCorrectSpanHierarchy()
    {
        // Arrange
        var request = new GameRequest
        {
            Generation = 0,
            Size = new SizeDto { Rows = 3, Cols = 3 },
            Cells = new char[3, 3]
            {
                { '.', '.', '.' },
                { '.', '*', '.' },
                { '.', '.', '.' }
            },
            Runs = 1
        };

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new MultiDimensionalArrayConverter() }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/game/run", request, options);
        Assert.True(response.IsSuccessStatusCode);

        _factory.ForceFlush();

        // Assert - verify span hierarchy
        var exportedActivities = GetExportedActivities();
        var gameEngineSpan = exportedActivities.FirstOrDefault(a => a.DisplayName == "GameEngine.RunGenerations");
        Assert.NotNull(gameEngineSpan);

        // Board.Tick should be a child of GameEngine.RunGenerations
        var boardTickSpans = exportedActivities.Where(a => a.DisplayName == "Board.Tick").ToList();

        foreach (var tickSpan in boardTickSpans)
        {
            Assert.Equal(gameEngineSpan.TraceId, tickSpan.TraceId);
            Assert.Equal(gameEngineSpan.SpanId, tickSpan.ParentSpanId);
        }
    }

    [Fact]
    public async Task ApiTelemetryTracksCellProcessing()
    {
        // Arrange - single cell should die (underpopulation)
        var request = new GameRequest
        {
            Generation = 0,
            Size = new SizeDto { Rows = 3, Cols = 3 },
            Cells = new char[3, 3]
            {
                { '.', '.', '.' },
                { '.', '*', '.' },
                { '.', '.', '.' }
            },
            Runs = 1
        };

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new MultiDimensionalArrayConverter() }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/game/run", request, options);
        Assert.True(response.IsSuccessStatusCode);

        _factory.ForceFlush();

        // Assert - verify cell processing telemetry
        var exportedActivities = GetExportedActivities();
        var boardTickSpans = exportedActivities.Where(a => a.DisplayName == "Board.Tick").ToList();
        Assert.Single(boardTickSpans);

        var tickSpan = boardTickSpans[0];

        // Single cell in 3x3 grid: 1 live, 8 dead
        Assert.Equal("1", tickSpan.GetTagItem("board.live_cells_processed")?.ToString());
        Assert.Equal("8", tickSpan.GetTagItem("board.dead_cells_processed")?.ToString());

        // Verify generation transition
        Assert.Equal("0", tickSpan.GetTagItem("board.current_generation")?.ToString());
        Assert.Equal("1", tickSpan.GetTagItem("board.next_generation")?.ToString());
    }
}
