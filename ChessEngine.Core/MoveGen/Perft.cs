using ChessEngine.Core.Board;

namespace ChessEngine.Core.MoveGen;

public class Perft
{
    private readonly MoveGenerator _moveGen = new();

    public long Run(Board.Board board, int depth)
    {
        if (depth == 0) return 1;

        var moves = _moveGen.GenerateLegalMoves(board);
        long nodes = 0;

        foreach (var move in moves)
        {
            board.MakeMove(move);
            nodes += Run(board, depth - 1);
            board.UnmakeMove(move);
        }

        return nodes;
    }

    public void Divide(Board.Board board, int depth)
    {
        var moves = _moveGen.GenerateLegalMoves(board);
        long total = 0;

        // Sort alphabetically so output is easy to compare against references
        //var sorted = moves.OrderBy(m => m.ToString()).ToList();

        foreach (var move in moves)
        {
            board.MakeMove(move);
            long nodes = Run(board, depth - 1);
            board.UnmakeMove(move);

            Console.WriteLine($"{move}: {nodes}");
            total += nodes;
        }

        Console.WriteLine();
        Console.WriteLine($"Total: {total}");
    }
}