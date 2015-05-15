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

    public class FormsMode
    {
        public static void Run( string[] args )
        {
            Runtime.RunFormsMode( args );
        }
    }

    public partial class Runtime
    {
        public static void RunFormsMode( string[] args )
        {
            CommandLineParser parser = new CommandLineParser();

            parser.AddOption( "-c", "--command code" );
            parser.AddOption( "-d", "--debug" );
            parser.AddOption( "-n", "--nodebug" );
            parser.AddOption( "-o", "--optimize" );

            parser.Parse( args );

            ConsoleMode = false;
            InteractiveMode = false;
            ListenerEnabled = false;

            string expr1 = parser.GetOption( "c" );
            UserArguments = AsList( parser.GetArgumentArray( 0 ) );

            if ( expr1 == null )
            {
                throw new LispException( "Must use --command option when running in windows mode" );
            }

            try
            {
                DebugMode = parser.GetOption( "d" ) == null;
                OptimizerEnabled = !DebugMode;
                Reset( false );
                TryLoadText( expr1, null, null, false, false, false );
            }
            catch ( Exception ex )
            {
                PrintLog( GetDiagnostics( ex ) );
            }

        }

    }
}