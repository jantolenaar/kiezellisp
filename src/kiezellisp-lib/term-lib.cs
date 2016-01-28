
// Copyright (C) Jan Tolenaar. See the file LICENSE for details.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Windows.Forms;

namespace Kiezel
{
    public class Acs // WGL4
    {
        public const char ULCORNER = (char)0x250c;
        public const char URCORNER = (char)0x2510;
        public const char LLCORNER = (char)0x2514;
        public const char LRCORNER = (char)0x2518;
        public const char HLINE = (char)0x2500;
        public const char VLINE = (char)0x2502;
        public const char LTEE = (char)0x251c;
        public const char RTEE = (char)0x2524;
        public const char TTEE = (char)0x252c;
        public const char BTEE = (char)0x2534;
        public const char PLUS = (char)0x253c;
        public const char LHALFBLOCK = (char)0x258c;
        public const char RHALFBLOCK = (char)0x2590;
        public const char FULLBLOCK = (char)0x2588;
    }

    public class TerminalAttribute
    {
        public const int Normal = 0;
        public const int Bold = 1;
        public const int Italic = 2;
        public const int Underline = 4;
        public const int StrikeOut = 8;
        public const int FontMask = 0xFF;
        public const int Reverse = 0x100;

        internal static int MakeTerminalAttribute(object[] values)
        {
            var result = 0;
            foreach (object value in values)
            {
                result |= MakeTerminalAttribute(value);
            }
            return result;
        }

        internal static int MakeTerminalAttribute(object value)
        {
            if (value is Int32)
            {
                var n = (int)value;
                return n;
            }
            else
            {
                switch (Runtime.GetDesignatedString(value))
                {
                    case "bold":
                    {
                        return TerminalAttribute.Bold;
                    }
                    case "italic":
                    {
                        return TerminalAttribute.Italic;
                    }
                    case "underline":
                    {
                        return TerminalAttribute.Underline;
                    }
                    case "strikeout":
                    case "strike-out":
                    {
                        return TerminalAttribute.StrikeOut;
                    }
                    case "reverse":
                    {
                        return TerminalAttribute.Reverse;
                    }
                    default:
                    {
                        return TerminalAttribute.Normal;
                    }
                }
            }
        }
    }

    public class Terminal
    {
        private Terminal()
        {
            // Prevents 'new' symbol in package
        }

        internal static Buffer ScreenBuffer
        {
            get
            {
                return TerminalWindow.ScreenBuffer;
            }
        }

        internal static void Init(Runtime.CommandLineOptions options, TerminalMainProgram mainProgram)
        {
            Width = options.Width;
            Height = options.Height;
            DefaultForeColor = new ColorType(options.ForeColor);
            DefaultBackColor = new ColorType(options.BackColor);
            History = new TerminalHistory();
            TerminalWindow = new TerminalMainForm(options.FontName, options.FontSize, Width, Height, mainProgram);
            WindowList = new List<Window>();
            StdScr = ReplWindow.CreateReplWindow(Width, Height, options.BufferHeight);
            Register(StdScr);
            RefreshAll();
            TerminalWindow.Show();
            TerminalWindow.Activate();
            Application.Run(TerminalWindow);
            //Runtime.Quit();
        }

        internal static void Endwin()
        {
            // not needed
        }

        public static void SetTerminalFont(string name,int size)
        {
            TerminalWindow.InitFont(name, size);
            TerminalWindow.InitClientRectangle();
            RefreshAll();
        }

        public static void SetTerminalSize(int width,int height,int bufferHeight)
        {
            if (WindowList.Count > 1)
            {
                Runtime.ThrowError("Cannot resize terminal when windows are open");
            }

            Width = width;
            Height = height;
            TerminalWindow.InitBuffer(width, height);
            TerminalWindow.InitClientRectangle();
            StdScr.Resize(width, height, bufferHeight);
            RefreshAll();
        }

        internal static TerminalMainForm TerminalWindow;
        internal static List<Window> WindowList;

