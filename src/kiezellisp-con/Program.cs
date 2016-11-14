// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Globalization;
using System.Threading;

[assembly: CLSCompliant(false)]

namespace Kiezel
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            RuntimeConsole.ResetRuntimeFunctionImp = RuntimeConsole.Reset;
            RuntimeConsole.ResetDisplayFunctionImp = RuntimeConsole.ReplResetDisplay;
            RuntimeConsole.ReadFunctionImp = RuntimeConsole.ReplRead;

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var options = RuntimeConsole.ParseArgs(args);

            RuntimeConsole.RunConsoleMode(options);
        }
    }

    public partial class RuntimeConsole
    {
        public static void Reset(int level)
        {
            Runtime.Reset();
            Runtime.RestartBuiltins(typeof(RuntimeConsole));
            Symbols.StdScr.ConstantValue = null;
            Symbols.StdErr.VariableValue = Console.Out;
            Symbols.StdLog.VariableValue = Console.Out;
            Symbols.StdOut.VariableValue = Console.Out;
            Symbols.StdIn.VariableValue = Console.In;
            Runtime.RestartLoadFiles(level);
        }

    }

}
