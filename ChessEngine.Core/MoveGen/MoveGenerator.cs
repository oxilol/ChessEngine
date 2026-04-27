using ChessEngine.Core.Board;

namespace ChessEngine.Core.MoveGen;

public class MoveGenerator
{
    private static readonly int[] KnightOffsets = { -17, -15, -10, -6, 6, 10, 15, 17 };
    private static readonly int[] KingOffsets = { -9, -8, -7, -1, 1, 7, 8, 9 };
    private static readonly int[] RookDirs = { 8, -8, 1, -1 };
    private static readonly int[] BishopDirs = { 9, -9, 7, -7 };

    private Board.Board _board = null!;
    private List<Move> _moves = null!;
    private int _color;
    private int _opponentColor;

    public List<Move> GenerateLegalMoves(Board.Board board)
    {
        _board = board;
        _color = board.ColorToMove;
        _opponentColor = Piece.OppositeColor(_color);
        _moves = new List<Move>(64);

        GeneratePseudoLegalMoves();
        FilterIllegalMoves();

        return _moves;
    }

    private void GeneratePseudoLegalMoves()
    {
        for (int sq = 0; sq < 64; sq++)
        {
            int piece = _board.Squares[sq];

            //skip empty squares and opponent pieces
            if (piece == Piece.None || !Piece.IsColor(piece, _color)) continue;

            switch (Piece.GetType(piece))
            {
                case Piece.Pawn: GeneratePawnMoves(sq); break;
                case Piece.Knight: GenerateKnightMoves(sq); break;
                case Piece.Bishop: GenerateSlidingMoves(sq, true); break;
                case Piece.Rook: GenerateSlidingMoves(sq, false); break;
                case Piece.Queen:
                    GenerateSlidingMoves(sq, true);
                    GenerateSlidingMoves(sq, false);
                    break;
                case Piece.King: GenerateKingMoves(sq); break;
            }
        }
    }

    private void GenerateKnightMoves(int from)
    {
        int fromFile = BoardHelper.FileOf(from);

        foreach (int offset in KnightOffsets)
        {
            int to = from + offset;
            if (!BoardHelper.IsValidSquare(to)) continue;

            // Guard against wrap-around
            if (Math.Abs(BoardHelper.FileOf(to) - fromFile) > 2) continue;

            int target = _board.Squares[to];

            // add to list if moves to an empty square or capture an opponent piece
            if (target == Piece.None || Piece.IsColor(target, _opponentColor))
                _moves.Add(new Move(from, to));
        }
    }

    private void GenerateSlidingMoves(int from, bool diagonal)
    {
        int[] dirs = diagonal ? BishopDirs : RookDirs;
        int fromFile = BoardHelper.FileOf(from);

        foreach (int dir in dirs)
        {
            int sq = from;

            while (true)
            {
                int prevFile = BoardHelper.FileOf(sq);
                sq += dir;

                if (!BoardHelper.IsValidSquare(sq)) break;

                // Prevent wrap-around
                if (Math.Abs(BoardHelper.FileOf(sq) - prevFile) > 1) break;

                int target = _board.Squares[sq];

                if (target == Piece.None)
                {
                    _moves.Add(new Move(from, sq));
                }
                else
                {
                    // Can capture opponent piece but must stop
                    if (Piece.IsColor(target, _opponentColor))
                        _moves.Add(new Move(from, sq));
                    break;
                }
            }
        }
    }

    private void GenerateKingMoves(int from)
    {
        int fromFile = BoardHelper.FileOf(from);

        foreach (int offset in KingOffsets)
        {
            int to = from + offset;
            if (!BoardHelper.IsValidSquare(to)) continue;

            if (Math.Abs(BoardHelper.FileOf(to) - fromFile) > 1) continue;

            int target = _board.Squares[to];

            if (target == Piece.None || Piece.IsColor(target, _opponentColor))
                _moves.Add(new Move(from, to));
        }

        GenerateCastlingMoves(from);
    }

    private void GenerateCastlingMoves(int kingSquare)
    {
        // King must not currently be in check
        if (IsSquareAttacked(kingSquare, _opponentColor)) return;

        if (_color == Piece.White)
        {
            // Kingside (e1 to g1)
            if (_board.CastlingRights[0]
                && _board.Squares[5] == Piece.None
                && _board.Squares[6] == Piece.None
                && !IsSquareAttacked(5, _opponentColor))
                _moves.Add(new Move(kingSquare, kingSquare + 2));

            // Queenside (e1 to c1)
            if (_board.CastlingRights[1]
                && _board.Squares[3] == Piece.None
                && _board.Squares[2] == Piece.None
                && _board.Squares[1] == Piece.None
                && !IsSquareAttacked(3, _opponentColor))
                _moves.Add(new Move(kingSquare, kingSquare - 2));
        }
        else
        {
            // Kingside (e8 to g8)
            if (_board.CastlingRights[2]
                && _board.Squares[61] == Piece.None
                && _board.Squares[62] == Piece.None
                && !IsSquareAttacked(61, _opponentColor))
                _moves.Add(new Move(kingSquare, kingSquare + 2));

            // Queenside (e8 to c8)
            if (_board.CastlingRights[3]
                && _board.Squares[59] == Piece.None
                && _board.Squares[58] == Piece.None
                && _board.Squares[57] == Piece.None
                && !IsSquareAttacked(59, _opponentColor))
                _moves.Add(new Move(kingSquare, kingSquare - 2));
        }
    }