        public static int Width { get; internal set; }

        public static int Height { get; internal set; }

        public static ColorType DefaultForeColor{ get ; internal set; }

        public static ColorType DefaultBackColor { get; internal set; }

        internal static TerminalHistory History;
        internal static ReplWindow StdScr;

        internal static Window GetStdScr()
        {
            if (Symbols.StdScr != null && Symbols.StdScr.Usage != SymbolUsage.None)
            {
                return (Window)Runtime.GetDynamic(Symbols.StdScr);
            }
            else
            {
                return StdScr;
            }
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
                h = height - h;
            }
            else
            {
                Runtime.Assert(0 <= y && y < height, "Invalid y");
                Runtime.Assert(0 < h && h <= height, "Invalid h");
                Runtime.Assert(y + h <= height, "Invalid y+h");
            }
        }

        public static Window MakeWindow(int x, int y, int w, int h)
        {
            return MakeWindow(x, y, w, h, h);
        }

        public static Window MakeWindow(int x, int y, int w, int h, int maxh)
        {
            CheckBounds(ref x, ref y, ref w, ref h, Terminal.Width, Terminal.Height);
            maxh = Math.Max(h, maxh);
            var pad = new Buffer(w, maxh);
            var win = new Window(x, y, pad, 0, 0, w, h);
            Register(win);
            return win;
        }

        public static Window MakeWindow(Window orig, int x, int y, int w, int h)
        {
            CheckBounds(ref x, ref y, ref w, ref h, orig.Width, orig.Height);
            var win = new Window(orig.ScreenLeft + x, orig.ScreenTop + y, orig.Buffer, x, y, w, h);
            Register(win);
            return win;
        }

        public static Window MakeBoxWindow(int x, int y, int w, int h)
        {
            return MakeBoxWindow(x, y, w, h, h, null);
        }

        public static Window MakeBoxWindow(int x, int y, int w, int h, int maxh)
        {
            return MakeBoxWindow(x, y, w, h, maxh, null);
        }

        public static Window MakeBoxWindow(int x, int y, int w, int h, string caption)
        {
            return MakeBoxWindow(x, y, w, h, h, caption);
        }

        public static Window MakeBoxWindow(int x, int y, int w, int h, int maxh, string caption)
        {
            CheckBounds(ref x, ref y, ref w, ref h, Terminal.Width, Terminal.Height);
            maxh = Math.Max(h, maxh);
            // box does not need maxh buffer
            var box = MakeWindow(x, y, w, h);
            //WindowList.Remove(box);
            box.DrawBox(caption);
            if (h == maxh)
            {
                // Reuse buffer
                var win = MakeWindow(box, 1, 1, w - 2, h - 2);
                win.BoxWindow = box;
                Unregister(box);
                return win;
            }
            else
            {
                // New buffer
                var win = MakeWindow(1, 1, w - 2, h - 2, maxh - 2);
                win.BoxWindow = box;
                Unregister(box);
                return win;
            }
        }


        public static void DeleteWindow(Window win)
        {
            // remove from stack and repaint the other windows according to z-order.
            if (win == StdScr)
            {
                return;
            }

            Unregister(win);
            RefreshAll();
        }

        internal static void Register(Window win)
        {
            WindowList.Add(win);
        }

        internal static void Unregister(Window win)
        {
            WindowList.Remove(win);
        }


        public static void SetScreenPos(int x,int y)
        {
            if (GetStdScr().SetWindowPos(x, y))
            {
                RefreshAll();
            }
        }

        public static void SetScreenPos(Window win,int x,int y)
        {
            if (win.SetWindowPos(x, y))
            {
                RefreshAll();
            }
        }

        public static void SetBufferPos(int x,int y)
        {
            GetStdScr().SetBufferPos(x, y);
        }

        public static void SetBufferPos(Window win,int x,int y)
        {
            win.SetBufferPos(x, y);
        }

        public static void BringToTop(Window win)
        {
            Unregister(win);
            Register(win);
            RefreshAll();
        }

