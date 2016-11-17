#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
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

        #region Methods

        void CmdEnterData()
        {
            CmdEnd();
            CmdDataChar('\n');
        }

        void CmdEnterDataOrCommand()
        {
            CmdEnd();
            Text = LineBuffer.ToString();
            if (RuntimeRepl.IsCompleteSourceCode(Text))
            {
                Done = true;
            }
            else
            {
                CmdDataChar('\n');
            }
        }

        void CmdHelp()
        {
            CmdMoveBackOverSpaces();
            var topic = Runtime.GetWordFromString(LineBuffer.ToString(), LineIndex, Runtime.IsLispWordChar);
            var helper = Symbols.HelpHook.Value as IApply;
            if (helper != null)
            {
                var sym = topic == "" ? null : Runtime.MakeSymbol(topic);
                Runtime.Funcall(helper, sym);
            }
        }

        void CmdHistoryNext()
        {
            if (RuntimeRepl.History != null)
            {
                LineBuffer.Clear();
                LineIndex = 0;
                InsertString(RuntimeRepl.History.Next());
            }
        }

        void CmdHistoryPrev()
        {
            if (RuntimeRepl.History != null)
            {
                LineBuffer.Clear();
                LineIndex = 0;
                InsertString(RuntimeRepl.History.Previous());
            }
        }

        void InitReplEditHandlers()
        {
            AddEditHandler(Keys.Enter | Keys.Control, CmdEnterData);
            AddEditHandler(Keys.Enter, CmdEnterDataOrCommand);
            AddEditHandler(Keys.Up, CmdHistoryPrev);
            AddEditHandler(Keys.Down, CmdHistoryNext);
            AddEditHandler(Keys.F1, CmdHelp);
        }

        #endregion Methods
    }
}