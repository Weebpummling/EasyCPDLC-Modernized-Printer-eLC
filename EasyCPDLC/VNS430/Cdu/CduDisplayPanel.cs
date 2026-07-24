using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EasyCPDLC.VNS430.Cdu
{
    internal sealed class CduLskEventArgs : EventArgs
    {
        public CduLskEventArgs(int index, bool rightSide)
        {
            Index = index;
            RightSide = rightSide;
        }

        // 1..6, top to bottom.
        public int Index { get; }
        public bool RightSide { get; }
    }

    // Self-contained on-screen renderer for the LSK-only CDU. It owns a CduGrid, paints it
    // as a bare character screen with six line-select keys down each side, and raises
    // LskPressed when a key is clicked (or an F1..F12 shortcut is pressed). It has no
    // knowledge of the datalink backend or of the Airbus/Boeing DCDU chrome; MainForm.Cdu
    // populates the grid and reacts to LskPressed. This keeps the whole CDU mode additive.
    internal sealed class CduDisplayPanel : Control
    {
        private const int ScreenMargin = 6;

        public CduDisplayPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            BackColor = Color.Black;
            TabStop = true;
            Grid = new CduGrid();
        }

        public CduGrid Grid { get; }

        // Where the rendered grid is mirrored (e.g. a WinWing CDU over websocket). Defaults
        // to a no-op; the "standby" export mod swaps in a real sink.
        public ICduDisplaySink Sink { get; set; } = NullCduDisplaySink.Instance;

        public event EventHandler<CduLskEventArgs> LskPressed;

        // A Boeing CDU keypad key was pressed (on-screen or via a bound hardware key).
        public event EventHandler<Vns430Command> KeyPressed;

        // Scratchpad entry (real-CDU style): typed characters, backspace and clear. The
        // page tree owns the scratchpad string and decides when a character is relevant.
        public event EventHandler<char> CharTyped;
        public event EventHandler ScratchpadBackspace;
        public event EventHandler ScratchpadClear;

        // Refresh the screen after the page tree has rewritten Grid.
        public void RefreshDisplay() => Invalidate();

        private int GutterWidth => Math.Max(30, (int)(Width * 0.10));

        // The keypad occupies the bottom of the panel; the CDU screen (grid + LSK gutters)
        // takes the region above it.
        private const int KeypadRows = 10;
        private const int KeypadCols = 9;
        private const int KeypadPad = 6;

        private int KeypadHeight
        {
            get
            {
                float keyWidth = (Width - (2 * KeypadPad)) / (float)KeypadCols;
                float keyHeight = keyWidth * 0.72f;
                return (int)((KeypadRows * keyHeight) + (2 * KeypadPad));
            }
        }

        private Rectangle KeypadRegion => new(0, Math.Max(0, Height - KeypadHeight), Width, Math.Min(Height, KeypadHeight));

        private Rectangle ScreenRectangle
        {
            get
            {
                int gutter = GutterWidth;
                int width = Math.Max(1, Width - (2 * gutter));
                int height = Math.Max(1, (Height - KeypadHeight) - (2 * ScreenMargin));
                return new Rectangle(gutter, ScreenMargin, width, height);
            }
        }

        private Rectangle KeyBounds(int row, int col)
        {
            Rectangle kp = KeypadRegion;
            float keyWidth = (kp.Width - (2 * KeypadPad)) / (float)KeypadCols;
            float keyHeight = (kp.Height - (2 * KeypadPad)) / (float)KeypadRows;
            return new Rectangle(
                (int)(kp.Left + KeypadPad + (col * keyWidth)) + 1,
                (int)(kp.Top + KeypadPad + (row * keyHeight)) + 1,
                (int)keyWidth - 2,
                (int)keyHeight - 2);
        }

        private Rectangle LskKeyBounds(int lsk, bool rightSide)
        {
            Rectangle screen = ScreenRectangle;
            float cellHeight = screen.Height / (float)CduGrid.Rows;
            int labelRow = CduLayout.LabelRow(lsk);
            int dataRow = CduLayout.DataRow(lsk);
            float top = screen.Top + (labelRow * cellHeight);
            float bottom = screen.Top + ((dataRow + 1) * cellHeight);
            float centerY = (top + bottom) / 2f;

            int gutter = GutterWidth;
            int keyWidth = (int)(gutter * 0.66f);
            int keyHeight = (int)(cellHeight * 1.35f);
            int keyX = rightSide
                ? Width - gutter + ((gutter - keyWidth) / 2)
                : (gutter - keyWidth) / 2;
            return new Rectangle(keyX, (int)(centerY - (keyHeight / 2f)), keyWidth, keyHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Black);

            Rectangle screen = ScreenRectangle;
            float cellWidth = screen.Width / (float)CduGrid.Cols;
            float cellHeight = screen.Height / (float)CduGrid.Rows;

            // Subtle screen border so the "LCD" reads as a device.
            using (Pen border = new(Color.FromArgb(70, 78, 84), 1f))
            {
                g.DrawRectangle(border, screen.Left - 1, screen.Top - 1, screen.Width + 1, screen.Height + 1);
            }

            DrawCells(g, screen, cellWidth, cellHeight);
            DrawLskKeys(g);
            DrawKeypad(g);

            // Mirror the same grid to any external display (WinWing CDU, etc.).
            Sink?.Push(Grid.ToWinwingData());
        }

        // Procedural placeholder key faces. Licensed Boeing CDU artwork will be blitted onto
        // these same KeyBounds zones once supplied; the layout and hit-testing do not change.
        private void DrawKeypad(Graphics g)
        {
            using Font keyFont = new("Segoe UI", Math.Max(6f, KeyBounds(0, 0).Height * 0.28f), FontStyle.Bold, GraphicsUnit.Pixel);
            using StringFormat format = new()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            foreach (CduKey key in CduKeyMap.Keys)
            {
                Rectangle bounds = KeyBounds(key.Row, key.Col);
                if (bounds.Width < 2 || bounds.Height < 2)
                {
                    continue;
                }

                Color top = key.Command == Vns430Command.CduExec
                    ? Color.FromArgb(40, 96, 54)
                    : Color.FromArgb(70, 74, 78);
                Color bottom = key.Command == Vns430Command.CduExec
                    ? Color.FromArgb(24, 60, 34)
                    : Color.FromArgb(34, 37, 40);

                using GraphicsPath path = RoundedRect(bounds, 3);
                using LinearGradientBrush face = new(bounds, top, bottom, LinearGradientMode.Vertical);
                using Pen edge = new(Color.FromArgb(18, 20, 22), 1f);
                g.FillPath(face, path);
                g.DrawPath(edge, path);

                using SolidBrush text = new(Color.FromArgb(226, 230, 233));
                g.DrawString(key.Legend, keyFont, text, bounds, format);
            }
        }

        private void DrawCells(Graphics g, Rectangle screen, float cellWidth, float cellHeight)
        {
            float largeSize = Math.Max(6f, cellHeight * 0.80f);
            float smallSize = Math.Max(5f, cellHeight * 0.62f);
            using Font largeFont = new("Consolas", largeSize, FontStyle.Bold, GraphicsUnit.Pixel);
            using Font smallFont = new("Consolas", smallSize, FontStyle.Regular, GraphicsUnit.Pixel);
            using StringFormat format = new(StringFormat.GenericTypographic)
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap
            };

            for (int row = 0; row < CduGrid.Rows; row++)
            {
                for (int col = 0; col < CduGrid.Cols; col++)
                {
                    CduCell cell = Grid[row, col];
                    if (cell.IsBlank)
                    {
                        continue;
                    }

                    RectangleF cellRect = new(
                        screen.Left + (col * cellWidth),
                        screen.Top + (row * cellHeight),
                        cellWidth,
                        cellHeight);

                    Color colour = cell.Color.Rgb();
                    if (cell.Inverse)
                    {
                        using SolidBrush block = new(colour);
                        g.FillRectangle(block, cellRect.X, cellRect.Y + 1, cellRect.Width, cellRect.Height - 2);
                    }

                    if (cell.Glyph == ' ')
                    {
                        continue;
                    }

                    Color glyphColour = cell.Inverse ? Color.Black : colour;
                    using SolidBrush textBrush = new(glyphColour);
                    g.DrawString(cell.Glyph.ToString(), cell.Small ? smallFont : largeFont, textBrush, cellRect, format);
                }
            }
        }

        private void DrawLskKeys(Graphics g)
        {
            for (int lsk = 1; lsk <= CduLayout.LskCount; lsk++)
            {
                DrawLskKey(g, LskKeyBounds(lsk, false));
                DrawLskKey(g, LskKeyBounds(lsk, true));
            }
        }

        private static void DrawLskKey(Graphics g, Rectangle bounds)
        {
            using GraphicsPath path = RoundedRect(bounds, 3);
            using LinearGradientBrush face = new(bounds, Color.FromArgb(78, 84, 88), Color.FromArgb(38, 42, 45), LinearGradientMode.Vertical);
            using Pen edge = new(Color.FromArgb(20, 22, 24), 1f);
            g.FillPath(face, path);
            g.DrawPath(edge, path);
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            for (int lsk = 1; lsk <= CduLayout.LskCount; lsk++)
            {
                if (LskKeyBounds(lsk, false).Contains(e.Location))
                {
                    RaiseLsk(lsk, false);
                    return;
                }
                if (LskKeyBounds(lsk, true).Contains(e.Location))
                {
                    RaiseLsk(lsk, true);
                    return;
                }
            }

            foreach (CduKey key in CduKeyMap.Keys)
            {
                if (KeyBounds(key.Row, key.Col).Contains(e.Location))
                {
                    KeyPressed?.Invoke(this, key.Command);
                    return;
                }
            }
        }

        // Keyboard fallback for desktop testing without hardware: F1..F6 = left LSK 1..6,
        // F7..F12 = right LSK 1..6.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData >= Keys.F1 && keyData <= Keys.F6)
            {
                RaiseLsk((keyData - Keys.F1) + 1, false);
                return true;
            }
            if (keyData >= Keys.F7 && keyData <= Keys.F12)
            {
                RaiseLsk((keyData - Keys.F7) + 1, true);
                return true;
            }
            if (keyData == Keys.Back)
            {
                ScratchpadBackspace?.Invoke(this, EventArgs.Empty);
                return true;
            }
            if (keyData == Keys.Delete)
            {
                ScratchpadClear?.Invoke(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Scratchpad character entry: A-Z, 0-9, space and the CDU punctuation set.
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            char c = char.ToUpperInvariant(e.KeyChar);
            if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == ' ' || c == '.' || c == '/' || c == '-')
            {
                CharTyped?.Invoke(this, c);
                e.Handled = true;
            }
        }

        private void RaiseLsk(int index, bool rightSide)
        {
            LskPressed?.Invoke(this, new CduLskEventArgs(index, rightSide));
        }
    }
}