        public static void RefreshAll()
        {
            foreach (var win in WindowList)
            {
                if (win.Visible)
                {
                    OutRefresh(win);
                }
            }
            Doupdate();
        }

        public static void Show(Window win)
        {
            if (!win.Visible)
            {
                win.Visible = true;
                RefreshAll();
            }
        }

        public static void Hide(Window win)
        {
            if (win == StdScr)
            {
                return;
            }

            if (win.Visible)
            {
                win.Visible = false;
                RefreshAll();
            }
        }

        public static void Clear()
        {
            GetStdScr().Clear();
        }

        public static void Clear(Window win)
        {
            win.Clear();
        }

        public static void ClearToEol()
        {
            GetStdScr().ClearToEol();
        }

        public static void ClearToEol(Window win)
        {
            win.ClearToEol();
        }

        public static void ClearToBot()
        {
            GetStdScr().ClearToBot();
        }

        public static void ClearToBot(Window win)
        {
            win.ClearToBot();
        }

        public static void OutRefresh()
        {
            GetStdScr().OutRefresh();
        }

        public static void OutRefresh(Window win)
        {
            win.OutRefresh();
        }

        public static void Refresh()
        {
            GetStdScr().Refresh();
        }

        public static void Refresh(Window win)
        {
            win.Refresh();
        }

        public static void ScrollOk(bool flag)
        {
            GetStdScr().ScrollOk(flag);
        }

        public static void ScrollOk(Window win, bool flag)
        {
            win.ScrollOk(flag);
        }

        public static void Scroll(int count)
        {
            Scroll(GetStdScr(),0,-1,count);
        }

        public static void Scroll(Window win, int count)
        {
            Scroll(win,0,-1,count);
        }

        public static void Scroll(int top,int height,int count)
        {
            Scroll( GetStdScr(), top, height, count);
        }

        public static void Scroll(Window win, int top,int height,int count)
        {
            win.Scroll( top, height, count);
        }

        public static int RunMenu(IEnumerable items)
        {
            return RunMenu(GetStdScr(), items);
        }

        public static int RunMenu(IEnumerable items, IApply handler)
        {
            return RunMenu(GetStdScr(), items, handler);
        }

        public static int RunMenu(Window win, IEnumerable items)
        {
            return win.RunMenu(items, null);
        }

        public static int RunMenu(Window win, IEnumerable items, IApply handler)
        {
            return win.RunMenu(items, handler);
        }

        public static object ForeColor(object color)
        {
            return ForeColor(GetStdScr(), color);
        }

        public static object ForeColor(Window win, object color)
        {
            if (color == null)
            {
                return null;
            }
            var fg = win.ForeColor;
            win.ForeColor = new ColorType(color);
            return fg;
        }

        public static object BackColor(object color)
        {
            return BackColor(GetStdScr(), color);
        }

        public static object BackColor(Window win, object color)
        {
            if (color == null)
            {
                return null;
            }
            var bg = win.BackColor;
            win.BackColor = new ColorType(color);
            return bg;
        }

        public static bool Standout()
        {
            return GetStdScr().Standout();
        }

        public static bool Standout(Window win)
        {
            return win.Standout();
        }

        public static bool Standout(bool flag)
        {
            return GetStdScr().Standout(flag);
        }

        public static bool Standout(Window win, bool flag)
        {
            return win.Standout(flag);
        }

        public static int Attr()
        {
            return GetStdScr().Attr;
        }

        public static int Attr(Window win)
        {
            return win.Attr;
        }

        public static int AttrSet(params object[] attrs)
        {
            return GetStdScr().AttrSet(attrs);
        }

        public static int AttrSet(Window win, params object[] attrs)
        {
            return win.AttrSet(attrs);
        }

        public static int AttrOn(params object[] attrs)
        {
            return GetStdScr().AttrOn(attrs);
        }

        public static int AttrOn(Window win, params object[] attrs)
        {
            return win.AttrOn(attrs);
        }

