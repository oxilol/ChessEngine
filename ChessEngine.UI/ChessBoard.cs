using ChessEngine.Core.Board;
using ChessEngine.Core.MoveGen;
using ChessEngine.Core.Search;
using Svg;

namespace ChessEngine.UI;

public class ChessBoard : Form
{
    private const int SquareSize = 80;
    private const int BoardSize = SquareSize * 8;
    private const int PanelWidth = 200;

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
    private int _playerColor = Piece.White;
    private bool _flipBoard = false;

    private System.Windows.Forms.Timer _engineTimer;
    private int _engineTimeMs = 300000; // 5 minutes
    private Label _lblEngineTimeLabel;
    private Label _lblEngineTime;
    private Panel _sidePanel;

    public ChessBoard()
    {
        Text = "Chess Engine";
        ClientSize = new Size(BoardSize + PanelWidth, BoardSize);
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        _board = new Core.Board.Board();
        _moveGenerator = new MoveGenerator();

        _legalMovesCache = _moveGenerator.GenerateLegalMoves(_board);

        LoadPieceImages();
        InitializeUI();
        MouseClick += OnMouseClick;
    }

    private void InitializeUI()
    {
        _sidePanel = new Panel
        {
            Location = new Point(BoardSize, 0),
            Size = new Size(PanelWidth, BoardSize),
            BackColor = Color.LightGray
        };
        Controls.Add(_sidePanel);

        var btnPlayWhite = new Button
        {
            Text = "Play White",
            Location = new Point(20, 20),
            Size = new Size(160, 40)
        };
        btnPlayWhite.Click += (s, e) => StartNewGame(Piece.White);
        _sidePanel.Controls.Add(btnPlayWhite);

        var btnPlayBlack = new Button
        {
            Text = "Play Black",
            Location = new Point(20, 70),
            Size = new Size(160, 40)
        };
        btnPlayBlack.Click += (s, e) => StartNewGame(Piece.Black);
        _sidePanel.Controls.Add(btnPlayBlack);

        var btn1min = new Button
        {
            Text = "1min",
            Location = new Point(20, 120),
            Size = new Size(55, 40)
        };
        btn1min.Click += (s, e) => SetEngineTime(1);
        _sidePanel.Controls.Add(btn1min);

        var btn5min = new Button
        {
            Text = "5min",
            Location = new Point(70, 120),
            Size = new Size(55, 40)
        };
        btn5min.Click += (s, e) => SetEngineTime(5);
        _sidePanel.Controls.Add(btn5min);

        var btn10min = new Button
        {
            Text = "10min",
            Location = new Point(120, 120),
            Size = new Size(60, 40)
        };
        btn10min.Click += (s, e) => SetEngineTime(10);
        _sidePanel.Controls.Add(btn10min);

        _lblEngineTimeLabel = new Label
        {
            Text = "Engine time : ",
            Location = new Point(20, 170),
            Size = new Size(160, 30),
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };
        _sidePanel.Controls.Add(_lblEngineTimeLabel);

        _lblEngineTime = new Label
        {
            Text = "05:00.0",
            Location = new Point(20, 200),
            Size = new Size(160, 30),
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };
        _sidePanel.Controls.Add(_lblEngineTime);

        _engineTimer = new System.Windows.Forms.Timer { Interval = 100 };
        _engineTimer.Tick += EngineTimer_Tick;
    }

