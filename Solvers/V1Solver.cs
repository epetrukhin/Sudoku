using static Solvers.Constants;

namespace Solvers;

public static class V1Solver
{
    public static void Solve(char[][] sudoku, char[][] result)
    {
        var board = new Board(sudoku);
        var resultBoard = Board.Solve(board);
        resultBoard.DumpTo(result);
    }

    private sealed class Board
    {
        private readonly Cell[][] _cells;

        public Board(char[][] board)
        {
            var cells = new Cell[BoardSize][];
            for (var ri = 0; ri < BoardSize; ri++)
            {
                var row = new Cell[BoardSize];
                cells[ri] = row;
                var sourceRow = board[ri];

                for (var ci = 0; ci < BoardSize; ci++)
                {
                    row[ci] = Cell.Parse(sourceRow[ci]);
                }
            }

            _cells = cells;
        }

        private Board(Cell[][] cells) =>
            _cells = cells;

        private Board Clone()
        {
            var sourceCells = _cells;
            var targetCells = new Cell[BoardSize][];
            for (var ri = 0; ri < BoardSize; ri++)
            {
                var sourceRow = sourceCells[ri];
                var targetRow = new Cell[BoardSize];
                targetCells[ri] = targetRow;

                for (var ci = 0; ci < BoardSize; ci++)
                    targetRow[ci] = sourceRow[ci].Clone();
            }

            return new(targetCells);
        }

        private bool IsSolved() =>
            _cells.All(row => row.All(cell => cell is Cell.Concrete));

        private bool IsValid()
        {
            if (_cells.SelectMany(_ => _).OfType<Cell.NonConcrete>().Any(cell => cell.IsEmpty))
                return false;

            for (var ri = 0; ri < BoardSize; ri++)
            {
                if (!IsValid(GetSameRowInterferCellsFor(ri)))
                    return false;
            }

            for (var ci = 0; ci < BoardSize; ci++)
            {
                if (!IsValid(GetSameColumnInterferCellsFor(ci)))
                    return false;
            }

            for (var ri = 0; ri < BoardSize; ri += 3)
            {
                for (var ci = 0; ci < BoardSize; ci += 3)
                {
                    if (!IsValid(GetSameBoxInterferCellsFor(ri, ci)))
                        return false;
                }
            }

            return true;

            static bool IsValid(IEnumerable<Cell> cells) =>
                cells
                    .OfType<Cell.Concrete>()
                    .Select(c => c.Num)
                    .GroupBy(_ => _)
                    .All(g => g.Count() == 1);

            IEnumerable<Cell> GetSameRowInterferCellsFor(int rowIndex) =>
                _cells[rowIndex];

            IEnumerable<Cell> GetSameColumnInterferCellsFor(int columnIndex)
            {
                var cells = _cells;

                for (var ri = 0; ri < BoardSize; ri++)
                {
                    yield return cells[ri][columnIndex];
                }
            }

            IEnumerable<Cell> GetSameBoxInterferCellsFor(int rowIndex, int columnIndex)
            {
                var cells = _cells;

                var startRowIndex = GetBoxStartIndex(rowIndex);
                var startColumnIndex = GetBoxStartIndex(columnIndex);
                for (var ri = startRowIndex; ri < startRowIndex + BoxSize; ri++)
                {
                    for (var ci = startColumnIndex; ci < startColumnIndex + BoxSize; ci++)
                    {
                        yield return cells[ri][ci];
                    }
                }

                static int GetBoxStartIndex(int index) =>
                    index switch
                    {
                        < BoxSize => 0,
                        < BoxSize * 2 => BoxSize,
                        _ => BoxSize * 2
                    };
            }
        }

        public void DumpTo(char[][] board)
        {
            var cells = _cells;
            for (var ri = 0; ri < BoardSize; ri++)
            {
                var row = cells[ri];
                var targetRow = board[ri];

                for (var ci = 0; ci < BoardSize; ci++)
                {
                    targetRow[ci] = row[ci].AsChar();
                }
            }
        }

        public static Board Solve(Board board)
        {
            board.Solve();

            return board.IsSolved()
                ? board
                : TrySolveWithSubstitute(board) ?? throw new ApplicationException("Failed to solve");

            static Board? TrySolveWithSubstitute(Board board)
            {
                var (ri, ci) = GetPositionOfNonConcreteCellWithMinNums(board);
                var cell = (Cell.NonConcrete)board._cells[ri][ci];

                foreach (var num in cell.GetNums())
                {
                    var clone = board.Clone();
                    clone._cells[ri][ci] = Cell.Concrete.GetFor(num);

                    clone.Solve();

                    if (!clone.IsValid())
                        continue;

                    if (clone.IsSolved())
                        return clone;

                    var next = TrySolveWithSubstitute(clone);
                    if (next != null)
                        return next;
                }

                return null;

                static (int ri, int ci) GetPositionOfNonConcreteCellWithMinNums(Board board)
                {
                    var minNumsCount = 10;
                    var minNumsRowIndex = BoardSize;
                    var minNumsColIndex = BoardSize;

                    var cells = board._cells;

                    for (var ri = 0; ri < BoardSize; ri++)
                    {
                        var row = cells[ri];
                        for (var ci = 0; ci < BoardSize; ci++)
                        {
                            var cell = row[ci];
                            if (cell is Cell.NonConcrete nc)
                            {
                                var numsCount = nc.Count;
                                if (numsCount == 2)
                                    return (ri, ci);

                                if (numsCount < minNumsCount)
                                {
                                    minNumsCount = numsCount;
                                    minNumsRowIndex = ri;
                                    minNumsColIndex = ci;
                                }
                            }
                        }
                    }

                    return (minNumsRowIndex, minNumsColIndex);
                }
            }
        }

