#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;

    #region Delegates

    public delegate void EditHandler();

    public delegate bool MouseHandler(int x, int y, int w);

    public delegate bool ScrollHandler();

    #endregion Delegates

    public partial class TextWindow : IDisposable, ILogWriter, IHasTextWriter, IHtmlWriter
    {
        #region Fields

        internal Color backColor;
        internal int cursorLeft;
        internal int cursorTop;
        internal bool dirty;
        internal Color foreColor;
        internal Color highlightBackColor;
        internal Color highlightForeColor;
        internal bool outputSuspended;
        internal Dictionary<Keys, object> scrollHandlers = new Dictionary<Keys, object>();
        internal Color shadowBackColor;
        internal TextWriter TextWriter;

        #endregion Fields

        #region Constructors

        internal TextWindow(TextControl parent, TextWindowCreateArgs args)
        {
            ParentControl = parent;
            ForeColor = args.ForeColor;
            BackColor = args.BackColor;
            HighlightForeColor = RuntimeRepl.DefaultHighlightForeColor;
            HighlightBackColor = RuntimeRepl.DefaultHighlightBackColor;
            ShadowBackColor = RuntimeRepl.DefaultShadowBackColor;
            Buffer = new TextBuffer(args.BufferWidth, args.BufferHeight, foreColor, backColor);
            CodeCompletion = args.CodeCompletion;
            HtmlPrefix = args.HtmlPrefix;
            HtmlSuffix = RuntimeRepl.GetSuffixFromPrefix(HtmlPrefix);
            TextWriter = TextWriter.Synchronized(new TextWindowTextWriter(this));
            InitEditHandlers();
            InitScrollHandlers();
            dirty = true;
            Style = 0;
            BufferMark = BufferBound = -1;
        }

        #endregion Constructors

        #region Internal Properties

        internal int BufferBound { get; set; }

        internal int BufferMark { get; set; }

        internal int FontIndex
        {
            get { return Style & TextStyle.FontMask; }
        }

        internal int LastPos { get; set; }

        internal int LastRow
        {
            get { return LastPos / BufferWidth; }
        }

        internal int WindowLeftMax
        {
            get { return BufferWidth - WindowWidth; }
        }

        internal int WindowTopMax
        {
            get { return BufferHeight - WindowHeight; }
        }

        #endregion Internal Properties

        #region Public Properties

        public object BackColor
        {
            get
            {
                return backColor;
            }
            set
            {
                backColor = RuntimeRepl.MakeColor(value);
                ParentControl.BackColor = backColor;
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
                else {
                    Style &= ~TextStyle.Bold;
                }
            }
        }

        public TextBuffer Buffer { get; internal set; }

        public int BufferHeight
        {
            get
            {
                return Buffer.Height;
            }
            set
            {
                ResizeBuffer(Buffer.Width, value);
            }
        }

        public int BufferHeightUsed
        {
            get { return LastRow + 1; }
        }

        public int BufferSize
        {
            get { return Buffer.Size; }
        }

        public int BufferWidth
        {
            get
            {
                return Buffer.Width;
            }
            set
            {
                ResizeBuffer(value, Buffer.Height);
            }
        }

        public string Caption
        {
            get
            {
                return ParentControl.Text;
            }
            set
            {
                ParentControl.Text = value;
            }
        }

        public int CaretLeft { get; set; }

        public int CaretPos
        {
            get
            {
                return CaretLeft + CaretTop * BufferWidth;
            }
            set
            {
                CaretLeft = value % BufferWidth;
                CaretTop = value / BufferWidth;
            }
        }

        public int CaretTop { get; set; }

        public bool CaretVisible { get; set; }

        public bool CodeCompletion { get; set; }

        public int CursorLeft
        {
            get
            {
                return cursorLeft;
            }
            set
            {
                cursorLeft = Math.Max(0, Math.Min(value, BufferWidth - 1));
                if (CursorPos > LastPos)
                {
                    LastPos = CursorPos;
                }
            }
        }

        public int CursorPos
        {
            get
            {
                return CursorTop * BufferWidth + CursorLeft;
            }
            set
            {
                CursorTop = value / BufferWidth;
                CursorLeft = value % BufferWidth;
            }
        }

        public int CursorTop
        {
            get
            {
                return cursorTop;
            }
            set
            {
                cursorTop = Math.Max(0, Math.Min(value, BufferHeight - 1));
                if (CursorPos > LastPos)
                {
                    LastPos = CursorPos;
                }
            }
        }

        public object ForeColor
        {
            get
            {
                return foreColor;
            }
            set
            {
                foreColor = RuntimeRepl.MakeColor(value);
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
                else {
                    Style &= ~TextStyle.Highlight;
                }
            }
        }

        public object HighlightBackColor
        {
            get
            {
                return highlightBackColor;
            }
            set
            {
                highlightBackColor = RuntimeRepl.MakeColor(value);
            }
        }

        public object HighlightForeColor
        {
            get
            {
                return highlightForeColor;
            }
            set
            {
                highlightForeColor = RuntimeRepl.MakeColor(value);
            }
        }

        public string HtmlPrefix { get; internal set; }

        public string HtmlSuffix { get; internal set; }

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
                else {
                    Style &= ~TextStyle.Italic;
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
                if (value)
                {
                    Style = 0;
                }
            }
        }

        public bool OutputSuspended
        {
            get { return outputSuspended; }

            set
            {
                var old = outputSuspended;
                outputSuspended = value;
                if (old && !outputSuspended)
                {
                    Refresh();
                }
            }
        }

        public TextControl ParentControl { get; internal set; }

        public TextFormBase ParentForm
        {
            get { return ParentControl.ParentForm; }
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
                else {
                    Style &= ~TextStyle.Reverse;
                }
            }
        }

        public int ScreenLeft
        {
            get { return ParentForm.DesktopLocation.X / ParentForm.CharWidth; }
        }

        public int ScreenTop
        {
            get { return ParentForm.DesktopLocation.Y / ParentForm.LineHeight; }
        }

        public bool Scrollable
        {
            get { return ParentForm.VertScrollBar != null; }
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
                else {
                    Style &= ~TextStyle.Shadow;
                }
            }
        }

        public object ShadowBackColor
        {
            get
            {
                return shadowBackColor;
            }
            set
            {
                shadowBackColor = RuntimeRepl.MakeColor(value);
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
                else {
                    Style &= ~TextStyle.Strikeout;
                }
            }
        }

        public int Style { get; set; }

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
                else {
                    Style &= ~TextStyle.Underline;
                }
            }
        }

        public bool Visible
        {
            get
            {
                return ParentForm.Visible;
            }
            set
            {
                // Cannot show or hide the repl
                if (this != RuntimeRepl.StdScr)
                {
                    RuntimeRepl.GuiInvoke(new Action(() =>
                    {
                        ParentForm.Visible = value;
                    }));
                }
            }
        }

        public int WindowHeight
        {
            get { return ParentControl.Rows; }
        }

        public int WindowLeft { get; internal set; }

        public int WindowTop { get; internal set; }

        public int WindowWidth
        {
            get { return ParentControl.Cols; }
        }

        #endregion Public Properties

        #region Private Methods

        void IDisposable.Dispose()
        {
            Close();
        }

        TextWriter IHasTextWriter.GetTextWriter()
        {
            return TextWriter;
        }

        string IHtmlWriter.Format(string style, string msg)
        {
            return FormatHtml(style, msg);
        }

        void ILogWriter.WriteLog(string style, string msg)
        {
            var oldStyle = Style;
            var oldColor = ForeColor;
            try
            {
                switch (style)
                {
                    case "info":
                        {
                            ForeColor = RuntimeRepl.DefaultInfoColor;
                            break;
                        }
                    case "warning":
                        {
                            ForeColor = RuntimeRepl.DefaultWarningColor;
                            break;
                        }
                    case "error":
                        {
                            ForeColor = RuntimeRepl.DefaultErrorColor;
                            break;
                        }
                    default:
                        {
                            ForeColor = RuntimeRepl.DefaultForeColor;
                            break;
                        }
                }
                Reverse = false;
                Highlight = false;
                Shadow = false;
                TextWriter.Write(msg);
            }
            finally
            {
                Style = oldStyle;
                ForeColor = oldColor;
            }
        }

        #endregion Private Methods

        #region Internal Methods

        internal void AddEditHandler(Keys key, IApply handler)
        {
            editHandlers[key] = handler;
        }

        internal void AddEditHandler(Keys key, EditHandler handler)
        {
            editHandlers[key] = handler;
        }

        internal void AddEditHandler(Keys key, MouseHandler handler)
        {
            editHandlers[key] = handler;
        }

        internal void AddScrollHandler(Keys key, ScrollHandler handler)
        {
            scrollHandlers[key] = handler;
        }

        internal void AddScrollHandler(Keys key, MouseHandler handler)
        {
            scrollHandlers[key] = handler;
        }

        internal bool CmdBufferEnd()
        {
            WindowTop = Math.Min(Math.Max(0, LastRow - WindowHeight / 2), WindowTopMax);
            Refresh();
            return true;
        }

        internal bool CmdBufferHome()
        {
            WindowTop = 0;
            Refresh();
            return true;
        }

        internal bool CmdPageDown()
        {
            if (VScroll(WindowHeight))
            {
                Refresh();
            }
            return true;
        }

        internal bool CmdPageUp()
        {
            if (VScroll(-WindowHeight))
            {
                Refresh();
            }
            return true;
        }

        internal bool CmdScrollDown()
        {
            if (VScroll(1))
            {
                Refresh();
            }
            return true;
        }

        internal bool CmdScrollLeft()
        {
            if (HScroll(-1))
            {
                Refresh();
            }
            return true;
        }

        internal bool CmdScrollRight()
        {
            if (HScroll(1))
            {
                Refresh();
            }
            return true;
        }

        internal bool CmdScrollUp()
        {
            if (VScroll(-1))
            {
                Refresh();
            }
            return true;
        }

        internal bool CmdWheelScroll(int x, int y, int w)
        {
            if (w < 0)
            {
                if (VScroll(1))
                {
                    Refresh();
                }
            }
            else if (w > 0)
            {
                if (VScroll(-1))
                {
                    Refresh();
                }
            }
            return true;
        }

        internal TextBufferItem Get(int pos)
        {
            return Buffer[pos];
        }

        internal TextBufferItem Get(int col, int line)
        {
            return Buffer[col, line];
        }

        internal string GetSelection(bool insertlf)
        {
            return Buffer.GetString(BufferMark, BufferBound, insertlf);
        }

        internal TextBufferItem GetDisplayAttributes(int col, int row)
        {
            var item = Buffer[col, row];
            var hasCaret = (CaretVisible && col == CaretLeft && row == CaretTop);
            var isSelected = Selected(col, row);

            if (isSelected)
            {
                if (hasCaret)
                {
                    item.Fg = RuntimeRepl.DefaultHighlightForeColor;
                    item.Bg = RuntimeRepl.DefaultHighlightBackColor;
                }
                else
                {
                    item.Fg = RuntimeRepl.DefaultHighlightForeColor;
                    item.Bg = Color.FromArgb(100, RuntimeRepl.DefaultHighlightBackColor);
                }
            }
            else if (hasCaret)
            {
                var temp = item.Fg;
                item.Fg = item.Bg;
                item.Bg = temp;
            }

            return item;
        }

        internal void HideCursor()
        {
            if (CaretVisible)
            {
                CaretVisible = false;
                RefreshPosition(CaretPos);
            }
        }

        internal bool HScroll(int count)
        {
            var c = Math.Max(0, Math.Min(WindowLeft + count, WindowLeftMax));
            if (c != WindowLeft)
            {
                WindowLeft = c;
                return true;
            }
            else {
                return false;
            }
        }

        internal void InitScrollHandlers()
        {
            AddScrollHandler(RuntimeRepl.PseudoKeyForMouseWheel, CmdWheelScroll);
            AddScrollHandler(Keys.PageUp, CmdPageUp);
            AddScrollHandler(Keys.PageDown, CmdPageDown);
            AddScrollHandler(Keys.Up | Keys.Control, CmdScrollUp);
            AddScrollHandler(Keys.Down | Keys.Control, CmdScrollDown);
            AddScrollHandler(Keys.Left | Keys.Control, CmdScrollLeft);
            AddScrollHandler(Keys.Right | Keys.Control, CmdScrollRight);
            AddScrollHandler(Keys.Home | Keys.Control, CmdBufferHome);
            AddScrollHandler(Keys.End | Keys.Control, CmdBufferEnd);
        }

        internal void Invalidate(int x, int y, int w, int h)
        {
            if (!OutputSuspended)
            {
                ParentControl.GuiInvalidate(x - WindowLeft, y - WindowTop, w, h);
            }
        }

        internal void Next()
        {
            ++CursorPos;

            if (CursorPos == BufferSize)
            {
                ScrollBuffer();
            }

            ScrollIntoView();
        }

        internal void OnResizeWindow()
        {
            if (BufferWidth < WindowWidth || BufferHeight < WindowHeight)
            {
                ResizeBuffer(BufferWidth, BufferHeight);
            }
        }

        internal int Rebase(int x, int y, int w, int h, int pos)
        {
            // Adjust pos to fit within the new buffer;
            var x1 = Math.Min(pos % BufferWidth, w - 1);
            var y1 = Math.Min(pos / BufferWidth, h - 1);
            var newpos = (x1 - x) + (y1 - y) * w;
            return newpos;
        }

        internal void ScrollIntoView(bool updateH = false)
        {
            var refresh = false;

            if (updateH)
            {
                if (CursorLeft < WindowLeft)
                {
                    WindowLeft = CursorLeft;
                    refresh = true;
                }
                else if (CursorLeft >= WindowLeft + WindowWidth)
                {
                    WindowLeft = CursorLeft - WindowWidth + 1;
                    refresh = true;
                }
            }

            if (CursorTop < WindowTop)
            {
                WindowTop = CursorTop;
                refresh = true;
            }
            else if (CursorTop >= WindowTop + WindowHeight)
            {
                WindowTop = CursorTop - WindowHeight + 1;
                refresh = true;
            }

            if (refresh)
            {
                Refresh();
            }
        }

        internal string SelectCompletion(string text)
        {
            return text;
            /*
			var prefix = text;
			var completions = RuntimeConsoleBase.GetCompletions(prefix);
			var x = Col;
			var y = Row;
			var w = 40;
			var h = 7;
			if (x + w > RuntimeRepl.Width)
			{
				x = RuntimeRepl.Width - w;
			}
			if (y + h > RuntimeRepl.Height)
			{
				y = RuntimeRepl.Height - h;
			}
			using (var win = Window.CreateFrameWindow(x, y, w, h, -1, null, null))
			{
				Func<KeyInfo,IEnumerable> keyHandler = (k) =>
				{
					if (k.KeyData == Keys.Back)
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
			*/
        }

        internal bool Selected(int col, int row)
        {
            if (BufferMark < BufferBound)
            {
                var p = col + BufferWidth * row;
                return (BufferMark <= p && p < BufferBound);
            }
            else if (BufferMark > BufferBound)
            {
                var p = col + BufferWidth * row;
                return (BufferBound <= p && p < BufferMark);
            }
            else {
                return false;
            }
        }

        internal void SendKey(KeyInfo key)
        {
            ParentControl.SendKey(key);
        }

        internal void Set(int x, int y, char ch)
        {
            // Only used for graphics! Use normal font.
            Set(x, y, ch, foreColor, backColor, 0);
        }

        internal void Set(int col, int line, char ch, Color fg, Color bg, int fontIndex)
        {
            Buffer[col, line] = new TextBufferItem(ch, fg, bg, fontIndex);
        }

        internal void Set(int pos, char ch, Color fg, Color bg, int fontIndex)
        {
            Buffer[pos] = new TextBufferItem(ch, fg, bg, fontIndex);
        }

        internal void Set(int pos, TextBufferItem item)
        {
            Buffer[pos] = item;
        }

        internal void ShowCursor()
        {
            HideCursor();
            CaretVisible = true;
            CaretPos = CursorPos;
            RefreshPosition(CaretPos);
        }

        internal bool VScroll(int count)
        {
            var t = Math.Max(0, Math.Min(WindowTop + count, WindowTopMax));
            if (t != WindowTop)
            {
                WindowTop = t;
                return true;
            }
            else {
                return false;
            }
        }

        #endregion Internal Methods

        #region Public Methods

        public void BringToFront()
        {
            ParentControl.GuiBringToFront();
        }

        public void Clear()
        {
            dirty = true;
            CursorPos = 0;
            ClearToBot();
        }

        public void ClearToBot()
        {
            ClearToPos(LastPos);
        }

        public void ClearToEol()
        {
            dirty = true;
            for (var col = CursorLeft; col < BufferWidth; ++col)
            {
                // maybe support reverse?
                Set(col, CursorTop, ' ', foreColor, backColor, 0);
            }
        }

        public void ClearToPos(int end)
        {
            dirty = true;
            for (var pos = CursorPos; pos < end; ++pos)
            {
                // maybe support reverse?
                Set(pos, ' ', foreColor, backColor, 0);
            }
            LastPos = CursorPos;
        }

        public void Close()
        {
            RuntimeRepl.CloseWindow(this);
        }

        public TextBuffer CopyBuffer()
        {
            return CopyBuffer(0, 0, BufferWidth, BufferHeight);
        }

        public TextBuffer CopyBuffer(int x, int y, int w, int h)
        {
            return Buffer.Copy(x, y, w, h);
        }

        public string FormatHtml(string style, string msg)
        {
            if (string.IsNullOrEmpty(HtmlPrefix) || string.IsNullOrEmpty(style))
            {
                return msg;
            }
            else {
                return Runtime.MakeString(HtmlPrefix, style, HtmlSuffix, msg, HtmlPrefix, "/", style, HtmlSuffix);
            }
        }

        public string GetStringFromBuffer(int beg, int end)
        {
            return Buffer.GetString(beg, end, false);
        }

        public string GetWordFromBuffer(int beg, int pos, int end, Func<char, bool> wordCharTest)
        {
            var text = GetStringFromBuffer(beg, end);
            return Runtime.GetWordFromString(text, pos - beg, wordCharTest);
        }

        public static TextWindow Open(params object[] args)
        {
            var createArgs = new TextWindowCreateArgs(args);
            return RuntimeRepl.OpenWindow(createArgs);
        }

        public void Paste(int x, int y, TextBuffer buffer)
        {
            Buffer.Paste(x, y, buffer);
            Refresh();
        }

        public void Refresh()
        {
            if (!OutputSuspended)
            {
                ParentControl.GuiInvalidate();
                ParentControl.GuiUpdateVertScrollBarPos();
                ParentControl.GuiUpdateHoriScrollBarPos();
                dirty = false;
            }
        }

        public void RefreshLine(int y)
        {
            // paints one line
            Invalidate(0, y, BufferWidth, 1);
        }

        public void RefreshPosition(int pos)
        {
            // paints one character
            var x = pos % BufferWidth;
            var y = pos / BufferWidth;
            Invalidate(x, y, 1, 1);
        }

        public void RefreshPositions(int beg, int end)
        {
            // paints lines between two offsets
            var y1 = beg / BufferWidth;
            var y2 = (end - 1) / BufferWidth;
            Invalidate(0, y1, BufferWidth, y2 - y1 + 1);
        }

        public void ResizeBuffer(int width, int height)
        {
            width = Math.Max(width, WindowWidth);
            height = Math.Max(height, WindowHeight);
            if (width != BufferWidth || height != BufferHeight)
            {
                var x = 0;
                var w = Math.Min(width, BufferWidth - x);
                var y = Math.Max(0, LastRow - height - 1);
                var h = Math.Min(height, BufferHeight - y);
                var buf = new TextBuffer(width, height, Buffer.ForeColor, Buffer.BackColor);
                buf.CopyRect(0, 0, Buffer, x, y, w, h);
                CursorPos = Rebase(x, y, width, height, CursorPos);
                homePos = Rebase(x, y, width, height, homePos);
                //caretPos = Rebase(x, y, width, height, caretPos);
                savedPos = Rebase(x, y, width, height, savedPos);
                LastPos = Rebase(x, y, width, height, LastPos);
                if (BufferMark != -1)
                {
                    BufferMark = Rebase(x, y, width, height, BufferMark);
                }
                if (BufferBound != -1)
                {
                    BufferBound = Rebase(x, y, width, height, BufferBound);
                }
                Buffer = buf;
                Refresh();
            }
        }

        public string ScrapeLispWordAt(int x, int y)
        {
            var beg = y * BufferWidth;
            var end = beg + BufferWidth;
            var pos = beg + x;
            var text = GetWordFromBuffer(beg, pos, end, Runtime.IsLispWordChar);
            return text;
        }

        public string ScrapeWordAt(int x, int y)
        {
            var beg = y * BufferWidth;
            var end = beg + BufferWidth;
            var pos = beg + x;
            var text = GetWordFromBuffer(beg, pos, end, Runtime.IsWordChar);
            return text;
        }

        public void ScrollBuffer()
        {
            var lines = Buffer.Scroll(1);
            var n = lines * BufferWidth;
            CursorPos -= n;
            homePos -= n;
            //caretPos -= n;
            savedPos -= n;
            LastPos -= n;
            if (BufferMark != -1)
            {
                BufferMark = Math.Max(0, BufferMark - n);
            }
            if (BufferBound != -1)
            {
                BufferBound = Math.Max(0, BufferBound - n);
            }
        }

        public int SelectKey(params Keys[] keys)
        {
            while (true)
            {
                var info = ReadKey(false);
                for (var i = 0; i < keys.Length; ++i)
                {
                    if (keys[i] == info.KeyData)
                    {
                        return i;
                    }
                }
            }
        }

        public void SendInterruptKey()
        {
            ParentControl.SendInterruptKey();
        }

        public void SendKey(char key)
        {
            SendKey(new KeyInfo(key));
        }

        public void SendKey(Keys key)
        {
            SendKey(new KeyInfo(key));
        }

        public void Set(int pos, char ch)
        {
            if (Reverse)
            {
                Set(pos, ch, backColor, foreColor, FontIndex);
            }
            else if (Highlight)
            {
                Set(pos, ch, highlightForeColor, highlightBackColor, FontIndex);
            }
            else if (Shadow)
            {
                Set(pos, ch, foreColor, shadowBackColor, FontIndex);
            }
            else {
                Set(pos, ch, foreColor, backColor, FontIndex);
            }
        }

        public void SetWindowPos(int left, int top)
        {
            WindowTop = Math.Max(0, Math.Min(top, WindowTopMax));
            WindowLeft = Math.Max(0, Math.Min(left, WindowLeftMax));
            Refresh();
        }

        public void Write(char ch)
        {
            HideCursor();

            if (CursorPos < 0 || CursorPos >= BufferSize)
            {
                return;
            }

            switch (ch)
            {
                case '\t':
                    {
                        do
                        {
                            Write(' ');
                        } while ((CursorLeft % 8) != 0);
                        break;
                    }

                case '\n':
                    {
                        do
                        {
                            Write(' ');
                        }
                        while (CursorLeft != 0);
                        RefreshLine(CursorTop - 1);
                        break;
                    }

                case '\r':
                    {
                        CursorLeft = 0;
                        break;
                    }

                default:
                    {
                        Set(CursorPos, ch);
                        dirty = true;
                        Next();
                        break;
                    }
            }
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

        public void WriteLine(char ch)
        {
            Write(ch);
            WriteLine();
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

        #endregion Public Methods
    }
}