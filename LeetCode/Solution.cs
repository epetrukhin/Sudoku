using System.Numerics;
using System.Runtime.CompilerServices;

public class Solution
{
    public void SolveSudoku(char[][] board)
    {
        var brd = new Board(board);
        var solved = Board.Solve(brd) ?? throw new InvalidOperationException("Failed to solve");
        solved.DumpTo(board);
    }

    private const int BoardSize = 9;
    private const int BoxSize = 3;

    private const int CellsCount = BoardSize * BoardSize;

    private static ReadOnlySpan<byte> RowIndices =>
    [
        0,1,2,3,4,5,6,7,8,
        9,10,11,12,13,14,15,16,17,
        18,19,20,21,22,23,24,25,26,
        27,28,29,30,31,32,33,34,35,
        36,37,38,39,40,41,42,43,44,
        45,46,47,48,49,50,51,52,53,
        54,55,56,57,58,59,60,61,62,
        63,64,65,66,67,68,69,70,71,
        72,73,74,75,76,77,78,79,80
    ];

    private static ReadOnlySpan<byte> ColIndices =>
    [
        0,9,18,27,36,45,54,63,72,
        1,10,19,28,37,46,55,64,73,
        2,11,20,29,38,47,56,65,74,
        3,12,21,30,39,48,57,66,75,
        4,13,22,31,40,49,58,67,76,
        5,14,23,32,41,50,59,68,77,
        6,15,24,33,42,51,60,69,78,
        7,16,25,34,43,52,61,70,79,
        8,17,26,35,44,53,62,71,80
    ];

    private static ReadOnlySpan<byte> BoxIndices =>
    [
        0,1,2,9,10,11,18,19,20,
        3,4,5,12,13,14,21,22,23,
        6,7,8,15,16,17,24,25,26,
        27,28,29,36,37,38,45,46,47,
        30,31,32,39,40,41,48,49,50,
        33,34,35,42,43,44,51,52,53,
        54,55,56,63,64,65,72,73,74,
        57,58,59,66,67,68,75,76,77,
        60,61,62,69,70,71,78,79,80
    ];

    private static ReadOnlySpan<byte> BoxOf =>
    [
        0,0,0,1,1,1,2,2,2,
        0,0,0,1,1,1,2,2,2,
        0,0,0,1,1,1,2,2,2,
        3,3,3,4,4,4,5,5,5,
        3,3,3,4,4,4,5,5,5,
        3,3,3,4,4,4,5,5,5,
        6,6,6,7,7,7,8,8,8,
        6,6,6,7,7,7,8,8,8,
        6,6,6,7,7,7,8,8,8
    ];

    private static ReadOnlySpan<byte> GetRowIndexes(int row) => RowIndices.Slice(row * BoardSize, BoardSize);

    private static ReadOnlySpan<byte> GetColumnIndexes(int col) => ColIndices.Slice(col * BoardSize, BoardSize);

    private static ReadOnlySpan<byte> GetBoxIndexes(int box) => BoxIndices.Slice(box * BoardSize, BoardSize);

    private const int SeenCellsCount = 2 * (BoardSize - 1) + (BoxSize - 1) * (BoxSize - 1);

