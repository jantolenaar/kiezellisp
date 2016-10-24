// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;

namespace Kiezel
{
    public class ReplWindow: Window, ILogWriter
    {
        internal ReplWindow(Buffer pad, int w, int h)
            : base(0, 0, pad, 0, 0, w, h, Terminal.DefaultForeColor, Terminal.DefaultBackColor)
        {
        }

        internal static ReplWindow CreateReplWindow(int w, int h, int maxh)
        {
            var pad = new Buffer(w, maxh);
            var win = new ReplWindow(pad, w, h);
            win.ScrollEnabled = true;
            win.ScrollPosEnabled = true;
            win.TabCompletionEnabled = true;
            win.InitHandlers();
            return win;
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
                        ForeColor = Terminal.DefaultInfoColor;
                        break;
                    }
                    case "warning":
                    {
                        ForeColor = Terminal.DefaultWarningColor;
                        break;
                    }
                    case "error":
                    {
                        ForeColor = Terminal.DefaultErrorColor;
                        break;
                    }
                    default:
                    {
                        ForeColor = Terminal.DefaultForeColor;
                        break;
                    }
                }
                Reverse = false;
                Highlight = false;
                Shadow = false;
                TextWriter.WriteLine(msg);
            }
            finally
            {
                Style = oldStyle;
                ForeColor = oldColor;
            }
        }

        internal void Resize(int w, int h, int maxh)
        {
            Buffer = new Buffer(w, maxh);
            SetViewPort(0, 0, w, h);
        }

        void InitHandlers()
        {
            AddEditHandler(TerminalKeys.Enter | TerminalKeys.Control, CmdEnterData);
            AddEditHandler(TerminalKeys.Enter, CmdEnterDataOrCommand);
            AddEditHandler(TerminalKeys.Up, CmdHistoryPrev);
            AddEditHandler(TerminalKeys.Down, CmdHistoryNext);
            AddEditHandler(TerminalKeys.F1, CmdHelp);
            AddEditHandler(TerminalKeys.Tab, CmdTabCompletion);
        }

        void CmdHelp()
        {
            CmdMoveBackOverSpaces();
            var topic = GetWordFromBuffer(HomePos, Pos, EndPos, Runtime.IsLispWordChar);
            var helper = Symbols.HelpHook.Value as IApply;
            if (helper != null)
            {
                var sym = topic == "" ? null : Runtime.MakeSymbol(topic);
                Runtime.Funcall(helper, sym);
            }
        }

        void CmdEnterData()
        {
            CmdEnd();
            CmdDataChar('\n');
        }

        void CmdEnterDataOrCommand()
        {
            CmdEnd();
            Text = GetStringFromBuffer(HomePos, EndPos);
            if (RuntimeGfx.IsCompleteSourceCode(Text))
            {
                Done = true;
            }
            else
            {
                CmdDataChar('\n');
            }
        }

        void CmdHistoryNext()
        {
            if (RuntimeGfx.History != null)
            {
                Pos = HomePos;
                while (HomePos < EndPos)
                {
                    CmdDeleteChar();
                }
                InsertString(RuntimeGfx.History.Next());
            }
        }

        void CmdHistoryPrev()
        {
            if (RuntimeGfx.History != null)
            {
                Pos = HomePos;
                while (HomePos < EndPos)
                {
                    CmdDeleteChar();
                }
                InsertString(RuntimeGfx.History.Previous());
            }
        }

        void CmdTabCompletion()
        {
            CmdMoveBackOverSpaces();
            var searchTerm = this.GetWordFromBuffer(HomePos, Pos, Pos, Runtime.IsLispWordChar);
            var completions = RuntimeConsoleBase.GetCompletions(searchTerm);
            var index = 0;
            var done = false;
            var scrollProtection = false;

            SavedPos = Pos;

            while (!done)
            {               
                var choicePos = 0;
                Pos = EndPos;
                WriteLine();
                for (int i = 0; i < completions.Count; ++i)
                {
                    Write(completions[i]);
                    if (i == index)
                    {
                        choicePos = Pos;
                    }
                    Write(' ');
                    Write(' ');
                }
                ClearToBot();
                if (!scrollProtection)
                {
                    scrollProtection = true;
                    continue;
                }
                Pos = choicePos;
                ShowCursor();
                var info2 = ReadKey(false);
                var key2 = info2.KeyData;
                HideCursor();
                if (key2 == TerminalKeys.Enter || (key2 == TerminalKeys.Tab && completions.Count == 1))
                {
                    Pos = EndPos;
                    ClearToBot();
                    Pos = SavedPos;
                    var newPos = Pos - searchTerm.Length;
                    while (Pos != newPos)
                    {
                        CmdBackspace();
                    }
                    InsertString(completions[index] + " ");
                    done = true;
                }
                else if (key2 == TerminalKeys.Tab)
                {
                    index = (index + 1) % completions.Count;
                }
                else if (key2 == TerminalKeys.Escape)
                {
                    Pos = EndPos;
                    ClearToBot();
                    Pos = SavedPos;
                    done = true;
                }
            }

        }

    }

}