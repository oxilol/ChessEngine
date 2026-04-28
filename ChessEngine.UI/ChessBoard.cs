using ChessEngine.Core.Board;
using ChessEngine.Core.MoveGen;
using ChessEngine.Core.Search;
using Svg;

namespace ChessEngine.UI;

public class ChessBoard : Form
{
    private const int SquareSize = 80;
    private const int BoardSize = SquareSize * 8;

    private static readonly Color LightSquare = Color.FromArgb(240, 217, 181);
    private static readonly Color DarkSquare = Color.FromArgb(181, 136, 99);
    private static readonly Color SelectedColor = Color.FromArgb(130, 151, 105);
    private static readonly Color LegalDotColor = Color.FromArgb(100, 0, 0, 0);

    private readonly Dictionary<string, Bitmap> _pieceImages = new();

    private readonly Core.Board.Board _board;
    private readonly MoveGenerator _moveGenerator;

    private int _selectedSquare = -1;
    private List<Move> _legalMovesCache = new();
    private List<int> _highlightSquares = new();

    private bool engineThinking = false;

    public ChessBoard()
    {
        Text = "Chess Engine";
        ClientSize = new Size(BoardSize, BoardSize);
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        _board = new Core.Board.Board();
        _moveGenerator = new MoveGenerator();

        _legalMovesCache = _moveGenerator.GenerateLegalMoves(_board);

        LoadPieceImages();
        MouseClick += OnMouseClick;
    }

    private void LoadPieceImages()
    {
        string[] pieces = { "wK","wQ","wR","wB","wN","wP",
                            "bK","bQ","bR","bB","bN","bP" };

        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string piecesDir = Path.Combine(baseDir, "pieces");

        foreach (string piece in pieces)
        {
            string path = Path.Combine(piecesDir, $"{piece}.svg");
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"Missing piece image: {path}");
                continue;
            }

            try
            {
                var svgDoc = SvgDocument.Open(path);
                var bitmap = new Bitmap(SquareSize, SquareSize);
                using var graphics = Graphics.FromImage(bitmap);
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                svgDoc.Width = new SvgUnit(SquareSize);
                svgDoc.Height = new SvgUnit(SquareSize);
                svgDoc.Draw(graphics, new SizeF(SquareSize, SquareSize));
                _pieceImages[piece] = bitmap;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load {piece}: {ex.Message}");
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        DrawSquares(e.Graphics);
        DrawLegalMoveDots(e.Graphics);
        DrawPieces(e.Graphics);
        DrawSquareLabels(e.Graphics);
        DrawCoordinates(e.Graphics);
        DrawStatusBar(e.Graphics);
    }

