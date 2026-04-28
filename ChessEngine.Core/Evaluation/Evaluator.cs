using ChessEngine.Core.Board;

namespace ChessEngine.Core.Evaluation;

public class Evaluator
{
    public int Evaluate(Board.Board board)
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

            int value = GetPieceValue(piece);

            if (Piece.IsWhite(piece))
            {
                whiteEvaluation += value;
            }
            else
            {
                blackEvaluation += value;
            }
        }

        int score = whiteEvaluation - blackEvaluation;

        // Return relative to side to move
        return board.ColorToMove == Piece.White ? score : -score;

    }

    public int GetPieceValue(int piece) => Piece.GetType(piece) switch
    {
        Piece.Pawn => 100,
        Piece.Knight => 300,
        Piece.Bishop => 320,
        Piece.Rook => 500,
        Piece.Queen => 900,
        _ => 0
    };
}