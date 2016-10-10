// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.IO;

namespace Kiezel
{
    public enum FrameType
    {
        None = 0,
        Thin = 1,
        Double = 2,
        Thick = 3
    }

    public class FrameCharacters // WGL4
    {
        public char ULCORNER = (char)0x250c;
        public char URCORNER = (char)0x2510;
        public char LLCORNER = (char)0x2514;
        public char LRCORNER = (char)0x2518;
        public char HLINE = (char)0x2500;
        public char VLINE = (char)0x2502;
        public char LTEE = (char)0x251c;
        public char RTEE = (char)0x2524;
        public char TTEE = (char)0x252c;
        public char BTEE = (char)0x2534;
        public char PLUS = (char)0x253c;

        public FrameCharacters()
            : this(FrameType.Thin)
        {
        }

        public FrameCharacters(FrameType set)
        {
            switch (set)
            {
                case FrameType.None:
                {
                    ULCORNER = (char)'+';
                    URCORNER = (char)'+';
                    LLCORNER = (char)'+';
                    LRCORNER = (char)'+';
                    HLINE = (char)'-';
                    VLINE = (char)'|';
                    LTEE = (char)'+';
                    RTEE = (char)'+';
                    TTEE = (char)'+';
                    BTEE = (char)'+';
                    PLUS = (char)'+';
                    break;
                }
                case FrameType.Thin:
                {
                    ULCORNER = (char)0x250c;
                    URCORNER = (char)0x2510;
                    LLCORNER = (char)0x2514;
                    LRCORNER = (char)0x2518;
                    HLINE = (char)0x2500;
                    VLINE = (char)0x2502;
                    LTEE = (char)0x251c;
                    RTEE = (char)0x2524;
                    TTEE = (char)0x252c;
                    BTEE = (char)0x2534;
                    PLUS = (char)0x253c;
                    break;
                }
                case FrameType.Double:
                {
                    ULCORNER = (char)0x2554;
                    URCORNER = (char)0x2557;
                    LLCORNER = (char)0x255a;
                    LRCORNER = (char)0x255d;
                    HLINE = (char)0x2550;
                    VLINE = (char)0x2551;
                    LTEE = (char)0x2560;
                    RTEE = (char)0x2563;
                    TTEE = (char)0x2566;
                    BTEE = (char)0x2569;
                    PLUS = (char)0x256c;
                    break;
                }
                case FrameType.Thick:
                {
                    ULCORNER = (char)0x250f;
                    URCORNER = (char)0x2513;
                    LLCORNER = (char)0x2517;
                    LRCORNER = (char)0x251b;
                    HLINE = (char)0x2501;
                    VLINE = (char)0x2503;
                    LTEE = (char)0x2523;
                    RTEE = (char)0x252b;
                    TTEE = (char)0x2533;
                    BTEE = (char)0x253b;
                    PLUS = (char)0x254b;
                    break;
                }
            }
        }

    }

    public class TextStyle
    {
        public const int Normal = 0;
        public const int Bold = 1;
        public const int Italic = 2;
        public const int Underline = 4;
        public const int Strikeout = 8;
        public const int FontMask = 0xFF;
        public const int Highlight = 0x100;
        public const int Shadow = 0x200;
        public const int Reverse = 0x400;

        internal static int MakeTextAttributes(IEnumerable values)
        {
            var attrs = 0;
            foreach (object value in Runtime.ToIter(values))
            {
                attrs |= MakeTextAttribute(value);
            }
            return attrs;
        }

