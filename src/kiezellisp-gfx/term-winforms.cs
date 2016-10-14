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
        public Font Font { get; set; }

        public static implicit operator Font(FontType ft)
        {
            return ft.Font;
        }

        public FontType(string name, int size)
        {
            Font = new Font(name, size);
        }

        public FontType(string name, int size, string style)
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
                    Font = new Font(name, size, FontStyle.Regular);
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
            //Color.CadetBlue;
            return ct.Color;
        }

        public static bool Equals(ColorType ct1, ColorType ct2)
        {
            return ct1.Color == ct2.Color;
        }

     
        public ColorType(object color)
        {
            if (color is ColorType)
            {
                _Color = ((ColorType)color).Color;
            }
            else if (Runtime.Integerp(color))
            {
                var number = Runtime.AsInt(color);
                _Color = Color.FromArgb(255, Color.FromArgb(number & 0xffffff));
            }
            else if (color is Color)
            {
                _Color = (Color)color;
            }
            else
            {
                var colorName = Runtime.GetDesignatedString(color);
                int number;
                object color2;

                if ((color2 = Runtime.GetStaticPropertyValue(typeof(Color), color)) != null)
                {
                    _Color = (Color)color2;
                }
                else if ((color2 = Runtime.GetStaticPropertyValue(typeof(SystemColors), color)) != null)
                {
                    _Color = (Color)color2;
                }
                else if (colorName.StartsWith("#"))
                {
                    var s = colorName.Substring(1);
                    if (s.Length == 6 && Number.TryParseHexNumber(s, out number))
                    {
                        _Color = Color.FromArgb(255, Color.FromArgb(number));
                    }
                    else
                    {
                        Runtime.ThrowError("Invalid #rrggbb color: ", color);
                        _Color = Color.Empty;
                    }
                }
                else
                {
                    Runtime.ThrowError("Invalid color name: ", color);
                    _Color = Color.Empty;
                }
            }
        }

        public override string ToString()
        {
            return _Color.ToString();
        }
    }

    public enum TerminalKeys
    {
        Home = Keys.Home,
        End = Keys.End,
        Delete = Keys.Delete,
        Insert = Keys.Insert,
        Enter = Keys.Enter,
        Escape = Keys.Escape,
        Up = Keys.Up,
        Down = Keys.Down,
        Left = Keys.Left,
        Right = Keys.Right,
        Back = Keys.Back,
        PageUp = Keys.PageUp,
        PageDown = Keys.PageDown,
        Control = Keys.Control,
        Shift = Keys.Shift,
        Alt = Keys.Alt,
        Tab = Keys.Tab,
        F1 = Keys.F1,
        F2 = Keys.F2,
        F3 = Keys.F3,
        F4 = Keys.F4,
        F5 = Keys.F5,
        F6 = Keys.F6,
        F7 = Keys.F7,
        F8 = Keys.F8,
        F9 = Keys.F9,
        F10 = Keys.F10,
        F11 = Keys.F11,
        F12 = Keys.F12,
        NumPad0 = Keys.NumPad0,
        NumPad1 = Keys.NumPad1,
        NumPad2 = Keys.NumPad2,
        NumPad3 = Keys.NumPad3,
        NumPad4 = Keys.NumPad4,
        NumPad5 = Keys.NumPad5,
        NumPad6 = Keys.NumPad6,
        NumPad7 = Keys.NumPad7,
        NumPad8 = Keys.NumPad8,
        NumPad9 = Keys.NumPad9,
        A = Keys.A,
        B = Keys.B,
        C = Keys.C,
        D = Keys.D,
        E = Keys.E,
        F = Keys.F,
        G = Keys.G,
        H = Keys.H,
        I = Keys.I,
        J = Keys.J,
        K = Keys.K,
        L = Keys.L,
        M = Keys.M,
        N = Keys.N,
        O = Keys.O,
        P = Keys.P,
        Q = Keys.Q,
        R = Keys.R,
        S = Keys.S,
        T = Keys.T,
        U = Keys.U,
        V = Keys.V,
        W = Keys.W,
        X = Keys.X,
        Y = Keys.Y,
        Z = Keys.Z,
        Space = Keys.Space,
        Scroll = Keys.Scroll,
        Pause = Keys.Pause,
        Menu = Keys.Menu,
        LButton = Keys.LButton,
        RButton = Keys.RButton,
        MButton = Keys.MButton,
        Wheel = Keys.F24
        // made up value
    }

    public class KeyInfo
    {
        public TerminalKeys KeyData { get; set; }

        public char KeyChar { get; set; }

        public int  MouseCol { get; set; }

        public int  MouseRow { get; set; }

        public int  MouseWheel { get; set; }

        public int MouseClicks { get; set; }

        public KeyInfo(Keys data)
        {
            KeyData = (TerminalKeys)data;
            KeyChar = (char)0;
        }

        public KeyInfo(TerminalKeys data, int col, int row, int clicks, int wheel)
        {
            KeyData = data;
            KeyChar = (char)0;
            MouseCol = col;
            MouseRow = row;
            MouseClicks = clicks;
            MouseWheel = wheel;
        }

        public KeyInfo(char data)
        {
            KeyData = 0;
            KeyChar = data;
        }

        public KeyInfo()
        {
        }
    }

    public class PlaybackInfo: KeyInfo
    {
        public int Time = 0;
        public List<string> Lines = new List<string>();
    }

    public delegate void TerminalMainProgram();


    public class TerminalMainForm: Form
    {
        static TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.NoClipping | TextFormatFlags.Left | TextFormatFlags.SingleLine;
        int charWidth;
        int charHeight;
        int lineHeight;
        int cols;
        int rows;
        Queue<KeyInfo> keyBuffer;
        Queue<KeyInfo> playbackBuffer;
        KeyInfo lastKeyDown;
        TerminalMainProgram main;
        public Buffer ScreenBuffer;
        bool gotShownEvent;
        BufferedGraphics bufferedGraphics = null;
        Graphics graphics;
        Font[] Fonts;

        public int Cols { get { return cols; } }

        public int Rows { get { return rows; } }

        public TerminalMainForm(string fontName, int fontSize, int w, int h, TerminalMainProgram mainProgram)
        {
            this.Text = "Kiezellisp";
            this.main = mainProgram;
            BackColor = Terminal.DefaultBackColor;
            keyBuffer = new Queue<KeyInfo>();
            playbackBuffer = new Queue<KeyInfo>();
            StartPosition = FormStartPosition.CenterScreen;
            gotShownEvent = false;
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.Fixed3D;
            InitBuffer(w, h);
            InitFont(fontName, fontSize);
            InitClientRectangle();
            //bufferedGraphics = BufferedGraphicsManager.Current.Allocate(graphics, ClientRectangle);
        }

        public void InitBuffer(int w, int h)
        {
            rows = h;
            cols = w;
            ScreenBuffer = new Buffer(w, h);
        }

        public void InitFont(string name, int size)
        {
            Fonts = new Font[]
            {
                new Font(name, size, FontStyle.Regular),
                new Font(name, size, FontStyle.Bold),
                new Font(name, size, FontStyle.Italic),
                new Font(name, size, FontStyle.Italic | FontStyle.Bold),
                new Font(name, size, FontStyle.Underline | FontStyle.Regular),
                new Font(name, size, FontStyle.Underline | FontStyle.Bold),
                new Font(name, size, FontStyle.Underline | FontStyle.Italic),
                new Font(name, size, FontStyle.Underline | FontStyle.Italic | FontStyle.Bold),
                new Font(name, size, FontStyle.Strikeout | FontStyle.Regular),
                new Font(name, size, FontStyle.Strikeout | FontStyle.Bold),
                new Font(name, size, FontStyle.Strikeout | FontStyle.Italic),
                new Font(name, size, FontStyle.Strikeout | FontStyle.Italic | FontStyle.Bold),
                new Font(name, size, FontStyle.Strikeout | FontStyle.Underline | FontStyle.Regular),
                new Font(name, size, FontStyle.Strikeout | FontStyle.Underline | FontStyle.Bold),
                new Font(name, size, FontStyle.Strikeout | FontStyle.Underline | FontStyle.Italic),
                new Font(name, size, FontStyle.Strikeout | FontStyle.Underline | FontStyle.Italic | FontStyle.Bold),
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
                RuntimeGfx.Quit();
            }
        }


        public void ClearKeyboardBuffer()
        {
            keyBuffer.Clear();
            playbackBuffer.Clear();
        }

        public void LoadKeyboardBuffer(List<KeyInfo> keys)
        {
            foreach (var key in keys)
            {
                playbackBuffer.Enqueue(key);
            }
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
                if ((lastKeyDown.KeyData & (TerminalKeys.Alt | TerminalKeys.Control)) == 0)
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

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            var x = (e.Location.X / charWidth) % cols;
            var y = (e.Location.Y / charHeight) % rows;
            var c = e.Clicks;
            switch (e.Button)
            {
                case MouseButtons.Left:
                {
                    keyBuffer.Enqueue(new KeyInfo(TerminalKeys.LButton, x, y, c, 0));
                    break;
                }
                case MouseButtons.Right:
                {
                    keyBuffer.Enqueue(new KeyInfo(TerminalKeys.RButton, x, y, c, 0));
                    break;
                }
            }
            base.OnMouseDoubleClick(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            var x = (e.Location.X / charWidth) % cols;
            var y = (e.Location.Y / charHeight) % rows;
            var c = e.Clicks;
            switch (e.Button)
            {
                case MouseButtons.Left:
                {
                    keyBuffer.Enqueue(new KeyInfo(TerminalKeys.LButton, x, y, c, 0));
                    break;
                }
                case MouseButtons.Right:
                {
                    keyBuffer.Enqueue(new KeyInfo(TerminalKeys.RButton, x, y, c, 0));
                    break;
                }
            }
            base.OnMouseClick(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            var x = (e.Location.X / charWidth) % cols;
            var y = (e.Location.Y / charHeight) % rows;
            var w = e.Delta;
            keyBuffer.Enqueue(new KeyInfo(TerminalKeys.Wheel, x, y, 0, w));
            base.OnMouseWheel(e);
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
            var x2 = (rect.Left + rect.Width + charWidth - 1) / charWidth;
            var y2 = (rect.Top + rect.Height + lineHeight - 1) / lineHeight;

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

        public void DoUpdate(int x, int y, int w, int h)
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

        public void DoUpdateDirect(int x, int y, int w, int h)
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
            //var bounds = new Rectangle(x * charWidth, y * lineHeight, w * charWidth, h * lineHeight);
            //Invalidate(bounds);
            PaintArea(graphics, x, y, w, h);
        }

        public KeyInfo ReadKey()
        {
            CheckInterruptKey();

            if (playbackBuffer.Count != 0)
            {
                return playbackBuffer.Dequeue();
            }
            else
            {
                return ReadTerminalKey();
            }
        }

        public KeyInfo ReadTerminalKey()
        {
            do
            {
                Application.DoEvents();
                Runtime.Sleep(1);
            }
            while (keyBuffer.Count == 0);

            CheckInterruptKey();

            return keyBuffer.Dequeue();
        }

        void CheckInterruptKey()
        {
            if (keyBuffer.Count != 0)
            {
                var e = keyBuffer.Peek();
                if (e.KeyData == (TerminalKeys.D | TerminalKeys.Control))
                {
                    ClearKeyboardBuffer();
                    throw new InterruptException();
                }
            }
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
                    var st = ScreenBuffer.FontIndex[beg];
                    PaintString(g2, text, left, top, run, fg, bg, st);
                    beg += run;
                }           
            }

            if (bufferedGraphics != null)
            {
                bufferedGraphics.Render(g);
            }
        }

        void PaintString(Graphics g, string text, int x, int y, int w, ColorType fg, ColorType bg, int fontIndex)
        {
            var xx = x * charWidth;
            var yy = y * lineHeight;
            var ww = w * charWidth;
            var hh = 1 * lineHeight;
            var bounds3 = new Rectangle(xx, yy, ww, hh);
            g.FillRectangle(new SolidBrush(bg), xx, yy, ww, hh);
            TextRenderer.DrawText(g, text, Fonts[fontIndex], bounds3, fg, bg, flags);             
        }

        public void ScrollUp(int x, int y, int w, int h)
        {
            // Not used
            // This is slower than a ordinary Window.Refresh()
            using (var g = CreateGraphics())
            {
                var xx = x * charWidth;
                var yy = y * lineHeight;
                var ww = w * charWidth;
                var hh = h * lineHeight;
                var scr = PointToScreen(new Point(xx, yy));
                var bitmap = new Bitmap(ww, hh, g);
                using (var g2 = Graphics.FromImage(bitmap))
                {
                    g2.CopyFromScreen(scr.X, scr.Y + lineHeight, 0, 0, new Size(ww, hh - lineHeight));
                    g.DrawImageUnscaled(bitmap, xx, yy);
                    PaintArea(g, x, y + h - 1, w, 1);
                }
            }
        }

        int FindColorLine(int beg, int end)
        {
            var count = 0;

            for (var p = beg; p < end; ++p)
            {
                if (ColorType.Equals(ScreenBuffer.Fg[beg], ScreenBuffer.Fg[p]) && ColorType.Equals(ScreenBuffer.Bg[beg], ScreenBuffer.Bg[p]) && ScreenBuffer.FontIndex[beg] == ScreenBuffer.FontIndex[p])
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

    public partial class RuntimeGfx
    {
        [LispAttribute("set-clipboard")]
        public static void SetClipboardData(string str)
        {
            if (String.IsNullOrEmpty(str))
            {
                System.Windows.Forms.Clipboard.Clear();
            }
            else
            {
                System.Windows.Forms.Clipboard.SetText(str);
            }
        }

        [LispAttribute("get-clipboard")]
        public static string GetClipboardData()
        {
            string str = System.Windows.Forms.Clipboard.GetText();
            return str;
        }

        [LispAttribute("load-clipboard")]
        public static void LoadClipboardData()
        {
            var code = GetClipboardData();
            using (var stream = new StringReader(code))
            {
                Runtime.TryLoadText(stream, null, null, false, false);
            }
        }

        [LispAttribute("run-clipboard")]
        public static void RunClipboardData()
        {
            var code = GetClipboardData();
            using (var stream = new StringReader(code))
            {
                Runtime.TryLoadText(stream, null, null, false, false);
                var main = Symbols.Main.Value as IApply;
                if (main != null)
                {
                    Runtime.Funcall(main);
                }
            }
        }

        public static void ProcessEvents()
        {
            Application.DoEvents();
        }

        public static void ApplicationRun(Form form)
        {
            Application.Run((Form)form);
        }
    }

}
