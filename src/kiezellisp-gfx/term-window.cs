// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Kiezel
{
    public delegate void EditHandler();
    public delegate bool ScrollHandler();
    public delegate bool MouseHandler(int x,int y,int w);
}
namespace Kiezel
{
    public class Window: IDisposable, IHasTextWriter
    {
        internal  Dictionary<TerminalKeys,object> ScrollHandlers = new Dictionary<TerminalKeys, object>();
        internal  Dictionary<TerminalKeys,object> EditHandlers = new Dictionary<TerminalKeys, object>();

        // location of window on the screen
        public int ScreenLeft { get; internal set; }

        public int ScreenTop { get; internal set; }

        // location of window on the buffer
        public Buffer Buffer { get; internal set; }

        public int BufferLeft { get; internal set; }

        public int BufferTop { get; internal set; }

        public int BufferHeight { get { return Buffer.Height; } }

        public int BufferWidth { get { return Buffer.Width; } }

        // size of the window
        public int Width { get; internal set; }

        public int Height { get; internal set; }

        public int Size { get; internal set; }

        // current insert position, colors and attributes
        public int Row { get; internal set; }

        public int Col { get; internal set; }

        public int Style { get; set; }

        public int FontIndex { get { return Style & TextStyle.FontMask; } }

        ColorType _ForeColor;

        public object ForeColor
        { 
            get
            {
                return _ForeColor;
            }
            set
            {
                _ForeColor = new ColorType(value);
            }
        }

        ColorType _BackColor;

        public object BackColor
        { 
            get
            {
                return _BackColor;
            }
            set
            {
                _BackColor = new ColorType(value);
            }
        }

        ColorType _HighlightForeColor;

        public object HighlightForeColor
        { 
            get
            {
                return _HighlightForeColor;
            }
            set
            {
                _HighlightForeColor = new ColorType(value);
            }
        }

        ColorType _HighlightBackColor;

        public object HighlightBackColor
        { 
            get
            {
                return _HighlightBackColor;
            }
            set
            {
                _HighlightBackColor = new ColorType(value);
            }
        }

        ColorType _ShadowBackColor;

        public object ShadowBackColor
        { 
            get
            {
                return _ShadowBackColor;
            }
            set
            {
                _ShadowBackColor = new ColorType(value);
            }
        }

        string _caption;

        public string Caption
        {
            get
            {
                return _caption;
            }
            set
            {
                _caption = value;
                if (BoxWindow != null)
                {
                    BoxWindow.DrawBox(_caption);
                }
            }
        }

        public bool Normal
        {
            get
            {
                return Style == 0;
            }
            set
            {
                Style = 0;
            }
        }

        public bool Bold
        {
            get
            {
                return (Style & TextStyle.Bold) != 0;
            }
            set
            {
                if (value)
                {
                    Style |= TextStyle.Bold;
                }
                else
                {
                    Style &= ~TextStyle.Bold;
                }
            }
        }

        public bool Italic
        {
            get
            {
                return (Style & TextStyle.Italic) != 0;
            }
            set
            {
                if (value)
                {
                    Style |= TextStyle.Italic;
                }
                else
                {
                    Style &= ~TextStyle.Italic;
                }
            }
        }

        public bool Underline
        {
            get
            {
                return (Style & TextStyle.Underline) != 0;
            }
            set
            {
                if (value)
                {
                    Style |= TextStyle.Underline;
                }
                else
                {
                    Style &= ~TextStyle.Underline;
                }
            }
        }

        public bool Strikeout
        {
            get
            {
                return (Style & TextStyle.Strikeout) != 0;
            }
            set
            {
                if (value)
                {
                    Style |= TextStyle.Strikeout;
                }
                else
                {
                    Style &= ~TextStyle.Strikeout;
                }
            }
        }

        public bool Highlight
        {
            get
            {
                return (Style & TextStyle.Highlight) != 0;
            }
            set
            {
                if (value)
                {
                    Highlight = Shadow = Reverse = false;
                    Style |= TextStyle.Highlight;
                }
                else
                {
                    Style &= ~TextStyle.Highlight;
                }
            }
        }

        public bool Shadow
        {
            get
            {
                return (Style & TextStyle.Shadow) != 0;
            }
            set
            {
                if (value)
                {
                    Highlight = Shadow = Reverse = false;
                    Style |= TextStyle.Shadow;
                }
                else
                {
                    Style &= ~TextStyle.Shadow;
                }
            }
        }

        public bool Reverse
        {
            get
            {
                return (Style & TextStyle.Reverse) != 0;
            }
            set
            {
                if (value)
                {
                    Highlight = Shadow = Reverse = false;
                    Style |= TextStyle.Reverse;
                }
                else
                {
                    Style &= ~TextStyle.Reverse;
                }
            }
        }

        public int Pos
        { 
            get
            { 
                return Row * Width + Col; 
            } 
            set
            {
                Row = value / Width;
                Col = value % Width;
            }
        }

        // window state
        public bool AutoRefreshEnabled { get; set; }

        public bool ScrollEnabled { get; set; }

        public bool TabCompletionEnabled { get; set; }

        public bool HtmlEnabled { get; set; }

        public bool Dirty;
        public bool ScrollPosEnabled;
        public Window BoxWindow;
        public WindowTextWriter TextWriter;

        ColorType fgUnderCursor;
        ColorType bgUnderCursor;
        int attrUnderCursor;
        int CursorRow;
        int CursorCol;
        bool CursorVisible;
        int HomeRow;
        int HomeCol;
        int EndRow;
        int EndCol;
        int SavedRow;
        int SavedCol;

        internal int HomePos
        { 
            get
            { 
                return HomeRow * Width + HomeCol; 
            } 
            set
            {
                HomeRow = value / Width;
                HomeCol = value % Width;
            }
        }

        internal int EndPos
        { 
            get
            { 
                return EndRow * Width + EndCol; 
            } 
            set
            {
                EndRow = value / Width;
                EndCol = value % Width;
            }
        }

        internal int SavedPos
        { 
            get
            { 
                return SavedRow * Width + SavedCol; 
            } 
            set
            {
                SavedRow = value / Width;
                SavedCol = value % Width;
            }
        }

        internal bool _visible;

