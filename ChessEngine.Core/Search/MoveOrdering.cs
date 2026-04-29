using ChessEngine.Core.Board;

namespace ChessEngine.Core.Search;

public class MoveOrdering
{

    public List<Move> OrderMoves(List<Move> moves, Board.Board board)
    {
        return moves.OrderByDescending(move => ScoreMove(move, board)).ToList();
    }

    public int ScoreMove(Move move, Board.Board board)
    {

        bool isCapture = board.Squares[move.To] != Piece.None;
        int score = 0;

        if (move.IsPromotion)
        {
            score += 3000;
        }

        if (isCapture)
        {
            int victimValue = board.Squares[move.To];
            int attackerValue = board.Squares[move.From];
            score += 1000 + GetPieceValue(victimValue) - GetPieceValue(attackerValue);
        }

        return score;

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