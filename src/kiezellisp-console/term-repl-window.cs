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
            AddEditHandler(TerminalKeys.Enter, CmdEnterDataOrCommand);
            AddEditHandler(TerminalKeys.Up, CmdHistoryPrev);
            AddEditHandler(TerminalKeys.Down, CmdHistoryNext);
            AddEditHandler(TerminalKeys.L | TerminalKeys.Alt, CmdLambda);
            AddEditHandler(TerminalKeys.F1, CmdHelp);
        }

        void CmdHelp()
        {
            var topic = GetWordFromBuffer(HomePos, Pos, EndPos, IsLispWordChar);
            var helper = Symbols.HelpHook.Value as IApply;
            if (helper != null)
            {
                var sym = topic == "" ? null : Runtime.MakeSymbol(topic);
                Runtime.Funcall(helper, sym);
                //RuntimeConsole.ClearKeyboardBuffer();
            }
        }

        void CmdEnterDataOrCommand()
        {
            CmdEnd();
            Text = GetStringFromBuffer(HomePos, EndPos);
            if (RuntimeConsole.IsCompleteSourceCode(Text))
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
            if (Terminal.History != null)
            {
                Pos = HomePos;
                while (HomePos < EndPos)
                {
                    CmdDeleteChar();
                }
                InsertString(Terminal.History.Next());
            }
        }

        void CmdHistoryPrev()
        {
            if (Terminal.History != null)
            {
                Pos = HomePos;
                while (HomePos < EndPos)
                {
                    CmdDeleteChar();
                }
                InsertString(Terminal.History.Previous());
            }
        }

        void CmdLambda()
        {
            InsertString(Runtime.LambdaCharacter);
        }

 
    }

}