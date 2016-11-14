#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace Kiezel
{
    #region Enumerations

    public enum FrameType
    {
        None = 0,
        Thin = 1,
        Double = 2,
        Thick = 3
    }

    #endregion Enumerations

    // WGL4
    public class FrameCharacters
    {
        #region Fields

        public char BTEE = (char)0x2534;
        public char HLINE = (char)0x2500;
        public char LLCORNER = (char)0x2514;
        public char LRCORNER = (char)0x2518;
        public char LTEE = (char)0x251c;
        public char PLUS = (char)0x253c;
        public char RTEE = (char)0x2524;
        public char TTEE = (char)0x252c;
        public char ULCORNER = (char)0x250c;
        public char URCORNER = (char)0x2510;
        public char VLINE = (char)0x2502;

        #endregion Fields

        #region Constructors

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

        #endregion Constructors
    }

    public partial class RuntimeRepl
    {
        #region Static Fields

        public static CommandLineOptions Options;
        public static ReplTextForm TerminalWindow;
        public static FrameCharacters Acs;
        public static int CharWidth;
        public static int CharHeight;
        public static int LineHeight;
        public static Font[] Fonts;
        public static Keys PseudoKeyForMouseWheel = Keys.F24;
        public static Keys PseudoKeyForResizeEvent = Keys.Escape | Keys.Control | Keys.Alt | Keys.Shift;
        public static Keys InterruptKey = Keys.Control | Keys.D;
        public static TextWindow StdScr;

        #endregion Static Fields

        #region Public Properties

        public static Color DefaultBackColor { get; set; }

        public static Color DefaultErrorColor { get ; set; }

        public static Color DefaultForeColor { get ; set; }

        public static Color DefaultHighlightBackColor { get; set; }

        public static Color DefaultHighlightForeColor { get ; set; }

        public static Color DefaultInfoColor { get ; set; }

        public static Color DefaultShadowBackColor { get; set; }

        public static Color DefaultWarningColor { get ; set; }

        public static int Height { get; set; }

        public static TextWriter Out
        {
            get
            {
                return StdScr.TextWriter;
            }
        }

        public static int Width { get; set; }

        #endregion Public Properties

        #region Public Methods

        public static void CloseWindow(TextWindow win)
        {
            GuiInvoke(new Action(() =>
            {
                GuiCloseWindow(win);
            }));
        }

        public static TextWindow GetStdScr()
        {
            if (Symbols.StdScr != null && Symbols.StdScr.Usage != SymbolUsage.None)
            {
                return (TextWindow)Runtime.GetDynamic(Symbols.StdScr);
            }
            else
            {
                return StdScr;
            }
        }

        public static void GuiCloseWindow(TextWindow win)
        {
            win.ParentControl.ParentForm.Close();
        }

        public static object GuiInvoke(Delegate method, params object[] args)
        {
            if (TerminalWindow == null)
            {
                return null;
            }
            if (TerminalWindow.InvokeRequired)
            {
                return TerminalWindow.Invoke(method, args);
            }
            else
            {
                return method.DynamicInvoke(args);
            }
        }

        public static TextWindow GuiOpenWindow(TextWindowCreateArgs args)
        {
            var form = new TextForm(args);
            if (args.Owned)
            {
                form.Owner = TerminalWindow;
            }
            return form.TermControl.Window;
        }

        public static void Init(CommandLineOptions options)
        {
            Options = options;
            Width = options.Width;
            Height = options.Height;
            SetFrameType(FrameType.Thin);
            DefaultForeColor = MakeColor(options.ForeColor);
            DefaultBackColor = MakeColor(options.BackColor);
            DefaultInfoColor = MakeColor(options.InfoColor);
            DefaultErrorColor = MakeColor(options.ErrorColor);
            DefaultWarningColor = MakeColor(options.WarningColor);
            DefaultHighlightForeColor = MakeColor(options.HighlightForeColor);
            DefaultHighlightBackColor = MakeColor(options.HighlightBackColor);
            DefaultShadowBackColor = MakeColor(options.ShadowBackColor);
            InitFonts(options.FontName, options.FontSize);
            TerminalWindow = new ReplTextForm(new TextWindowCreateArgs(options));
            StdScr = TerminalWindow.TermControl.Window;
            TerminalWindow.Show();
            TerminalWindow.Activate();
            Runtime.CreateThread(RunGuiReplMode);
            Application.Run(TerminalWindow);
        }

        public static void InitFonts(string name, int size)
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

            var form = new Form();
            var graphics = form.CreateGraphics();
            var s = "xyz";
            var flags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.NoClipping | TextFormatFlags.Left | TextFormatFlags.SingleLine;
            var textsize = TextRenderer.MeasureText(graphics, s, Fonts[0], Size.Empty, flags);
            CharWidth = textsize.Width / s.Length;
            CharHeight = textsize.Height;
            LineHeight = CharHeight;
        }

        public static Color MakeColor(object color)
        {
            Color _Color;

            if (Runtime.Integerp(color))
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
                object color2;

                if ((color2 = Runtime.GetStaticPropertyValue(typeof(Color), color)) != null)
                {
                    _Color = (Color)color2;
                }
                else if ((color2 = Runtime.GetStaticPropertyValue(typeof(SystemColors), color)) != null)
                {
                    _Color = (Color)color2;
                }
                else
                {
                    try
                    {
                        _Color = ColorTranslator.FromHtml(colorName);
                    }
                    catch
                    {
                        _Color = Color.Black;
                    }
                }
            }

            return _Color;
        }

        public static TextWindow OpenWindow(TextWindowCreateArgs args)
        {
            return (TextWindow)GuiInvoke(new Func<TextWindow>(() =>
            {
                return GuiOpenWindow(args);
            }));
        }

        public static void ResetColors()
        {
            StdScr.ForeColor = DefaultForeColor;
            StdScr.BackColor = DefaultBackColor;
        }

        public static void SetFrameType(FrameType set)
        {
            Acs = new FrameCharacters(set);
        }

        #endregion Public Methods
    }

    public class TextStyle
    {
        #region Constants

        public const int Bold = 1;
        public const int FontMask = 0xFF;
        public const int Highlight = 0x100;
        public const int Italic = 2;
        public const int Normal = 0;
        public const int Reverse = 0x400;
        public const int Shadow = 0x200;
        public const int Strikeout = 8;
        public const int Underline = 4;

        #endregion Constants
    }

    public class TextWindowCreateArgs
    {
        #region Constructors

        public TextWindowCreateArgs()
        {
            var defaults = RuntimeRepl.StdScr;
            var shiftRight = 5;
            var shiftDown = 3;
            Left = defaults.ScreenLeft + shiftRight;
            Top = defaults.ScreenTop + shiftDown;
            Width = defaults.WindowWidth;
            Height = defaults.WindowHeight;
            BufferWidth = defaults.BufferWidth;
            BufferHeight = defaults.BufferHeight;
            BufferWidth = Math.Max(Width, BufferWidth);
            BufferHeight = Math.Max(Height, BufferHeight);
            ForeColor = (Color)defaults.ForeColor;
            BackColor = (Color)defaults.BackColor;
            Caption = "";
            Visible = true;
            Resizable = true;
            Scrollable = true;
            CodeCompletion = false;
            HtmlPrefix = defaults.HtmlPrefix;
            Owned = false;
            Border = true;
            OnCloseFunction = null;
        }

        public TextWindowCreateArgs(CommandLineOptions options)
        {
            //
            // Called for REPL window
            //
            Left = -1;
            Top = -1;
            Width = options.Width;
            Height = options.Height;
            BufferWidth = Math.Max(Width, options.BufferWidth);
            BufferHeight = Math.Max(Height, options.BufferHeight);
            ForeColor = RuntimeRepl.MakeColor(options.ForeColor);
            BackColor = RuntimeRepl.MakeColor(options.BackColor);
            Caption = "Kiezellisp";
            Visible = true;
            Resizable = true;
            Scrollable = true;
            CodeCompletion = true;
            HtmlPrefix = options.HtmlPrefix;
            Owned = false;
            Border = true;
            OnCloseFunction = null;
        }

        public TextWindowCreateArgs(object[] args)
        {
            //
            // Called for non-REPL windows
            //
            var dict = new Prototype(args);
            var defaults = (TextWindow)(dict["defaults"] ?? RuntimeRepl.StdScr);
            var shiftRight = 5;
            var shiftDown = 3;
            Left = (int)(dict["left"] ?? defaults.ScreenLeft + shiftRight);
            Top = (int)(dict["top"] ?? defaults.ScreenTop + shiftDown);
            Width = (int)(dict["width"] ?? defaults.WindowWidth);
            Height = (int)(dict["height"] ?? defaults.WindowHeight);
            BufferWidth = (int)(dict["buffer-width"] ?? dict["width"] ?? defaults.BufferWidth);
            BufferHeight = (int)(dict["buffer-height"] ?? dict["height"] ?? defaults.BufferHeight);
            BufferWidth = Math.Max(Width, BufferWidth);
            BufferHeight = Math.Max(Height, BufferHeight);
            ForeColor = RuntimeRepl.MakeColor(dict["fore-color"] ?? defaults.ForeColor);
            BackColor = RuntimeRepl.MakeColor(dict["back-color"] ?? defaults.BackColor);
            Caption = (string)dict["caption"];
            Visible = (bool)(dict["visible"] ?? true);
            Resizable = (bool)(dict["resizable"] ?? true);
            Scrollable = Resizable || (bool)(dict["scrollable"] ?? true);
            CodeCompletion = (bool)(dict["code-completion"] ?? false);
            HtmlPrefix = (string)(dict["html-prefix"] ?? defaults.HtmlPrefix);
            Owned = (bool)(dict["owned"] ?? false);
            Border = (bool)(dict["border"] ?? true);
            OnCloseFunction = (IApply)(dict["on-close"] ?? null);

            if (!Border)
            {
                Resizable = Scrollable = false;
                BufferWidth = Width;
                BufferHeight = Height;
            }
        }

        #endregion Constructors

        #region Public Properties

        public Color BackColor { get; set; }

        public bool Border { get; set; }

        public int BufferHeight { get; set; }

        public int BufferWidth { get; set; }

        public string Caption { get; set; }

        public IApply OnCloseFunction { get; set; }

        public Color ForeColor { get; set; }

        public int Height { get; set; }

        public string HtmlPrefix { get; set; }

        public int Left { get; set; }

        public bool Owned { get; set; }

        public bool Resizable { get; set; }

        public bool Scrollable { get; set; }

        public bool CodeCompletion { get; set; }

        public int Top { get; set; }

        public bool Visible { get; set; }

        public int Width { get; set; }

        #endregion Public Properties
    }
}
