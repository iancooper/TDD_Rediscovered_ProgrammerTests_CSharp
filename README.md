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
│   │   ├── Game.cs                 # Game use case (orchestration layer)
│   │   ├── SeedReader.cs           # Reader adapter for loading seed files
│   │   └── BoardWriter.cs          # Writer adapter for output
│   └── Conway.CLI/                 # CLI application using Spectre.Console
│       └── Program.cs              # Main entry point with Spectre.Console rendering
└── tests/
    └── Conway.Tests/               # xUnit test project
        └── BoardTests.cs           # All programmer tests for Board entity
```

## Key Design Decisions

### Testing Philosophy

The tests in `BoardTests.cs` follow the **Programmer Tests** philosophy:

- Tests are written for requirements, not implementation details
- The `Board` class is public and tested extensively
- The `Row`, `Cell`, and `Neighbours` classes are marked `internal` and are NOT tested directly
- This allows refactoring of implementation details without breaking tests
- We only test the **entities**, not the adapters or orchestration layers

### Clean Architecture

The project follows Clean Architecture principles:

**Entities (Domain Layer)**
- `Board` - The core game board entity
- `Row`, `Cell` - Internal value objects (implementation details)
- `Neighbours` - Internal calculation logic (implementation detail)

**Use Cases (Application Layer)**
- `Game` - Orchestrates the game loop, coordinates reading, processing, and writing

**Adapters (Interface Layer)**
- `IReader` / `SeedReader` - Reads seed files from disk
- `BoardWriter` - Writes board state to console
- `Program` - CLI adapter with Spectre.Console rendering

The dependency rule is maintained: entities don't know about adapters or use cases. Use cases depend on entities. Adapters depend on everything but can be easily replaced.

### C# Specific Adaptations

From Python to C#:

- **Immutability**: Board is designed to be immutable (readonly fields)
- **Value Equality**: Implements `IEquatable<Board>` for proper equality comparison
- **Tuples**: Uses C# value tuples `(int, int)` for size representation
- **2D Arrays**: Uses `char[,]` instead of `List<List<char>>` for better performance
- **Access Modifiers**: Uses `internal` for the Neighbours class to mark it as an implementation detail

## Building and Running

### Prerequisites

- .NET 9.0 SDK or later

### Build the Solution

```bash
dotnet build
```

### Run the Tests

```bash
dotnet test
```

All 6 tests should pass:
- AllCellsDead
- SingleCellWithNoNeighboursDies
- TwoAdjacentCellsWithInsufficientNeighboursDie
- AnAdjacentCellWithSufficientNeighboursLives
- ThreeLiveNeighboursLiveFourLiveNeighboursDie
- CellsPositionedAtTheEdge

### Run the CLI Application

```bash
dotnet run --project src/Conway.CLI/Conway.CLI.csproj
```

The CLI provides an interactive experience:
1. Enter the path to a seed file (default: seed.txt)
2. Enter the number of generations to run
3. Watch the simulation with beautiful Spectre.Console rendering

Example seed files are provided:
- `seed.txt` - Blinker oscillator (5x5)
- `glider.txt` - Glider pattern (10x10)

### Seed File Format

Seed files follow this format:
```
Generation 0
{rows} {cols}
{grid of . and * characters}
```

Example:
```
Generation 0
3 3
.*.
***
.*.
```

## Conway's Game of Life Rules

1. Any live cell with 2 or 3 live neighbours survives
2. Any dead cell with exactly 3 live neighbours becomes a live cell
3. All other live cells die in the next generation
4. All other dead cells stay dead

## Testing Approach

Each test represents a requirement:

1. **All cells dead** - Empty board stays empty
2. **Single cell dies** - Underpopulation rule
3. **Two cells die** - Still underpopulation
4. **Cell with 2-3 neighbours lives** - Survival rule
5. **Three neighbours creates life, four causes death** - Birth and overpopulation rules
6. **Edge cells** - Boundary conditions (treat off-grid as dead)

## Why This Approach?

This code demonstrates:

- **Test requirements, not implementation**: The Neighbours class is refactored out but never tested directly
- **Public interface focus**: Only the Board.Tick() method is tested
- **Refactoring safety**: Implementation can change without test changes
- **Programmer Tests vs Unit Tests**: These tests verify behavior from the programmer's perspective, not every unit of code

## Technologies Used

- **xUnit**: Testing framework following the xUnit pattern
- **Spectre.Console**: Beautiful console output with colors, panels, and interactive prompts
- **.NET 9**: Latest .NET platform

## Differences from Python Version

While maintaining the same TDD philosophy and Clean Architecture, this C# version:

1. Uses C# idioms (properties, readonly fields, value tuples)
2. Implements proper equality comparison using IEquatable
3. Uses `internal` access modifier instead of Python's underscore convention
4. Uses xUnit instead of pytest
5. Adds Spectre.Console for beautiful terminal rendering
6. Implements the same Clean Architecture layers (Entities, Use Cases, Adapters)
7. Follows the same dependency rules and testing approach

## License

This is an educational project to support the TDD Rediscovered course.
