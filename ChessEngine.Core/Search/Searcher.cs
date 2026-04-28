using ChessEngine.Core.Board;
using ChessEngine.Core.Evaluation;
using ChessEngine.Core.MoveGen;

namespace ChessEngine.Core.Search;

public class Searcher
{
    private readonly Board.Board _board;
    private readonly MoveGenerator _moveGenerator;
    private readonly Evaluator _evaluator;

    public Searcher(Board.Board board)
    {
        _board = board;
        _moveGenerator = new MoveGenerator();
        _evaluator = new Evaluator();
    }

    public Move FindBestMove(int depth)
    {
        List<Move> legalMoves = _moveGenerator.GenerateLegalMoves(_board);

        int currentBestMoveIndex = 0;
        int currentBestScore = int.MinValue + 1;
        int alpha = int.MinValue + 1;
        int beta = int.MaxValue;

        for (int i = 0; i < legalMoves.Count(); i++)
        {
            // try this move, then ask "how good is the resulting position?"
            _board.MakeMove(legalMoves[i]);

            // opponents alpha is my beta and his beta is my alpha
            int score = -Search(depth - 1, -beta, -alpha); // negate because it's now opponent's turn

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

    public int Search(int depth, int alpha, int beta)
    {
        // reached the bottom, evaluate the position
        if (depth == 0)
        {
            return _evaluator.Evaluate(_board);
        }

        List<Move> legalMoves = _moveGenerator.GenerateLegalMoves(_board);

        // if no move, either a checkmate or a stalemate
        if (legalMoves.Count == 0)
        {
            bool inCheck = _moveGenerator.IsInCheck(_board, _board.ColorToMove);
            return inCheck ? -100000 : 0;
        }

        for (int i = 0; i < legalMoves.Count(); i++)
        {
            // try first move, go one level deeper
            _board.MakeMove(legalMoves[i]);
            int score = -Search(depth - 1, -beta, -alpha); // negate because sides flip and switch beta and alpha
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
}