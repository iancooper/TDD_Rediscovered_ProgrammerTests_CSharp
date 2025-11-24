# TDD Rediscovered - ProgrammerTests (C# Version)

## What is this?

This is a C# implementation of [Conway's Game of Life](https://github.com/iancooper/GameOfLife), ported from the Python version used to support the course: TDD Rediscovered.

It demonstrates the same TDD principles and testing approach as the Python version, adapted to C# idioms and conventions.

## The Rules

The code was created as a kata, using the following rules:

* We are writing [Programmer Tests](https://wiki.c2.com/?ProgrammerTest). A failing test tells us that the last edit to the source is the source of an issue

* The prompt to write a test is a new requirement. We assume that we obtained these when elaborating the story with the customer.

* When we discover that we need new collaborators by refactoring we ask: are they part of our public interface or an implementation detail. We don't test implementation details

## Project Structure

```
TDD_Rediscovered_ProgrammerTests_CSharp/
├── Conway.sln                      # Solution file
├── seed.txt                        # Example seed file (blinker pattern)
├── glider.txt                      # Example seed file (glider pattern)
├── src/
│   ├── Conway.Core/                # Core library with game logic
│   │   ├── Board.cs                # Board entity and internal Row, Cell, Neighbours classes
│   │   └── Telemetry.cs            # OpenTelemetry configuration
│   ├── Conway.API/                 # Minimal API for running the game
│   │   ├── Program.cs              # API entry point with endpoints
│   │   ├── GameEngine.cs           # Game engine (orchestration layer)
│   │   ├── GameModels.cs           # API DTOs (GameRequest, GameResponse)
│   │   └── JsonConverters.cs       # Custom JSON converters for multi-dimensional arrays
│   └── Conway.CLI/                 # CLI application - thin wrapper around API
│       ├── Program.cs              # Main entry point - reads files and calls API
│       ├── SeedReader.cs           # Reader for loading seed files
│       ├── BoardWriter.cs          # Writer for console output
│       └── ApiModels.cs            # API DTOs for CLI
└── tests/
    ├── Conway.Tests/               # xUnit test project for entities
    │   └── BoardTests.cs           # All programmer tests for Board entity
    └── Conway.API.Tests/           # xUnit integration tests for API
        └── GameApiTests.cs         # API tests with telemetry verification
```

## Key Design Decisions

### Testing Philosophy

The tests in `BoardTests.cs` follow the **Programmer Tests** philosophy:

- Tests are written for requirements, not implementation details
- The `Board` class is public and tested extensively
- The `Row`, `Cell`, and `Neighbours` classes are marked `internal` and are NOT tested directly
- This allows refactoring of implementation details without breaking tests
- We only test the **entities**, not the adapters or orchestration layers

### Clean Architecture with API

The project follows Clean Architecture principles with an API-first approach:

**Entities (Domain Layer)**
- `Board` - The core game board entity
- `Row`, `Cell` - Internal value objects (implementation details)
- `Neighbours` - Internal calculation logic (implementation detail)

**Use Cases (Application Layer)**
- `GameEngine` - Orchestrates running the game generations (in Conway.API)

**Adapters (Interface Layer)**
- **API Layer** (`Conway.API`) - HTTP API that exposes game functionality with telemetry
  - `POST /api/game/run` - Accepts board state and number of generations, returns final state
  - Uses OpenTelemetry for distributed tracing
- **CLI Layer** (`Conway.CLI`) - Thin client that reads files and calls the API
  - `SeedReader` - Reads seed files from disk
  - `BoardWriter` - Writes board state to console
  - Uses Spectre.Console for beautiful rendering

**Architecture Benefits**:
- **Separation of Concerns**: CLI handles file I/O and presentation; API handles business logic
- **Testability**: API can be tested independently with integration tests
- **Observability**: OpenTelemetry in API provides full tracing of game execution
- **Scalability**: API can be deployed separately and called by multiple clients

The dependency rule is maintained: entities don't know about use cases or adapters. The API depends on entities. The CLI depends on the API contract (DTOs).

## Conway's Game of Life Rules

1. Any live cell with 2 or 3 live neighbours survives
2. Any dead cell with exactly 3 live neighbours becomes a live cell
3. All other live cells die in the next generation
4. All other dead cells stay dead

## Testing Approach

### Entity Tests (BoardTests.cs)
Each test represents a requirement for the Board entity:

1. **All cells dead** - Empty board stays empty
2. **Single cell dies** - Underpopulation rule
3. **Two cells die** - Still underpopulation
4. **Cell with 2-3 neighbours lives** - Survival rule
5. **Three neighbours creates life, four causes death** - Birth and overpopulation rules
6. **Edge cells** - Boundary conditions (treat off-grid as dead)

### API Integration Tests (GameApiTests.cs)
API tests verify both functionality and observability:

1. **ApiRunsGameOfLifeSingleGeneration** - API correctly runs one generation
2. **ApiRunsGameOfLifeMultipleGenerations** - API correctly runs multiple generations
3. **ApiCapturesTelemetryForGameExecution** - Telemetry spans are created with correct attributes
4. **ApiTelemetryShowsCorrectSpanHierarchy** - Parent-child relationships are correct
5. **ApiTelemetryTracksCellProcessing** - Cell processing metrics are captured

These tests use **trace-based testing**: they verify behavior by examining the OpenTelemetry traces generated during execution, proving that the system is both working correctly and observable.

## Why This Approach?

This code demonstrates:

- **Test requirements, not implementation**: The Neighbours class is refactored out but never tested directly
- **Public interface focus**: Only the Board.Tick() method is tested
- **Refactoring safety**: Implementation can change without test changes
- **Programmer Tests vs Unit Tests**: These tests verify behavior from the programmer's perspective, not every unit of code

## Technologies Used

- **xUnit**: Testing framework following the xUnit pattern
- **Spectre.Console**: Beautiful console output with colors, panels, and interactive prompts
- **ASP.NET Core Minimal API**: Lightweight HTTP API framework
- **OpenTelemetry**: Distributed tracing and observability
- **Microsoft.AspNetCore.Mvc.Testing**: Integration testing for APIs
- **.NET 9**: Latest .NET platform

## Differences from Python Version

While maintaining the same TDD philosophy and Clean Architecture, this C# version:

1. Uses C# idioms (properties, readonly fields, value tuples, records)
2. Implements proper equality comparison using IEquatable
3. Uses `internal` access modifier instead of Python's underscore convention
4. Uses xUnit instead of pytest
5. Adds Spectre.Console for beautiful terminal rendering
6. **Implements an API-first architecture** with CLI as a thin client
7. **Uses OpenTelemetry** for distributed tracing and observability
8. **Demonstrates trace-based testing** to verify both behavior and observability
9. Implements the same Clean Architecture layers (Entities, Use Cases, Adapters)
10. Follows the same dependency rules and testing approach

## Running the Application

### Option 1: Run API and CLI separately

1. Start the API:
   ```bash
   cd src/Conway.API
   dotnet run
   ```

2. In another terminal, run the CLI:
   ```bash
   cd src/Conway.CLI
   dotnet run
   ```
   When prompted, enter the API URL (default: `https://localhost:5001`)

### Option 2: Run tests

```bash
# Run entity tests
dotnet test tests/Conway.Tests

# Run API integration tests (includes telemetry verification)
dotnet test tests/Conway.API.Tests
```

## License

This is an educational project to support the TDD Rediscovered course.
