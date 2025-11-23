// Author: Ian Cooper
// Date: 23 November 2025
// Notes: C# port of the Python TDD kata for Conway's Game of Life

namespace Conway.Core;

/// <summary>
/// A game board for the game of life, should be immutable
/// </summary>
public class Board : IEquatable<Board>
{
    private readonly int _generation;
    private readonly (int rows, int cols) _size;
    // _rows field removed - would change public interface and is unsafe
    private readonly int _cols;
    private readonly List<Row> _rows;

    public Board(int generation, (int rows, int cols) size, char[,] cells)
    {
        _generation = generation;
        _size = size;
        _cols = size.cols;

        // Convert char[,] to List<Row>
        _rows = new List<Row>();
        for (int row = 0; row < size.rows; row++)
        {
            var rowCells = new List<char>();
            for (int col = 0; col < size.cols; col++)
            {
                rowCells.Add(cells[row, col]);
            }
            _rows.Add(new Row(rowCells));
        }
    }

    public Board Tick()
    {
        var nextBoard = new List<Row>();
        for (int i = 0; i < _rows.Count; i++)
        {
            var rowCells = new List<char>();
            for (int j = 0; j < _cols; j++)
            {
                rowCells.Add('.');
            }
            nextBoard.Add(new Row(rowCells));
        }

        for (int row = 0; row < _rows.Count; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                var cell = _rows[row][col];
                if (cell == '*')
                {
                    var liveNeighbourCount = new Neighbours(row, col, _size).GetCount(_rows);
                    if (liveNeighbourCount == 2 || liveNeighbourCount == 3)
                    {
                        nextBoard[row][col] = '*';
                    }
                }
                else
                {
                    var liveNeighbourCount = new Neighbours(row, col, _size).GetCount(_rows);
                    if (liveNeighbourCount == 3)
                    {
                        nextBoard[row][col] = '*';
                    }
                }
            }
        }

        var grid = BoardToGrid(nextBoard);

        return new Board(_generation + 1, _size, grid);
    }

    private char[,] BoardToGrid(List<Row> nextBoard)
    {
        var grid = new char[nextBoard.Count, _cols];
        for (int i = 0; i < nextBoard.Count; i++)
        {
            var cells = nextBoard[i].GetCells();
            for (int j = 0; j < cells.Count; j++)
            {
                grid[i, j] = cells[j];
            }
        }
        return grid;
    }

    public bool Equals(Board? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (_size != other._size) return false;

        for (int i = 0; i < _rows.Count; i++)
        {
            if (!_rows[i].Equals(other._rows[i]))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Board);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_generation, _size, _rows);
    }

    public override string ToString()
    {
        var result = new System.Text.StringBuilder();
        result.AppendLine($"Generation {_generation}");
        result.AppendLine($"{_size.rows} {_size.cols}");

        for (int r = 0; r < _rows.Count; r++)
        {
            result.AppendLine(_rows[r].ToString());
        }

        return result.ToString();
    }
}

/// <summary>
/// A neighbour calculator for a cell in a grid
/// * It finds neighbours north, north-east, east, south-east, south, south-west, west, and north-west
/// * It counts the number of those neighbours that are live
/// * It does not count neighbours beyond the borders of the grid, effectively treating them as dead
/// </summary>
internal class Neighbours
{
    private readonly (int rows, int cols) _size;
    private readonly int _sameColumn;
    private readonly int _priorColumn;
    private readonly int _nextColumn;
    private readonly int _lastColumn;

    private readonly int _sameRow;
    private readonly int _priorRow;
    private readonly int _nextRow;
    private readonly int _lastRow;

    public Neighbours(int row, int col, (int rows, int cols) size)
    {
        _size = size;
        _sameColumn = col;
        _priorColumn = col - 1;
        _nextColumn = col + 1;
        _lastColumn = size.cols - 1;

        _sameRow = row;
        _priorRow = row - 1;
        _nextRow = row + 1;
        _lastRow = size.rows - 1;
    }

    public int GetCount(List<Row> rows)
    {
        var liveNeighbourCount = RowCount(_priorRow, rows);
        liveNeighbourCount += RowCount(_sameRow, rows, exclude: true);
        liveNeighbourCount += RowCount(_nextRow, rows);

        return liveNeighbourCount;
    }

    private int RowCount(int row, List<Row> rows, bool exclude = false)
    {
        var liveNeighbourCount = 0;
        if (row >= 0 && row <= _lastRow)
        {
            if (_priorColumn >= 0 && rows[row][_priorColumn] == '*')
                liveNeighbourCount++;

            if (!exclude && rows[row][_sameColumn] == '*')
                liveNeighbourCount++;

            if (_nextColumn <= _lastColumn && rows[row][_nextColumn] == '*')
                liveNeighbourCount++;
        }
        return liveNeighbourCount;
    }
}

internal class Row : IEquatable<Row>
{
    private readonly List<Cell> _cells;

    public Row(List<char> row)
    {
        _cells = row.Select(s => new Cell(s)).ToList();
    }

    public Cell this[int index]
    {
        get => _cells[index];
        set => _cells[index] = value;
    }

    public List<char> GetCells()
    {
        return _cells.Select(c => c.ToChar()).ToList();
    }

    public bool Equals(Row? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        if (_cells.Count != other._cells.Count) return false;

        for (int i = 0; i < _cells.Count; i++)
        {
            if (!_cells[i].Equals(other._cells[i]))
                return false;
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Row);
    }

    public override int GetHashCode()
    {
        return _cells.GetHashCode();
    }

    public override string ToString()
    {
        return string.Join("", _cells.Select(c => c.ToString()));
    }
}

internal class Cell : IEquatable<Cell>
{
    private readonly char _state;

    public Cell(char val = '.')
    {
        _state = val;
    }

    public char ToChar() => _state;

    public override string ToString()
    {
        return _state.ToString();
    }

    public bool Equals(Cell? other)
    {
        if (other is null) return false;
        return _state == other._state;
    }

    public override bool Equals(object? obj)
    {
        if (obj is Cell cell)
            return Equals(cell);
        if (obj is char c)
            return _state == c;
        return false;
    }

    public static bool operator ==(Cell? left, char right)
    {
        if (left is null) return false;
        return left._state == right;
    }

    public static bool operator !=(Cell? left, char right)
    {
        return !(left == right);
    }

    public static bool operator ==(char left, Cell? right)
    {
        return right == left;
    }

    public static bool operator !=(char left, Cell? right)
    {
        return !(left == right);
    }

    public static implicit operator Cell(char c) => new Cell(c);

    public override int GetHashCode()
    {
        return _state.GetHashCode();
    }
}
