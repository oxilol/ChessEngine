using ChessEngine.Core.Board;

namespace ChessEngine.Core.Search;

// Ordering move is useful for alpha-beta pruning because it allows the engine to search the moves
// that are most promising first like captures and promotions, this way it can cutoff more branches of
// the tree and search deeper faster and more efficiently !
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