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

    public partial class RuntimeGfx
    {
        public static void RunGuiMode(CommandLineOptions options)
        {
            Runtime.ConsoleMode = false;
            Runtime.GraphicalMode = true;
            Runtime.EmbeddedMode = false;
            Runtime.DebugMode = options.Debug;
            Runtime.Repl = options.Repl;
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