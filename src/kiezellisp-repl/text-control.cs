#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Concurrent;
    using System.Drawing;
    using System.Windows.Forms;
    using System.Text;

    public class TextControl : Control
    {
        #region Fields

        BlockingCollection<KeyInfo> keyBuffer;

        #endregion Fields

        #region Constructors

        public TextControl(TextFormBase parent)
        {
            ParentForm = parent;
            Dock = DockStyle.Fill;
            Text = "Kiezellisp";
            BackColor = RuntimeRepl.DefaultBackColor;
            DoubleBuffered = true;
            keyBuffer = new BlockingCollection<KeyInfo>();
        }

        #endregion Constructors

        #region Public Properties

        public int CharHeight
        {
            get { return RuntimeRepl.CharHeight; }
        }

        public int CharWidth
        {
            get { return RuntimeRepl.CharWidth; }
        }

        public int Cols
        {
            get { return ParentForm.Cols; }
        }

        public Font[] Fonts
        {
            get { return RuntimeRepl.Fonts; }
        }

        public TerminalHScrollBar HoriScrollBar
        {
            get { return ParentForm.HoriScrollBar; }
        }

        public int LineHeight
        {
            get { return RuntimeRepl.LineHeight; }
        }

        public TextFormBase ParentForm { get; internal set; }

        public int Rows
        {
            get { return ParentForm.Rows; }
        }

        public TerminalVScrollBar VertScrollBar
        {
            get { return ParentForm.VertScrollBar; }
        }

        public TextWindow Window { get; internal set; }

        public int WindowLeftPixels
        {
            get
            {
                return Window.WindowLeft * CharWidth;
            }
        }

        public int WindowTopPixels
        {
            get
            {
                return Window.WindowTop * LineHeight;
            }
        }

        #endregion Public Properties

        #region Private Methods

        void PaintArea(Graphics g, int x, int y, int w, int h)
        {
            if (Window == null)
            {
                return;
            }

            for (var i = 0; i < h; ++i)
            {
                var beg = x;
                var end = x + w;

                while (beg < end)
                {
                    var top = y + i;
                    var left = beg;
                    Color fg;
                    Color bg;
                    int fontIndex;
                    var text = FindColorRun(left, end, top, out fg, out bg, out fontIndex);
                    PaintString(g, text, left, top, text.Length, fg, bg, fontIndex);
                    beg += text.Length;
                }
            }
        }

        void PaintString(Graphics g, string text, int x, int y, int w, Color fg, Color bg, int fontIndex)
        {
            var xx = x * CharWidth - WindowLeftPixels;
            var yy = y * LineHeight - WindowTopPixels;
            var ww = w * CharWidth;
            var hh = 1 * LineHeight;
            var bounds = new Rectangle(xx, yy, ww, hh);
            g.FillRectangle(new SolidBrush(bg), xx, yy, ww, hh);
            var flags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.NoClipping
                                       | TextFormatFlags.Left | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(g, text, Fonts[fontIndex], bounds, fg, flags);
        }

        string FindColorRun(int col, int end, int row, out Color fg, out Color bg, out int fontIndex)
        {
            var line = new StringBuilder();
            var item = Window.GetDisplayAttributes(col, row);
            fg = item.Fg;
            bg = item.Bg;
            fontIndex = item.FontIndex;
            line.Append(item.Code);

            for (var c = col + 1; c < end; ++c)
            {
                item = Window.GetDisplayAttributes(c, row);
                if (fg != item.Fg)
                {
                    break;
                }
                if (bg != item.Bg)
                {
                    break;
                }
                if (fontIndex != item.FontIndex)
                {
                    break;
                }
                line.Append(item.Code);
            }
            return line.ToString();
        }

        #endregion Private Methods

        #region Protected Methods

        protected override bool IsInputKey(Keys keyData)
        {
            return true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyData)
            {
                case Keys.Control | Keys.ControlKey:
                case Keys.Shift | Keys.ShiftKey:
                case Keys.Alt | Keys.Menu:
                    break;
                default:
                    keyBuffer.Add(new KeyInfo(e.KeyData));
                    break;
            }
            e.Handled = true;
            base.OnKeyDown(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            keyBuffer.Add(new KeyInfo(e.KeyChar));
            e.Handled = true;
            base.OnKeyPress(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            var x = (e.Location.X - WindowLeftPixels) / CharWidth;
            var y = (e.Location.Y - WindowTopPixels) / LineHeight;
            var c = e.Clicks;
            switch (e.Button)
            {
                case MouseButtons.Left:
                    {
                        keyBuffer.Add(new KeyInfo(Keys.LButton, x, y, c, 0));
                        break;
                    }
                case MouseButtons.Right:
                    {
                        keyBuffer.Add(new KeyInfo(Keys.RButton, x, y, c, 0));
                        break;
                    }
            }
            base.OnMouseClick(e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            var x = (e.Location.X - WindowLeftPixels) / CharWidth;
            var y = (e.Location.Y - WindowTopPixels) / LineHeight;
            var c = e.Clicks;
            switch (e.Button)
            {
                case MouseButtons.Left:
                    {
                        keyBuffer.Add(new KeyInfo(Keys.LButton, x, y, c, 0));
                        break;
                    }
                case MouseButtons.Right:
                    {
                        keyBuffer.Add(new KeyInfo(Keys.RButton, x, y, c, 0));
                        break;
                    }
            }
            base.OnMouseDoubleClick(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var x = (e.Location.X - WindowLeftPixels) / CharWidth;
            var y = (e.Location.Y - WindowTopPixels) / LineHeight;
            var w = e.Delta;
            keyBuffer.Add(new KeyInfo(RuntimeRepl.PseudoKeyForMouseWheel, x, y, 0, w));
            base.OnMouseWheel(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            base.OnPaint(e);
            var rect = e.ClipRectangle;
            rect.Offset(WindowLeftPixels, WindowTopPixels);
            var x1 = rect.Left / CharWidth;
            var y1 = rect.Top / LineHeight;
            var x2 = (rect.Left + rect.Width + CharWidth - 1) / CharWidth;
            var y2 = (rect.Top + rect.Height + LineHeight - 1) / LineHeight;
            PaintArea(g, x1, y1, x2 - x1 + 1, y2 - y1 + 1);
        }

        #endregion Protected Methods

        #region Public Methods

        public void ClearKeyboardBuffer()
        {
            //keyBuffer.Clear();
        }

        public void GuiBringToFront()
        {
            RuntimeRepl.GuiInvoke(new Action(ParentForm.BringToFront));
        }

        public void GuiInvalidate(int x, int y, int w, int h)
        {
            if (w == 1)
            {
                // cursor update needs more bits to avoid artefacts
                if (x > 0)
                {
                    --x;
                    ++w;
                }
                if (x + w < Cols)
                {
                    ++w;
                }
            }

            RuntimeRepl.GuiInvoke(new Action(() =>
            {
                var screen = new Rectangle(0, 0, Cols, Rows);
                var dirty = new Rectangle(x, y, w, h);
                dirty.Intersect(screen);
                var bounds = new Rectangle(dirty.Left * CharWidth, dirty.Top * LineHeight, dirty.Width * CharWidth, dirty.Height * LineHeight);
                Invalidate(bounds);
            }));
        }

        public void GuiInvalidate()
        {
            RuntimeRepl.GuiInvoke(new Action(Invalidate));
        }

        public void GuiUpdateHoriScrollBarPos()
        {
            RuntimeRepl.GuiInvoke(new Action(() =>
            {
                if (HoriScrollBar != null)
                {
                    HoriScrollBar.Value = 0;
                    HoriScrollBar.Maximum = Window.BufferWidth - 1;
                    HoriScrollBar.Value = Window.WindowLeft;
                }
            }));
        }

        public void GuiUpdateVertScrollBarPos()
        {
            RuntimeRepl.GuiInvoke(new Action(() =>
            {
                if (VertScrollBar != null)
                {
                    VertScrollBar.Value = 0;
                    VertScrollBar.Maximum = Window.BufferHeight - 1;
                    VertScrollBar.Value = Window.WindowTop;
                }
            }));
        }

        public void InitScrollBars()
        {
            Window.WindowLeft = 0;
            Window.WindowTop = 0;

            if (HoriScrollBar != null)
            {
                HoriScrollBar.Minimum = 0;
                HoriScrollBar.Maximum = Window.BufferWidth - 1;
                HoriScrollBar.Value = 0;
                HoriScrollBar.LargeChange = Cols;
                HoriScrollBar.SmallChange = 1;
            }

            if (VertScrollBar != null)
            {
                VertScrollBar.Minimum = 0;
                VertScrollBar.Maximum = Window.BufferHeight - 1;
                VertScrollBar.Value = 0;
                VertScrollBar.LargeChange = Rows;
                VertScrollBar.SmallChange = 1;
            }
        }

        public KeyInfo ReadKey()
        {
            KeyInfo info = keyBuffer.Take();
            if (info.KeyData == RuntimeRepl.InterruptKey)
            {
                throw new InterruptException();
            }
            return info;
        }

        public void SendInterruptKey()
        {
            SendKey(new KeyInfo(RuntimeRepl.InterruptKey));
        }

        public void SendKey(KeyInfo key)
        {
            keyBuffer.Add(key);
        }

        #endregion Public Methods
    }
}