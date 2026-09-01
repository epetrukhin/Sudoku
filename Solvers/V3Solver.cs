using System.Numerics;
using System.Runtime.CompilerServices;
using static Solvers.Constants;

namespace Solvers;

public static class V3Solver
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
        // InlineArray-структуры хранят данные внутри объекта Board —
        // без отдельного выделения в куче, как у обычных массивов.

        [InlineArray(CellsCount)]
        private struct Candidates { private int _e0; }

        [InlineArray(CellsCount)]
        private struct Solved { private bool _e0; }

        [InlineArray(BoardSize * 3)]
        private struct GroupMask { private int _e0; }

        private Candidates _candidates; // маска кандидатов для каждой из 81 клетки
        private Solved _solved;         // зафиксирована ли клетка в масках занятости
        private GroupMask _groupMask;   // занятые значения: [0..8] строки, [9..17] столбцы, [18..26] боксы
        private int _solvedCount;

        private Board()
        {}

        public Board(char[][] board)
        {
            ((Span<int>)_candidates).Fill(FullMask);

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

        private void CopyTo(Board target)
        {
            ((Span<int>)_candidates).CopyTo(target._candidates);
            ((Span<bool>)_solved).CopyTo(target._solved);
            ((Span<int>)_groupMask).CopyTo(target._groupMask);
            target._solvedCount = _solvedCount;
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

            _groupMask[row] |= valueBit;
            _groupMask[BoardSize + col] |= valueBit;
            _groupMask[BoardSize * 2 + box] |= valueBit;

            var removeMask = ~valueBit;

            // Единый предрасчитанный набор «видимых» ячеек вместо трёх
            // отдельных обходов строки/столбца/бокса.
            foreach (var i in GetClearIndexes(idx))
                _candidates[i] &= removeMask;

            _candidates[idx] = valueBit;
            _solved[idx] = true;
            _solvedCount++;
        }

        // Constraint propagation: naked singles и hidden singles, пока не сойдётся.
        // Возвращает false при конфликте (клетка без кандидатов).
        private bool Propagate()
        {
            while (true)
            {
                var solvedBeforePropogation = _solvedCount;

                // naked singles: единственный кандидат → фиксируем
                if (!AssignNakedSingles())
                    return false;

                // hidden singles: строки
                for (var row = 0; row < BoardSize; row++)
                    FindHiddenSingles(GetRowIndexes(row), _groupMask[row]);

                // hidden singles: столбцы
                for (var col = 0; col < BoardSize; col++)
                    FindHiddenSingles(GetColumnIndexes(col), _groupMask[BoardSize + col]);

                // hidden singles: боксы
                for (var box = 0; box < BoardSize; box++)
                    FindHiddenSingles(GetBoxIndexes(box), _groupMask[BoardSize * 2 + box]);

                if (_solvedCount == solvedBeforePropogation)
                    return true;
            }

            bool AssignNakedSingles()
            {
                // Полный проход по клеткам повторяется, пока хотя бы один
                // naked single был найден в предыдущем проходе: фиксация одной
                // клетки может открыть новую одиночку у её соседей.
                while (true)
                {
                    var anyAssigned = false;

                    for (var idx = 0; idx < CellsCount; idx++)
                    {
                        if (_solved[idx])
                            continue;

                        var candidate = _candidates[idx];
                        if (candidate == 0)
                            return false;

                        if ((candidate & (candidate - 1)) == 0)
                        {
                            Assign(idx, BitOperations.TrailingZeroCount(candidate));
                            anyAssigned = true;
                        }
                    }

                    if (!anyAssigned)
                        return true;
                }
            }

            void FindHiddenSingles(ReadOnlySpan<byte> cells, int used)
            {
                // Итерируем только по свободным (ещё не занятым в группе) значениям:
                // маска ~used сразу даёт их набор, занятые пропускаются без проверки.
                var free = FullMask & ~used;

                while (free != 0)
                {
                    var value = BitOperations.TrailingZeroCount(free);
                    var valueBit = 1 << value;

                    var pos = -1;
                    var count = 0;
                    foreach (var idx in cells)
                    {
                        if ((_candidates[idx] & valueBit) != 0)
                        {
                            if (++count > 1)
                                break;
                            pos = idx;
                        }
                    }

                    if (count == 1)
                        Assign(pos, value);

                    free &= free - 1;
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

            var clone = new Board();
            var candidates = board._candidates[bestIdx];
            while (candidates != 0)
            {
                var v = BitOperations.TrailingZeroCount(candidates);

                board.CopyTo(clone);
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
