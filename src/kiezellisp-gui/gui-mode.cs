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

    public partial class RuntimeGui
    {
        public static void RunGuiMode(CommandLineOptions options)
        {
            Runtime.ProgramFeature = "kiezellisp-gui";
            Runtime.DebugMode = options.Debug;
            Runtime.Repl = false;
            Runtime.OptimizerEnabled = !Runtime.DebugMode;
            Runtime.ScriptName = options.ScriptName;
            Runtime.UserArguments = options.UserArguments;

            try
            {
                Runtime.Reset();
                Runtime.RestartLoadFiles(0);
                Runtime.Run(options.ScriptName, Symbols.LoadPrintKeyword, false, Symbols.LoadVerboseKeyword, false);
                Runtime.Exit();
            }
            catch (Exception ex)
            {
                Runtime.PrintTrace(Runtime.GetDiagnostics(ex));
            }

        }

    }
}