using ChessEngine.Core.Board;
using ChessEngine.Core.Evaluation;
using ChessEngine.Core.MoveGen;
using System.Diagnostics;

namespace ChessEngine.Core.Search;

// Searcher class, this is the main class of the engine, it contains the search
// algorithm and is responsible for finding the best move given a position
public class Searcher
{
    private readonly Board.Board _board;
    private readonly MoveGenerator _moveGenerator;
    private readonly Evaluator _evaluator;
    private readonly MoveOrdering _moveOrdering;

    public int NodesSearched = 0;

    private Stopwatch _timer = new();
    private long _softLimitMs;   // when to stop starting new iterations
    private long _hardLimitMs;   // absolute abort mid-search
    private bool _stopped;
    private Move _bestMoveThisIteration;
    private int _bestScoreThisIteration;

    private const int MaxDepth = 50;

    public Searcher(Board.Board board)
    {
        _board = board;
        _moveGenerator = new MoveGenerator();
        _evaluator = new Evaluator();
        _moveOrdering = new MoveOrdering();
    }

    // Stop searching flag, called from the UCI
    public void Stop() => _stopped = true;

    // Depth limited search, no time limit
    public Move FindBestMove(int depth)
    {
        return FindBestMoveIterative(depth, long.MaxValue, long.MaxValue);
    }

    // Time Limited search
    // softLimit : limit before starting a new iteration
    // hardLimit: absolute limit to abort mid-search
    public Move FindBestMoveTimed(long softLimitMs, long hardLimitMs)
    {
        return FindBestMoveIterative(MaxDepth, softLimitMs, hardLimitMs);
    }

    //public Move FindBestMoveTimed(long timeLimitMs)
    //{
    //    return FindBestMoveIterative(MaxDepth, timeLimitMs, timeLimitMs);
    //}


    // Search Algorithm : https://www.chessprogramming.org/Alpha-Beta
    //
    // The alpha-beta pruning algorithm is an improvement of the minimax algorithm that
    // reduces the number of nodes evaluated in the search tree without changing the outcome.
    // It does this by maintaining two values, alpha and beta. They represent the minimum score
    // that the maximizing player is assured of and the maximum score that the minimizing player
    // is assured of respectively.
    //
    // Iterative deepening : https://www.chessprogramming.org/Iterative_Deepening
    // It also uses iterative deepening, a time management strategy where the engine first
    // searches depth 1, then depth 2, then depth 3, etc. until it runs out of time.
    private Move FindBestMoveIterative(int maxDepth, long softLimitMs, long hardLimitMs)
    {
        _timer = Stopwatch.StartNew();

        _softLimitMs = softLimitMs;
        _hardLimitMs = hardLimitMs;
        _stopped = false;

        NodesSearched = 0;

        bool isEndgame = _evaluator.IsEndgame(_board);

        List<Move> legalMoves = _moveGenerator.GenerateLegalMoves(_board);

        if (legalMoves.Count == 0)
        {
            return Move.Null;
        }

        // forced move, no need to search
        if (legalMoves.Count == 1)
        {
            return legalMoves[0];
        }

        Move bestMove = legalMoves[0];
        int bestScore = int.MinValue + 1;

        // Iterative deepening
        for (int depth = 1; depth <= maxDepth; depth++)
        {
            _bestMoveThisIteration = Move.Null;
            _bestScoreThisIteration = int.MinValue + 1;

            legalMoves = _moveOrdering.OrderMoves(legalMoves, _board);

            int alpha = int.MinValue + 1;
            int beta = int.MaxValue;

            for (int i = 0; i < legalMoves.Count; i++)
            {
                _board.MakeMove(legalMoves[i]);

                // start recursion
                int score = -Search(depth - 1, -beta, -alpha, isEndgame);

                _board.UnmakeMove(legalMoves[i]);

                if (_stopped)
                {
                    break;
                }

                if (score > _bestScoreThisIteration)
                {
                    // keep track of best move this iteration
                    _bestScoreThisIteration = score;
                    _bestMoveThisIteration = legalMoves[i];
                }

                if (score > alpha)
                {
                    alpha = score;
                }

                if (alpha >= beta)
                {
                    break;
                }
            }

            // If we were stopped mid-search, keep previous best move, probably more reliable
            if (_stopped)
            {
                break;
            }

            // keep track of best move 
            bestMove = _bestMoveThisIteration;
            bestScore = _bestScoreThisIteration;

            // If we found a checkmate, no need to search deeper
            if (Math.Abs(bestScore) > 90000)
            {
                break;
            }

            // if we're past the softLimit, dont start a new iteration, we are unlikely to finish it
            if (_timer.ElapsedMilliseconds > _softLimitMs)
            {
                break;
            }
        }

        return bestMove;
    }

