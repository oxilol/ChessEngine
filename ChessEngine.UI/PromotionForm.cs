using System;
using System.Drawing;
using System.Windows.Forms;
using ChessEngine.Core.Board;

namespace ChessEngine.UI
{
    public class PromotionForm : Form
    {
        public int SelectedPiece { get; private set; } = Piece.None;
        
        public PromotionForm(Image queen, Image rook, Image bishop, Image knight, bool isWhite)
        {
            Text = "Promote Pawn";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 100);
            
            int color = isWhite ? Piece.White : Piece.Black;
            
            AddPieceButton(queen, color | Piece.Queen, 10);
            AddPieceButton(rook, color | Piece.Rook, 90);
            AddPieceButton(bishop, color | Piece.Bishop, 170);
            AddPieceButton(knight, color | Piece.Knight, 250);
        }
        
        private void AddPieceButton(Image img, int pieceCode, int x)
        {
            var btn = new Button
            {
                Image = img,
                Location = new Point(x, 10),
                Size = new Size(80, 80),
                FlatStyle = FlatStyle.Flat
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (sender, e) =>
            {
                SelectedPiece = pieceCode;
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(btn);
        }
    }
}