        public static int AttrOff(params object[] attrs)
        {
            return GetStdScr().AttrOff(attrs);
        }

        public static int AttrOff(Window win, params object[] attrs)
        {
            return win.AttrOff(attrs);
        }

        public static void Put(string s)
        {
            Put(GetStdScr(), s);
        }

        public static void Put(Window w, string s)
        {
            w.Put(s);
        }

        public static void Put(char ch)
        {
            Put(GetStdScr(), ch);
        }

        public static void Put(Window w, char ch)
        {
            w.Put(ch);
        }

        public static void PutLine()
        {
            GetStdScr().PutLine();
        }

        public static void PutLine(string s)
        {
            GetStdScr().PutLine(s);
        }

        public static void PutLine(Window w)
        {
            w.PutLine();
        }

        public static void PutLine(Window w, string s)
        {
            w.PutLine(s);
        }

        public static void PutLine(char ch)
        {
            GetStdScr().PutLine(ch);
        }

        public static void PutLine(Window w, char ch)
        {
            w.PutLine(ch);
        }

        internal static TextWriter Out
        {
            get
            { 
                return StdScr.TextWriter;
            }
        }

        internal static string ReadLine()
        {
            return StdScr.GetLine("", -1);
        }

        internal static void Write(string str)
        {
            Out.Write(str);
            Application.DoEvents();
        }

        internal static void Write(string fmt, params object[] args)
        {
            Write(String.Format(fmt, args));
        }

        internal static void WriteLine()
        {
            Write("\n");
        }

        internal static void WriteLine(string str)
        {
            Write(str);
            WriteLine();
        }

        internal static void WriteLine(string fmt, params object[] args)
        {
            Write(String.Format(fmt, args));
            WriteLine();
        }

        public static char GetChar()
        {
            return GetStdScr().GetChar();
        }

        public static char GetChar(Window win)
        {
            return win.GetChar();
        }

        public static KeyInfo GetKey()
        {
            return GetStdScr().GetKey(false);
        }

        public static KeyInfo GetKey(Window win)
        {
            return win.GetKey(false);
        }

        public static KeyInfo GetKey(bool echo)
        {
            return GetStdScr().GetKey(echo);
        }

        public static KeyInfo GetKey(Window win, bool echo)
        {
            return win.GetKey(echo);
        }

        public static int SelectKey(params Keys[] keys)
        {
            return GetStdScr().SelectKey(keys);
        }

        public static int SelectKey(Window win, params Keys[] keys)
        {
            return win.SelectKey(keys);
        }

        public static string Get()
        {
            return GetStdScr().Get("", -1);
        }

        public static string Get(string s)
        {
            return GetStdScr().Get(s, -1);
        }

        public static string Get(string s, int maxChars)
        {
            return GetStdScr().Get(s, maxChars);
        }

        public static string Get(Window win)
        {
            return win.Get("", -1);
        }

        public static string Get(Window win, string s)
        {
            return win.Get(s, -1);
        }

        public static string Get(Window win, string s, int maxChars)
        {
            return win.Get(s, maxChars);
        }

        public static string GetLine()
        {
            return GetStdScr().GetLine("", -1);
        }

        public static string GetLine(string s)
        {
            return GetStdScr().GetLine(s, -1);
        }

        internal static string GetLine(string s, int maxChars)
        {
            var str = GetStdScr().GetStringInput(s, maxChars);
            PutLine();
            return str;
        }

        public static string GetLine(Window win)
        {
            return win.GetLine("", -1);
        }

        public static string GetLine(Window win, string s)
        {
            return win.GetLine(s, -1);
        }

        public static string GetLine(Window win, string s, int maxChars)
        {
            return win.GetLine(s, maxChars);
        }

        public static void Move(int col, int row)
        {
            GetStdScr().Move(col, row);
        }

        public static void Move(Window win, int col, int row)
        {
            win.Move(col, row);
        }

        public static void Doupdate()
        {
            TerminalWindow.Invalidate();
        }
    }
}