    private void GeneratePawnMoves(int from)
    {
        int rank = BoardHelper.RankOf(from);
        int file = BoardHelper.FileOf(from);
        int dir = _color == Piece.White ? 1 : -1;
        int startRank = _color == Piece.White ? 1 : 6;
        int promoRank = _color == Piece.White ? 7 : 0;

        // signle push
        int onePush = from + dir * 8;
        if (BoardHelper.IsValidSquare(onePush) && _board.Squares[onePush] == Piece.None)
        {
            if (BoardHelper.RankOf(onePush) == promoRank)
                AddPromotions(from, onePush);
            else
            {
                _moves.Add(new Move(from, onePush));

                // Double push
                if (rank == startRank)
                {
                    int twoPush = onePush + dir * 8;
                    if (_board.Squares[twoPush] == Piece.None)
                        _moves.Add(new Move(from, twoPush));
                }
            }
        }

        // Captures
        foreach (int captureFileDelta in new[] { -1, 1 })
        {
            int captureFile = file + captureFileDelta;
            if (captureFile < 0 || captureFile > 7) continue;

            int captureSq = from + dir * 8 + captureFileDelta;
            if (!BoardHelper.IsValidSquare(captureSq)) continue;

            int target = _board.Squares[captureSq];

            // Normal capture
            if (target != Piece.None && Piece.IsColor(target, _opponentColor))
            {
                if (BoardHelper.RankOf(captureSq) == promoRank)
                    AddPromotions(from, captureSq);
                else
                    _moves.Add(new Move(from, captureSq));
            }

            // En passant
            if (_board.EnPassantFile == captureFile)
            {
                int epRank = _color == Piece.White ? 4 : 3;
                if (rank == epRank)
                    _moves.Add(new Move(from, captureSq));
            }
        }
    }

    private void AddPromotions(int from, int to)
    {
        _moves.Add(new Move(from, to, Piece.Queen));
        _moves.Add(new Move(from, to, Piece.Rook));
        _moves.Add(new Move(from, to, Piece.Bishop));
        _moves.Add(new Move(from, to, Piece.Knight));
    }

    private void FilterIllegalMoves()
    {
        for (int i = _moves.Count - 1; i >= 0; i--)
        {
            _board.MakeMove(_moves[i]);

            // After the move, check if the king that just moved is in check
            bool inCheck = IsSquareAttacked(
                _board.KingSquare(_color),
                _opponentColor);

            _board.UnmakeMove(_moves[i]);

            if (inCheck) _moves.RemoveAt(i);
        }
    }

    public bool IsSquareAttacked(int square, int byColor)
    {
        int byOpponent = Piece.OppositeColor(byColor);

        // Knight attacks
        int sqFile = BoardHelper.FileOf(square);
        foreach (int offset in KnightOffsets)
        {
            int sq = square + offset;
            if (!BoardHelper.IsValidSquare(sq)) continue;
            if (Math.Abs(BoardHelper.FileOf(sq) - sqFile) > 2) continue;
            if (_board.Squares[sq] == (byColor | Piece.Knight)) return true;
        }

        // Sliding attacks (rook/queen on ranks+files, bishop/queen on diagonals)
        foreach (int dir in RookDirs)
        {
            int sq = square;
            while (true)
            {
                int prevFile = BoardHelper.FileOf(sq);
                sq += dir;
                if (!BoardHelper.IsValidSquare(sq)) break;
                if (Math.Abs(BoardHelper.FileOf(sq) - prevFile) > 1) break;
                int piece = _board.Squares[sq];
                if (piece == Piece.None) continue;
                if (Piece.IsColor(piece, byColor)
                    && (Piece.IsType(piece, Piece.Rook) || Piece.IsType(piece, Piece.Queen)))
                    return true;
                break;
            }
        }

        foreach (int dir in BishopDirs)
        {
            int sq = square;
            while (true)
            {
                int prevFile = BoardHelper.FileOf(sq);
                sq += dir;
                if (!BoardHelper.IsValidSquare(sq)) break;
                if (Math.Abs(BoardHelper.FileOf(sq) - prevFile) > 1) break;
                int piece = _board.Squares[sq];
                if (piece == Piece.None) continue;
                if (Piece.IsColor(piece, byColor)
                    && (Piece.IsType(piece, Piece.Bishop) || Piece.IsType(piece, Piece.Queen)))
                    return true;
                break;
            }
        }

        // Pawn attacks
        int pawnDir = byColor == Piece.White ? -1 : 1;
        foreach (int fileDelta in new[] { -1, 1 })
        {
            int attackFile = BoardHelper.FileOf(square) + fileDelta;
            if (attackFile < 0 || attackFile > 7) continue;
            int attackSq = square + pawnDir * 8 + fileDelta;
            if (!BoardHelper.IsValidSquare(attackSq)) continue;
            if (_board.Squares[attackSq] == (byColor | Piece.Pawn)) return true;
        }

        // King attacks
        int kingFile = BoardHelper.FileOf(square);
        foreach (int offset in KingOffsets)
        {
            int sq = square + offset;
            if (!BoardHelper.IsValidSquare(sq)) continue;
            if (Math.Abs(BoardHelper.FileOf(sq) - kingFile) > 1) continue;
            if (_board.Squares[sq] == (byColor | Piece.King)) return true;
        }

        return false;
    }

    public bool IsInCheck(Board.Board board, int color) =>
        IsSquareAttacked(board.KingSquare(color), Piece.OppositeColor(color));
}