        private void Solve()
        {
            while (true)
            {
                var reduced = false;

                for (var ri = 0; ri < BoardSize; ri++)
                {
                    for (var ci = 0; ci < BoardSize; ci++)
                    {
                        reduced |= TryReduce(ri, ci);
                    }
                }

                if (!reduced)
                    return;
            }

            bool TryReduce(int ri, int ci)
            {
                var cells = _cells;

                var targetCell = cells[ri][ci] as Cell.NonConcrete;
                if (targetCell == null)
                    return false;

                var reduced = false;
                foreach (var cell in GetInterferCellsFor(ri, ci).OfType<Cell.Concrete>())
                {
                    var reduceResult = targetCell.Reduce(cell);
                    if (reduceResult != null)
                    {
                        cells[ri][ci] = reduceResult;

                        if (reduceResult is Cell.NonConcrete nc)
                            targetCell = nc;
                        else
                            return true;

                        reduced = true;
                    }
                }

                foreach (var num in targetCell.GetNums())
                {
                    if (IsHiddenSingle(num, GetSameRowInterferCellsFor(ri, ci)) ||
                        IsHiddenSingle(num, GetSameColumnInterferCellsFor(ri, ci)) ||
                        IsHiddenSingle(num, GetSameBoxInterferCellsFor(ri, ci)))
                    {
                        cells[ri][ci] = Cell.Concrete.GetFor(num);
                        return true;
                    }

                    static bool IsHiddenSingle(int num, IEnumerable<Cell> cells) =>
                        cells.OfType<Cell.NonConcrete>().All(c => !c.ContainsNum(num));
                }

                return reduced;

                IEnumerable<Cell> GetSameRowInterferCellsFor(int rowIndex, int columnIndex)
                {
                    var cells = _cells;

                    var row = cells[rowIndex];
                    for (var ci = 0; ci < BoardSize; ci++)
                    {
                        if (ci == columnIndex)
                            continue;

                        yield return row[ci];
                    }
                }

                IEnumerable<Cell> GetSameColumnInterferCellsFor(int rowIndex, int columnIndex)
                {
                    var cells = _cells;

                    for (var ri = 0; ri < BoardSize; ri++)
                    {
                        if (ri == rowIndex)
                            continue;

                        yield return cells[ri][columnIndex];
                    }
                }

                IEnumerable<Cell> GetSameBoxInterferCellsFor(int rowIndex, int columnIndex)
                {
                    var cells = _cells;

                    var startRowIndex = GetBoxStartIndex(ri);
                    var startColumnIndex = GetBoxStartIndex(ci);
                    for (var ri = startRowIndex; ri < startRowIndex + BoxSize; ri++)
                    {
                        for (var ci = startColumnIndex; ci < startColumnIndex + BoxSize; ci++)
                        {
                            if (ri == rowIndex && ci == columnIndex)
                                continue;

                            yield return cells[ri][ci];
                        }
                    }

                    static int GetBoxStartIndex(int index) =>
                        index switch
                        {
                            < BoxSize => 0,
                            < BoxSize * 2 => BoxSize,
                            _ => BoxSize * 2
                        };
                }

                IEnumerable<Cell> GetInterferCellsFor(int rowIndex, int columnIndex) =>
                [
                    ..GetSameRowInterferCellsFor(rowIndex, columnIndex),
                    ..GetSameColumnInterferCellsFor(rowIndex, columnIndex),
                    ..GetSameBoxInterferCellsFor(rowIndex, columnIndex)
                ];
            }
        }
    }

    private abstract class Cell
    {
        private Cell()
        {}

        public abstract char AsChar();

        public abstract Cell Clone();

        public static Cell Parse(char c) =>
            c == '.'
                ? new NonConcrete()
                : Concrete.GetFor(int.Parse(new ReadOnlySpan<char>(ref c)));

        public sealed class Concrete : Cell
        {
            private static readonly Concrete[] Instances = Enumerable
                .Range(1, 9)
                .Select(num => new Concrete(num))
                .ToArray();

            public static Concrete GetFor(int num) =>
                Instances[num - 1];

            private Concrete(int num) =>
                Num = num;

            public int Num { get; }

            public override char AsChar() =>
                Num.ToString()[0];

            public override Concrete Clone() =>
                this;
        }

        public sealed class NonConcrete : Cell
        {
            private static readonly int[] AllNums = [1, 2, 3, 4, 5, 6, 7, 8, 9];

            private readonly HashSet<int> _possibleNums;

            public NonConcrete()
                : this(new(AllNums))
            {}

            private NonConcrete(HashSet<int> possibleNums) =>
                _possibleNums = possibleNums;

            public int Count =>
                _possibleNums.Count;

            public bool IsEmpty =>
                _possibleNums.Count == 0;

            public override char AsChar() =>
                '.';

            public override NonConcrete Clone() =>
                new(new(_possibleNums));

            public IEnumerable<int> GetNums() =>
                _possibleNums;

            public bool ContainsNum(int num) =>
                _possibleNums.Contains(num);

            public Cell? Reduce(Concrete cc)
            {
                var possibleNums = _possibleNums;

                if (!possibleNums.Remove(cc.Num))
                    return null;

                if (possibleNums.Count == 1)
                    return Concrete.GetFor(possibleNums.First());

                return this;
            }
        }
    }
}