    private void EngineTimer_Tick(object? sender, EventArgs e)
    {
        _engineTimeMs -= 100;
        if (_engineTimeMs <= 0)
        {
            _engineTimeMs = 0;
            _engineTimer.Stop();
        }
        TimeSpan ts = TimeSpan.FromMilliseconds(_engineTimeMs);

        _lblEngineTime.Text = $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds / 100}";
    }

    private async void StartNewGame(int color)
    {
        if (engineThinking)
        {
            return;
        }

        _board.LoadFEN(Core.Board.Board.StartFEN);
        _playerColor = color;
        _flipBoard = (color == Piece.Black);
        _engineTimeMs = 300000;
        _lblEngineTime.Text = "05:00.0";
        _legalMovesCache = _moveGenerator.GenerateLegalMoves(_board);
        Deselect();
        Invalidate();

        if (color == Piece.Black)
        {
            engineThinking = true;
            _engineTimer.Start();
            Move engineMove = await Task.Run(() => findEngineBestMove(_board.Clone()));
            _engineTimer.Stop();
            MakeEngineMove(engineMove);
            engineThinking = false;
            Invalidate();
        }
    }

    private async void SetEngineTime(int minutes)
    {
        if (engineThinking)
        {
            return;
        }

        _engineTimeMs = minutes * 60 * 1000;
        TimeSpan ts = TimeSpan.FromMilliseconds(_engineTimeMs);
        _lblEngineTime.Text = $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds / 100}";
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

    private void GetDisplayCoordinates(int square, out int x, out int y)
    {
        int file = BoardHelper.FileOf(square);
        int rank = BoardHelper.RankOf(square);
        if (_flipBoard)
        {
            file = 7 - file;
            rank = 7 - rank;
        }
        x = file * SquareSize;
        y = (7 - rank) * SquareSize;
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

                GetDisplayCoordinates(square, out int x, out int y);
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
            GetDisplayCoordinates(sq, out int x, out int y);

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
            GetDisplayCoordinates(sq, out int x, out int y);
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

                int x = 0, y = 0; // The variables will be updated by GetDisplayCoordinates
                GetDisplayCoordinates(square, out x, out y);

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

        for (int file = 0; file < 8; file++)
        {
            int square = BoardHelper.SquareIndex(file, 0); // rank 0
            bool isLight = (0 + file) % 2 != 0;
            var brush = new SolidBrush(isLight ? DarkSquare : LightSquare);

            int displayFile = _flipBoard ? 7 - file : file;
            g.DrawString(files[file], font, brush,
                displayFile * SquareSize + SquareSize - 14,
                BoardSize - 18);
        }

        for (int rank = 0; rank < 8; rank++)
        {
            int square = BoardHelper.SquareIndex(0, rank); // file 0
            bool isLight = (rank + 0) % 2 != 0;
            var brush = new SolidBrush(isLight ? DarkSquare : LightSquare);

            int displayRank = _flipBoard ? rank : 7 - rank;
            g.DrawString((rank + 1).ToString(), font, brush,
                3,
                displayRank * SquareSize + 3);
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

            int displayFile = e.X / SquareSize;
            int displayRank = 7 - (e.Y / SquareSize);

            if (displayFile < 0 || displayFile > 7 || displayRank < 0 || displayRank > 7)
            {
                return;
            }

            int file = _flipBoard ? 7 - displayFile : displayFile;
            int rank = _flipBoard ? 7 - displayRank : displayRank;
            int square = BoardHelper.SquareIndex(file, rank);

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
                    Invalidate();

                    if (!isGameOver() && _board.ColorToMove != _playerColor)
                    {
                        engineThinking = true;
                        _engineTimer.Start();
                        Move engineMove = await Task.Run(() => findEngineBestMove(_board.Clone()));
                        _engineTimer.Stop();
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
        var moves = _legalMovesCache
            .Where(m => m.From == _selectedSquare && m.To == toSquare)
            .ToList();

        Move move = moves.First();

        if (moves.Count > 1 && moves.Any(m => m.IsPromotion))
        {
            bool isWhite = _board.ColorToMove == Piece.White;
            string colorPrefix = isWhite ? "w" : "b";
            var form = new PromotionForm(
                _pieceImages[$"{colorPrefix}Q"],
                _pieceImages[$"{colorPrefix}R"],
                _pieceImages[$"{colorPrefix}B"],
                _pieceImages[$"{colorPrefix}N"],
                isWhite
            );

            if (form.ShowDialog() == DialogResult.OK)
            {
                int promoPiece = Piece.GetType(form.SelectedPiece);
                move = moves.FirstOrDefault(m => m.PromoPiece == promoPiece);
                if (move.IsNull)
                {
                    move = moves.First();
                }
            }
            else
            {
                // default to queen if canceled
                move = moves.OrderByDescending(m => m.PromoPiece).First();
            }
        }

        _board.MakeMove(move);
        _legalMovesCache = _moveGenerator.GenerateLegalMoves(_board);
        _selectedSquare = -1;
        _highlightSquares = new();
        _highlightSquares.Add(toSquare);
    }

    private void MakeEngineMove(Move move)
    {
        _board.MakeMove(move);
        _legalMovesCache = _moveGenerator.GenerateLegalMoves(_board);
        _selectedSquare = -1;
        _highlightSquares = new();
        _highlightSquares.Add(move.To);
    }

    private Move findEngineBestMove(Board board)
    {
        Searcher searcher = new(board);

        long timeBudget = Math.Max(100, _engineTimeMs / 20);
        long hardLimit = Math.Max(200, _engineTimeMs / 5);

        Move move = searcher.FindBestMoveTimed(timeBudget, hardLimit);
        Console.Error.WriteLine($"Nodes searched: {searcher.NodesSearched}");

        return move;
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

    private static Color Blend(Color c1, Color c2, float ratio) =>
        Color.FromArgb(
            (int)(c1.R + (c2.R - c1.R) * ratio),
            (int)(c1.G + (c2.G - c1.G) * ratio),
            (int)(c1.B + (c2.B - c1.B) * ratio));
}