// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Kiezel
{
    public partial class Runtime
    {
        [ThreadStatic]
        internal static ThreadContext _Context = null;

        internal static Dictionary<Type, List<Type>> AbstractTypes;
        internal static bool AdaptiveCompilation = true;
        internal static int CompilationThreshold = 100;
        internal static bool ConsoleMode;
        internal static bool EmbeddedMode;
        internal static bool DebugMode;
        internal static Readtable DefaultReadtable;
        internal static Dictionary<object, string> Documentation;
        internal static long GentempCounter = 0;
        internal static string HomeDirectory = Directory.GetCurrentDirectory();
        internal static bool InteractiveMode;
        internal static Package KeywordPackage;
        internal static char LambdaCharacter = ( char ) 0x3bb;
        internal static Package LispDocPackage;
        internal static Package LispPackage;
        internal static bool ListenerEnabled;
        internal static Package MathPackage;
        internal static bool OptimizerEnabled;
        internal static bool ReadDecimalNumbers;
        internal static bool SetupMode;
        internal static Stopwatch StopWatch = Stopwatch.StartNew();
        internal static Package SystemPackage;
        internal static Package TempPackage;
        internal static bool UnicodeOutputEnable = true;
        internal static Cons UserArguments;
        internal static Package UserPackage;
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

        [Lisp( "set-read-decimal-numbers" )]
        public static void SetReadDecimalNumbers( bool flag )
        {
            ReadDecimalNumbers = flag;
        }

        internal static void AbortListeners()
        {
            if ( ListenerEnabled )
            {
                AbortCommandListener();
            }
        }

        internal static void AddFeature( string name )
        {
            var key = MakeSymbol( name, KeywordPackage );
            Symbols.Features.Value = MakeCons( key, ( Cons ) Symbols.Features.Value );
        }

        internal static string GetApplicationInitFile()
        {
            // application config file is same folder as kiezellisp-lib.dll
            var assembly = Assembly.GetExecutingAssembly();
            var root = assembly.Location;
            var dir = Path.GetDirectoryName( root );
            return PathExtensions.Combine( dir, "kiezellisp-init" );
        }

        internal static bool HasFeature( string feature )
        {
            var list = ( Cons ) Symbols.Features.Value;
            var result = SeqBase.FindItem( list, feature, Eql, SymbolName, null );
            return result.Item2 != null;
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
                typeof(Symbol),typeof(KeywordClass),null,
                typeof(Atom),typeof(Symbol),typeof(KeywordClass),typeof(ValueType),typeof(string),typeof(Number),
                        typeof(Complex), typeof(Integer), typeof(BigInteger), typeof(Numerics.BigRational), typeof(Rational),null,
                null
            };

            AbstractTypes = new Dictionary<Type, List<Type>>();

            Type key = null;
            List<Type> subtypes = null;

            foreach ( var t in types )
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

        internal static void RestartBuiltins( Type type )
        {
            var flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public;
            var names = type.GetMethods( flags ).Select( x => x.Name ).Distinct();

            foreach ( var name in names )
            {
                var members = type.GetMember( name, flags ).Select( x => x as MethodInfo ).Where( x => x != null ).ToArray();
                var attrs = members[ 0 ].GetCustomAttributes( typeof( LispAttribute ), false );

                if ( attrs.Length != 0 )
                {
                    var pure = members[ 0 ].GetCustomAttributes( typeof( PureAttribute ), false ).Length != 0;
                    var builtin = new ImportedFunction( name, type, members, pure );

                    foreach ( string symbolName in ( ( LispAttribute ) attrs[ 0 ] ).Names )
                    {
                        var sym = FindSymbol( symbolName, creating: true );
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

        internal static void RestartListeners()
        {
            if ( ListenerEnabled )
            {
                CreateCommandListener( Convert.ToInt32( Symbols.ReplListenerPort.Value ) );
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

        internal static void RestartSettings()
        {
            if ( Environment.OSVersion.Platform == PlatformID.Win32NT )
            {
                AddFeature( "windows" );
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
            AddFeature( "clr40" );
#endif

#if CLR45
            AddFeature( "clr45");
#endif

            AddFeature( "kiezellisp" );
            AddFeature( ConsoleMode ? "console-mode" : "graphical-mode" );

            Symbols.Features.VariableValue = AsList( Sort( ( Cons ) Symbols.Features.Value ) );
        }

        internal static void RestartSymbols()
        {
            // these two do not use lisp package
            KeywordPackage = MakePackage( "keyword", false );
            TempPackage = MakePackage( "temp", true );

            LispPackage = MakePackage( "lisp", true );

            UserPackage = MakePackage( "user", true );
            LispDocPackage = MakePackage( "example", true );
            SystemPackage = MakePackage( "system", true );
            MathPackage = MakePackage( "math", true );

            Symbols.Create();

            // standard set of variables
            Symbols.CommandLineArguments.ReadonlyValue = UserArguments;
            Symbols.DebugMode.ConstantValue = DebugMode;
            Symbols.E.ConstantValue = Math.E;
            Symbols.EnableWarnings.VariableValue = true;
            Symbols.Exception.ReadonlyValue = null;
            Symbols.Features.VariableValue = null;
            Symbols.HelpHook.VariableValue = null;
            Symbols.I.ConstantValue = Complex.ImaginaryOne;
            Symbols.InteractiveMode.ConstantValue = InteractiveMode;
            Symbols.It.VariableValue = null;
            Symbols.LazyImport.VariableValue = true;
            Symbols.LoadPath.ReadonlyValue = null;
            Symbols.LoadPrint.VariableValue = false;
            Symbols.LoadVerbose.VariableValue = false;
            Symbols.Modules.ReadonlyValue = null;
            Symbols.Package.VariableValue = LispPackage;
            Symbols.PackageNamePrefix.VariableValue = null;
            Symbols.PI.ConstantValue = Math.PI;
            Symbols.PrettyPrintHook.VariableValue = null;
            Symbols.PrintBackgroundColor.Value = null;
            Symbols.PrintBase.VariableValue = 10;
            Symbols.PrintColor.Value = null;
            Symbols.PrintCompact.Value = true;
            Symbols.PrintEscape.VariableValue = true;
            Symbols.PrintForce.VariableValue = true;
            Symbols.PrintShortSymbolNames.VariableValue = false;
            Symbols.ReadEval.VariableValue = null;
            Symbols.Readtable.VariableValue = GetStandardReadtable();
            Symbols.Recur.ReadonlyValue = null;
            Symbols.ReplListenerPort.VariableValue = 8080;
            Symbols.ScriptDirectory.ReadonlyValue = NormalizePath( HomeDirectory );
            Symbols.ScriptName.ReadonlyValue = null;
            Symbols.StdErr.VariableValue = true;
            Symbols.StdIn.VariableValue = Console.In;
            Symbols.StdLog.VariableValue = true;
            Symbols.StdOut.VariableValue = true;
            Symbols.StandoutColor.Value = Console.BackgroundColor;
            Symbols.StandoutBackgroundColor.Value = Console.ForegroundColor;
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
            PackagesByType = new Dictionary<Type, Package>();
            Types = new Dictionary<Symbol, object>();
            
            InitRandom();

            DefaultReadtable = GetStandardReadtable();
        }
    }
}