        public bool Visible
        {
            get
            {
                return _visible;
            }
            set
            {
                // Cannot hide repl
                if (value || this != Terminal.StdScr)
                {
                    if (_visible != value)
                    {
                        _visible = value;
                        RefreshAll();
                    }
                }
            }
        }


        internal Window(int screenLeft, int screenTop, Buffer pad, int left, int top, int width, int height, object fg, object bg)
        {
            ScreenLeft = screenLeft;
            ScreenTop = screenTop;
            Buffer = pad;
            SetViewPort(left, top, width, height);
            ForeColor = fg ?? Terminal.DefaultForeColor;
            BackColor = bg ?? Terminal.DefaultBackColor;
            HighlightForeColor = Terminal.DefaultHighlightForeColor;
            HighlightBackColor = Terminal.DefaultHighlightBackColor;
            ShadowBackColor = Terminal.DefaultShadowBackColor;
            Visible = true;
            ScrollEnabled = false;
            AutoRefreshEnabled = false;
            TabCompletionEnabled = false;
            HtmlEnabled = false;
            TextWriter = new WindowTextWriter(this);
            BoxWindow = null;
            ScrollPosEnabled = false;
            InitScrollHandlers();
            InitEditHandlers();
            Dirty = true;
            Style = 0;
            CursorVisible = false;
        }

        static void CloseAll()
        {
            Terminal.CloseAllWindows();
        }

        static void RefreshAll()
        {
            Terminal.RefreshAllWindows();
        }

        internal static void CheckBounds(ref int x, ref int y, ref int w, ref int h, int width, int height)
        {
            if (x == -1)
            {
                Runtime.Assert(0 < w && w <= width, "Invalid w");
                x = (width - w) / 2;
            }
            else if (w == 0)
            {
                Runtime.Assert(0 <= x && x < width, "Invalid x");
                w = width - x;
            }
            else
            {
                Runtime.Assert(0 <= x && x < width, "Invalid x");
                Runtime.Assert(0 < w && w <= width, "Invalid w");
                Runtime.Assert(x + w <= width, "Invalid x+w");
            }

            if (y == -1)
            {
                Runtime.Assert(0 < h && h <= height, "Invalid h");
                y = (height - h) / 2;
            }
            else if (h == 0)
            {
                Runtime.Assert(0 <= y && y < height, "Invalid y");
                h = height - y;
            }
            else
            {
                Runtime.Assert(0 <= y && y < height, "Invalid y");
                Runtime.Assert(0 < h && h <= height, "Invalid h");
                Runtime.Assert(y + h <= height, "Invalid y+h");
            }
        }

        public static Window Create(int x, int y, int w, int h, int maxh, object fg, object bg, Window parent, bool border)
        {
            maxh = Math.Max(h, maxh);

            if (parent != null)
            {
                return CreateSubwindow(parent, x, y, w, h, fg, bg);
            }
            else if (border)
            {
                return CreateFrameWindow(x, y, w, h, maxh, fg, bg);
            }
            else
            {
                return CreateWindow(x, y, w, h, maxh, fg, bg);
            }
        }

        static Window CreateWindow(int x, int y, int w, int h, int maxh, object fg, object bg)
        {
            CheckBounds(ref x, ref y, ref w, ref h, Terminal.Width, Terminal.Height);
            maxh = Math.Max(h, maxh);
            var pad = new Buffer(w, maxh);
            var win = new Window(x, y, pad, 0, 0, w, h, fg, bg);
            if (h != maxh)
            {
                win.ScrollEnabled = true;
                win.ScrollPosEnabled = true;
            }
            Terminal.Register(win);
            return win;
        }

        static Window CreateSubwindow(Window orig, int x, int y, int w, int h, object fg, object bg)
        {
            CheckBounds(ref x, ref y, ref w, ref h, orig.Width, orig.Height);
            var win = new Window(orig.ScreenLeft + x, orig.ScreenTop + y, orig.Buffer, x, y, w, h, fg, bg);
            Terminal.Register(win);
            return win;
        }

        static Window CreateFrameWindow(int x, int y, int w, int h, int maxh, object fg, object bg)
        {
            CheckBounds(ref x, ref y, ref w, ref h, Terminal.Width, Terminal.Height);
            maxh = Math.Max(h, maxh);
            // box does not need maxh buffer
            var box = CreateWindow(x, y, w, h, -1, fg, bg);
            box.DrawBox();
            var win = CreateWindow(x + 1, y + 1, w - 2, h - 2, maxh - 2, fg, bg);
            win.BoxWindow = box;
            Terminal.Unregister(box);
            return win;
        }


        public void Close()
        {
            // remove from stack and repaint the other windows according to z-order.
            if (this == Terminal.StdScr)
            {
                return;
            }

            Terminal.Unregister(this);
            RefreshAll();
        }

        void IDisposable.Dispose()
        {
            Close();
        }


        internal void SetViewPort(int left, int top, int width, int height)
        {
            if (left == -1)
            {
                left = (Buffer.Width - width) / 2;
            }

            if (top == -1)
            {
                top = (Buffer.Height - height) / 2;
            }

            if (width == 0)
            {
                width = Buffer.Width - left;
            }

            if (height == 0)
            {
                height = Buffer.Height - top;
            }

            BufferLeft = left;
            BufferTop = top;
            Width = width;
            Height = height;
            Size = Width * Height;

            Pos = 0;
            HomePos = 0;
            EndPos = 0;

        }

        public bool SetWindowPos(int x, int y)
        {
            if (x != ScreenLeft || y != ScreenTop)
            {
                ScreenLeft = x;
                ScreenTop = y;
                RefreshAll();
                return true;
            }
            return false;
        }

        public bool SetBufferPos(int x, int y)
        {
            if (x != BufferLeft || y != BufferTop)
            {
                if (0 <= x && x + Width <= Buffer.Width && 0 <= y && y + Height <= Buffer.Height)
                {
                    BufferLeft = x;
                    BufferTop = y;
                    Refresh();
                    return true;
                }
            }
            return false;
        }

        public void BringToTop()
        {
            Terminal.BringToTop(this);
        }

