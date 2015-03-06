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

        public static void Init( bool consoleMode=true, bool debugMode=false )
        {
            Runtime.EmbeddedMode = true;
            Runtime.DebugMode = debugMode;
            Runtime.ConsoleMode = consoleMode;
            Runtime.InteractiveMode = false;
            Runtime.OptimizerEnabled = !Runtime.DebugMode;
            Runtime.ListenerEnabled = false;

            Runtime.Reset( false );
        }

        public static void Load( string filename )
        {
            Runtime.TryLoad( filename, false, false, false );
        }

        public static object Eval( string statement )
        {
            var code = Runtime.ReadFromString( statement );
            var result = Runtime.Eval( code );
            return result;
        }

        public static string GetDiagnostics( Exception ex )
        {
            return Runtime.GetDiagnostics( ex );
        }

        public static object Funcall( string functionName, params object[] args )
        {
            var sym = Runtime.FindSymbol( functionName );
            return Runtime.Apply( sym, args );
        }
    }

}