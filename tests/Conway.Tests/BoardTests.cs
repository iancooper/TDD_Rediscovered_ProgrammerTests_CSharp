// Author: Ian Cooper
// Date: 23 November 2025
// Notes: C# port of the Python TDD kata for Conway's Game of Life

using Conway.Core;
using Xunit;

namespace Conway.Tests;

public class BoardTests
{
    [Fact]
    public void AllCellsDead()
    {
        // All the cells are dead, no change
        var seed = new Board(0, (3, 3), new[,]
        {
            { '.', '.', '.' },
            { '.', '.', '.' },
            { '.', '.', '.' }
        });

        var expectedGenerationOne = new Board(1, (3, 3), new[,]
        {
            { '.', '.', '.' },
            { '.', '.', '.' },
            { '.', '.', '.' }
        });

        var generationOne = seed.Tick();

        Assert.Equal(expectedGenerationOne, generationOne);
    }

    [Fact]
    public void SingleCellWithNoNeighboursDies()
    {
        // Just one cell is live, and it has no neighbours, so must die
        var seed = new Board(0, (3, 3), new[,]
        {
            { '.', '.', '.' },
            { '.', '*', '.' },
            { '.', '.', '.' }
        });

        var expectedGenerationOne = new Board(1, (3, 3), new[,]
        {
            { '.', '.', '.' },
            { '.', '.', '.' },
            { '.', '.', '.' }
        });

        var generationOne = seed.Tick();

        Assert.Equal(expectedGenerationOne, generationOne);
    }

    [Fact]
    public void TwoAdjacentCellsWithInsufficientNeighboursDie()
    {
        // Two adjacent cells with insufficient neighbours die
        var seed = new Board(0, (3, 3), new[,]
        {
            { '.', '*', '.' },
            { '.', '*', '.' },
            { '.', '.', '.' }
        });

        var expectedGenerationOne = new Board(1, (3, 3), new[,]
        {
            { '.', '.', '.' },
            { '.', '.', '.' },
            { '.', '.', '.' }
        });

        var generationOne = seed.Tick();

        Assert.Equal(expectedGenerationOne, generationOne);
    }

    [Fact]
    public void AnAdjacentCellWithSufficientNeighboursLives()
    {
        // Cells with two or three live neighbours survive
        var seed = new Board(0, (3, 3), new[,]
        {
            { '.', '.', '*' },
            { '.', '*', '.' },
            { '*', '.', '.' }
        });

        var expectedGenerationOne = new Board(1, (3, 3), new[,]
        {
            { '.', '.', '.' },
            { '.', '*', '.' },
            { '.', '.', '.' }
        });

        var generationOne = seed.Tick();

        Assert.Equal(expectedGenerationOne, generationOne);
    }

    [Fact]
    public void ThreeLiveNeighboursLiveFourLiveNeighboursDie()
    {
        // Only cells with two or three live neighbours survive a generation
        var seed = new Board(0, (3, 3), new[,]
        {
            { '.', '*', '.' },
            { '*', '*', '*' },
            { '.', '*', '.' }
        });

        var expectedGenerationOne = new Board(1, (3, 3), new[,]
        {
            { '*', '*', '*' },
            { '*', '.', '*' },
            { '*', '*', '*' }
        });

        var generationOne = seed.Tick();

        Assert.Equal(expectedGenerationOne, generationOne);
    }

    [Fact]
    public void CellsPositionedAtTheEdge()
    {
        // We consider neighbours outside the grid to be dead
        var seed = new Board(0, (3, 3), new[,]
        {
            { '*', '*', '*' },
            { '*', '.', '*' },
            { '*', '*', '*' }
        });

        var expectedGenerationOne = new Board(1, (3, 3), new[,]
        {
            { '*', '.', '*' },
            { '.', '.', '.' },
            { '*', '.', '*' }
        });

        var generationOne = seed.Tick();

        Assert.Equal(expectedGenerationOne, generationOne);
    }
}
