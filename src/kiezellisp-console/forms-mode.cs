// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Kiezel
{

    public partial class RuntimeConsole
    {
        public static void RunFormsMode(CommandLineOptions options)
        {
            Runtime.ConsoleMode = false;
            Runtime.InteractiveMode = false;
            Runtime.DebugMode = options.Debug;
            Runtime.OptimizerEnabled = !Runtime.DebugMode;
            Runtime.ScriptName = options.ScriptName;
            Runtime.UserArguments = options.UserArguments;

            try
            {
                Reset(0);
                Runtime.Run(options.ScriptName, Symbols.LoadPrintKeyword, false, Symbols.LoadVerboseKeyword, false);
                Runtime.Exit();
            }
            catch (Exception ex)
            {
                Runtime.PrintLog(Runtime.GetDiagnostics(ex));
            }

        }

    }
}