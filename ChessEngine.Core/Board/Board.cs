namespace ChessEngine.Core.Board;

public class Board
{
    public const string StartFEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    public int[] Squares = new int[64];
    public int ColorToMove;
    public int EnPassantFile;                       // -1 if none
    public bool[] CastlingRights = new bool[4];     // [wK, wQ, bK, bQ]
    public int HalfMoveClock;
    public int FullMoveNumber;

    // king positions for fast check detection
    public int WhiteKingSquare;
    public int BlackKingSquare;

    private readonly Stack<GameState> _history = new();

    // Constructors
    public Board()
    {
        LoadFEN(StartFEN);
    }

    public Board(string fen)
    {
        LoadFEN(fen);
    }

    public Board Clone()
    {
        var clone = new Board();
        clone.Squares = (int[])Squares.Clone();
        clone.ColorToMove = ColorToMove;
        clone.EnPassantFile = EnPassantFile;
        clone.CastlingRights = (bool[])CastlingRights.Clone();
        clone.HalfMoveClock = HalfMoveClock;
        clone.FullMoveNumber = FullMoveNumber;
        clone.WhiteKingSquare = WhiteKingSquare;
        clone.BlackKingSquare = BlackKingSquare;
        return clone;
    }

    // FEN Parsing and Generation

    public void LoadFEN(string fen)
    {
        Array.Clear(Squares, 0, 64);
        CastlingRights = new bool[4];
        _history.Clear();

        var parts = fen.Trim().Split(' ');

        // Part 0 — piece placement
        int rank = 7;
        int file = 0;
        foreach (char c in parts[0])
        {
            if (c == '/')
            {
                // a "/", means a new rank
                rank--;
                file = 0;
            }
            else if (char.IsDigit(c))
            {
                // a digit means we skip x squares
                file += int.Parse(c.ToString());
            }
            else
            {
                // a char means a piece
                int square = BoardHelper.SquareIndex(file, rank);
                int piece = Piece.FromChar(c);
                Squares[square] = piece;

                if (piece == Piece.WhiteKing)
                {
                    WhiteKingSquare = square;
                }

                if (piece == Piece.BlackKing)
                {
                    BlackKingSquare = square;
                }

                file++;
            }
        }

        // Part 1 — side to move

        if (parts[1] == "w")
        {
            ColorToMove = Piece.White;
        }
        else
        {
            ColorToMove = Piece.Black;
        }

        // Part 2 — castling rights
        CastlingRights[0] = parts[2].Contains('K');
        CastlingRights[1] = parts[2].Contains('Q');
        CastlingRights[2] = parts[2].Contains('k');
        CastlingRights[3] = parts[2].Contains('q');

        // Part 3 — en passant

        if (parts[3] == "-")
        {
            EnPassantFile = -1;
        }
        else
        {
            EnPassantFile = parts[3][0] switch
            {
                'a' => 0,
                'b' => 1,
                'c' => 2,
                'd' => 3,
                'e' => 4,
                'f' => 5,
                'g' => 6,
                'h' => 7,
                _ => -1
            };
        }

        // Part 4 — counts the number of moves since the last pawn move or capture (used for the fifty-move rule)
        HalfMoveClock = int.Parse(parts[4]);

        // Part 5 - counts number of moves total
        FullMoveNumber = int.Parse(parts[5]);
    }

    public String ToFEN()
    {

        var sb = new System.Text.StringBuilder();


        int fileEmptyCounter = 0;

        for (int rank = 8; rank > 0; rank--)
        {
            fileEmptyCounter = 0;

            for (int file = 8; file > 0; file--)
            {

                int piece = Squares[(rank * 8) - file];

                if (piece != 0)
                {
                    if (fileEmptyCounter != 0)
                    {
                        sb.Append(fileEmptyCounter);
                        fileEmptyCounter = 0;
                    }
                    sb.Append(Piece.ToChar(piece));
                }
                else
                {
                    fileEmptyCounter++;
                }

                if (file == 1 && fileEmptyCounter != 0)
                {
                    sb.Append(fileEmptyCounter);
                }
            }

            if (rank != 1)
            {
                sb.Append("/");
            }

        }

        sb.Append(ColorToMove == Piece.White ? " w " : " b ");

        sb.Append(CastlingRights[0] ? "K" : "");
        sb.Append(CastlingRights[1] ? "Q" : "");
        sb.Append(CastlingRights[2] ? "k" : "");
        sb.Append(CastlingRights[3] ? "q" : "");

        sb.Append(' ');

        sb.Append(EnPassantFile == -1 ? "-"
            : $"{(char)('a' + EnPassantFile)}{(ColorToMove == Piece.White ? 6 : 3)}");

        sb.Append($" {HalfMoveClock} {FullMoveNumber}");


        return sb.ToString();

    }

    // ─── Make / Unmake ────────────────────────────────────────────────────────

