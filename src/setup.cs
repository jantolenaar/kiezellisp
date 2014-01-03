// Copyright (C) 2012-2014 Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Numerics;

namespace Kiezel
{
    public partial class Runtime
    {
        internal static int CompilationThreshold = 100;
        internal static bool AdaptiveCompilation = true;

        internal static bool ReadDecimalNumbers;
        internal static bool ConsoleMode;
        internal static bool DebugMode;
        internal static bool OptimizerEnabled;
        internal static bool SetupMode;
        internal static bool InteractiveMode;
        internal static bool UnicodeOutputEnable = true;
        internal static char LambdaCharacter = (char)0x3bb;
        internal static Cons UserArguments;
        internal static Package LispPackage;
        internal static Package UserPackage;
        internal static Package KeywordPackage;
        internal static Package LispDocPackage;
        internal static Package SystemPackage;
        internal static Package MathPackage;
        internal static Dictionary<object, string> Documentation;
        internal static string HomeDirectory = Directory.GetCurrentDirectory();
        internal static long GentempCounter = 0;
        internal static Dictionary<Type, List<Type>> AbstractTypes;

        internal static Stopwatch StopWatch = Stopwatch.StartNew();

        [ThreadStatic]
        internal static ThreadContext _Context = null;

        internal static ThreadContext CurrentThreadContext
        {
            get
            {
                if ( _Context == null )
                {
                    _Context = new ThreadContext( null );
                }
                return _Context;
            }
            set
            {
                _Context = value;
            }
        }

        internal static void Reset( bool loadDebug )
        {
            SetupMode = true;

            InitAbstractTypes();

            RestartVariables();
            RestartDependencies();
            RestartBinders();
            RestartSymbols();
            RestartCompiler();
            RestartSettings();
            RestartBuiltins( typeof( Runtime ) );

            SetupMode = false;

            RestartLoadFiles( loadDebug );
        }

        internal static void RestartVariables()
        {
            ReadDecimalNumbers = true;
            GentempCounter = 0;
            LispPackage = null;
            KeywordPackage = null;
            UserPackage = null;
            Documentation = new Dictionary<object, string>();
            Packages = new Dictionary<string, Package>();
            Types = new Dictionary<Symbol, object>();
        }

        internal static void InitAbstractTypes()
        {
            var types = new Type[]
            {
                typeof(Number), typeof(Complex), typeof(Integer), typeof(BigInteger), typeof(Numerics.BigRational), 
                        typeof(Rational), typeof(decimal), typeof(double), typeof(long), typeof(int), null,
                typeof(Rational), typeof(Numerics.BigRational), typeof(Integer), typeof(BigInteger), typeof(long), typeof(int), null,
                typeof(Integer), typeof(BigInteger), typeof(long), typeof(int), null,
                typeof(List), typeof(Cons),null,
                typeof(Sequence), typeof( List), typeof(Vector), null,
                typeof(Enumerable), typeof(IEnumerable),null,
                typeof(Symbol),typeof(Keyword),null,
                typeof(Atom),typeof(Symbol),typeof(Keyword),typeof(ValueType),typeof(string),typeof(Number),
                        typeof(Complex), typeof(Integer), typeof(BigInteger), typeof(Numerics.BigRational), typeof(Rational),null,
                null
            };

            AbstractTypes = new Dictionary<Type, List<Type>>();

            Type key = null;
            List<Type> subtypes = null;

            foreach (var t in types)
            {
                if ( key == null )
                {
                    key = t;
                    subtypes = new List<Type>();
                }
                else if ( t == null )
                {
                    AbstractTypes[ key ] = subtypes;
                    key = null;
                }
                else
                {
                    subtypes.Add( t );
                }
            }
        }

