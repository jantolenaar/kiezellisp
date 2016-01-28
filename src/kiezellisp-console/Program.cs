// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Globalization;
using System.Threading;

namespace Kiezel
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var options = RuntimeConsole.ParseArgs(args);

            if (options.HasGui)
            {
                RuntimeConsole.RunFormsMode(options);
            }
            else
            {
                TerminalMainProgram main = 
                    () =>
                    {
                        RuntimeConsole.ProcessEvents();
                        RuntimeConsole.RunConsoleMode(options);
                    };
                Terminal.Init(options, main);
            }
        }
    }

    public partial class RuntimeConsole
    {
        public static void Reset(int level)
        {
            Runtime.Reset();
            Runtime.RestartBuiltins(typeof(RuntimeConsole));
            Symbols.StdScr.ConstantValue = Terminal.StdScr;
            Symbols.StdOut.ConstantValue = Terminal.StdScr;
            Symbols.StdErr.VariableValue = Terminal.StdScr;
            Symbols.StdIn.ConstantValue = null;
            Runtime.RestartLoadFiles(level);
        }

        [Lisp("terminal-stream?")]
        public static bool TerminalStreamp(object stream)
        {
            return stream is Window || stream is WindowTextWriter;
        }
    }

}
