

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace Kiezel
{
    public delegate void KeyHandler();
    public delegate void MouseHandler(int x,int y);
}
namespace Kiezel
{
    public class Window: IDisposable
    {
        protected Dictionary<Keys,object> KeyHandlers = new Dictionary<Keys, object>();
        protected Dictionary<Keys,object> EditHandlers = new Dictionary<Keys, object>();

        // location of window on the screen
        public int ScreenLeft { get; internal set; }
        public int ScreenTop { get; internal set; }

        // location of window on the buffer
        public Buffer Buffer { get; internal set; }
        public int BufferLeft { get; internal set; }
        public int BufferTop { get; internal set; }

        // size of the window
        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public int Size { get; internal set; }

        // current insert position, colors and attributes
        public int Row { get; internal set; }
        public int Col { get; internal set; }
        public ColorType ForeColor{ get; internal set; }
        public ColorType BackColor{ get; internal set; }
        public int Attr { get; internal set; }

        public int Pos
        { 
            get
            { 
                return Row * Width + Col; 
            } 
            internal set
            {
                Row = value / Width;
                Col = value % Width;
            }
        }

        // window state
        public bool IsScrollOk { get; internal set; }
        public bool IsScrollPadOk  { get; internal set; }
        public bool Visible { get; internal set; }
        public bool Dirty { get; internal set; }

        internal Window BoxWindow { get; set; }
        internal WindowTextWriter TextWriter { get; set; }

        ColorType fgUnderCursor;
        ColorType bgUnderCursor;
        int attrUnderCursor;
        int CursorRow;
        int CursorCol;
        bool CursorVisible = false;
        int HomeRow;
        int HomeCol;
        int EndRow;
        int EndCol;

        public int HomePos
        { 
            get
            { 
                return HomeRow * Width + HomeCol; 
            } 
            internal set
            {
                HomeRow = value / Width;
                HomeCol = value % Width;
            }
        }

        public int EndPos
        { 
            get
            { 
                return EndRow * Width + EndCol; 
            } 
            internal set
            {
                EndRow = value / Width;
                EndCol = value % Width;
            }
        }

        internal Window(int screenLeft, int screenTop, Buffer pad, int left, int top, int width,int height)
        {
            ScreenLeft = screenLeft;
            ScreenTop = screenTop;
            Buffer = pad;
            SetViewPort(left, top, width, height);
            ForeColor = Terminal.DefaultForeColor;
            BackColor = Terminal.DefaultBackColor;
            Visible = true;
            IsScrollOk = false;
            TextWriter = new WindowTextWriter(this);
            BoxWindow = null;
            ScrollPadOk(false);
            InitKeyHandlers();
            InitEditHandlers();
            Dirty = true;
            Attr = 0;
        }

        internal void SetViewPort(int left,int top,int width,int height)
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

        internal bool SetWindowPos(int x, int y)
        {
            if (x != ScreenLeft || y != ScreenTop)
            {
                ScreenLeft = x;
                ScreenTop = y;
                return true;
            }
            return false;
        }

        internal bool SetBufferPos(int x, int y)
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

        void IDisposable.Dispose()
        {
            Terminal.DeleteWindow(this);
        }

        internal int AttrSet(params object[] values)
        {
            var b = Attr;
            var a = TerminalAttribute.MakeTerminalAttribute(values);
            Attr = a;
            return b;
        }

        internal int AttrOn(params object[] values)
        {
            var b = Attr;
            var a = TerminalAttribute.MakeTerminalAttribute(values);
            Attr |= a;
            return b;
        }

        internal int AttrOff(params object[] values)
        {
            var b = Attr;
            var a = TerminalAttribute.MakeTerminalAttribute(values);
            Attr &= ~a;
            return b;
        }

        internal void ScrollOk(bool flag)
        {
            IsScrollOk = flag;
        }

        internal void ScrollPadOk(bool flag)
        {
            if (flag)
            {
                IsScrollPadOk = true;
                Terminal.TerminalWindow.DoSetScrollPos(BufferTop, Height, Buffer.Height);
            }
            else
            {
                IsScrollPadOk = false;
                Terminal.TerminalWindow.DoSetScrollPos(-1, -1, -1);
            }
        }

