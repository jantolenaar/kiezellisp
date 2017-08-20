#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Threading;

    class Program
    {
        #region Private Methods

        [STAThread]
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var options = RuntimeConsole.ParseArgs(args);

            //
            // Mono5 option is a workaround for a performance problem in the compilation of
            // linq expressions (https://bugzilla.xamarin.com/show_bug.cgi?id=56240).
            // 
            // The problem disappears when not running on the primary thread. Therefore this
            // workaraound is not a solution for gui programs.
            //

            if (options.Mono5)
            {
                var t = new Thread(() => RuntimeConsole.RunConsoleMode(options));
                t.Start();
                t.Join();
            }
            else
            {
                RuntimeConsole.RunConsoleMode(options);
            }
        }

        #endregion Private Methods
    }

    public partial class RuntimeConsole
    {
        #region Public Methods

        public static void Reset(int level)
        {
            Runtime.Reset();
            Runtime.RestartBuiltins(typeof(RuntimeConsole));
            Symbols.StdScr.ConstantValue = null;
            Symbols.StdErr.VariableValue = Console.Out;
            Symbols.StdLog.VariableValue = Console.Out;
            Symbols.StdOut.VariableValue = Console.Out;
            Symbols.StdIn.VariableValue = Console.In;
            Runtime.SetDebugLevel(level);
            Runtime.RestartLoadFiles(level);
            if (level != -1)
            {
                // Could be changed by action in loaded files.
                Runtime.SetDebugLevel(level);
            }
        }

        #endregion Public Methods
    }
}