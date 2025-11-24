// Author: Ian Cooper
// Date: 24 November 2025
// Notes: Conway's Game of Life Minimal API with OpenTelemetry

using Conway.API;
using Conway.Core;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(Telemetry.ServiceName, serviceVersion: Telemetry.ServiceVersion))
            .AddSource(Telemetry.ServiceName)
            .AddAspNetCoreInstrumentation()
            .AddConsoleExporter();
    });

// Add GameEngine as a service
builder.Services.AddSingleton<GameEngine>();

// Configure JSON serialization for multi-dimensional arrays
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new MultiDimensionalArrayConverter());
});

// Add OpenAPI/Swagger for development
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Conway's Game of Life API endpoint
app.MapPost("/api/game/run", (GameRequest request, GameEngine engine) =>
{
    // Create initial board from request
    var board = new Board(request.Generation, (request.Size.Rows, request.Size.Cols), request.Cells);

    // Run the game for specified generations
    var finalBoard = engine.RunGenerations(board, request.Runs);

    // Build response
    var size = finalBoard.GetSize();
    var response = new GameResponse
    {
        Generation = finalBoard.GetGeneration(),
        Size = new SizeDto { Rows = size.rows, Cols = size.cols },
        Cells = finalBoard.GetCells(),
        BoardString = finalBoard.ToString()
    };

    return Results.Ok(response);
})
.WithName("RunGame")
.WithDescription("Run Conway's Game of Life for a specified number of generations");

app.Run();

// Make Program partial for integration tests
public partial class Program { }