    // recursive search function, returns the evaluation of the position
    public int Search(int depth, int alpha, int beta, bool isEndgame)
    {
        // Check time every 512 nodes
        if ((NodesSearched % 512) == 0)
        {
            CheckTime();
        }

        if (_stopped)
        {
            return 0;
        }

        NodesSearched++;

        // when in check, add another depth, this allows the engine to find checkmates in 1
        bool inCheck = _moveGenerator.IsInCheck(_board, _board.ColorToMove);

        if (inCheck)
        {
            depth++;
        }

        // reached the bottom, evaluate the position with quiescence search
        // this is the stop condition for the recursion
        if (depth == 0)
        {
            return Quiesce(alpha, beta, isEndgame);
        }

        List<Move> legalMoves = _moveGenerator.GenerateLegalMoves(_board);
        legalMoves = _moveOrdering.OrderMoves(legalMoves, _board);

        // if no move, either a checkmate or a stalemate
        if (legalMoves.Count == 0)
        {
            inCheck = _moveGenerator.IsInCheck(_board, _board.ColorToMove);
            return inCheck ? -(100000 - depth) : 0;
        }

        for (int i = 0; i < legalMoves.Count; i++)
        {
            // try first move, go one level deeper
            _board.MakeMove(legalMoves[i]);

            // recurse deeper, negate because sides flip and switch beta and alpha
            int score = -Search(depth - 1, -beta, -alpha, isEndgame);

            _board.UnmakeMove(legalMoves[i]);

            if (_stopped)
            {
                return 0;
            }

            // this move is too good — opponent would have avoided this position
            if (score >= beta)
            {
                return beta;
            }

            // found a better move raise alpha
            if (score > alpha)
            {
                alpha = score;
            }
        }

        // return the best score
        return alpha;
    }

    // https://www.chessprogramming.org/Quiescence_Search
    //
    // With Quiescence search, before evaluating a position we check all the captures and
    // only evaluate once we reach a "quiet" position where there are no more captures.
    // With this we get better evaluations because we avoid moves where opponent can easily
    // take back a piece and change the evaluation drastically but the engine cant see it
    // because it is past its depth.
    private int Quiesce(int alpha, int beta, bool isEndgame)
    {
        if (_stopped)
        {
            return 0;
        }

        int positionEvaluation = _evaluator.Evaluate(_board, isEndgame);

        int best_value = positionEvaluation;

        if (best_value >= beta)
        {
            return best_value;
        }

        if (best_value > alpha)
        {
            alpha = best_value;
        }

        List<Move> legalMoves = _moveGenerator.GenerateLegalMoves(_board);

        List<Move> captureMoves = legalMoves
            .Where(move => _board.Squares[move.To] != Piece.None)
            .OrderByDescending(move => _moveOrdering.ScoreMove(move, _board))
            .ToList();

        // Play out the captures and see if any of them raise the evaluation above alpha or beta
        for (int i = 0; i < captureMoves.Count; i++)
        {
            NodesSearched++;

            _board.MakeMove(captureMoves[i]);

            int score = -Quiesce(-beta, -alpha, isEndgame);

            _board.UnmakeMove(captureMoves[i]);

            if (_stopped)
            {
                return 0;
            }

            if (score >= beta)
            {
                return score;
            }

            if (score > best_value)
            {
                best_value = score;
            }

            if (score > alpha)
            {
                alpha = score;
            }
        }

        return best_value;
    }

    private void CheckTime()
    {
        // Abort if we've exceeded the hard time limit
        if (_hardLimitMs != long.MaxValue && _timer.ElapsedMilliseconds >= _hardLimitMs)
        {
            _stopped = true;
        }
    }
}