        internal void ScrollOne()
        {
            if (ScrollPosEnabled && BufferTop + Height < Buffer.Height)
            {
                ++BufferTop;
                Refresh();
            }
            else
            {
                Scroll(0, Height, 1);
            }

            --Row;
            --HomeRow;
            --EndRow;
            --CursorRow;
            --SavedRow;
        }

        internal void Next(bool scrollok = false, bool refresh = false)
        {
            if (scrollok || ScrollEnabled)
            {
                if (Pos + 1 == Size)
                {
                    ScrollOne();
                }
                Pos += 1;
            }
            else
            {
                // wrap
                Pos = (Pos + 1) % Size;
            }
            if (refresh && Col == 0)
            {
                RuntimeGfx.ProcessEvents();
            }
        }

        public void Clear()
        {
            Dirty = true;
            Buffer.ClearRect(BufferLeft, BufferTop, Width, Height, _ForeColor, _BackColor);
            SetCursorPos(0, 0);
        }

        public void ClearToEol()
        {
            Dirty = true;
            for (var col = Col; col < Width; ++col)
            {
                Set(col, Row, ' ', _ForeColor, _BackColor, 0);
            }
        }

        public void ClearToBot()
        {
            Dirty = true;
            for (var pos = Pos; pos < Size; ++pos)
            {
                Set(pos, ' ', _ForeColor, _BackColor, 0);
            }
        }

        public void DrawBox()
        {
            DrawBox(null);
        }

        public void DrawBox(string caption)
        {
            Dirty = true;
  
            var w = Width - 1;
            var h = Height - 1;

            DrawLine(0, 0, 0, h);
            DrawLine(0, 0, w, 0);
            DrawLine(0, h, w, h);
            DrawLine(w, 0, w, h);

            if (!String.IsNullOrEmpty(caption))
            {
                var s = " " + caption.Trim() + " ";
                var offset = (Width - s.Length) / 2;
                SetCursorPos(offset, 0);
                var style = Style;
                Reverse = true;
                Write(s);
                Style = style;
            }
        }

        internal void VerifySizes(bool boxed, int totalSize, int[] sizes)
        {
            if (sizes.Length < 2)
            {
                Runtime.ThrowError("Window split: cannot split in less than 2 parts");
            }
            var knownSize = boxed ? sizes.Length + 1 : 0;
            var holeCount = 0;
            foreach (var s in sizes)
            {
                if (s > 0)
                {
                    knownSize += s;
                }
                else
                {
                    holeCount += 1;
                }
            }
            if (knownSize + holeCount > totalSize)
            {
                Runtime.ThrowError("Window split: insufficient width/height to accommodate all parts");
            }
            if (holeCount == 0)
            {
                if (knownSize != totalSize)
                {
                    Runtime.ThrowError("Window split: parts do not add up to correct width/length");
                }
                return;
            }
            else
            {
                var holeSize = (totalSize - knownSize) / holeCount;
                var roundError = totalSize - knownSize - holeSize * holeCount;
                for (int i = 0; i < sizes.Length; ++i)
                {
                    if (sizes[i] <= 0)
                    {
                        sizes[i] = holeSize + roundError;
                        roundError = 0;
                    }
                }
            }
        }
        //        public Window[] Vsplit(bool boxed, params int[] widths)
        //        {
        //            widths = (int[])widths.Clone();
        //            VerifySizes(boxed, Width, widths);
        //            var d = boxed ? 1 : 0;
        //            var x = Left + d;
        //            var y = Top + d;
        //            var h = Height - 2 * d;
        //            var list = new List<Window>();
        //            foreach (var w in widths)
        //            {
        //                list.Add(new Window(this, x, y, w, h));
        //                x += w + d;
        //            }
        //            if (boxed)
        //            {
        //                DrawBox();
        //                x = Left;
        //                foreach (var w in widths)
        //                {
        //                    DrawLine(x, Top, x, Top + Height - 1);
        //                    x += w + d;
        //                }
        //            }
        //            return list.ToArray();
        //        }
        //        public Window[] Hsplit(bool boxed, params int[] heights)
        //        {
        //            heights = (int[])heights.Clone();
        //            VerifySizes(boxed, Height, heights);
        //            var d = boxed ? 1 : 0;
        //            var x = Left + d;
        //            var y = Top + d;
        //            var w = Width - 2 * d;
        //            var list = new List<Window>();
        //            foreach (var h in heights)
        //            {
        //                list.Add(new Window(this, x, y, w, h));
        //                y += h + d;
        //            }
        //            if (boxed)
        //            {
        //                DrawBox();
        //                y = Top;
        //                foreach (var h in heights)
        //                {
        //                    DrawLine(Left, y, Left + Width - 1, y);
        //                    y += h + d;
        //                }
        //            }
        //            return list.ToArray();
        //        }

        public void Scroll(int count)
        {
            Scroll(0, -1, count);
        }

        internal void Scroll(int top, int height, int count)
        {
            if (top < 0)
            {
                return;
            }

            if (height < 0 || top + height > Height)
            {
                height = Height - top;
            }

            if (height <= 0)
            {
                return;
            }

            if (count > 0)
            {
                if (count > height)
                {
                    count = height;
                }
                for (int i = 0; i < count; ++i)
                {
                    ScrollUp(top, height);
                }
                Refresh(); 
            }
            else if (count < 0)
            {
                count = -count;
                if (count > height)
                {
                    count = height;
                }
                for (int i = 0; i < count; ++i)
                {
                    ScrollDown(top, height);
                }
                Refresh();
            }
        }

        public void Write(char ch)
        {
            Write(ch, false);
        }

        public void WriteLine(char ch)
        {
            Write(ch, false);
            WriteLine();
        }

        public void Write(char ch, bool refresh)
        {
            HideCursor();

            if (Pos < 0 || Pos >= Size)
            {
                return;
            }


            switch (ch)
            {
                case '\t':
                {
                    do
                    {
                        Write(' ', refresh);
                    } while ((Col % 8) != 0);
                    break;
                }
                
                case '\n':
                {
                    do
                    {
                        Write(' ', refresh);
                    }
                    while (Col != 0);
                    break;
                }

                case '\r':
                {
                    Col = 0;
                    break;
                }

                default:
                {
                    if (Reverse)
                    {
                        Set(Pos, ch, _BackColor, _ForeColor, FontIndex);
                    }
                    else if (Highlight)
                    {
                        Set(Pos, ch, _HighlightForeColor, _HighlightBackColor, FontIndex);
                    }
                    else if (Shadow)
                    {
                        Set(Pos, ch, _ForeColor, _ShadowBackColor, FontIndex);
                    }
                    else
                    {
                        Set(Pos, ch, _ForeColor, _BackColor, FontIndex);
                    }

                    if (refresh)
                    {
                        RefreshPosition(Col, Row);
                    }
                    else
                    {
                        Dirty = true;
                    }
                    Next(false, refresh);
                    break;
                }
            }

        }

