using ChessEngine.Core.Board;

namespace ChessEngine.Core.Evaluation;

public class Evaluator
{
    public int Evaluate(Board.Board board, bool isEndgame)
    {

        int whiteEvaluation = 0;
        int blackEvaluation = 0;

        for (int i = 0; i < 64; i++)
        {
            int piece = board.Squares[i];

            if (piece == Piece.None)
            {
                continue;
            }

            int bonus = PieceSquareTable.GetBonus(piece, i, isEndgame);

            if (Piece.IsWhite(piece))
            {
                whiteEvaluation += Piece.GetValue(piece) + bonus;
            }
            else
            {
                blackEvaluation += Piece.GetValue(piece) + bonus;
            }
        }

        int score = whiteEvaluation - blackEvaluation;

        // Return relative to side to move
        return board.ColorToMove == Piece.White ? score : -score;

    }

    public bool IsEndgame(Board.Board board)
    {
        bool noQueens = !board.Squares.Any(p => Piece.GetType(p) == Piece.Queen);
        int material = CountMaterial(board);
        return noQueens || material < 2000;
    }

    public int CountMaterial(Board.Board board)
    {
        int total = 0;

        for (int i = 0; i < board.Squares.Length; i++)
        {
            int currentValue = Piece.GetValue(board.Squares[i]);

            if (currentValue != 0)
            {
                total += currentValue;
            }
        }

        return total;
    }
}