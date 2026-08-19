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
            for (var i = 0; i < CellsCount; i++)
                _candidates[i] = FullMask;

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
            var r = idx / BoardSize;
            var c = idx % BoardSize;
            var b = (r / BoxSize) * BoxSize + c / BoxSize;
            var bit = 1 << value;

            _rowMask[r] |= bit;
            _colMask[c] |= bit;
            _boxMask[b] |= bit;

            var rm = ~bit;

            // строка
            var rowStart = r * BoardSize;
            for (var i = 0; i < BoardSize; i++)
            {
                var j = rowStart + i;
                if (j != idx)
                    _candidates[j] &= rm;
            }

            // столбец
            for (var i = 0; i < BoardSize; i++)
            {
                var j = i * BoardSize + c;
                if (j != idx)
                    _candidates[j] &= rm;
            }

            // бокс
            var br = (b / BoxSize) * BoxSize;
            var bc = (b % BoxSize) * BoxSize;
            for (var rr = br; rr < br + BoxSize; rr++)
            {
                for (var cc = bc; cc < bc + BoxSize; cc++)
                {
                    var j = rr * BoardSize + cc;
                    if (j != idx)
                        _candidates[j] &= rm;
                }
            }

            _candidates[idx] = bit;
            if (!_solved[idx])
            {
                _solved[idx] = true;
                _solvedCount++;
            }
        }

        // Constraint propagation: naked singles и hidden singles, пока не сойдётся.
        // Возвращает false при конфликте (клетка без кандидатов).
        private bool Propagate()
        {
            while (true)
            {
                var changed = false;

                // naked singles: единственный кандидат → фиксируем
                for (var idx = 0; idx < CellsCount; idx++)
                {
                    if (_solved[idx])
                        continue;

                    var m = _candidates[idx];
                    if (m == 0)
                        return false;
                    if ((m & (m - 1)) == 0)
                    {
                        Assign(idx, BitOperations.TrailingZeroCount(m));
                        changed = true;
                    }
                }

                // hidden singles: строки
                for (var r = 0; r < BoardSize; r++)
                {
                    var used = _rowMask[r];
                    var rowStart = r * BoardSize;
                    for (var v = 1; v <= BoardSize; v++)
                    {
                        var bit = 1 << v;
                        if ((used & bit) != 0)
                            continue;

                        var pos = -1;
                        var count = 0;
                        for (var c = 0; c < BoardSize; c++)
                        {
                            var idx = rowStart + c;
                            if (!_solved[idx] && (_candidates[idx] & bit) != 0)
                            {
                                if (++count > 1)
                                    break;
                                pos = idx;
                            }
                        }

                        if (count == 1)
                        {
                            Assign(pos, v);
                            changed = true;
                        }
                    }
                }

                // hidden singles: столбцы
                for (var c = 0; c < BoardSize; c++)
                {
                    var used = _colMask[c];
                    for (var v = 1; v <= BoardSize; v++)
                    {
                        var bit = 1 << v;
                        if ((used & bit) != 0)
                            continue;

                        var pos = -1;
                        var count = 0;
                        for (var r = 0; r < BoardSize; r++)
                        {
                            var idx = r * BoardSize + c;
                            if (!_solved[idx] && (_candidates[idx] & bit) != 0)
                            {
                                if (++count > 1)
                                    break;
                                pos = idx;
                            }
                        }

                        if (count == 1)
                        {
                            Assign(pos, v);
                            changed = true;
                        }
                    }
                }

                // hidden singles: боксы
                for (var b = 0; b < BoardSize; b++)
                {
                    var used = _boxMask[b];
                    var br = (b / BoxSize) * BoxSize;
                    var bc = (b % BoxSize) * BoxSize;
                    for (var v = 1; v <= BoardSize; v++)
                    {
                        var bit = 1 << v;
                        if ((used & bit) != 0)
                            continue;

                        var pos = -1;
                        var count = 0;
                        for (var rr = br; rr < br + BoxSize; rr++)
                        {
                            for (var cc = bc; cc < bc + BoxSize; cc++)
                            {
                                var idx = rr * BoardSize + cc;
                                if (!_solved[idx] && (_candidates[idx] & bit) != 0)
                                {
                                    if (++count > 1)
                                        break;
                                    pos = idx;
                                }
                            }
                            if (count > 1)
                                break;
                        }

                        if (count == 1)
                        {
                            Assign(pos, v);
                            changed = true;
                        }
                    }
                }

                if (!changed)
                    return true;
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