        internal static int MakeTextAttribute(object value)
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
                    case "shadow":
                    {
                        return TextStyle.Shadow;
                    }
                    case "bold":
                    {
                        return TextStyle.Bold;
                    }
                    case "italic":
                    {
                        return TextStyle.Italic;
                    }
                    case "underline":
                    {
                        return TextStyle.Underline;
                    }
                    case "strikeout":
                    case "strike-out":
                    {
                        return TextStyle.Strikeout;
                    }
                    case "highlight":
                    {
                        return TextStyle.Highlight;
                    }
                    case "reverse":
                    {
                        return TextStyle.Reverse;
                    }
                    default:
                    {
                        return TextStyle.Normal;
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

        public static Buffer ScreenBuffer
        {
            get
            {
                return TerminalWindow.ScreenBuffer;
            }
        }

        internal static void Init(CommandLineOptions options, TerminalMainProgram mainProgram)
        {
            Width = options.Width;
            Height = options.Height;
            Terminal.SetFrameType(FrameType.Thin);
            DefaultForeColor = new ColorType(options.ForeColor);
            DefaultBackColor = new ColorType(options.BackColor);
            DefaultInfoColor = new ColorType(options.InfoColor);
            DefaultErrorColor = new ColorType(options.ErrorColor);
            DefaultWarningColor = new ColorType(options.WarningColor);
            DefaultHighlightForeColor = new ColorType(options.HighlightForeColor);
            DefaultHighlightBackColor = new ColorType(options.HighlightBackColor);
            DefaultShadowBackColor = new ColorType(options.ShadowBackColor);
            History = new TerminalHistory();
            TerminalWindow = new TerminalMainForm(options.FontName, options.FontSize, Width, Height, mainProgram);
            WindowList = new List<Window>();
            StdScr = ReplWindow.CreateReplWindow(Width, Height, options.BufferHeight);
            Register(StdScr);
            RefreshAllWindows();
            TerminalWindow.Show();
            TerminalWindow.Activate();
            RuntimeGfx.ApplicationRun(TerminalWindow);
            //Runtime.Quit();
        }

        internal static void Endwin()
        {
            // not needed
        }

        public static void SetFont(string name, int size)
        {
            TerminalWindow.InitFont(name, size);
            TerminalWindow.InitClientRectangle();
            RefreshAllWindows();
        }

        public static void SetFrameType(FrameType set)
        {
            Acs = new FrameCharacters(set);
        }

        public static void SetSize(int width, int height)
        {
            SetSize(width, height, 10 * height);
        }

        public static void SetSize(int width, int height, int bufferHeight)
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
            RefreshAllWindows();
        }

        internal static TerminalMainForm TerminalWindow;
        internal static List<Window> WindowList;
        internal static FrameCharacters Acs;

        public static int Width { get; internal set; }

        public static int Height { get; internal set; }

        public static ColorType DefaultForeColor{ get ; set; }

        public static ColorType DefaultBackColor { get; set; }

        public static ColorType DefaultHighlightForeColor{ get ; set; }

        public static ColorType DefaultHighlightBackColor { get; set; }

        public static ColorType DefaultShadowBackColor { get; set; }

        public static ColorType DefaultInfoColor{ get ; set; }

        public static ColorType DefaultWarningColor{ get ; set; }

        public static ColorType DefaultErrorColor{ get ; set; }

        internal static TerminalHistory History;
        internal static ReplWindow StdScr;

        public static void ResetColors()
        {
            StdScr.ForeColor = DefaultForeColor;
            StdScr.BackColor = DefaultBackColor;
        }

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


        public static void CloseAllWindows()
        {
            WindowList.Clear();
            Register(StdScr);
            RefreshAllWindows();
        }

        internal static void Register(Window win)
        {
            if (win != null)
            {
                WindowList.Add(win);
            }
        }

        internal static void Unregister(Window win)
        {
            WindowList.Remove(win);
        }

        internal static void BringToTop(Window win)
        {
            Unregister(win);
            Register(win);
            RefreshAllWindows();
        }

        public static void RefreshAllWindows()
        {
            foreach (var win in WindowList)
            {
                if (win.Visible)
                {
                    win.OutRefresh();
                }
            }
            DoUpdate();
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
            return StdScr.ReadLine();
        }

        internal static void Write(string str)
        {
            Out.Write(str);
            RuntimeGfx.ProcessEvents();
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

        public static void DoUpdate()
        {
            TerminalWindow.Invalidate();
            //RuntimeConsole.ProcessEvents();
        }

    }
}

