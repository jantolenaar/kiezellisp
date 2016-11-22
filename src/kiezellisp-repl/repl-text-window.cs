#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System.Windows.Forms;

    public class ReplTextWindow : TextWindow
    {
        #region Constructors

        internal ReplTextWindow(TextControl parent, TextWindowCreateArgs args)
            : base(parent, args)
        {
            InitReplEditHandlers();
        }

        #endregion Constructors

        #region Private Methods

        void CmdEnterData()
        {
            CmdEnd();
            CmdDataChar('\n');
        }

        void CmdEnterDataOrCommand()
        {
            CmdEnd();
            if (RuntimeConsoleBase.IsCompleteSourceCode(lineBuffer.ToString()))
            {
                done = true;
            }
            else {
                CmdDataChar('\n');
            }
        }

        void CmdHelp()
        {
            var line = lineBuffer.ToString();
            var loc = Runtime.LocateLeftWord(line, lineIndex);
            var topic = line.Substring(loc.Begin, loc.Span);
            var helper = Symbols.HelpHook.Value as IApply;
            if (helper != null)
            {
                var sym = topic == "" ? null : Runtime.MakeSymbol(topic);
                Runtime.Funcall(helper, sym);
            }
        }

        void CmdHistoryNext()
        {
            if (RuntimeConsoleBase.History != null)
            {
                lineBuffer.Clear();
                lineIndex = 0;
                InsertString(RuntimeConsoleBase.History.Next());
            }
        }

        void CmdHistoryPrev()
        {
            if (RuntimeConsoleBase.History != null)
            {
                lineBuffer.Clear();
                lineIndex = 0;
                InsertString(RuntimeConsoleBase.History.Previous());
            }
        }

        void InitReplEditHandlers()
        {
            AddEditHandler(Keys.L | Keys.Control, CmdClearScreen);
            AddEditHandler(Keys.Enter | Keys.Control, CmdEnterData);
            AddEditHandler(Keys.Enter, CmdEnterDataOrCommand);
            AddEditHandler(Keys.Up, CmdHistoryPrev);
            AddEditHandler(Keys.Down, CmdHistoryNext);
            AddEditHandler(Keys.F1, CmdHelp);
        }

        #endregion Private Methods
    }
}