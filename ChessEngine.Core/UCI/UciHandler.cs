using ChessEngine.Core.Board;
using ChessEngine.Core.MoveGen;
using ChessEngine.Core.Search;

namespace ChessEngine.Core.UCI;

// UCI handler class, responsible for parsing UCI commands and interacting with the searcher and board
// it allows the engine to be used with cute chess to test it's strength against other engines
public class UciHandler
{
    private Board.Board _board;
    private MoveGenerator _moveGen;
    private Searcher? _searcher;

    private const int DefaultDepth = 4;

    public UciHandler()
    {
        _board = new Board.Board();
        _moveGen = new MoveGenerator();
    }

    public void Loop()
    {
        var stdout = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
        Console.SetOut(stdout);

        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            var tokens = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0)
            {
                continue;
            }

            switch (tokens[0])
            {
                case "uci":
                    Console.WriteLine("id name NathEngine");
                    Console.WriteLine("id author Nathan Croteau");
                    Console.WriteLine("uciok");
                    break;

                case "isready":
                    Console.WriteLine("readyok");
                    break;

                case "ucinewgame":
                    _board = new Board.Board();
                    _searcher = null;
                    break;

                case "position":
                    HandlePosition(tokens);
                    break;

                case "go":
                    HandleGo(tokens);
                    break;

                case "stop":
                    _searcher?.Stop();
                    break;

                case "quit":
                    return;
            }
        }
    }

    private void HandleGo(string[] tokens)
    {
        int? depth = null;
        long? moveTime = null;
        long wtime = -1;
        long btime = -1;
        long winc = 0;
        long binc = 0;

        int movestogo = -1;

        for (int i = 1; i < tokens.Length - 1; i++)
        {
            switch (tokens[i])
            {
                case "depth":
                    depth = int.Parse(tokens[i + 1]);
                    break;
                case "movetime":
                    moveTime = long.Parse(tokens[i + 1]);
                    break;
                case "wtime":
                    wtime = long.Parse(tokens[i + 1]);
                    break;
                case "btime":
                    btime = long.Parse(tokens[i + 1]);
                    break;
                case "winc":
                    winc = long.Parse(tokens[i + 1]);
                    break;
                case "binc":
                    binc = long.Parse(tokens[i + 1]);
                    break;
                case "movestogo":
                    movestogo = int.Parse(tokens[i + 1]);
                    break;
                case "infinite":
                    depth = 64;
                    break;
            }
        }

        _searcher = new Searcher(_board.Clone());
        Move best;

        if (depth.HasValue)
        {
            // Fixed depth search
            best = _searcher.FindBestMove(depth.Value);
        }
        else if (wtime >= 0 || btime >= 0)
        {
            // Time control: allocate a percentage of remaining time

            long remaining = _board.ColorToMove == Piece.White ? wtime : btime;
            long increment = _board.ColorToMove == Piece.White ? winc : binc;

            long softLimit;
            long hardLimit;

            if (movestogo > 0)
            {
                // We know how many moves until next time control
                softLimit = remaining / movestogo + increment * 3 / 4;
                hardLimit = Math.Min(remaining / 2, softLimit * 4);
            }
            else
            {
                // Sudden death or unknown

                // 5% of remaining time + 75% of increment for soft limit
                softLimit = remaining / 20 + increment * 3 / 4;

                // 3x the soft limit
                hardLimit = Math.Min(remaining * 2 / 5, softLimit * 3);
            }

            // for safety never use more than 80% of remaining time
            hardLimit = Math.Min(hardLimit, remaining * 4 / 5);
            softLimit = Math.Min(softLimit, hardLimit);

            // avoid instant moves
            softLimit = Math.Max(50, softLimit);
            hardLimit = Math.Max(100, hardLimit);

            best = _searcher.FindBestMoveTimed(softLimit, hardLimit);
        }
        else
        {
            // Fallback to fixed depth
            best = _searcher.FindBestMove(DefaultDepth);
        }

        Console.WriteLine($"bestmove {best}");
    }

    private void HandlePosition(string[] tokens)
    {
        int movesIndex = -1;

        if (tokens.Length < 2)
        {
            return;
        }

        if (tokens[1] == "startpos")
        {
            _board.LoadFEN(Board.Board.StartFEN);

            // Find where "moves" keyword is
            int movesKeyword = Array.IndexOf(tokens, "moves");
            if (movesKeyword >= 0)
            {
                movesIndex = movesKeyword + 1;
            }
        }
        else if (tokens[1] == "fen")
        {
            // Find the "moves" keyword (if present)
            int fenEnd = Array.IndexOf(tokens, "moves");

            if (fenEnd < 0)
            {
                // No moves, entire remainder is the FEN
                string fen = string.Join(" ", tokens[2..]);
                _board.LoadFEN(fen);
            }
            else
            {
                string fen = string.Join(" ", tokens[2..fenEnd]);
                _board.LoadFEN(fen);
                movesIndex = fenEnd + 1;
            }
        }

        // Apply moves
        if (movesIndex > 0 && movesIndex < tokens.Length)
        {
            for (int i = movesIndex; i < tokens.Length; i++)
            {
                var legalMoves = _moveGen.GenerateLegalMoves(_board);
                var move = legalMoves.FirstOrDefault(m => m.ToString() == tokens[i]);
                if (!move.IsNull)
                {
                    _board.MakeMove(move);
                }
            }
        }
    }
}