    private static ReadOnlySpan<byte> ClearIndices =>
    [
        1,2,3,4,5,6,7,8,9,10,11,18,19,20,27,36,45,54,63,72,
        0,2,3,4,5,6,7,8,9,10,11,18,19,20,28,37,46,55,64,73,
        0,1,3,4,5,6,7,8,9,10,11,18,19,20,29,38,47,56,65,74,
        0,1,2,4,5,6,7,8,12,13,14,21,22,23,30,39,48,57,66,75,
        0,1,2,3,5,6,7,8,12,13,14,21,22,23,31,40,49,58,67,76,
        0,1,2,3,4,6,7,8,12,13,14,21,22,23,32,41,50,59,68,77,
        0,1,2,3,4,5,7,8,15,16,17,24,25,26,33,42,51,60,69,78,
        0,1,2,3,4,5,6,8,15,16,17,24,25,26,34,43,52,61,70,79,
        0,1,2,3,4,5,6,7,15,16,17,24,25,26,35,44,53,62,71,80,
        0,1,2,10,11,12,13,14,15,16,17,18,19,20,27,36,45,54,63,72,
        0,1,2,9,11,12,13,14,15,16,17,18,19,20,28,37,46,55,64,73,
        0,1,2,9,10,12,13,14,15,16,17,18,19,20,29,38,47,56,65,74,
        3,4,5,9,10,11,13,14,15,16,17,21,22,23,30,39,48,57,66,75,
        3,4,5,9,10,11,12,14,15,16,17,21,22,23,31,40,49,58,67,76,
        3,4,5,9,10,11,12,13,15,16,17,21,22,23,32,41,50,59,68,77,
        6,7,8,9,10,11,12,13,14,16,17,24,25,26,33,42,51,60,69,78,
        6,7,8,9,10,11,12,13,14,15,17,24,25,26,34,43,52,61,70,79,
        6,7,8,9,10,11,12,13,14,15,16,24,25,26,35,44,53,62,71,80,
        0,1,2,9,10,11,19,20,21,22,23,24,25,26,27,36,45,54,63,72,
        0,1,2,9,10,11,18,20,21,22,23,24,25,26,28,37,46,55,64,73,
        0,1,2,9,10,11,18,19,21,22,23,24,25,26,29,38,47,56,65,74,
        3,4,5,12,13,14,18,19,20,22,23,24,25,26,30,39,48,57,66,75,
        3,4,5,12,13,14,18,19,20,21,23,24,25,26,31,40,49,58,67,76,
        3,4,5,12,13,14,18,19,20,21,22,24,25,26,32,41,50,59,68,77,
        6,7,8,15,16,17,18,19,20,21,22,23,25,26,33,42,51,60,69,78,
        6,7,8,15,16,17,18,19,20,21,22,23,24,26,34,43,52,61,70,79,
        6,7,8,15,16,17,18,19,20,21,22,23,24,25,35,44,53,62,71,80,
        0,9,18,28,29,30,31,32,33,34,35,36,37,38,45,46,47,54,63,72,
        1,10,19,27,29,30,31,32,33,34,35,36,37,38,45,46,47,55,64,73,
        2,11,20,27,28,30,31,32,33,34,35,36,37,38,45,46,47,56,65,74,
        3,12,21,27,28,29,31,32,33,34,35,39,40,41,48,49,50,57,66,75,
        4,13,22,27,28,29,30,32,33,34,35,39,40,41,48,49,50,58,67,76,
        5,14,23,27,28,29,30,31,33,34,35,39,40,41,48,49,50,59,68,77,
        6,15,24,27,28,29,30,31,32,34,35,42,43,44,51,52,53,60,69,78,
        7,16,25,27,28,29,30,31,32,33,35,42,43,44,51,52,53,61,70,79,
        8,17,26,27,28,29,30,31,32,33,34,42,43,44,51,52,53,62,71,80,
        0,9,18,27,28,29,37,38,39,40,41,42,43,44,45,46,47,54,63,72,
        1,10,19,27,28,29,36,38,39,40,41,42,43,44,45,46,47,55,64,73,
        2,11,20,27,28,29,36,37,39,40,41,42,43,44,45,46,47,56,65,74,
        3,12,21,30,31,32,36,37,38,40,41,42,43,44,48,49,50,57,66,75,
        4,13,22,30,31,32,36,37,38,39,41,42,43,44,48,49,50,58,67,76,
        5,14,23,30,31,32,36,37,38,39,40,42,43,44,48,49,50,59,68,77,
        6,15,24,33,34,35,36,37,38,39,40,41,43,44,51,52,53,60,69,78,
        7,16,25,33,34,35,36,37,38,39,40,41,42,44,51,52,53,61,70,79,
        8,17,26,33,34,35,36,37,38,39,40,41,42,43,51,52,53,62,71,80,
        0,9,18,27,28,29,36,37,38,46,47,48,49,50,51,52,53,54,63,72,
        1,10,19,27,28,29,36,37,38,45,47,48,49,50,51,52,53,55,64,73,
        2,11,20,27,28,29,36,37,38,45,46,48,49,50,51,52,53,56,65,74,
        3,12,21,30,31,32,39,40,41,45,46,47,49,50,51,52,53,57,66,75,
        4,13,22,30,31,32,39,40,41,45,46,47,48,50,51,52,53,58,67,76,
        5,14,23,30,31,32,39,40,41,45,46,47,48,49,51,52,53,59,68,77,
        6,15,24,33,34,35,42,43,44,45,46,47,48,49,50,52,53,60,69,78,
        7,16,25,33,34,35,42,43,44,45,46,47,48,49,50,51,53,61,70,79,
        8,17,26,33,34,35,42,43,44,45,46,47,48,49,50,51,52,62,71,80,
        0,9,18,27,36,45,55,56,57,58,59,60,61,62,63,64,65,72,73,74,
        1,10,19,28,37,46,54,56,57,58,59,60,61,62,63,64,65,72,73,74,
        2,11,20,29,38,47,54,55,57,58,59,60,61,62,63,64,65,72,73,74,
        3,12,21,30,39,48,54,55,56,58,59,60,61,62,66,67,68,75,76,77,
        4,13,22,31,40,49,54,55,56,57,59,60,61,62,66,67,68,75,76,77,
        5,14,23,32,41,50,54,55,56,57,58,60,61,62,66,67,68,75,76,77,
        6,15,24,33,42,51,54,55,56,57,58,59,61,62,69,70,71,78,79,80,
        7,16,25,34,43,52,54,55,56,57,58,59,60,62,69,70,71,78,79,80,
        8,17,26,35,44,53,54,55,56,57,58,59,60,61,69,70,71,78,79,80,
        0,9,18,27,36,45,54,55,56,64,65,66,67,68,69,70,71,72,73,74,
        1,10,19,28,37,46,54,55,56,63,65,66,67,68,69,70,71,72,73,74,
        2,11,20,29,38,47,54,55,56,63,64,66,67,68,69,70,71,72,73,74,
        3,12,21,30,39,48,57,58,59,63,64,65,67,68,69,70,71,75,76,77,
        4,13,22,31,40,49,57,58,59,63,64,65,66,68,69,70,71,75,76,77,
        5,14,23,32,41,50,57,58,59,63,64,65,66,67,69,70,71,75,76,77,
        6,15,24,33,42,51,60,61,62,63,64,65,66,67,68,70,71,78,79,80,
        7,16,25,34,43,52,60,61,62,63,64,65,66,67,68,69,71,78,79,80,
        8,17,26,35,44,53,60,61,62,63,64,65,66,67,68,69,70,78,79,80,
        0,9,18,27,36,45,54,55,56,63,64,65,73,74,75,76,77,78,79,80,
        1,10,19,28,37,46,54,55,56,63,64,65,72,74,75,76,77,78,79,80,
        2,11,20,29,38,47,54,55,56,63,64,65,72,73,75,76,77,78,79,80,
        3,12,21,30,39,48,57,58,59,66,67,68,72,73,74,76,77,78,79,80,
        4,13,22,31,40,49,57,58,59,66,67,68,72,73,74,75,77,78,79,80,
        5,14,23,32,41,50,57,58,59,66,67,68,72,73,74,75,76,78,79,80,
        6,15,24,33,42,51,60,61,62,69,70,71,72,73,74,75,76,77,79,80,
        7,16,25,34,43,52,60,61,62,69,70,71,72,73,74,75,76,77,78,80,
        8,17,26,35,44,53,60,61,62,69,70,71,72,73,74,75,76,77,78,79,
    ];

