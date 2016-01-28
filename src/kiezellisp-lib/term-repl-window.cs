using System;
using System.Windows.Forms;
using System.Drawing;

namespace Kiezel
{
    public class ReplWindow: Window
    {
        protected ReplWindow(Buffer pad,int w,int h)
            : base(0,0,pad,0,0,w,h)
        {
        }

        internal static ReplWindow CreateReplWindow(int w,int h,int maxh)
        {
            var pad = new Buffer(w, maxh);
            var win = new ReplWindow(pad,w,h);
            win.ScrollOk(true);
            win.ScrollPadOk(true);
            win.InitHandlers();
            return win;
        }

        internal void Resize(int w,int h,int maxh)
        {
            Buffer = new Buffer(w, maxh);
            SetViewPort(0, 0, w, h);
        }

        void InitHandlers()
        {
            AddEditHandler(Keys.Enter, CmdEnterDataOrCommand);
            AddEditHandler(Keys.Up, CmdHistoryPrev);
            AddEditHandler(Keys.Down, CmdHistoryNext);
            AddEditHandler(Keys.Tab, CmdTabCompletion);
            AddEditHandler(Keys.L | Keys.Alt, CmdLambda);
            AddEditHandler(Keys.F1, CmdHelp);
        }

        void CmdHelp()
        {
            var topic = ExtractTerm(this.GetStringFromBuffer(HomePos, Pos));
            var helper = Symbols.HelpHook.Value as IApply;
            if (topic != "" && helper != null)
            {
                Runtime.Funcall(helper, Runtime.MakeSymbol(topic));
                Terminal.TerminalWindow.ClearKeyboardBuffer();
            }
        }

        void CmdEnterDataOrCommand()
        {
            CmdEnd();
            Text = GetStringFromBuffer(HomePos, EndPos);
            if (Runtime.IsCompleteSourceCode(Text))
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
                while (HomePos<EndPos)
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
                while (HomePos<EndPos)
                {
                    CmdDeleteChar();
                }
                InsertString(Terminal.History.Previous());
            }
        }

        void CmdTabCompletion()
        {
            var text = this.GetStringFromBuffer(HomePos, Pos);
            if (text != "")
            {
                text = SelectCompletion(text);
                if (text != null)
                {
                    InsertString(text);
                }
            }
        }

        void CmdLambda()
        {
            InsertString(Runtime.LambdaCharacter);
        }

        string ExtractTerm(string text)
        {
            var start = text.Length-1;

            while ( start > 0 )
            {
                var ch = text[ start - 1 ];
                if ( !Runtime.IsWordChar( ch ) )
                {
                    if (ch == '\\' && start > 1 && text[start - 2] == '#')
                    {
                        start -= 2;
                    }
                    break;
                }
                --start;
            }

            var prefix = text.Substring( start );
            return prefix;

        }

        string SelectCompletion(string text)
        {
            var prefix = ExtractTerm(text);
            var completions = Runtime.GetCompletions(prefix);

            if (completions.Count == 0)
            {
                return null;
            }

            if (completions.Count == 1)
            {
                return completions[0].Substring(prefix.Length);
            }

            var x = Col;
            var y = Row;
            var w = 40;
            var h = 7;
            if ( x+w > Width)
            {
                x = Width-w;
            }
            if ( y+h > Height)
            {
                y = Height-h;
            }
            using (var box = Terminal.MakeBoxWindow(x,y,w,h))
            {
                var choice = Terminal.RunMenu(box, completions);
                if (choice != -1)
                {
                    return completions[choice].Substring(prefix.Length);
                }
            }

            return null;
        }

  
    }

}