    private void DrawSquares(Graphics g)
    {
        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                int square = BoardHelper.SquareIndex(file, rank);
                bool isSelected = square == _selectedSquare;
                bool isLight = (rank + file) % 2 != 0;
                bool isLegal = _highlightSquares.Contains(square);

                Color color = isSelected ? SelectedColor
                            : isLight ? LightSquare
                                        : DarkSquare;

                if (isLegal)
                {
                    color = Blend(color, Color.FromArgb(255, 255, 255, 0), 0.35f);
                }

                int x = file * SquareSize;
                int y = (7 - rank) * SquareSize;
                g.FillRectangle(new SolidBrush(color), x, y, SquareSize, SquareSize);
            }
        }
    }

    private void DrawLegalMoveDots(Graphics g)
    {
        if (_selectedSquare == -1)
        {
            return;
        }

        int dotSize = SquareSize / 4;
        int offset = (SquareSize - dotSize) / 2;

        foreach (int sq in _highlightSquares)
        {
            int file = BoardHelper.FileOf(sq);
            int rank = BoardHelper.RankOf(sq);
            int x = file * SquareSize;
            int y = (7 - rank) * SquareSize;

            bool isCapture = _board.Squares[sq] != Piece.None;

            if (!isCapture)
            {
                g.FillEllipse(
                    new SolidBrush(LegalDotColor),
                    x + offset, y + offset, dotSize, dotSize);
            }
            else
            {
                int ring = 6;
                bool isLight = (rank + file) % 2 != 0;
                g.FillEllipse(new SolidBrush(LegalDotColor), x, y, SquareSize, SquareSize);
                g.FillEllipse(
                    new SolidBrush(isLight ? LightSquare : DarkSquare),
                    x + ring, y + ring,
                    SquareSize - ring * 2, SquareSize - ring * 2);
            }
        }
    }

    private void DrawPieces(Graphics g)
    {
        for (int sq = 0; sq < 64; sq++)
        {
            int piece = _board.Squares[sq];
            if (piece == Piece.None)
            {
                continue;
            }

            string key = Piece.ToImageKey(piece);
            if (!_pieceImages.TryGetValue(key, out Bitmap? bmp))
            {
                continue;
            }

            int file = BoardHelper.FileOf(sq);
            int rank = BoardHelper.RankOf(sq);
            int x = file * SquareSize;
            int y = (7 - rank) * SquareSize;
            g.DrawImage(bmp, x, y, SquareSize, SquareSize);
        }
    }

    private void DrawSquareLabels(Graphics g)
    {
        var indexFont = new Font("Segoe UI", 8, FontStyle.Regular, GraphicsUnit.Pixel);
        var coordFont = new Font("Segoe UI", 8, FontStyle.Regular, GraphicsUnit.Pixel);

        string[] fileLetters = { "a", "b", "c", "d", "e", "f", "g", "h" };

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                int square = BoardHelper.SquareIndex(file, rank);
                bool isLight = (rank + file) % 2 != 0;

                Color textColor = isLight ? DarkSquare : LightSquare;
                var brush = new SolidBrush(textColor);

                int x = file * SquareSize;
                int y = (7 - rank) * SquareSize;

                g.DrawString(
                    square.ToString(),
                    indexFont,
                    brush,
                    x + 2,
                    y + 2);

                string coord = $"{fileLetters[file]}{rank + 1}";
                g.DrawString(
                    coord,
                    coordFont,
                    brush,
                    x + 2,
                    y + SquareSize - 12);
            }
        }
    }

    private void DrawCoordinates(Graphics g)
    {
        var font = new Font("Segoe UI", 9, FontStyle.Bold);
        string[] files = { "a", "b", "c", "d", "e", "f", "g", "h" };

        for (int i = 0; i < 8; i++)
        {
            bool isLight = i % 2 != 0;
            var brush = new SolidBrush(isLight ? DarkSquare : LightSquare);

            g.DrawString(files[i], font, brush,
                i * SquareSize + SquareSize - 14,
                BoardSize - 18);

            isLight = i % 2 == 0;
            brush = new SolidBrush(isLight ? DarkSquare : LightSquare);
            g.DrawString((i + 1).ToString(), font, brush,
                3,
                (7 - i) * SquareSize + 3);
        }
    }

    private void DrawStatusBar(Graphics g)
    {
        bool isWhiteTurn = _board.ColorToMove == Piece.White;
        bool inCheck = _moveGenerator.IsInCheck(_board, _board.ColorToMove);
        bool noMoves = _legalMovesCache.Count == 0;

        string status;
        if (noMoves && inCheck)
        {
            status = isWhiteTurn ? "Black wins by checkmate!" : "White wins by checkmate!";
        }
        else if (noMoves)
        {
            status = "Stalemate — draw!";
        }
        else if (inCheck)
        {
            status = (isWhiteTurn ? "White" : "Black") + " is in check!";
        }
        else
        {
            status = (isWhiteTurn ? "White" : "Black") + " to move";
        }

        Text = $"Chess Engine — {status}";
    }


    private async void OnMouseClick(object? sender, MouseEventArgs e)
    {
        if (!engineThinking)
        {

            int file = e.X / SquareSize;
            int rank = 7 - (e.Y / SquareSize);
            int square = BoardHelper.SquareIndex(file, rank);

            if (file < 0 || file > 7 || rank < 0 || rank > 7)
            {
                return;
            }

            if (_legalMovesCache.Count == 0)
            {
                return;
            }

            if (_selectedSquare == -1)
            {
                TrySelectSquare(square);
            }
            else
            {
                if (square == _selectedSquare)
                {
                    Deselect();
                }
                else if (_highlightSquares.Contains(square))
                {
                    MakeMove(square);

                    if (!isGameOver())
                    {
                        engineThinking = true;
                        Move engineMove = await Task.Run(() => findEngineBestMove(_board.Clone()));

                        MakeEngineMove(engineMove);
                        engineThinking = false;
                    }
                }
                else if (_board.Squares[square] != Piece.None
                      && Piece.IsColor(_board.Squares[square], _board.ColorToMove))
                {
                    TrySelectSquare(square);
                }
                else
                {
                    Deselect();
                }
            }
        }
        Invalidate();
    }

    private void TrySelectSquare(int square)
    {
        int piece = _board.Squares[square];
        if (piece == Piece.None || !Piece.IsColor(piece, _board.ColorToMove))
        {
            return;
        }

        _selectedSquare = square;
        _highlightSquares = _legalMovesCache
            .Where(m => m.From == square)
            .Select(m => m.To)
            .ToList();
    }

    private void MakeMove(int toSquare)
    {
        Move move = _legalMovesCache
            .Where(m => m.From == _selectedSquare && m.To == toSquare)
            .OrderByDescending(m => m.PromoPiece)
            .First();

        _board.MakeMove(move);
        _legalMovesCache = _moveGenerator.GenerateLegalMoves(_board);
        _selectedSquare = -1;
        _highlightSquares = new();
    }

    private void MakeEngineMove(Move move)
    {
        _board.MakeMove(move);
        _legalMovesCache = _moveGenerator.GenerateLegalMoves(_board);
        _selectedSquare = -1;
        _highlightSquares = new();
    }

    private Move findEngineBestMove(Board board)
    {

        Searcher searcher = new(board);

        return searcher.FindBestMove(4);
    }

    private bool isGameOver()
    {
        return _legalMovesCache.Count == 0;

    }

    private void Deselect()
    {
        _selectedSquare = -1;
        _highlightSquares = new();
    }

    private void InitializeComponent()
    {

    }

    private static Color Blend(Color c1, Color c2, float ratio) =>
        Color.FromArgb(
            (int)(c1.R + (c2.R - c1.R) * ratio),
            (int)(c1.G + (c2.G - c1.G) * ratio),
            (int)(c1.B + (c2.B - c1.B) * ratio));
}