using ChessEngine.Core.Board;
using ChessEngine.Core.Evaluation;
using ChessEngine.Core.MoveGen;

namespace ChessEngine.Core.Search;

public class Searcher
{
    private readonly Board.Board _board;
    private readonly MoveGenerator _moveGenerator;
    private readonly Evaluator _evaluator;
    private readonly MoveOrdering _moveOrdering;

    public int NodesSearched = 0;

    public Searcher(Board.Board board)
    {
        _board = board;
        _moveGenerator = new MoveGenerator();
        _evaluator = new Evaluator();
        _moveOrdering = new MoveOrdering();
    }

    // Algorithm : https://www.chessprogramming.org/Alpha-Beta

    public Move FindBestMove(int depth)
    {
        NodesSearched = 0;
        bool isEndgame = _evaluator.IsEndgame(_board);

        List<Move> legalMoves = _moveGenerator.GenerateLegalMoves(_board);

        legalMoves = _moveOrdering.OrderMoves(legalMoves, _board);

        int currentBestMoveIndex = 0;
        int currentBestScore = int.MinValue + 1;
        int alpha = int.MinValue + 1;
        int beta = int.MaxValue;

        for (int i = 0; i < legalMoves.Count(); i++)
        {
            // try this move, then ask "how good is the resulting position?"
            _board.MakeMove(legalMoves[i]);

            // opponents alpha is my beta and his beta is my alpha
            int score = -Search(depth - 1, -beta, -alpha, isEndgame); // negate because it's now opponent's turn

            _board.UnmakeMove(legalMoves[i]);

            // best move we found so far?
            if (score > currentBestScore)
            {
                currentBestScore = score;
                currentBestMoveIndex = i;
            }

            // raise alpha — we now know we can do at least this well
            if (score > alpha)
            {
                alpha = score;
            }

            // opponent already has a better option we cant reach this so no point evaluating the remaining moves
            if (alpha >= beta)
            {
                break;
            }
        }

        return legalMoves[currentBestMoveIndex];
    }

    public int Search(int depth, int alpha, int beta, bool isEndgame)
    {
        NodesSearched++;

        // reached the bottom, evaluate the position
        if (depth == 0)
        {
            return Quiesce(alpha, beta, isEndgame);
        }

        List<Move> legalMoves = _moveGenerator.GenerateLegalMoves(_board);

        legalMoves = _moveOrdering.OrderMoves(legalMoves, _board);

        // if no move, either a checkmate or a stalemate
        if (legalMoves.Count == 0)
        {
            bool inCheck = _moveGenerator.IsInCheck(_board, _board.ColorToMove);
            return inCheck ? -(100000 - depth) : 0;
        }

        for (int i = 0; i < legalMoves.Count(); i++)
        {
            // try first move, go one level deeper
            _board.MakeMove(legalMoves[i]);
            int score = -Search(depth - 1, -beta, -alpha, isEndgame); // negate because sides flip and switch beta and alpha
            _board.UnmakeMove(legalMoves[i]);

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

    // ref : https://www.chessprogramming.org/Quiescence_Search
    int Quiesce(int alpha, int beta, bool isEndgame)
    {
        int static_eval = _evaluator.Evaluate(_board, isEndgame);

        // Stand Pat
        int best_value = static_eval;
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

        for (int i = 0; i < captureMoves.Count(); i++)
        {

            _board.MakeMove(captureMoves[i]);

            int score = -Quiesce(-beta, -alpha, isEndgame);

            _board.UnmakeMove(captureMoves[i]);

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
}