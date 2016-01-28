
// Copyright (C) Jan Tolenaar. See the file LICENSE for details.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace Kiezel
{
    public class FontType
    {
        public Font Font { get; internal set; }

        public static implicit operator Font(FontType ft)
        {
            return ft.Font;
        }

        public FontType(string name,int size)
        {
            Font = new Font(name, size);
        }

        public FontType(string name,int size, string style)
        {
            switch (style)
            {
                case "bold":
                {
                    Font = new Font(name, size, FontStyle.Bold);
                    break;
                }
                case "italic":
                {
                    Font = new Font(name, size, FontStyle.Italic);
                    break;
                }
                case "underline":
                {
                    Font = new Font(name, size, FontStyle.Underline);
                    break;
                }
                case "strikeout":
                {
                    Font = new Font(name, size, FontStyle.Strikeout);
                    break;
                }
                default:
                {
                    Font = new Font(name, size,FontStyle.Regular );
                    break;
                }
            }
        }

    }

    public struct ColorType
    {
        Color _Color;

        public Color Color { get { return _Color; } }

        public static implicit operator Color(ColorType ct)
        {
            return ct.Color;
        }

        public static bool Equals(ColorType ct1,ColorType ct2)
        {
            return ct1.Color == ct2.Color;
        }

     
        public ColorType( object color )
        {
            if (color is ColorType)
            {
                _Color = ((ColorType)color).Color;
            }
            else if (color is Int32)
            {
                _Color = Color.FromArgb(255, Color.FromArgb((int)color));
            }
            else if (color is Color)
            {
                _Color=(Color)color;
            }
            else
            {
                var colorName = Runtime.GetDesignatedString(color).LispToPascalCaseName();
                var member = typeof(Color).GetRuntimeProperty(colorName);
                if (member == null)
                {
                    Runtime.ThrowError("Invalid color name: ", color);
                }
                var color2 = (Color)member.GetValue(null);
                _Color=color2;
            }
        }

        public override string ToString()
        {
            return _Color.ToString();
        }
    }

    public class KeyType
    {
        public const int Enter = (int)Keys.Enter;
        public const int Escape = (int)Keys.Escape;
        public const int Up = (int)Keys.Up;
        public const int Down = (int)Keys.Down;
        public const int Left = (int)Keys.Left;
        public const int Right = (int)Keys.Right;
        public const int Back = (int)Keys.Back;
        public const int PageUp = (int)Keys.PageUp;
        public const int PageDown = (int)Keys.PageDown;
        public const int Control = (int)Keys.Control;
        public const int Alt = (int)Keys.Alt;

    }

    public class KeyInfo
    {
        public Keys KeyData { get; internal set; }
        public char KeyChar { get; internal set; }
        public int  MouseCol { get; internal set; }
        public int  MouseRow { get; internal set; }

        public KeyInfo(Keys data)
        {
            KeyData = data;
            KeyChar = (char)0;
        }

        public KeyInfo(Keys data,int col,int row)
        {
            KeyData = data;
            KeyChar = (char)0;
            MouseCol = col;
            MouseRow = row;
        }

        public KeyInfo(char data)
        {
            KeyData = Keys.None;
            KeyChar = data;
        }

    }

    public delegate void TerminalMainProgram();


    public class TerminalMainForm: Form
    {
        static TextFormatFlags flags = TextFormatFlags.NoPrefix|TextFormatFlags.NoPadding | TextFormatFlags.NoClipping | TextFormatFlags.Left | TextFormatFlags.SingleLine;
        int charWidth;
        int charHeight;
        int lineHeight;
        int cols;
        int rows;
        Queue<KeyInfo> keyBuffer;
        KeyInfo lastKeyDown;
        TerminalMainProgram main;
        public Buffer ScreenBuffer;
        bool gotShownEvent;
        BufferedGraphics bufferedGraphics = null;
        Graphics graphics;
        Font[] Fonts;

        public int Cols { get { return cols; } }

        public int Rows { get { return rows; } }

        public TerminalMainForm(string fontName,int fontSize,int w, int h, TerminalMainProgram mainProgram)
        {
            this.Text = "Kiezellisp";
            this.main = mainProgram;
            BackColor = Terminal.DefaultBackColor;
            keyBuffer = new Queue<KeyInfo>();
            StartPosition = FormStartPosition.CenterScreen;
            gotShownEvent = false;
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.Fixed3D;
            InitBuffer(w, h);
            InitFont(fontName, fontSize);
            InitClientRectangle();
            //bufferedGraphics = BufferedGraphicsManager.Current.Allocate(graphics, ClientRectangle);
        }

        public void InitBuffer(int w,int h)
        {
            rows = h;
            cols = w;
            ScreenBuffer = new Buffer(w, h);
        }

        public void InitFont(string name,int size)
        {
            Fonts = new Font[]
            {
                new Font(name,size,FontStyle.Regular),
                new Font(name,size,FontStyle.Bold),
                new Font(name,size,FontStyle.Italic),
                new Font(name,size,FontStyle.Italic|FontStyle.Bold),
                new Font(name,size,FontStyle.Underline|FontStyle.Regular),
                new Font(name,size,FontStyle.Underline|FontStyle.Bold),
                new Font(name,size,FontStyle.Underline|FontStyle.Italic),
                new Font(name,size,FontStyle.Underline|FontStyle.Italic|FontStyle.Bold),
                new Font(name,size,FontStyle.Strikeout|FontStyle.Regular),
                new Font(name,size,FontStyle.Strikeout|FontStyle.Bold),
                new Font(name,size,FontStyle.Strikeout|FontStyle.Italic),
                new Font(name,size,FontStyle.Strikeout|FontStyle.Italic|FontStyle.Bold),
                new Font(name,size,FontStyle.Strikeout|FontStyle.Underline|FontStyle.Regular),
                new Font(name,size,FontStyle.Strikeout|FontStyle.Underline|FontStyle.Bold),
                new Font(name,size,FontStyle.Strikeout|FontStyle.Underline|FontStyle.Italic),
                new Font(name,size,FontStyle.Strikeout|FontStyle.Underline|FontStyle.Italic|FontStyle.Bold),
            };
 
        }

        public void InitClientRectangle()
        {
            graphics = graphics ?? CreateGraphics();
            var s = "xyz";
            var size = TextRenderer.MeasureText(graphics, s, Fonts[0], Size.Empty, flags);
            charWidth = size.Width / s.Length;
            charHeight = size.Height;
            lineHeight = charHeight;
            ClientSize = new Size(charWidth * cols, lineHeight * rows);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            //base.Hide();
            base.OnFormClosed(e);
            if (main != null)
            {
                Runtime.Quit();
            }
        }

        internal void ClearKeyboardBuffer()
        {
            keyBuffer.Clear();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode != Keys.ControlKey)
            {
                var mod = ModifierKeys;
                lastKeyDown = new KeyInfo(e.KeyData);
                keyBuffer.Enqueue(lastKeyDown);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            e.Handled = true;

            if (lastKeyDown != null)
            {
                if ((lastKeyDown.KeyData & (Keys.Alt | Keys.Control)) == 0)
                {
                    // Combine ascii codes with key codes.
                    lastKeyDown.KeyChar = e.KeyChar;
                }
                else
                {
                    // Discard ASCII when the key combination is 'special'.
                }

                lastKeyDown = null;
            }
            else
            {
                keyBuffer.Enqueue(new KeyInfo(e.KeyChar));
            }

            base.OnKeyPress(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            var x = (e.Location.X/charWidth) % cols;
            var y = (e.Location.Y/charHeight) % rows;
            switch (e.Button)
            {
                case MouseButtons.Left:
                {
                keyBuffer.Enqueue( new KeyInfo(Keys.LButton,x,y));
                    break;
                }
                case MouseButtons.Right:
                {
                    keyBuffer.Enqueue( new KeyInfo(Keys.RButton,x,y));
                    break;
                }
            }
            base.OnMouseClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            base.OnPaint(e);
            var rect = e.ClipRectangle;
            var x1 = rect.Left / charWidth;
            var y1 = rect.Top / lineHeight;
            if (x1 >= cols)
            {
                x1 = cols - 1;
            }
            if (y1 >= rows)
            {
                y1 = rows - 1;
            }
            var x2 = (rect.Left + rect.Width + charWidth-1) / charWidth;
            var y2 = (rect.Top + rect.Height + lineHeight-1) / lineHeight;

            if (x2 >= cols)
            {
                x2 = cols - 1;
            }
            if (y2 >= rows)
            {
                y2 = rows - 1;
            }

            PaintArea(g, x1, y1, x2 - x1 + 1, y2 - y1 + 1);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (main != null)
            {
                if (!gotShownEvent)
                {
                    main();
                    gotShownEvent = true;
                }
            }
        }

        public void DoSetScrollPos(int row, int height, int maxHeight)
        {
//            if (maxHeight == -1)
//            {
//                this.VerticalScroll.Enabled = false;
//                this.VScroll = false;
//            }
//            else
//            {
//                this.VerticalScroll.Minimum = 0;
//                this.VerticalScroll.Maximum = maxHeight-height;
//                this.VerticalScroll.LargeChange = height;
//                this.VerticalScroll.SmallChange = 1;
//                this.VerticalScroll.Value = row;
//                this.VerticalScroll.Enabled = true;
//                this.VScroll = true;
//            }
        }

        public void DoUpdate(int x,int y,int w,int h)
        {
            if (w == 1)
            {
                // cursor update needs more bits to avoid artefacts
                if (x > 0)
                {
                    --x;
                    ++w;
                }
                if (x + w < cols)
                {
                    ++w;
                }
            }
            var bounds = new Rectangle(x * charWidth, y * lineHeight, w * charWidth, h * lineHeight);
            Invalidate(bounds);
        }

        public KeyInfo ReadKey()
        {
            do
            {
                Application.DoEvents();
                Runtime.Sleep( 1 );
            }
            while (keyBuffer.Count==0);
            return keyBuffer.Dequeue();
        }

        void PaintArea(Graphics g, int x, int y, int w, int h)
        {
            var g2 = (bufferedGraphics != null) ? bufferedGraphics.Graphics : g;
            for (var i = 0; i < h; ++i)
            {
                var beg = (y + i) * cols + x;
                var end = beg + w;

                while (beg < end)
                {
                    var run = FindColorLine(beg, end);
                    //var run2 = run;
                    var top = y + i;
                    var left = beg % cols;
                    var text = new String(ScreenBuffer.Data, beg, run);
                    var fg = ScreenBuffer.Fg[beg];
                    var bg = ScreenBuffer.Bg[beg];
                    var st = ScreenBuffer.Attr[beg];
                    PaintString(g2, text, left, top, run, fg, bg, st);
                    beg += run;
                }           
            }

            if (bufferedGraphics != null)
            {
                bufferedGraphics.Render(g);
            }
        }

        void PaintString(Graphics g, string text, int x, int y, int w, ColorType fg,ColorType bg, int attr )
        {
//            if (y == 0 && w==2)
//            {
//                int n = 37;
//            }
            var xx = x * charWidth;
            var yy = y * lineHeight;
            var ww = w * charWidth;
            var hh = 1 * lineHeight;
            var bounds3 = new Rectangle(xx, yy, ww, hh);
            //var bounds1 = (RectangleF)bounds3;
            int fontIndex = attr & TerminalAttribute.FontMask;
            bool reverse = (attr & TerminalAttribute.Reverse) != 0;
            if (reverse)
            {
                g.FillRectangle(new SolidBrush(fg), xx, yy, ww, hh);
                //g.DrawString(text, Font, new SolidBrush(bg), bounds1);
                TextRenderer.DrawText(g, text, Fonts[fontIndex], bounds3, bg, fg, flags);             
            }
            else
            {
                g.FillRectangle(new SolidBrush(bg), xx, yy, ww, hh);
                //g.DrawString(text, Font, new SolidBrush(fg), bounds1);
                TextRenderer.DrawText(g, text, Fonts[fontIndex], bounds3, fg, bg, flags);             
            }
        }

        public void ScrollUp(int x, int y, int w,int h)
        {
            // This is slower than a ordinary Window.Refresh()
            using (var g = CreateGraphics())
            {
                var xx = x * charWidth;
                var yy = y * lineHeight;
                var ww = w * charWidth;
                var hh = h * lineHeight;
                var scr = PointToScreen(new Point(xx, yy ));
                var bitmap = new Bitmap(ww, hh, g);
                using (var g2 = Graphics.FromImage(bitmap))
                {
                    g2.CopyFromScreen(scr.X, scr.Y + lineHeight, 0, 0, new Size(ww, hh - lineHeight));
                    g.DrawImageUnscaled(bitmap, xx, yy);
                    //g.DrawImage(bitmap, xx, yy);
                    PaintArea(g, x, y + h - 1, w, 1);
                }
            }
        }

        int FindColorLine(int beg, int end)
        {
            var count = 0;

            for (var p = beg; p < end; ++p)
            {
                if (ColorType.Equals(ScreenBuffer.Fg[beg], ScreenBuffer.Fg[p]) && ColorType.Equals(ScreenBuffer.Bg[beg], ScreenBuffer.Bg[p]) && ScreenBuffer.Attr[beg]==ScreenBuffer.Attr[p])
                {
                    ++count;
                }
                else
                {
                    break;
                }
            }
            return count;
        }

    }
}