        internal bool Standout()
        {
            return (Attr & TerminalAttribute.Reverse) != 0;
        }

        internal bool Standout(bool flag)
        {
            var oldflag = Standout();
            Attr |= TerminalAttribute.Reverse;
            return oldflag;
        }

        internal void ScrollOne()
        {
            if (IsScrollPadOk && BufferTop + Height < Buffer.Height)
            {
                ++BufferTop;
                Terminal.TerminalWindow.DoSetScrollPos(BufferTop, Height, Buffer.Height);
                Refresh();
            }
            else
            {
                Scroll(0,Height,1);
            }

            --Row;
            --HomeRow;
            --EndRow;
            --CursorRow;
        }

        internal void Next(bool scrollok=false, bool refresh=false)
        {
            if (scrollok || IsScrollOk)
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
                Application.DoEvents();
            }
        }

        internal void Clear()
        {
            Dirty = true;
            Buffer.ClearRect(BufferLeft, BufferTop, Width, Height,ForeColor,BackColor);
            Move(0, 0);
        }

        internal void ClearToEol()
        {
            Dirty = true;
            for (var col=Col; col < Width; ++col)
            {
                Set(col, Row, ' ', ForeColor, BackColor, 0);
            }
        }

        internal void ClearToBot()
        {
            Dirty = true;
            for (var pos=Pos; pos < Size; ++pos)
            {
                Set(pos, ' ', ForeColor, BackColor, 0);
            }
        }

        internal void DrawBox(string caption)
        {
            Dirty = true;
  
            var w = Width - 1;
            var h = Height - 1;

            DrawLine(0, 0, 0, h);
            DrawLine(0, 0, w, 0);
            DrawLine(0, h, w, h);
            DrawLine(w, 0, w, h);

            if (caption != null)
            {
//                Set(0, 0, Acs.RHALFBLOCK);
//                for (int x=1; x<w; ++x)
//                {
//                    Set(x, 0, Acs.FULLBLOCK);
//                }
//                Set(w, 0, Acs.LHALFBLOCK);
                if (!String.IsNullOrEmpty(caption))
                {
                    var s = " " + caption.Trim() + " ";
                    var offset = (Width - s.Length) / 2;
                    Move(offset, 0);
                    Standout(true);
                    Put(s);
                    Standout(false);
                }
            }
        }

        void VerifySizes(bool boxed, int totalSize, int[] sizes)
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

        internal void Scroll(int top,int height,int count)
        {
            if (top < 0)
            {
                return;
            }

            if (height < 0 || top+height > Height)
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
                for (int i = 0; i<count; ++i)
                {
                    ScrollUp(top, height );
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
                for (int i = 0; i<count; ++i)
                {
                    ScrollDown(top, height);
                }
                Refresh();
            }
        }

        internal void Put(char ch)
        {
            Put(ch, false);
        }

        internal void PutLine(char ch)
        {
            Put(ch, false);
            PutLine();
        }

