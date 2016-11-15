#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Windows.Forms;

    using ThreadFunc = System.Func<object>;

    [RestrictedImport]
    public partial class RuntimeRepl : RuntimeConsoleBase
    {
        #region Methods

        public static void Reset(int level)
        {
            Runtime.Reset();
            Runtime.RestartBuiltins(typeof(RuntimeRepl));
            Symbols.StdScr.VariableValue = StdScr;
            Symbols.StdOut.VariableValue = StdScr;
            Symbols.StdLog.VariableValue = StdScr;
            Symbols.StdErr.VariableValue = StdScr;
            Symbols.StdIn.VariableValue = StdScr;
            Runtime.RestartLoadFiles(level);
        }

        [Lisp("terminal-stream?")]
        public static bool TerminalStreamp(object stream)
        {
            return stream is TextWindow || stream is TextWindowTextWriter;
        }

        [STAThread]
        static void Main(string[] args)
        {
            ResetRuntimeFunctionImp = Reset;
            ReadFunctionImp = ReplRead;
            ResetDisplayFunctionImp = ReplResetDisplay;

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            try
            {
                var options = ParseArgs(args);
                Init(options);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "EXCEPTION", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion Methods
    }
}