        public void WriteLine(string str)
        {
            Write(str);
            WriteLine();
        }

        public void WriteLine()
        {
            Write('\n');
        }

        public void Write(string str)
        {
            if (str != null)
            {
                foreach (var ch in str)
                {
                    Write(ch);
                }
            }
        }

        public void Write(string str, bool refresh)
        {
            foreach (var ch in str)
            {
                Write(ch, refresh);
            }
        }

        public void SetCursorPos(int col, int row)
        {
            Row = row;
            Col = col;
        }

        void DrawMenu(int prefixLength, List<string> items, int offset, int cursor)
        {
            var style = Style;
            for (int r = 0, i = offset; r < this.Height; ++r, ++i)
            {
                if (i < items.Count)
                {
                    var s = items[i];
                    SetCursorPos(0, r);
                    if (r == cursor)
                    {
                        var s1 = items[i].Substring(0, prefixLength);
                        var s2 = items[i].Substring(prefixLength);
                        var fg = _ForeColor;
                        var bg = _BackColor;
                        Style = TextStyle.Bold | TextStyle.Underline;
                        _ForeColor = _HighlightForeColor;
                        _BackColor = _HighlightBackColor;
                        Write(s1);
//                        _ForeColor = bg;
//                        _BackColor = fg;
                        Style = 0;
                        Write(s2);
                        _ForeColor = fg;
                        _BackColor = bg;
                    }
                    else
                    {
                        Write(s);
                    }
                }
                else
                {                   
                    SetCursorPos(0, r);
                    ClearToEol();
                }

            }
            Style = style;
        }

        public object RunMenu(IEnumerable items)
        {
            var index = RunMenu(items, null, null);
            return index >= 0 ? (object)index : null;
        }

        public object RunMenu(IEnumerable items, IApply handler)
        {
            var index = RunMenu(items, handler, null);
            return index >= 0 ? (object)index : null;
        }

        internal int RunMenu(IEnumerable items, IApply handler, Func<KeyInfo,IEnumerable> keyHandler)
        {
            var v = new List<string>();
            var offset = 0;
            var cursor = 0;
            var prefixLength = -1;
            var extra = 0;
            System.Action<IEnumerable> setup = (seq) =>
            {
                prefixLength = -1;
                extra = 0;
                v = new List<string>();
                foreach (var item in seq)
                {
                    var s = item.ToString();
                    if (keyHandler != null && prefixLength == -1)
                    {
                        prefixLength = s.Length;
                    }
                    if (s.Length > Width)
                    {
                        s = s.Substring(0, Width);
                    }
                    v.Add(s.PadRight(Width));
                }
                if (prefixLength != -1 && v.Count > 1)
                {
                    v.RemoveAt(0);
                    extra = 1;
                }
                offset = 0;
                cursor = 0;
                if (prefixLength == -1)
                {
                    prefixLength = 0;
                }
            };
            System.Func<bool> upArrow = () =>
            {
                if (cursor > 0)
                {
                    --cursor;
                    return true;
                }
                else if (offset > 0)
                {
                    --offset;
                    return true;
                }
                else
                {
                    return false;
                }
            };
            System.Func<bool> downArrow = () =>
            {
                if (cursor + offset + 1 < v.Count)
                {
                    if (cursor + 1 < Height)
                    {
                        ++cursor;
                        return true;
                    }
                    else if (offset + 1 < v.Count)
                    {
                        ++offset;
                        return true;
                    }
                }
                return false;
            };
            setup(items);
            Clear();
            while (true)
            {
                DrawMenu(prefixLength, v, offset, cursor);
                var k = ReadKey(false);
                switch (k.KeyData)
                {
                    case TerminalKeys.LButton:
                    {
                        break;
                    }
                    case TerminalKeys.Enter:
                    {
                        var choice = offset + cursor + extra;
                        if (handler == null)
                        {
                            return choice;
                        }
                        else
                        {
                            DrawMenu(prefixLength, v, offset, -1);
                            Refresh();
                            if (!Runtime.FuncallBool(handler, choice))
                            {
                                return -1;
                            }
                        }
                        break;
                    }
                    case TerminalKeys.Escape:
                    {
                        return -1;
                    }
                    case TerminalKeys.Home:
                    {
                        cursor = offset = 0;
                        break;
                    }
                    case TerminalKeys.PageUp:
                    {
                        cursor = 0;
                        for (var i = 0; i < Height; ++i)
                        {
                            upArrow();
                        }
                        break;
                    }
                    case TerminalKeys.Up:
                    {
                        upArrow();
                        break;
                    }
                    case TerminalKeys.Down:
                    case TerminalKeys.Tab:
                    {
                        downArrow();
                        break;
                    }
                    case TerminalKeys.PageDown:
                    {
                        cursor = Height - 1;
                        for (var i = 0; i < Height; ++i)
                        {
                            downArrow();
                        }
                        break;
                    }
                    case TerminalKeys.End:
                    {
                        while (downArrow())
                        {
                        }
                        break;
                    }
                    default:
                    {
                        if (keyHandler != null)
                        {
                            var seq = keyHandler(k);
                            if (seq != null)
                            {
                                setup(seq);
                            }
                        }
                        break;
                    }
                }
            }
        }

        internal void ShowCursor()
        {
            HideCursor();
            if (0 <= Row && Row < Height)
            {
                CursorVisible = true;
                CursorRow = Row;
                CursorCol = Col;
                char dummy;
                Get(CursorCol, CursorRow, out dummy, out fgUnderCursor, out bgUnderCursor, out attrUnderCursor);
                Set(CursorCol, CursorRow, (char)0, bgUnderCursor, fgUnderCursor, attrUnderCursor);
                RefreshPosition(CursorCol, CursorRow);
            }
        }

