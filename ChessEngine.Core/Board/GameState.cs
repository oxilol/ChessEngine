namespace ChessEngine.Core.Board;

// Checkpoint of irreversible state saved before each move
public readonly struct GameState
{
    public readonly int CapturedPiece;
    public readonly int EnPassantFile;
    public readonly bool[] CastlingRights;
    public readonly int HalfMoveClock;
    public readonly int WhiteKingSquare;
    public readonly int BlackKingSquare;

    public GameState(
        int capturedPiece,
        int enPassantFile,
        bool[] castlingRights,
        int halfMoveClock,
        int whiteKingSquare,
        int blackKingSquare)
    {
        CapturedPiece   = capturedPiece;
        EnPassantFile   = enPassantFile;
        CastlingRights  = castlingRights;
        HalfMoveClock   = halfMoveClock;
        WhiteKingSquare = whiteKingSquare;
        BlackKingSquare = blackKingSquare;
    }
}