namespace ChessEngine.Core.Board;

public static class Piece
{
    // Types (bits 0–2)
    public const int None = 0b000;
    public const int Pawn = 0b001;
    public const int Knight = 0b010;
    public const int Bishop = 0b011;
    public const int Rook = 0b100;
    public const int Queen = 0b101;
    public const int King = 0b110;

    // Colors (bits 3–4)
    public const int White = 0b01000;
    public const int Black = 0b10000;

    // Combined pieces
    public const int WhitePawn   = White | Pawn;
    public const int WhiteKnight = White | Knight;
    public const int WhiteBishop = White | Bishop;
    public const int WhiteRook   = White | Rook;
    public const int WhiteQueen  = White | Queen;
    public const int WhiteKing   = White | King;
    public const int BlackPawn   = Black | Pawn;
    public const int BlackKnight = Black | Knight;
    public const int BlackBishop = Black | Bishop;
    public const int BlackRook   = Black | Rook;
    public const int BlackQueen  = Black | Queen;
    public const int BlackKing   = Black | King;

    public static int GetType(int piece)  => piece & 0b00111; 
    public static int GetColor(int piece) => piece & 0b11000;
    public static bool IsWhite(int piece) => (piece & White) != 0;
    public static bool IsBlack(int piece) => (piece & Black) != 0;
    public static bool IsColor(int piece, int color) => GetColor(piece) == color;
    public static bool IsType(int piece, int type)   => GetType(piece) == type;
    public static int OppositeColor(int color) => color == White ? Black : White;

    public static int FromChar(char c)
    {
        switch (c)
        {
            case 'P': return WhitePawn;
            case 'N': return WhiteKnight;
            case 'B': return WhiteBishop;
            case 'R': return WhiteRook;
            case 'Q': return WhiteQueen;
            case 'K': return WhiteKing;
            case 'p': return BlackPawn;
            case 'n': return BlackKnight;
            case 'b': return BlackBishop;
            case 'r': return BlackRook;
            case 'q': return BlackQueen;
            case 'k': return BlackKing;
            default: return None;
        }
    }

    public static char ToChar(int piece)
    {
        switch (GetType(piece))
        {
            case Pawn: return IsWhite(piece) ? 'P' : 'p';
            case Knight: return IsWhite(piece) ? 'N' : 'n';
            case Bishop: return IsWhite(piece) ? 'B' : 'b';
            case Rook: return IsWhite(piece) ? 'R' : 'r';
            case Queen: return IsWhite(piece) ? 'Q' : 'q';
            case King: return IsWhite(piece) ? 'K' : 'k';
            default: return '.';
        }
    }

    // Maps to the UI image filename like "wK" or "bP"
    public static string ToImageKey(int piece)
    {
        if (piece == None) return "";
        char color = IsWhite(piece) ? 'w' : 'b';
        char type  = char.ToUpper(ToChar(piece));
        return $"{color}{type}";
    }
}