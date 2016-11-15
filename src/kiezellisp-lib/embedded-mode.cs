#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    public class EmbeddedMode
    {
        #region Methods

        public static object Funcall(string functionName, params object[] args)
        {
            var sym = Runtime.FindSymbol(functionName);
            return Runtime.Apply(sym, args);
        }

        public static string GetDiagnostics(Exception ex)
        {
            return Runtime.GetDiagnostics(ex);
        }

        public static void Init(bool debugMode = false)
        {
            Runtime.ProgramFeature = "kiezellisp-lib";
            Runtime.DebugMode = debugMode;
            Runtime.Repl = false;
            Runtime.OptimizerEnabled = !Runtime.DebugMode;
            Runtime.Reset();
            Runtime.RestartLoadFiles(0);
        }

        #endregion Methods
    }
}