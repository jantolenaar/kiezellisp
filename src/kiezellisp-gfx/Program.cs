// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

[assembly: CLSCompliant(false)]

namespace Kiezel
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            try
            {
                var options = RuntimeGfx.ParseArgs(args);

                if (options.Gui)
                {
                    RuntimeGfx.RunGuiMode(options);
                }
                else
                {
                    TerminalMainProgram main = 
                        () =>
                        {
                            RuntimeGfx.ProcessEvents();
                            RuntimeGfx.RunGuiReplMode(options);
                        };
                    Terminal.Init(options, main);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "EXCEPTION", MessageBoxButtons.OK, MessageBoxIcon.Error);                
            }
        }
    }

    public partial class RuntimeGfx
    {
        public static void Reset(int level)
        {
            Runtime.Reset();
            Runtime.RestartBuiltins(typeof(RuntimeGfx));
            Symbols.StdScr.ConstantValue = Terminal.StdScr;
            Symbols.StdOut.ConstantValue = Terminal.StdScr;
            Symbols.StdErr.VariableValue = Terminal.StdScr;
            Symbols.StdIn.ConstantValue = Terminal.StdScr;
            Runtime.RestartLoadFiles(level);
        }

        [Lisp("terminal-stream?")]
        public static bool TerminalStreamp(object stream)
        {
            return stream is Window || stream is WindowTextWriter;
        }
    }

}