    private static ReadOnlySpan<byte> GetClearIndexes(int idx) =>
        ClearIndices.Slice(idx * SeenCellsCount, SeenCellsCount);

    private const int FullMask = 0b_111_111_111_0;

    private sealed class Board
    {
        [InlineArray(CellsCount)]
        private struct Candidates { private int _e; }

        [InlineArray(CellsCount)]
        private struct Solved { private bool _e; }

        [InlineArray(BoardSize * 3)]
        private struct GroupMask { private int _e; }

        private Candidates _candidates;
        private Solved _solved;
        private GroupMask _groupMask;
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

            foreach (var i in GetClearIndexes(idx))
                _candidates[i] &= removeMask;

            _candidates[idx] = valueBit;
            _solved[idx] = true;
            _solvedCount++;
        }

        private bool Propagate()
        {
            while (true)
            {
                var solvedBeforePropogation = _solvedCount;

                if (!AssignNakedSingles())
                    return false;

                for (var row = 0; row < BoardSize; row++)
                    FindHiddenSingles(GetRowIndexes(row), _groupMask[row]);

                for (var col = 0; col < BoardSize; col++)
                    FindHiddenSingles(GetColumnIndexes(col), _groupMask[BoardSize + col]);

                for (var box = 0; box < BoardSize; box++)
                    FindHiddenSingles(GetBoxIndexes(box), _groupMask[BoardSize * 2 + box]);

                if (_solvedCount == solvedBeforePropogation)
                    return true;
            }

            bool AssignNakedSingles()
            {
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