        internal void Put(char ch, bool refresh)
        {
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
                        Put(' ', refresh);
                    }   while ((Col % 8)!=0);
                    break;
                }
                
                case '\n':
                {
                    do
                    {
                        Put(' ', refresh);
                    }
                    while (Col!=0);
                    break;
                }
                
                default:
                {
                    Set(Pos, ch, ForeColor, BackColor, Attr);
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

        internal void PutLine(string str)
        {
            Put(str);
            PutLine();
        }

        internal void PutLine()
        {
            Put('\n');
        }

        internal void Put(string str)
        {
            if (str != null)
            {
                foreach (var ch in str)
                {
                    Put(ch);
                }
            }
        }

        internal void Put(string str, bool refresh)
        {
            foreach (var ch in str)
            {
                Put(ch, refresh);
            }
        }

        internal void Move(int col, int row)
        {
            Row = row;
            Col = col;
        }

        void DrawMenu(List<string> items, int offset, int cursor)
        {
            for (int r = 0, i=offset; r < this.Height; ++r, ++i)
            {
                if (i < items.Count)
                {
                    var s = items[i];
                    Move(0, r);
                    if (r == cursor)
                    {
                        var fg = ForeColor;
                        var bg = BackColor;
                        BackColor = new ColorType("cornflower-blue");
                        ForeColor = new ColorType("white");
                        Put(s);
                        ForeColor = fg;
                        BackColor = bg;
                    }
                    else
                    {
                        Put(s);
                    }
                }
                else
                {
                    Move(0, r);
                    ClearToEol();
                }

            }
        }

        internal int RunMenu(IEnumerable items, IApply handler)
        {
            var v = new List<string>();
            foreach (var item in items)
            {
                var s = item.ToString();
                if (s.Length > Width)
                {
                    s = s.Substring(0, Width);
                }
                v.Add(s.PadRight(Width));
            }
            var offset = 0;
            var cursor = 0;
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
            Clear();
            while (true)
            {
                DrawMenu(v, offset, cursor);
                var k = GetKey(false);
                switch (k.KeyData)
                {
                    case Keys.LButton:
                    {
                        break;
                    }
                    case Keys.Enter:
                    {
                        var choice = offset + cursor;
                        if (handler == null)
                        {
                            return choice;
                        }
                        else
                        {
                            DrawMenu(v, offset, -1);
                            Refresh();
                            if (!Runtime.FuncallBool(handler, choice))
                            {
                                return -1;
                            }
                        }
                        break;
                    }
                    case Keys.Escape:
                    case Keys.Back:
                    {
                        return -1;
                    }
                    case Keys.Home:
                    {
                        cursor = offset = 0;
                        break;
                    }
                    case Keys.PageUp:
                    {
                        cursor = 0;
                        for (var i=0; i< Height; ++i)
                        {
                            upArrow();
                        }
                        break;
                    }
                    case Keys.Up:
                    {
                        upArrow();
                        break;
                    }
                    case Keys.Down:
                    case Keys.Tab:
                    {
                        downArrow();
                        break;
                    }
                    case Keys.PageDown:
                    {
                        cursor = Height - 1;
                        for (var i=0; i< Height; ++i)
                        {
                            downArrow();
                        }
                        break;
                    }
                    case Keys.End:
                    {
                        while (downArrow())
                        {
                        }
                        break;
                    }
                }
            }
        }

        void ShowCursor()
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

        void HideCursor()
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

        internal void OutRefresh()
        {
            if (BoxWindow != null)
            {
                BoxWindow.OutRefresh();
            }
            Buffer.CopyRect(Terminal.ScreenBuffer, ScreenLeft, ScreenTop, Buffer, BufferLeft, BufferTop, Width, Height);
        }

        internal void Refresh()
        {
            OutRefresh();
            DoUpdate();
            Dirty = false;
        }

        void Set(int x, int y, char ch)
        {
            // Only used for graphics! Use normal font and allow reversed colors.
            Set(x, y, ch, ForeColor, BackColor, Attr & ~TerminalAttribute.FontMask);
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

        internal void DrawLine(int x1, int y1, int x2, int y2)
        {
            if (x1 == x2)
            {
                // vertical
                for (int y=y1; y<=y2; ++y)
                {
                    Set(x1, y, (char)Acs.VLINE);
                }
                for (int y=y1; y<=y2; ++y)
                {
                    var east = GetAcs(Acs.HLINE, x1 + 1, y);
                    var west = GetAcs(Acs.HLINE, x1 - 1, y);
                    if (west && east)
                    {
                        if (y == y1)
                        {
                            Set(x1, y, (char)Acs.TTEE);
                        }
                        else if (y < y2)
                        {
                            Set(x1, y, (char)Acs.PLUS);
                        }
                        else
                        {
                            Set(x1, y, (char)Acs.BTEE);
                        }
                    }
                    else if (west)
                    {
                        if (y == y1)
                        {
                            Set(x1, y, (char)Acs.URCORNER);
                        }
                        else if (y < y2)
                        {
                            Set(x1, y, (char)Acs.RTEE);
                        }
                        else
                        {
                            Set(x1, y, (char)Acs.LRCORNER);
                        }
                    }
                    else if (east)
                    {
                        if (y == y1)
                        {
                            Set(x1, y, (char)Acs.ULCORNER);
                        }
                        else if (y < y2)
                        {
                            Set(x1, y, (char)Acs.LTEE);
                        }
                        else
                        {
                            Set(x1, y, (char)Acs.LLCORNER);
                        }
                    }
                }
            }
            else if (y1 == y2)
            {
                // horizontal
                for (int x=x1; x<x2; ++x)
                {
                    Set(x, y1, (char)Acs.HLINE);
                }
                for (int x=x1; x<=x2; ++x)
                {
                    var north = GetAcs(Acs.VLINE, x, y1 - 1);
                    var south = GetAcs(Acs.VLINE, x, y1 + 1);
                    if (north && south)
                    {
                        if (x == x1)
                        {
                            Set(x, y1, (char)Acs.LTEE);
                        }
                        else if (x < x2)
                        {
                            Set(x, y1, (char)Acs.PLUS);
                        }
                        else
                        {
                            Set(x, y1, (char)Acs.RTEE);
                        }
                    }
                    else if (north)
                    {
                        if (x == x1)
                        {
                            Set(x, y1, (char)Acs.LLCORNER);
                        }
                        else if (x < x2)
                        {
                            Set(x, y1, (char)Acs.BTEE);
                        }
                        else
                        {
                            Set(x, y1, (char)Acs.LRCORNER);
                        }
                    }
                    else if (south)
                    {
                        if (x == x1)
                        {
                            Set(x, y1, (char)Acs.ULCORNER);
                        }
                        else if (x < x2)
                        {
                            Set(x, y1, (char)Acs.TTEE);
                        }
                        else
                        {
                            Set(x, y1, (char)Acs.URCORNER);
                        }
                    }
                }
            }
            else
            {
                Runtime.ThrowError("DrawLine: must be horizontal or vertical");
            }
        }

        internal KeyInfo GetKey(bool echo)
        {
            if (Dirty)
            {
                Refresh();
            }

            while (true)
            {
                var info = Terminal.TerminalWindow.ReadKey();
                object handler;
                if (KeyHandlers.TryGetValue(info.KeyData, out handler))
                {
                    ((KeyHandler)handler)();
                    continue;
                }

                if (info.KeyData == Keys.LButton || info.KeyData == Keys.RButton)
                {
                    var x = info.MouseCol - ScreenLeft;
                    var y = info.MouseRow - ScreenTop;
                    if (0 <= x && x < Width && 0 <= y && y < Height)
                    {
                        // Allow mouse click if screen coordinates are within the window bounds.
                        //MessageBox.Show(String.Format("x={0} y={1}", x, y));
                        return new KeyInfo(info.KeyData, x, y);
                    }
                }
                else if (echo && info.KeyChar != 0)
                {
                    Put(info.KeyChar);
                    return info;
                }
                else
                {
                    return info;
                }
            }
        }

        internal int SelectKey(params Keys[] keys)
        {
            while (true)
            {
                var info = GetKey(false);
                for (int i = 0; i < keys.Length; ++i)
                {
                    if (keys[i] == info.KeyData)
                    {
                        return i;
                    }
                }
            }
        }

        internal string Get(string initialText, int maxChars)
        {
            return GetStringInput(initialText, maxChars);
        }

        internal string GetLine(string initialText, int maxChars)
        {
            var s = GetStringInput(initialText, maxChars);
            PutLine();
            return s;
        }

        internal char GetChar()
        {
            ShowCursor();

            while (true)
            {
                var key = GetKey(true);
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

        internal int MaxChars;
        internal string Text;
        internal bool Done;

        internal string GetStringInput(string initialText, int maxChars)
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
                var info = GetKey(false);
                HideCursor();

                Runtime.InitRandom();

                bool handled = false;

                if (info.KeyData != Keys.None)
                {
                    object handler;
                    if (EditHandlers.TryGetValue(info.KeyData, out handler))
                    {
                        switch (info.KeyData)
                        {
                            case Keys.PageUp:
                            case Keys.PageDown:
                            case Keys.Up|Keys.Control:
                            case Keys.Down|Keys.Control:
                            case Keys.Home|Keys.Control:
                            case Keys.End|Keys.Control:
                            {
                                break;
                            }
                            default:
                            {
                                ScrollIntoView();
                                break;
                            }
                        }

                        if (handler is KeyHandler)
                        {
                            ((KeyHandler)handler)();
                            handled = true;
                        }
                        else if (handler is MouseHandler)
                        {
                            handled = true;
                            ((MouseHandler)handler)(info.MouseCol, info.MouseRow);
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
            for (var p = endPos-1; p > pos; --p)
            {
                Get(p - 1, out ch, out fg, out bg, out attr);
                Set(p, ch, fg, bg, attr);
            }
            Set(pos, ' ', ForeColor, BackColor, Attr);
        }

        void RemoveChar(int pos, int endPos)
        {
            char ch;
            ColorType fg;
            ColorType bg;
            int attr;
            for (var p = pos; p+1 < endPos; ++p)
            {
                Get(p + 1, out ch, out fg, out bg, out attr);
                Set(p, ch, fg, bg, attr);
            }
            Set(endPos - 1, ' ', ForeColor, BackColor, Attr);
        }

        protected string GetStringFromBuffer(int beg, int end)
        {
            using (var s = new StringWriter())
            {
                for (var pos=beg; pos<end; ++pos)
                {
                    s.Write(Get(pos));
                }
                return s.ToString();
            }
        }

        protected void CmdDataChar(char ch)
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

        protected void CmdSimpleDataChar(char ch)
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
            Set(Pos, ch, ForeColor, BackColor, Attr);
            RefreshPositions(Pos, EndPos);
            Next(true);
        }

        protected void CmdEnter()
        {
            CmdEnd();
            Text = GetStringFromBuffer(HomePos, EndPos);
            Done = true;
        }

        protected void CmdEscape()
        {
            Done = true;
            CmdEnd();
            Text = null;
        }

        void CmdHome()
        {
            Pos = HomePos;
        }

        protected void CmdEnd()
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

        void CmdBackspace()
        {
            if (HomePos < Pos)
            {
                --Pos;
                CmdDeleteChar();
            }
        }

        protected void CmdDeleteChar()
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

        protected void InsertString(string str)
        {
            if (str != null)
            {
                foreach (var ch in str)
                {
                    CmdDataChar(ch);
                }
            }
        }

        protected void CmdCut()
        {
            CmdCopy();
            while (HomePos != EndPos)
            {
                Pos = EndPos;
                CmdBackspace();
            }
        }

        void CmdPageUp()
        {
            if (ScrollUp(Height))
            {
                Refresh();
            }
        }

        void CmdScrollUp()
        {
            if (ScrollUp(1))
            {
                Refresh();
            }
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
                return true;
            }
            else
            {
                return false;
            }
        }

        void CmdPageDown()
        {
            if (ScrollDown(Height))
            {
                Refresh();
            }
        }

        void CmdScrollDown()
        {
            if (ScrollDown(1))
            {
                Refresh();
            }
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
                return true;
            }
            else
            {
                return false;
            }
        }

        void CmdBufferHome()
        {
            if (ScrollUp(int.MaxValue))
            {
                Refresh();
            }
        }

        void CmdBufferEnd()
        {
            if (ScrollDown(int.MaxValue))
            {
                Refresh();
            }
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
                Application.DoEvents();
            }
        }

        protected void AddEditHandler(Keys key, IApply handler)
        {
            EditHandlers[key] = (object)handler;
        }

        protected void AddEditHandler(Keys key, KeyHandler handler)
        {
            EditHandlers[key] = (object)handler;
        }

        protected void AddEditHandler(Keys key, MouseHandler handler)
        {
            EditHandlers[key] = (object)handler;
        }

        void InitEditHandlers()
        {
            AddEditHandler(Keys.Home, CmdHome);
            AddEditHandler(Keys.End, CmdEnd);
            AddEditHandler(Keys.Left, CmdLeft);
            AddEditHandler(Keys.Right, CmdRight);
            AddEditHandler(Keys.Enter, CmdEnter);
            AddEditHandler(Keys.Escape, CmdEscape);
            AddEditHandler(Keys.Back, CmdBackspace);
            AddEditHandler(Keys.Delete, CmdDeleteChar);
            AddEditHandler(Keys.PageUp, CmdPageUp);
            AddEditHandler(Keys.PageDown, CmdPageDown);
            AddEditHandler(Keys.Up | Keys.Control, CmdScrollUp);
            AddEditHandler(Keys.Down | Keys.Control, CmdScrollDown);
            AddEditHandler(Keys.Home | Keys.Control, CmdBufferHome);
            AddEditHandler(Keys.End | Keys.Control, CmdBufferEnd);

        }

        protected void AddKeyHandler(Keys key, KeyHandler handler)
        {
            KeyHandlers[key] = (object)handler;
        }

        void InitKeyHandlers()
        {
            AddKeyHandler(Keys.PageUp, CmdPageUp);
            AddKeyHandler(Keys.PageDown, CmdPageDown);
            AddKeyHandler(Keys.Up | Keys.Control, CmdScrollUp);
            AddKeyHandler(Keys.Down | Keys.Control, CmdScrollDown);
            AddKeyHandler(Keys.Home | Keys.Control, CmdBufferHome);
            AddKeyHandler(Keys.End | Keys.Control, CmdBufferEnd);
        }

        internal void ScrollUp(int row,int height)
        {
            Buffer.CopyRect(Buffer, BufferLeft, BufferTop+row, Buffer, BufferLeft, BufferTop + row + 1, Width, height - 1);
            Buffer.ClearRect(BufferLeft, BufferTop + height - 1, Width, 1, ForeColor, BackColor);
        }

        internal void ScrollDown(int row,int height)
        {
            Buffer.CopyRect(Buffer, BufferLeft, BufferTop + row + 1, Buffer, BufferLeft, BufferTop + row, Width, height - 1);
            Buffer.ClearRect(BufferLeft, BufferTop + row, Width, 1, ForeColor, BackColor);
        }

        internal void RefreshPosition(int x,int y)
        {
            Buffer.CopyRect(Terminal.ScreenBuffer, ScreenLeft+x, ScreenTop+y, Buffer, BufferLeft+x, BufferTop+y, 1, 1);
            Terminal.TerminalWindow.DoUpdate(ScreenLeft + x, ScreenTop + y,1,1);
        }

        internal void RefreshPositions(int beg,int end)
        {
            var y1 = beg / Width;
            var y2 = (end - 1) / Width;
            Buffer.CopyRect(Terminal.ScreenBuffer, ScreenLeft, ScreenTop + y1, Buffer, BufferLeft, BufferTop + y1, Width, y2 - y1 + 1);
            Terminal.TerminalWindow.DoUpdate(ScreenLeft, ScreenTop + y1, Width, y2 - y1 + 1);
        }

        internal void DoUpdate(int x,int y,int w,int h)
        {
            if (Visible)
            {
                Terminal.TerminalWindow.DoUpdate(ScreenLeft + x, ScreenTop + y, w, h);
            }
        }

        internal void Set(int col, int row, char ch, ColorType fg, ColorType bg, int attr)
        {
            Buffer.Set(BufferLeft + col, BufferTop + row, ch, fg, bg, attr);
        }

        internal char Get(int col, int row)
        {
            return Buffer.Get(BufferLeft + col, BufferTop + row);
        }

        internal void Set(int pos, char ch, ColorType fg, ColorType bg, int attr)
        {
            var row = pos / Width;
            var col = pos % Width;
            Set(col, row, ch, fg, bg, attr);
        }

        internal char Get(int pos)
        {
            var row = pos / Width;
            var col = pos % Width;
            return Get(col, row);
        }

        internal void Get(int pos, out char ch, out ColorType fg, out ColorType bg,out int attr)
        {
            var row = pos / Width;
            var col = pos % Width;
            Get(col, row, out ch, out fg, out bg,out attr);
        }

        internal void Get(int col, int row, out char ch, out ColorType fg, out ColorType bg, out int attr)
        {
            Buffer.Get(BufferLeft + col, BufferTop + row, out ch, out fg, out bg, out attr);
        }


    }

}

