using System.Numerics;
using static Solvers.Constants;

namespace Solvers;

public static class V2Solver
{
    // Биты 1..9 установлены: 1<<1 | 1<<2 | ... | 1<<9 = 0x3FE.
    // Значение v ∈ [1..9] кодируется битом 1<<v, поэтому индекс младшего
    // установленного бита (BitOperations.TrailingZeroCount) совпадает с самим значением.
    private const int FullMask = 0b_111_111_111_0;

    public static void Solve(char[][] sudoku, char[][] result)
    {
        var board = new Board(sudoku);
        var solved = Board.Solve(board) ?? throw new InvalidOperationException("Failed to solve");
        solved.DumpTo(result);
    }

    private sealed class Board
    {
        private readonly int[] _candidates; // маска кандидатов для каждой из 81 клетки
        private readonly bool[] _solved;    // зафиксирована ли клетка в масках занятости
        private readonly int[] _rowMask;    // занятые значения по строкам
        private readonly int[] _colMask;    // занятые значения по столбцам
        private readonly int[] _boxMask;    // занятые значения по боксам
        private int _solvedCount;

        private Board()
        {
            _candidates = new int[CellsCount];
            _solved = new bool[CellsCount];
            _rowMask = new int[BoardSize];
            _colMask = new int[BoardSize];
            _boxMask = new int[BoardSize];
        }

        public Board(char[][] board) : this()
        {
            Array.Fill(_candidates, FullMask);

            for (var ri = 0; ri < BoardSize; ri++)
            {
                var row = board[ri];
                for (var ci = 0; ci < BoardSize; ci++)
                {
                    var ch = row[ci];
                    if (ch != '.')
                        Assign(ri * BoardSize + ci, ch - '0');
                }
            }
        }

        private Board Clone()
        {
            var copy = new Board();
            Array.Copy(_candidates, copy._candidates, CellsCount);
            Array.Copy(_solved, copy._solved, CellsCount);
            Array.Copy(_rowMask, copy._rowMask, BoardSize);
            Array.Copy(_colMask, copy._colMask, BoardSize);
            Array.Copy(_boxMask, copy._boxMask, BoardSize);
            copy._solvedCount = _solvedCount;
            return copy;
        }

        // Фиксирует в клетке значение: обновляет маски занятости и редуцирует
        // кандидатов всех соседей по строке/столбцу/боксу. Конфликт (сосед
        // остаётся без кандидатов) обнаруживается позже в Propagate.
        private void Assign(int idx, int value)
        {
            var row = idx / BoardSize;
            var col = idx % BoardSize;
            var box = BoxOf[idx];
            var valueBit = 1 << value;

            _rowMask[row] |= valueBit;
            _colMask[col] |= valueBit;
            _boxMask[box] |= valueBit;

            var removeMask = ~valueBit;

            RemoveFromGroup(GetRowIndexes(row), removeMask);
            RemoveFromGroup(GetColumnIndexes(col), removeMask);
            RemoveFromGroup(GetBoxIndexes(box), removeMask);

            _candidates[idx] = valueBit;
            _solved[idx] = true;
            _solvedCount++;

            void RemoveFromGroup(ReadOnlySpan<byte> cells, int mask)
            {
                foreach (var j in cells)
                    _candidates[j] &= mask;
            }
        }

        // Constraint propagation: naked singles и hidden singles, пока не сойдётся.
        // Возвращает false при конфликте (клетка без кандидатов).
        private bool Propagate()
        {
            while (true)
            {
                var solvedBeforePropogation = _solvedCount;

                // naked singles: единственный кандидат → фиксируем
                for (var idx = 0; idx < CellsCount; idx++)
                {
                    if (_solved[idx])
                        continue;

                    var candidate = _candidates[idx];
                    if (candidate == 0)
                        return false;

                    if ((candidate & (candidate - 1)) == 0)
                        Assign(idx, BitOperations.TrailingZeroCount(candidate));
                }

                // hidden singles: строки
                for (var row = 0; row < BoardSize; row++)
                    FindHiddenSingles(GetRowIndexes(row), _rowMask[row]);

                // hidden singles: столбцы
                for (var col = 0; col < BoardSize; col++)
                    FindHiddenSingles(GetColumnIndexes(col), _colMask[col]);

                // hidden singles: боксы
                for (var box = 0; box < BoardSize; box++)
                    FindHiddenSingles(GetBoxIndexes(box), _boxMask[box]);

                if (_solvedCount == solvedBeforePropogation)
                    return true;
            }

            void FindHiddenSingles(ReadOnlySpan<byte> cells, int used)
            {
                for (var value = 1; value <= BoardSize; value++)
                {
                    var valueBit = 1 << value;
                    if ((used & valueBit) != 0)
                        continue;

                    var pos = -1;
                    var count = 0;
                    foreach (var idx in cells)
                    {
                        if (!_solved[idx] && (_candidates[idx] & valueBit) != 0)
                        {
                            if (++count > 1)
                                break;
                            pos = idx;
                        }
                    }

                    if (count == 1)
                    {
                        Assign(pos, value);
                    }
                }
            }
        }

        public static Board? Solve(Board board)
        {
            if (!board.Propagate())
                return null;
            if (board._solvedCount == CellsCount)
                return board;
            return Backtrack(board);
        }

        private static Board? Backtrack(Board board)
        {
            // MRV: нерешённая клетка с минимальным числом кандидатов.
            // После Propagate таких клеток не меньше 2 кандидатов, поэтому
            // cutoff на 2 разрывает поиск досрочно.
            var bestIdx = -1;
            var bestCount = BoardSize + 1;
            for (var idx = 0; idx < CellsCount; idx++)
            {
                if (board._solved[idx])
                    continue;

                var count = BitOperations.PopCount((uint)board._candidates[idx]);
                if (count < bestCount)
                {
                    bestCount = count;
                    bestIdx = idx;
                    if (count == 2)
                        break;
                }
            }

            var candidates = board._candidates[bestIdx];
            while (candidates != 0)
            {
                var v = BitOperations.TrailingZeroCount(candidates);

                var clone = board.Clone();
                clone.Assign(bestIdx, v);

                var result = Solve(clone);
                if (result != null)
                    return result;

                candidates &= candidates - 1;
            }

            return null;
        }

        public void DumpTo(char[][] board)
        {
            for (var ri = 0; ri < BoardSize; ri++)
            {
                var targetRow = board[ri];
                var rowStart = ri * BoardSize;
                for (var ci = 0; ci < BoardSize; ci++)
                {
                    var m = _candidates[rowStart + ci];
                    targetRow[ci] = (char)('0' + BitOperations.TrailingZeroCount(m));
                }
            }
        }
    }
}
