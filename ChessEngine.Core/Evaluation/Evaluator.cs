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

        whiteEvaluation += EvaluatePawnStructure(board, Piece.White);
        blackEvaluation += EvaluatePawnStructure(board, Piece.Black);

        whiteEvaluation += EvaluateKingSafety(board, Piece.White, isEndgame);
        blackEvaluation += EvaluateKingSafety(board, Piece.Black, isEndgame);

        whiteEvaluation += EvaluateRooks(board, Piece.White);
        blackEvaluation += EvaluateRooks(board, Piece.Black);

        int score = whiteEvaluation - blackEvaluation;
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

    private int EvaluatePawnStructure(Board.Board board, int color)
    {
        int score = 0;
        int opponentColor = Piece.OppositeColor(color);

        // track pawns for each file
        int[] pawnsOnFile = new int[8];
        for (int sq = 0; sq < 64; sq++)
        {
            int piece = board.Squares[sq];
            if (piece == (color | Piece.Pawn))
            {
                pawnsOnFile[BoardHelper.FileOf(sq)]++;
            }
        }

        for (int sq = 0; sq < 64; sq++)
        {
            int piece = board.Squares[sq];
            if (piece != (color | Piece.Pawn))
            {
                continue;
            }

            int file = BoardHelper.FileOf(sq);
            int rank = BoardHelper.RankOf(sq);

            // Doubled pawn = penalty
            if (pawnsOnFile[file] > 1)
            {
                score -= 20;
            }

            // Isolated pawn = penalty
            bool leftEmpty = file == 0 || pawnsOnFile[file - 1] == 0;
            bool rightEmpty = file == 7 || pawnsOnFile[file + 1] == 0;
            if (leftEmpty && rightEmpty)
            {
                score -= 15;
            }

            // Passed pawn = bonus
            if (IsPassedPawn(board, sq, file, rank, color, opponentColor))
            {
                // closer to promotion = higher bonus
                int advancementBonus = (color == Piece.White) ? rank : 7 - rank;
                score += 20 + advancementBonus * 10;
            }
        }

        return score;
    }
    private bool IsPassedPawn(Board.Board board, int square, int file, int rank, int color, int opponentColor)
    {
        int direction = (color == Piece.White) ? 1 : -1;

        // check all squares ahead on same and adjacent files for enemy pawns
        for (int r = rank + direction; r >= 0 && r <= 7; r += direction)
        {
            for (int fileIndex = -1; fileIndex <= 1; fileIndex++)
            {
                int f = file + fileIndex;

                //skip edge of board
                if (f < 0 || f > 7)
                {
                    continue;
                }

                int sq = BoardHelper.SquareIndex(f, r);
                if (board.Squares[sq] == (opponentColor | Piece.Pawn))
                {
                    return false; // not a passed pawn, enemy pawn blocking / guarding
                }
            }
        }

        return true;
    }

    private int EvaluateKingSafety(Board.Board board, int color, bool isEndgame)
    {
        // only matters in opening and middlegame
        if (isEndgame)
        {
            return 0;
        }

        int score = 0;
        int kingSquare = board.KingSquare(color);
        int kingFile = BoardHelper.FileOf(kingSquare);
        int kingRank = BoardHelper.RankOf(kingSquare);
        int direction = color == Piece.White ? 1 : -1;

        // Pawn shield = bonus
        for (int fileIndex = -1; fileIndex <= 1; fileIndex++)
        {
            int shieldFile = kingFile + fileIndex;
            if (shieldFile < 0 || shieldFile > 7)
            {
                continue;
            }

            // one square ahead, pawn directly in front
            int shieldSq = BoardHelper.SquareIndex(shieldFile, kingRank + direction);
            if (BoardHelper.IsValidSquare(shieldSq)
                && board.Squares[shieldSq] == (color | Piece.Pawn))
            {
                score += 10;
            }

            // two squares ahead, weaker shield
            int shieldSq2 = BoardHelper.SquareIndex(shieldFile, kingRank + direction * 2);
            if (BoardHelper.IsValidSquare(shieldSq2)
                && board.Squares[shieldSq2] == (color | Piece.Pawn))
            {
                score += 5;
            }
        }

        // on open file or near it = penalty
        for (int fileIndex = -1; fileIndex <= 1; fileIndex++)
        {
            int nearFile = kingFile + fileIndex;

            // skip edge of board
            if (nearFile < 0 || nearFile > 7)
            {
                continue;
            }

            if (IsOpenFile(board, nearFile))
            {
                score -= 20;
            }
        }

        return score;
    }

    private int EvaluateRooks(Board.Board board, int color)
    {
        int score = 0;

        for (int sq = 0; sq < 64; sq++)
        {
            int piece = board.Squares[sq];
            if (piece != (color | Piece.Rook))
            {
                continue;
            }

            int file = BoardHelper.FileOf(sq);

            // rook on open file = bonus
            if (IsOpenFile(board, file))
            {
                score += 20;
            }

            // rook on Semi-open file = smaller bonus
            else if (IsSemiOpenFile(board, file, color))
            {
                score += 10;
            }

            // rook on 7th rank = bonus
            int seventhRank = color == Piece.White ? 6 : 1;
            if (BoardHelper.RankOf(sq) == seventhRank)
            {
                score += 20;
            }
        }

        return score;
    }

    // no pawn friendly or enemy on the same
    private bool IsOpenFile(Board.Board board, int file)
    {
        for (int rank = 0; rank < 8; rank++)
        {
            int sq = BoardHelper.SquareIndex(file, rank);
            int piece = board.Squares[sq];
            if (Piece.IsType(piece, Piece.Pawn))
            {
                return false;
            }
        }
        return true;
    }

    // no friendly pawn but maybe enemy pawn
    private bool IsSemiOpenFile(Board.Board board, int file, int color)
    {
        for (int rank = 0; rank < 8; rank++)
        {
            int sq = BoardHelper.SquareIndex(file, rank);
            int piece = board.Squares[sq];
            if (piece == (color | Piece.Pawn))
            {
                return false; // friendly pawn blocks it
            }
        }
        return true;
    }
}