#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;

    public static class EmbeddedMode
    {
        #region Public Methods

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

        #endregion Public Methods
    }
}