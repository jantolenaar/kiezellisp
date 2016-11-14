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
    public class EmbeddedMode
    {

        public static void Init(bool debugMode = false)
        {
            Runtime.ProgramFeature = "kiezellisp-lib";
            Runtime.DebugMode = debugMode;
            Runtime.Repl = false;
            Runtime.OptimizerEnabled = !Runtime.DebugMode;
            Runtime.Reset();
            Runtime.RestartLoadFiles(0);
        }

        public static string GetDiagnostics(Exception ex)
        {
            return Runtime.GetDiagnostics(ex);
        }

        public static object Funcall(string functionName, params object[] args)
        {
            var sym = Runtime.FindSymbol(functionName);
            return Runtime.Apply(sym, args);
        }
    }

}