        internal static void RestartSymbols()
        {
            KeywordPackage = MakePackage( "keyword", false );
            LispPackage = MakePackage( "lisp", true );
            UserPackage = MakePackage( "user", true );
            LispDocPackage = MakePackage( "example", true );
            SystemPackage = MakePackage( "system", true );
            MathPackage = MakePackage( "math", true );

            Symbols.Create();

            // standard set of variables
            Symbols.Recur.ReadonlyValue = null;
            Symbols.ReadEval.VariableValue = null;
            Symbols.ReadSuppress.ReadonlyValue = false;
            Symbols.Features.VariableValue = null;
            Symbols.LoadPath.ReadonlyValue = null;
            Symbols.ScriptDirectory.ReadonlyValue = NormalizePath( HomeDirectory );
            Symbols.CommandLineArguments.ReadonlyValue = UserArguments;
            Symbols.ScriptName.ReadonlyValue = null;
            Symbols.StdIn.VariableValue = Console.In;
            Symbols.StdOut.VariableValue = true;
            Symbols.StdErr.VariableValue = true;
            Symbols.StdLog.VariableValue = true;
            Symbols.Package.VariableValue = LispPackage;
            Symbols.PrintForce.VariableValue = true;
            Symbols.PrintEscape.VariableValue = true;
            Symbols.PrintBase.VariableValue = 10;
            Symbols.PrintMaxElements.VariableValue = 50;
            Symbols.PrintShortSymbolNames.VariableValue = false;
            Symbols.It.VariableValue = null;
            Symbols.HelpHook.VariableValue = null;
            Symbols.PrettyPrintHook.VariableValue = null;
            Symbols.ReplListenerPort.VariableValue = 8080;
            Symbols.Modules.ReadonlyValue = null;
            Symbols.LoadVerbose.VariableValue = false;
            Symbols.LoadPrint.VariableValue = false;
            Symbols.DebugMode.ConstantValue = DebugMode;
            Symbols.InteractiveMode.ConstantValue = InteractiveMode;
            Symbols.EnableWarnings.VariableValue = true;
            Symbols.Exception.ReadonlyValue = null;
            Symbols.PrintCompact.Value = false;
            Symbols.PrintColor.Value = null;
            Symbols.PrintBackgroundColor.Value = null;
            Symbols.Strict.Value = false;
            Symbols.I.ConstantValue = Complex.ImaginaryOne;
            Symbols.E.ConstantValue = Math.E;
            Symbols.PI.ConstantValue = Math.PI;
            Symbols.QuickImport.VariableValue = true;

#if KIEZELLISPW
            Symbols.StandoutColor.Value = null;
            Symbols.StandoutBackgroundColor.Value = null;
#else
            Symbols.StandoutColor.Value = Console.BackgroundColor;
            Symbols.StandoutBackgroundColor.Value = Console.ForegroundColor;
#endif

        }

      
        internal static void RestartSettings()
        {
            if ( Environment.OSVersion.Platform == PlatformID.Win32NT )
            {
                AddFeature( "windows-nt" );
            }
            else if ( Environment.OSVersion.Platform == PlatformID.Unix )
            {
                // osx too?
                AddFeature( "unix" );
            }
            else if ( Environment.OSVersion.Platform == PlatformID.MacOSX )
            {
                AddFeature( "macosx" );
            }

            if ( Type.GetType( "Mono.Runtime" ) != null )
            {
                AddFeature( "mono" );
            }

            if ( Environment.Is64BitProcess )
            {
                AddFeature( "x64" );
            }
            else
            {
                AddFeature( "x32" );
                AddFeature( "x86" );
            }

#if CLR40
            AddFeature( "clr40");
#endif

#if CLR45
            AddFeature( "clr45");
#endif

            AddFeature( "kiezellisp" );

#if KIEZELLISPW
            AddFeature( "windows-mode" );
#else
            AddFeature( "console-mode" );
#endif

            Symbols.Features.VariableValue = AsList( Sort( ( Cons ) Symbols.Features.Value ) );
        }

        [Lisp( "exit" )]
        public static void Exit()
        {
            Environment.Exit( 0 );
        }

        [Lisp( "exit" )]
        public static void Exit( int code )
        {
            Environment.Exit( code );
        }

        internal static object RestartDependencies()
        {
            // Make sure dlls are loaded when Kiezellisp starts.
            var temp = new object[]
            {
                new System.Data.Common.DataColumnMapping(),
                new System.Data.DataTable(),
                new System.Runtime.CompilerServices.ExtensionAttribute()
            };

            return temp;
        }

        internal static void RestartBuiltins( Type type )
        {
            var flags =  BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public;
            var names = type.GetMethods( flags ).Select( x => x.Name ).Distinct();

            foreach ( var name in names )
            {
                var members = type.GetMember( name, flags ).Select( x => x as MethodInfo ).Where( x => x != null ).ToArray();
                var attrs = members[0].GetCustomAttributes( typeof( LispAttribute ), false );

                if ( attrs.Length != 0 )
                {
                    var pure = members[ 0 ].GetCustomAttributes( typeof( PureAttribute ), false ).Length != 0;
                    var builtin = new ImportedFunction( members, pure );

                    foreach ( string symbolName in ( ( LispAttribute ) attrs[ 0 ] ).Names )
                    {
                        var sym = ( symbolName == "." ) ? Symbols.Accessor : FindSymbol( symbolName, true );
                        if ( !sym.IsUndefined || sym.SpecialFormValue != null )
                        {
                            PrintWarning( "Duplicate builtin name: ", sym.Name );
                        }
                        sym.FunctionValue = builtin;
                        sym.Package.Export( sym.Name );
                    }
                }
            }
        }

        internal static void RestartLoadFiles( bool loadDebug )
        {
            SetLoadPath( "." );

            Symbols.Package.Value = UserPackage;

            string path = GetApplicationInitFile();

            if ( path != null )
            {
                TryLoad( path, loadDebug, false, false );
            }
        }

        internal static void RestartListeners()
        {
            if ( InteractiveMode )
            {
                CreateCommandListener( Convert.ToInt32( Symbols.ReplListenerPort.Value ) );
            }
        }

        internal static void AbortListeners()
        {
            if ( InteractiveMode )
            {
                AbortCommandListener();
            }
        }

        internal static string GetUserFile( string name )
        {
            var root = System.Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData );
            return PathExtensions.Combine( PathExtensions.Combine( root, "kiezellisp" ), name );
        }

        internal static string GetApplicationInitFile()
        {
            // application config file is same folder as kiezellisp.exe
            var assembly = Assembly.GetExecutingAssembly();
            var root = assembly.Location;
            var dir = Path.GetDirectoryName( root );
            var file = Path.GetFileNameWithoutExtension( root );
            return PathExtensions.Combine( dir, file + "-init" );
        }

        internal static bool HasFeature( string feature )
        {
            var list = ( Cons ) Symbols.Features.Value;
            var result = FindItem( list, feature, Eql, SymbolName, null  );
            return result.Item2 != null;
        }

        internal static void AddFeature( string name )
        {
            var key = MakeSymbol( name, KeywordPackage );
            Symbols.Features.Value = MakeCons( key, ( Cons ) Symbols.Features.Value );
        }

        [Lisp( "set-read-decimal-numbers" )]
        public static void SetReadDecimalNumbers( bool flag )
        {
            ReadDecimalNumbers = flag;
        }
	}
}