        internal void HideCursor()
        {
            if (CursorVisible)
            {
                CursorVisible = false;
                Set(CursorCol, CursorRow, (char)0, fgUnderCursor, bgUnderCursor, attrUnderCursor);
                RefreshPosition(CursorCol, CursorRow);
            }
        }

        internal void DoUpdate()
        {
            if (Visible)
            {
                if (BoxWindow != null)
                {
                    BoxWindow.DoUpdate();
                }
                else
                {
                    DoUpdate(0, 0, Width, Height);
                }
            }
        }

        public void OutRefresh()
        {
            if (BoxWindow != null)
            {
                BoxWindow.OutRefresh();
            }
            Buffer.CopyRect(Terminal.ScreenBuffer, ScreenLeft, ScreenTop, Buffer, BufferLeft, BufferTop, Width, Height);
        }

        public void Refresh()
        {
            OutRefresh();
            DoUpdate();
            Dirty = false;
        }

        void Set(int x, int y, char ch)
        {
            // Only used for graphics! Use normal font.
            Set(x, y, ch, _ForeColor, _BackColor, 0);
        }

        bool GetAcs(char target, int col, int row)
        {
            if (0 <= col && col < Width && 0 <= row && row < Height)
            {
                char code = Get(col, row);
                if (code == target)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public void DrawLine(int x1, int y1, int x2, int y2)
        {
            if (x1 == x2)
            {
                // vertical
                for (int y = y1; y <= y2; ++y)
                {
                    Set(x1, y, (char)Terminal.Acs.VLINE);
                }
                for (int y = y1; y <= y2; ++y)
                {
                    var east = GetAcs(Terminal.Acs.HLINE, x1 + 1, y);
                    var west = GetAcs(Terminal.Acs.HLINE, x1 - 1, y);
                    if (west && east)
                    {
                        if (y == y1)
                        {
                            Set(x1, y, (char)Terminal.Acs.TTEE);
                        }
                        else if (y < y2)
                        {
                            Set(x1, y, (char)Terminal.Acs.PLUS);
                        }
                        else
                        {
                            Set(x1, y, (char)Terminal.Acs.BTEE);
                        }
                    }
                    else if (west)
                    {
                        if (y == y1)
                        {
                            Set(x1, y, (char)Terminal.Acs.URCORNER);
                        }
                        else if (y < y2)
                        {
                            Set(x1, y, (char)Terminal.Acs.RTEE);
                        }
                        else
                        {
                            Set(x1, y, (char)Terminal.Acs.LRCORNER);
                        }
                    }
                    else if (east)
                    {
                        if (y == y1)
                        {
                            Set(x1, y, (char)Terminal.Acs.ULCORNER);
                        }
                        else if (y < y2)
                        {
                            Set(x1, y, (char)Terminal.Acs.LTEE);
                        }
                        else
                        {
                            Set(x1, y, (char)Terminal.Acs.LLCORNER);
                        }
                    }
                }
            }
            else if (y1 == y2)
            {
                // horizontal
                for (int x = x1; x < x2; ++x)
                {
                    Set(x, y1, (char)Terminal.Acs.HLINE);
                }
                for (int x = x1; x <= x2; ++x)
                {
                    var north = GetAcs(Terminal.Acs.VLINE, x, y1 - 1);
                    var south = GetAcs(Terminal.Acs.VLINE, x, y1 + 1);
                    if (north && south)
                    {
                        if (x == x1)
                        {
                            Set(x, y1, (char)Terminal.Acs.LTEE);
                        }
                        else if (x < x2)
                        {
                            Set(x, y1, (char)Terminal.Acs.PLUS);
                        }
                        else
                        {
                            Set(x, y1, (char)Terminal.Acs.RTEE);
                        }
                    }
                    else if (north)
                    {
                        if (x == x1)
                        {
                            Set(x, y1, (char)Terminal.Acs.LLCORNER);
                        }
                        else if (x < x2)
                        {
                            Set(x, y1, (char)Terminal.Acs.BTEE);
                        }
                        else
                        {
                            Set(x, y1, (char)Terminal.Acs.LRCORNER);
                        }
                    }
                    else if (south)
                    {
                        if (x == x1)
                        {
                            Set(x, y1, (char)Terminal.Acs.ULCORNER);
                        }
                        else if (x < x2)
                        {
                            Set(x, y1, (char)Terminal.Acs.TTEE);
                        }
                        else
                        {
                            Set(x, y1, (char)Terminal.Acs.URCORNER);
                        }
                    }
                }
            }
            else
            {
                Runtime.ThrowError("DrawLine: must be horizontal or vertical");
            }
        }

        public KeyInfo ReadKey()
        {
            return ReadKey(false);
        }

        public KeyInfo ReadKey(bool echo)
        {
            if (Dirty)
            {
                Refresh();
            }

            while (true)
            {
                var info = Terminal.TerminalWindow.ReadKey();

                if (info is PlaybackInfo)
                {
                    var playback = (PlaybackInfo)info;
                    var interactive = playback.Time <= 0;

                    if (playback.Lines.Count != 0)
                    {
                        List<string> lines = playback.Lines;
                        if (interactive)
                        {
                            lines.Add("");
                            lines.Add("<bold><gray>Press any key to continue.</gray></bold>");
                        }
                        var w = lines.Max(xx => xx.Length) + 2;
                        var h = lines.Count;
                        int x;
                        int y;
                        if (CursorVisible)
                        {
                            x = ScreenLeft + Col + 2;
                            y = ScreenTop + Row + 1;
                        }
                        else
                        {
                            x = (Terminal.Width - w - 2) / 2;
                            y = Terminal.Height - h - 3;
                        }
                        if (x + w + 2 > Terminal.Width)
                        {
                            x = Terminal.Width - w - 2;
                        }
                        if (y + h + 2 > Terminal.Height)
                        {
                            y = Terminal.Height - h - 3;
                        }
                        if (x < 0)
                        {
                            x = 0;
                        }
                        if (y < 0)
                        {
                            y = 0;
                        }
                        var fg = "blue";
                        var bg = Terminal.DefaultBackColor;

                        using (var win = Window.CreateFrameWindow(x, y, w + 2, h + 2, -1, fg, bg))
                        {
                            win.HtmlEnabled = true;
                            foreach (var line in playback.Lines)
                            {
                                win.TextWriter.WriteLine(line);
                                //win.WriteLine(line);
                            }
                            win.Refresh();
                            RuntimeGfx.ProcessEvents();
                            if (interactive)
                            {  
                                Terminal.TerminalWindow.ReadTerminalKey();
                            }
                            else
                            {
                                Runtime.Sleep(playback.Time);
                            }
                        }
                    }
                    else
                    {
                        Runtime.Sleep(playback.Time);
                    }
                    continue;
                }

                if (info.KeyData == TerminalKeys.LButton || info.KeyData == TerminalKeys.RButton || info.KeyData == TerminalKeys.Wheel)
                {
                    var x = info.MouseCol - ScreenLeft;
                    var y = info.MouseRow - ScreenTop;
                    var c = info.MouseClicks;
                    var w = info.MouseWheel;
                    if (0 <= x && x < Width && 0 <= y && y < Height)
                    {
                        // Allow mouse click if screen coordinates are within the window bounds.
                        //MessageBox.Show(String.Format("x={0} y={1}", x, y));
                        info = new KeyInfo(info.KeyData, x, y, c, w);
                    }
                    else
                    {
                        continue;
                    }
                }

                object handler;
                if (ScrollHandlers.TryGetValue(info.KeyData, out handler))
                {
                    if (handler is ScrollHandler)
                    {
                        var handled = ((ScrollHandler)handler)();
                        if (handled)
                        {
                            continue;
                        }
                    }
                    else if (handler is MouseHandler)
                    {
                        var handled = ((MouseHandler)handler)(info.MouseCol, info.MouseRow, info.MouseWheel);
                        if (handled)
                        {
                            continue;
                        }
                    }
                }

                if (echo && info.KeyChar != 0)
                {
                    Write(info.KeyChar);
                    return info;
                }
                else
                {
                    return info;
                }
            }
        }

        public int SelectKey(params TerminalKeys[] keys)
        {
            while (true)
            {
                var info = ReadKey(false);
                for (int i = 0; i < keys.Length; ++i)
                {
                    if (keys[i] == info.KeyData)
                    {
                        return i;
                    }
                }
            }
        }

        public string Read(params object[] args)
        {
            object[] kwargs = Runtime.ParseKwargs(args, new string[] { "initial-value", "max-chars" }, "", -1);
            var initialText = (string)kwargs[0];
            var maxChars = (int)kwargs[1];
            return GetStringInput(initialText, maxChars);
        }

        public string ReadLine(params object[] args)
        {
            var s = Read(args);
            WriteLine();
            ShowCursor();
            RuntimeGfx.ProcessEvents();
            return s;
        }

        public char ReadChar()
        {
            ShowCursor();

            while (true)
            {
                var key = ReadKey(true);
                if (key.KeyChar != 0)
                {
                    HideCursor();
                    return key.KeyChar;
                }
            }
        }

        ///
        /// GetLine stuff
        ///

        public int MaxChars;
        public string Text;
        public bool Done;

        string GetStringInput(string initialText, int maxChars)
        {
            MaxChars = maxChars;
            HomePos = EndPos = Pos;
            Done = false;

            InsertString(initialText);
            Refresh();

            while (!Done)
            {
                ShowCursor();
                //Application.DoEvents();
                var info = ReadKey(false);
                HideCursor();

                Runtime.InitRandom();

                bool handled = false;

                if (info.KeyData != 0)
                {
                    object handler;
                    if (EditHandlers.TryGetValue(info.KeyData, out handler))
                    {
                        switch (info.KeyData)
                        {
                            case TerminalKeys.PageUp:
                            case TerminalKeys.PageDown:
                            case TerminalKeys.Up|TerminalKeys.Control:
                            case TerminalKeys.Down|TerminalKeys.Control:
                            case TerminalKeys.Home|TerminalKeys.Control:
                            case TerminalKeys.End|TerminalKeys.Control:
                            {
                                break;
                            }
                            default:
                            {
                                ScrollIntoView();
                                break;
                            }
                        }

                        if (handler is EditHandler)
                        {
                            ((EditHandler)handler)();
                            handled = true;
                        }
                        if (handler is ScrollHandler)
                        {
                            ((ScrollHandler)handler)();
                            handled = true;
                        }
                        else if (handler is MouseHandler)
                        {
                            handled = true;
                            ((MouseHandler)handler)(info.MouseCol, info.MouseRow, info.MouseWheel);
                        }
                    }
                }

                if (!handled && info.KeyChar >= ' ')
                {
                    ScrollIntoView();
                    CmdDataChar(info.KeyChar);
                }
            }

            Pos = EndPos;
 
            return Text;
        }

        void InsertSpace(int pos, int endPos)
        {
            char ch;
            ColorType fg;
            ColorType bg;
            int attr;
            for (var p = endPos - 1; p > pos; --p)
            {
                Get(p - 1, out ch, out fg, out bg, out attr);
                Set(p, ch, fg, bg, attr);
            }
            Set(pos, ' ', _ForeColor, _BackColor, Style);
        }

        void RemoveChar(int pos, int endPos)
        {
            char ch;
            ColorType fg;
            ColorType bg;
            int attr;
            for (var p = pos; p + 1 < endPos; ++p)
            {
                Get(p + 1, out ch, out fg, out bg, out attr);
                Set(p, ch, fg, bg, attr);
            }
            Set(endPos - 1, ' ', _ForeColor, _BackColor, Style);
        }

        public string GetStringFromBuffer(int beg, int end)
        {
            using (var s = new StringWriter())
            {
                for (var pos = beg; pos < end; ++pos)
                {
                    s.Write(Get(pos));
                }
                return s.ToString();
            }
        }

        public string GetWordFromBuffer(int beg, int pos, int end, Func<char,bool> wordCharTest)
        {
            var text = GetStringFromBuffer(beg, end);
            return Runtime.GetWordFromString(text, pos - beg, wordCharTest);
        }

        public string ScrapeWordAt(int x, int y)
        {
            var beg = y * Width;
            var end = beg + Width;
            var pos = beg + x;
            var text = GetWordFromBuffer(beg, pos, end, Runtime.IsWordChar);
            return text;
        }

        public string ScrapeLispWordAt(int x, int y)
        {
            var beg = y * Width;
            var end = beg + Width;
            var pos = beg + x;
            var text = GetWordFromBuffer(beg, pos, end, Runtime.IsLispWordChar);
            return text;
        }

        internal void CmdMoveBackOverSpaces()
        {
            if (Get(Pos) == ' ')
            {
                while (HomePos < Pos && Get(Pos - 1) == ' ')
                {
                    --Pos;
                }
            }
        }

        internal void CmdDataChar(char ch)
        {
            switch (ch)
            {
                case '\n':
                {
                    while (Col != 0 && MaxChars == -1)
                    {
                        CmdSimpleDataChar(' ');
                    }
                    break;
                }
                case '\t':
                {
                    CmdSimpleDataChar(' ');
                    break;
                }
                default:
                {
                    CmdSimpleDataChar(ch);
                    break;
                }
            }
        }

        void CmdSimpleDataChar(char ch)
        {
            // Inserts a character at Pos and increments EndPos.
            if (MaxChars != -1 && EndPos - HomePos >= MaxChars)
            {
                return;
            }

            if (EndPos == Size)
            {
                ScrollOne();
            }

            ++EndPos;

            InsertSpace(Pos, EndPos);
            Set(Pos, ch, _ForeColor, _BackColor, Style);
            RefreshPositions(Pos, EndPos);
            Next(true);
        }

        void CmdEnter()
        {
            CmdEnd();
            Text = GetStringFromBuffer(HomePos, EndPos);
            Done = true;
        }

        void CmdEscape()
        {
            while (HomePos != EndPos)
            {
                Pos = EndPos;
                CmdBackspace();
            }
        }

        void CmdHome()
        {
            Pos = HomePos;
        }

        public void CmdEnd()
        {
            Pos = EndPos;
        }

        void CmdLeft()
        {
            if (HomePos < Pos)
            {
                --Pos;
            }
        }

        void CmdRight()
        {
            if (Pos < EndPos)
            {
                ++Pos;
            }
        }

        internal void CmdBackspace()
        {
            if (HomePos < Pos)
            {
                --Pos;
                CmdDeleteChar();
            }
        }

        internal void CmdDeleteChar()
        {
            if (Pos < EndPos)
            {
                RemoveChar(Pos, EndPos);
                --EndPos;
                RefreshPositions(Pos, EndPos + 1);
            }
        }

        void CmdCopy()
        {
            var text = GetStringFromBuffer(HomePos, EndPos);
            Runtime.SetClipboardData(text.ToString());
        }

        void CmdPaste()
        {
            string str = Runtime.GetClipboardData();
            InsertString(str);
        }

        internal void InsertString(string str)
        {
            if (str != null)
            {
                foreach (var ch in str)
                {
                    CmdDataChar(ch);
                }
            }
        }

        bool CmdPageUp()
        {
            if (!ScrollEnabled)
            {
                return false;
            }
            if (ScrollUp(Height))
            {
                Refresh();
            }
            return true;
        }

        bool CmdScrollUp()
        {
            if (!ScrollEnabled)
            {
                return false;
            }
            if (ScrollUp(1))
            {
                Refresh();
            }
            return true;
        }

        bool ScrollUp(int count)
        {
            count = Math.Min(BufferTop, count);
            if (count > 0)
            {
                BufferTop -= count;
                Row += count;
                HomeRow += count;
                EndRow += count;
                CursorRow += count;
                SavedRow += count;
                return true;
            }
            else
            {
                return false;
            }
        }

        bool CmdPageDown()
        {
            if (!ScrollEnabled)
            {
                return false;
            }
            if (ScrollDown(Height))
            {
                Refresh();
            }
            return true;
        }

        bool CmdScrollDown()
        {
            if (!ScrollEnabled)
            {
                return false;
            }
            if (ScrollDown(1))
            {
                Refresh();
            }
            return true;
        }

        bool CmdWheelScroll(int x, int y, int w)
        {
            if (!ScrollEnabled)
            {
                return false;
            }
            if (w < 0)
            {
                if (ScrollDown(1))
                {
                    Refresh();
                }
            }
            else if (w > 0)
            {
                if (ScrollUp(1))
                {
                    Refresh();
                }
            }
            return true;
        }

        bool ScrollDown(int count)
        {
            count = Math.Min(Buffer.Height - BufferTop - Height, count);
            if (count > 0)
            {
                BufferTop += count;
                Row -= count;
                HomeRow -= count;
                EndRow -= count;
                CursorRow -= count;
                SavedRow -= count;
                return true;
            }
            else
            {
                return false;
            }
        }

        bool CmdBufferHome()
        {
            if (ScrollUp(int.MaxValue))
            {
                Refresh();
            }
            return true;
        }

        bool CmdBufferEnd()
        {
            if (ScrollDown(int.MaxValue))
            {
                Refresh();
            }
            return true;
        }

        void ScrollIntoView()
        {
            bool refresh = false;

            while (Row < 0)
            {
                if (ScrollUp(Height))
                {
                    refresh = true;
                }
            }
            while (Row >= Height)
            {
                if (ScrollDown(Height))
                {
                    refresh = true;
                }
            }
            if (refresh)
            {
                Refresh();
                RuntimeGfx.ProcessEvents();
            }
        }

        void CmdTabCompletion()
        {
            if (!TabCompletionEnabled)
            {
                return;
            }
            CmdMoveBackOverSpaces();
            var text = this.GetWordFromBuffer(HomePos, Pos, Pos, Runtime.IsLispWordChar);
            var textPos = Pos - text.Length;
            text = SelectCompletion(text);
            if (text != null)
            {
                while (Pos != textPos)
                {
                    CmdBackspace();
                }
                InsertString(text + " ");
            }
        }

        internal string SelectCompletion(string text)
        {
            var prefix = text;
            var completions = RuntimeConsoleBase.GetCompletions(prefix);
            var x = ScreenLeft + Col;
            var y = ScreenTop + Row;
            var w = 40;
            var h = 7;
            if (x + w > Terminal.Width)
            {
                x = Terminal.Width - w;
            }
            if (y + h > Terminal.Height)
            {
                y = Terminal.Height - h;
            }
            using (var win = Window.CreateFrameWindow(x, y, w, h, -1, null, null))
            {
                Func<KeyInfo,IEnumerable> keyHandler = (k) =>
                {
                    if (k.KeyData == TerminalKeys.Back)
                    {
                        if (prefix != "")
                        {
                            prefix = prefix.Substring(0, prefix.Length - 1);
                            completions = RuntimeGfx.GetCompletions(prefix);
                            return completions;
                        }
                    }
                    else if (' ' <= k.KeyChar)
                    {
                        prefix = prefix + k.KeyChar.ToString();
                        completions = RuntimeGfx.GetCompletions(prefix);
                        return completions;
                    }
                    return null;
                };
                var choice = win.RunMenu(completions, null, keyHandler);
                if (choice != -1)
                {
                    return completions[choice];
                }
            }

            return null;
        }


        internal void AddEditHandler(TerminalKeys key, IApply handler)
        {
            EditHandlers[key] = (object)handler;
        }

        internal void AddEditHandler(TerminalKeys key, EditHandler handler)
        {
            EditHandlers[key] = (object)handler;
        }

        internal void AddEditHandler(TerminalKeys key, MouseHandler handler)
        {
            EditHandlers[key] = (object)handler;
        }

        void InitEditHandlers()
        {
            AddEditHandler(TerminalKeys.Home, CmdHome);
            AddEditHandler(TerminalKeys.End, CmdEnd);
            AddEditHandler(TerminalKeys.Left, CmdLeft);
            AddEditHandler(TerminalKeys.Right, CmdRight);
            AddEditHandler(TerminalKeys.Enter, CmdEnter);
            AddEditHandler(TerminalKeys.Escape, CmdEscape);
            AddEditHandler(TerminalKeys.Back, CmdBackspace);
            AddEditHandler(TerminalKeys.Delete, CmdDeleteChar);
            AddEditHandler(TerminalKeys.Tab, CmdTabCompletion);
            AddEditHandler(TerminalKeys.C | TerminalKeys.Control, CmdCopy);
            AddEditHandler(TerminalKeys.V | TerminalKeys.Control, CmdPaste);

        }

        internal void AddScrollHandler(TerminalKeys key, ScrollHandler handler)
        {
            ScrollHandlers[key] = (object)handler;
        }

        internal void AddScrollHandler(TerminalKeys key, MouseHandler handler)
        {
            ScrollHandlers[key] = (object)handler;
        }

        void InitScrollHandlers()
        {
            AddScrollHandler(TerminalKeys.Wheel, CmdWheelScroll);
            AddScrollHandler(TerminalKeys.PageUp, CmdPageUp);
            AddScrollHandler(TerminalKeys.PageDown, CmdPageDown);
            AddScrollHandler(TerminalKeys.Up | TerminalKeys.Control, CmdScrollUp);
            AddScrollHandler(TerminalKeys.Down | TerminalKeys.Control, CmdScrollDown);
            AddScrollHandler(TerminalKeys.Home | TerminalKeys.Control, CmdBufferHome);
            AddScrollHandler(TerminalKeys.End | TerminalKeys.Control, CmdBufferEnd);
        }

        public void ScrollUp(int row, int height)
        {
            Buffer.CopyRect(Buffer, BufferLeft, BufferTop + row, Buffer, BufferLeft, BufferTop + row + 1, Width, height - 1);
            Buffer.ClearRect(BufferLeft, BufferTop + height - 1, Width, 1, _ForeColor, _BackColor);
        }

        public void ScrollDown(int row, int height)
        {
            Buffer.CopyRect(Buffer, BufferLeft, BufferTop + row + 1, Buffer, BufferLeft, BufferTop + row, Width, height - 1);
            Buffer.ClearRect(BufferLeft, BufferTop + row, Width, 1, _ForeColor, _BackColor);
        }

        public void RefreshPosition(int x, int y)
        {
            Buffer.CopyRect(Terminal.ScreenBuffer, ScreenLeft + x, ScreenTop + y, Buffer, BufferLeft + x, BufferTop + y, 1, 1);
            Terminal.TerminalWindow.DoUpdate(ScreenLeft + x, ScreenTop + y, 1, 1);
        }

        public void RefreshPositions(int beg, int end)
        {
            var y1 = beg / Width;
            var y2 = (end - 1) / Width;
            Buffer.CopyRect(Terminal.ScreenBuffer, ScreenLeft, ScreenTop + y1, Buffer, BufferLeft, BufferTop + y1, Width, y2 - y1 + 1);
            Terminal.TerminalWindow.DoUpdate(ScreenLeft, ScreenTop + y1, Width, y2 - y1 + 1);
        }

        internal void DoUpdate(int x, int y, int w, int h)
        {
            if (Visible)
            {
                Terminal.TerminalWindow.DoUpdate(ScreenLeft + x, ScreenTop + y, w, h);
            }
        }

        internal void Set(int col, int row, char ch, ColorType fg, ColorType bg, int fontIndex)
        {
            Buffer.Set(BufferLeft + col, BufferTop + row, ch, fg, bg, fontIndex);
        }

        internal char Get(int col, int row)
        {
            return Buffer.Get(BufferLeft + col, BufferTop + row);
        }

        internal void Set(int pos, char ch, ColorType fg, ColorType bg, int fontIndex)
        {
            var row = pos / Width;
            var col = pos % Width;
            Set(col, row, ch, fg, bg, fontIndex);
        }

        internal char Get(int pos)
        {
            var row = pos / Width;
            var col = pos % Width;
            return Get(col, row);
        }

        internal void Get(int pos, out char ch, out ColorType fg, out ColorType bg, out int fontIndex)
        {
            var row = pos / Width;
            var col = pos % Width;
            Get(col, row, out ch, out fg, out bg, out fontIndex);
        }

        internal void Get(int col, int row, out char ch, out ColorType fg, out ColorType bg, out int fontIndex)
        {
            Buffer.Get(BufferLeft + col, BufferTop + row, out ch, out fg, out bg, out fontIndex);
        }


        TextWriter IHasTextWriter.GetTextWriter()
        {
            return TextWriter;
        }

    }

}