    public void MakeMove(Move move)
    {
        int from = move.From;
        int to = move.To;
        int piece = Squares[from];
        int captured = Squares[to];
        int color = Piece.GetColor(piece);

        // Save state
        _history.Push(new GameState(
            captured,
            EnPassantFile,
            (bool[])CastlingRights.Clone(),
            HalfMoveClock,
            WhiteKingSquare,
            BlackKingSquare
        ));

        // Move the piece
        Squares[to] = piece;
        Squares[from] = Piece.None;

        // Update king position
        if (piece == Piece.WhiteKing)
        {
            WhiteKingSquare = to;
        }

        if (piece == Piece.BlackKing)
        {
            BlackKingSquare = to;
        }

        // Handle en passant capture
        if (Piece.IsType(piece, Piece.Pawn) && to == EnPassantCaptureSquare())
        {
            int capturedPawnSquare = color == Piece.White ? to - 8 : to + 8;
            Squares[capturedPawnSquare] = Piece.None;
        }

        // Handle castling rook move
        if (Piece.IsType(piece, Piece.King))
        {
            int fileDiff = BoardHelper.FileOf(to) - BoardHelper.FileOf(from);
            if (Math.Abs(fileDiff) == 2)
            {
                // 2 = king side, -2 = queen side
                bool kingside = fileDiff > 0;

                int rookFrom = kingside ? from + 3 : from - 4;
                int rookTo = kingside ? from + 1 : from - 1;

                Squares[rookTo] = Squares[rookFrom];
                Squares[rookFrom] = Piece.None;
            }
        }

        // Handle promotion
        if (move.IsPromotion)
        {
            Squares[to] = Piece.GetColor(piece) | move.PromoPiece;
        }

        // Update en passant file
        bool isDoublePawnPush = Piece.IsType(piece, Piece.Pawn)
                             && Math.Abs(BoardHelper.RankOf(to) - BoardHelper.RankOf(from)) == 2;

        if (isDoublePawnPush)
        {
            EnPassantFile = BoardHelper.FileOf(to);
        }
        else
        {
            EnPassantFile = -1;
        }

        // Update castling rights
        UpdateCastlingRights(from, to);

        // Update half move clock
        bool isCapture = captured != Piece.None;
        bool isPawnMove = Piece.IsType(piece, Piece.Pawn);
        HalfMoveClock = (isCapture || isPawnMove) ? 0 : HalfMoveClock + 1;

        // Update FullMoveNumber
        if (ColorToMove == Piece.Black)
        {
            FullMoveNumber++;
        }

        ColorToMove = Piece.OppositeColor(ColorToMove);
    }

    public void UnmakeMove(Move move)
    {
        ColorToMove = Piece.OppositeColor(ColorToMove);
        if (ColorToMove == Piece.Black)
        {
            FullMoveNumber--;
        }

        GameState state = _history.Pop();

        int from = move.From;
        int to = move.To;
        int piece = Squares[to];
        int color = ColorToMove;

        // Restore piece (undo promotion first)
        if (move.IsPromotion)
        {
            piece = color | Piece.Pawn;
        }

        Squares[from] = piece;
        Squares[to] = state.CapturedPiece;

        // Restore king positions
        WhiteKingSquare = state.WhiteKingSquare;
        BlackKingSquare = state.BlackKingSquare;

        // Undo en passant capture
        if (Piece.IsType(piece, Piece.Pawn))
        {
            // Restore captured pawn if it was en passant
            int prevEnPassantFile = state.EnPassantFile;
            if (prevEnPassantFile != -1 && BoardHelper.FileOf(to) == prevEnPassantFile)
            {
                int epRank = color == Piece.White ? 4 : 3;
                if (BoardHelper.RankOf(from) == epRank)
                {
                    int capturedPawnSquare = color == Piece.White ? to - 8 : to + 8;
                    Squares[capturedPawnSquare] = Piece.OppositeColor(color) | Piece.Pawn;
                    Squares[to] = Piece.None;
                }
            }
        }

        // Undo castling rook move
        if (Piece.IsType(piece, Piece.King))
        {
            int fileDiff = BoardHelper.FileOf(to) - BoardHelper.FileOf(from);
            if (Math.Abs(fileDiff) == 2)
            {
                bool kingside = fileDiff > 0;
                int rookFrom = kingside ? from + 3 : from - 4;
                int rookTo = kingside ? from + 1 : from - 1;
                Squares[rookFrom] = Squares[rookTo];
                Squares[rookTo] = Piece.None;
            }
        }

        // Restore saved state
        EnPassantFile = state.EnPassantFile;
        CastlingRights = state.CastlingRights;
        HalfMoveClock = state.HalfMoveClock;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private int EnPassantCaptureSquare()
    {
        if (EnPassantFile == -1)
        {
            return -1;
        }

        int rank = ColorToMove == Piece.White ? 5 : 2;
        return BoardHelper.SquareIndex(EnPassantFile, rank);
    }

    private void UpdateCastlingRights(int from, int to)
    {
        // If king moves, lose both castling rights for that color
        if (from == 4) { CastlingRights[0] = false; CastlingRights[1] = false; } // white king
        if (from == 60) { CastlingRights[2] = false; CastlingRights[3] = false; } // black king

        // If a rook moves or is captured, lose that specific right
        if (from == 7 || to == 7)
        {
            CastlingRights[0] = false; // white kingside rook
        }

        if (from == 0 || to == 0)
        {
            CastlingRights[1] = false; // white queenside rook
        }

        if (from == 63 || to == 63)
        {
            CastlingRights[2] = false; // black kingside rook
        }

        if (from == 56 || to == 56)
        {
            CastlingRights[3] = false; // black queenside rook
        }
    }

    public int KingSquare(int color) =>
        color == Piece.White ? WhiteKingSquare : BlackKingSquare;

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        for (int rank = 7; rank >= 0; rank--)
        {
            for (int file = 0; file < 8; file++)
            {
                int p = Squares[BoardHelper.SquareIndex(file, rank)];
                sb.Append(p == Piece.None ? '.' : Piece.ToChar(p));
                sb.Append(' ');
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}