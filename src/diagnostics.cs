// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Numerics;
using System.Reflection;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Kiezel
{
  
    public partial class Runtime
    {
        public static object GetColor( object color )
        {
            if ( color is ConsoleColor )
            {
                return color;
            }

            var colorName = GetDesignatedString( color );

            foreach ( var f in typeof( ConsoleColor ).GetFields() )
            {
                if ( f.Name.LispName() == colorName )
                {
                    return f.GetValue( null );
                }
            }
            return null;
        }

        [Lisp( "print-warning" )]
        public static void PrintWarning( params object[] args )
        {
            if ( DebugMode && ToBool( GetDynamic( Symbols.EnableWarnings ) ) )
            {
                var text = ";;; Warning: " + MakeString( args );
                PrintLogColor( GetDynamic( Symbols.StandoutColor ), GetDynamic( Symbols.StandoutBackgroundColor ), text );
            }
        }

        [Lisp( "throw-error" )]
        public static void ThrowError( params object[] args )
        {
            var text = MakeString( args );
            throw new LispException( text );
        }

        [Lisp( "get-description" )]
        public static Prototype GetDescription( object obj )
        {
            return GetDescription( obj, false );
        }

        [Lisp( "get-description" )]
        public static Prototype GetDescription( object obj, bool showNonPublic )
        {
            var result = new Prototype();
            var z = result.Dict;
            object a = obj;
            var sym = a as Symbol;
            var isVariable = false;

            if ( sym != null )
            {
                z[ "symbol" ] = sym;
                z[ "name" ] = sym.Name;
                z[ "package" ] = sym.Package == null ? null : sym.Package.Name;

                a = sym.Value;

                switch ( sym.Usage )
                {
                    case SymbolUsage.None:
                    {
                        z[ "usage" ] = Symbols.Undefined;
                        return result;
                    }
                    case SymbolUsage.Constant:
                    {
                        if ( sym.IsDynamic )
                        {
                            z[ "usage" ] = Symbols.SpecialConstant;
                            isVariable = true;
                        }
                        else
                        {
                            z[ "usage" ] = Symbols.Constant;
                            isVariable = true;
                        }
                        break;
                    }
                    case SymbolUsage.ReadonlyVariable:
                    {
                        if ( sym.IsDynamic )
                        {
                            z[ "usage" ] = Symbols.SpecialReadonlyVariable;
                            isVariable = true;
                        }
                        else
                        {
                            z[ "usage" ] = Symbols.ReadonlyVariable;
                            isVariable = true;
                        }
                        break;
                    }
                    case SymbolUsage.Variable:
                    {
                        if ( sym.IsDynamic )
                        {
                            z[ "usage" ] = Symbols.SpecialVariable;
                            isVariable = true;
                        }
                        else
                        {
                            z[ "usage" ] = Symbols.Variable;
                            isVariable = true;
                        }
                        break;
                    }
                    case SymbolUsage.Function:
                    {
                        z[ "usage" ] = Symbols.Function;
                        break;
                    }

                }

                if ( !Emptyp( sym.Documentation ) )
                {
                    z[ "documentation" ] = AsList( ( IEnumerable ) sym.Documentation );
                }

                if ( !Emptyp( sym.FunctionSyntax ) )
                {
                    z[ "function-syntax" ] = AsList( ( IEnumerable ) sym.FunctionSyntax );
                }
            }

            if ( !( a is ICollection ) )
            {
                z[ "value" ] = a;
            }

            if ( Nullp( a ) )
            {
                return result;
            }

            if ( !( a is ICollection ) )
            {
                z[ "type" ] = a.GetType().ToString();
            }

            Symbol usage = null;

            if ( !isVariable )
            {
                if ( a is ISyntax && !z.ContainsKey( "function-syntax" ) )
                {
                    var b = ( ( ISyntax ) a ).GetSyntax( obj as Symbol );
                    if ( b != null )
                    {
                        z[ "function-syntax" ] = b;
                    }
                }

                if ( a is MultiMethod )
                {
                    usage = Symbols.GenericFunction;
                }
                else if ( a is ImportedConstructor )
                {
                    var kiezel = ( ( ImportedConstructor ) a ).HasKiezelMethods;
                    usage = kiezel ? Symbols.BuiltinConstructor : Symbols.ImportedConstructor;
                }
                else if ( a is ImportedFunction )
                {
                    var kiezel = ( ( ImportedFunction ) a ).HasKiezelMethods;
                    usage = kiezel ? Symbols.BuiltinFunction : Symbols.ImportedFunction;
                }
                else if ( a is SpecialForm )
                {
                    usage = Symbols.SpecialForm;
                }
                else if ( a is Lambda )
                {
                    var l = ( Lambda ) a;

                    z[ "function-source" ] = l.Source;

                    switch ( l.Kind )
                    {
                        case LambdaKind.Macro:
                        {
                            usage = Symbols.Macro;
                            break;
                        }
                        case LambdaKind.Method:
                        {
                            usage = Symbols.Method;
                            break;
                        }
                        case LambdaKind.Function:
                        default:
                        {
                            usage = Symbols.Function;
                            break;
                        }
                    }
                }
            }

            if ( obj is Symbol && usage != null )
            {
                z[ "usage" ] = usage;
            }

            if ( a is Prototype )
            {
                var p = ( Prototype ) a;
                z[ "superclasses" ] = p.GetSuperclasses();
            }
            else if ( a is IList )
            {
                int index = 0;
                foreach ( object item in ( IList ) a )
                {
                    z[ "[" + index.ToString() + "]" ] = item;
                    ++index;
                }
            }
            else
            {
                var dict = AsPrototype( a );
                
                if ( dict.Dict.Count != 0 )
                {
                    z[ "members" ] = dict;
                }
            }

            return result;
        }

        internal static object GetSyntax( object a )
        {
            var z = GetDescription( a );
            return z.GetValue( "function-syntax" );
        }

        [Lisp( "describe" )]
        public static void Describe( object obj )
        {
            Describe( obj, false );
        }

        [Lisp( "describe" )]
        public static void Describe( object obj, bool showNonPublic )
        {
            var description = GetDescription( obj, showNonPublic );
            WriteLine( description, Symbols.Pretty, true );
        }

        [Lisp( "as-prototype" )]
        public static Prototype AsPrototype( object obj )
        {
            Type type = obj.GetType();
            TypeCode typecode = Type.GetTypeCode( type );

            if ( obj is IDictionary )
            {
                var dict = ConvertToLispDictionary( ( IDictionary ) obj );
                return Prototype.FromDictionary( dict );
            }
            else if ( obj is Type )
            {
                var dict = ConvertToDictionary( ( Type ) obj, null, false );
                return Prototype.FromDictionary( dict );
            }
            else if ( true || typecode == TypeCode.Object )
            {
                var dict = ConvertToDictionary( obj.GetType(), obj, false  );
                return Prototype.FromDictionary( dict );
            }
            else
            {
                return null;
            }
        }

        internal static PrototypeDictionary ConvertToLispDictionary( IDictionary obj )
        {
            var dict = new PrototypeDictionary();
            foreach ( DictionaryEntry item in obj )
            {
                dict[ item.Key ] = item.Value;
            }
            return dict;
        }

        internal static PrototypeDictionary ConvertToDictionary( Type type, object obj, bool showNonPublic )
        {
            var flags = BindingFlags.Public | ( obj == null ? BindingFlags.Static : BindingFlags.Instance ) | BindingFlags.FlattenHierarchy;

            if ( showNonPublic )
            {
                flags |= BindingFlags.NonPublic;
            }
            var members = type.GetMembers( flags );
            var dict = new PrototypeDictionary();

            foreach ( var m in members )
            {
                var name = m.Name;
                object value = null;

                try
                {
                    if ( m is PropertyInfo )
                    {
                        var p = ( PropertyInfo ) m;
                        value = p.GetValue( obj, new object[ 0 ] );
                        dict[ name.LispName() ] = value;
                    }
                    else if ( m is FieldInfo )
                    {
                        var f = ( FieldInfo ) m;
                        value = f.GetValue( obj );
                        dict[ name.LispName() ] = value;
                    }
                }
                catch
                {
                }

            }

            return dict;
        }

        internal static void PrintLogColor( object color, object bkcolor, params object[] args )
        {
            var msg = MakeString( args );
            WriteLine( msg, Symbols.Stream, GetDynamic( Symbols.StdLog ), Symbols.Escape, false, Symbols.Color, color, Symbols.BackgroundColor, bkcolor );
        }

        internal static void PrintLog( params object[] args )
        {
            PrintLogColor( null, null, args );
        }

        internal static int GetConsoleWidth()
        {
            try
            {
                return Console.WindowWidth - 1;
            }
            catch
            {
                // When running as windows application.
                return 80 - 1;
            }
        }

        internal static void DumpDictionary( object stream, Prototype prototype )
        {
            var dict = prototype.Dict;

            foreach ( string key in ToIter( Sort( dict.Keys, NaturalLess, Identity ) ) )
            {
                object val = dict[ key ];
                string line = String.Format( "{0} => ", key );
                Write( line, Symbols.Escape, false, Symbols.Stream, stream );
                PrettyPrint( stream, line.Length, val );
            }
        }

        //[Lisp( "get-global-variables-dictionary" )]
        public static Prototype GetGlobalVariablesDictionary()
        {
            return GetGlobalVariablesDictionary( "" );
        }

        //[Lisp( "get-global-variables-dictionary" )]
        public static Prototype GetGlobalVariablesDictionary( string pattern )
        {
            var env = new PrototypeDictionary();
            var pat = (pattern ?? "").ToLower();

            foreach ( var package in Packages.Values )
            {
                if ( package.Name != "" )
                {
                    foreach ( Symbol sym in package.Dict.Values )
                    {
                        var name = sym.DiagnosticsName;

                        if ( !sym.IsUndefined && ( pattern == null || name.ToLower().IndexOf( pat ) != -1 ) )
                        {
                            env[ name ] = sym.Value;
                        }
                    }
                }
            }

            return Prototype.FromDictionary( env );
        }

        [Lisp( "get-global-symbols" )]
        public static Cons GetGlobalSymbols()
        {
            var env = new HashSet<Symbol>();
            foreach ( var package in Packages.Values )
            {
                if ( package.Name != "" )
                {
                    foreach ( Symbol sym in package.Dict.Values )
                    {
                        var name = sym.DiagnosticsName;

                        if ( !sym.IsUndefined )
                        {
                            env.Add( sym );
                        }
                    }
                }
            }

            return AsList( env.ToArray() );
        }

        //[Lisp( "get-dynamic-variables-dictionary" )]
        public static Prototype GetDynamicVariablesDictionary( int pos )
        {
            var env = new PrototypeDictionary();

            for ( var entry = GetSpecialVariablesAt( pos ); entry != null; entry = entry.Link )
            {
                var key = entry.Sym.DiagnosticsName;
                if ( !env.ContainsKey( key ) )
                {
                    env[ key ] = entry.Value;
                }
            }

            return Prototype.FromDictionary( env );
        }

        //[Lisp( "get-lexical-variables-dictionary" )]
        public static Prototype GetLexicalVariablesDictionary( int pos )
        {
            var env = new PrototypeDictionary();

            for ( Frame frame = GetFrameAt(pos); frame != null; frame = frame.Link )
            {
                if ( frame.Names != null )
                {
                    for ( int i = 0; i < frame.Names.Count; ++i )
                    {
                        var key = frame.Names[ i ].DiagnosticsName;
                        object value = null;
                        if ( frame.Values != null && i < frame.Values.Count )
                        {
                            value = frame.Values[ i ];
                        }
                        if ( key == Symbols.Tilde.Name )
                        {
                            if ( frame == CurrentThreadContext.Frame )
                            {
                                env[ key ] = value;
                            }
                            else if ( !env.ContainsKey( key ) )
                            {
                                env[ key ] = value;
                            }
                        }
                        else if ( !env.ContainsKey( key ) )
                        {
                            env[ key ] = value;
                        }
                    }
                }
            }

            return Prototype.FromDictionary( env );
        }


        [Lisp( "get-diagnostics" )]
        public static string GetDiagnostics( Exception exception )
        {
            var buf = new StringWriter();
            if ( exception != null )
            {
                var ex2 = exception;
                while ( ex2.InnerException != null && ex2 is System.Reflection.TargetInvocationException )
                {
                    ex2 = ex2.InnerException;
                }
                buf.WriteLine( "EXCEPTION" );
                buf.WriteLine( new string( '=', 80 ) );
                buf.WriteLine( ex2.Message );
                buf.WriteLine( new string( '=', 80 ) );
                buf.WriteLine( exception.ToString() );
                buf.WriteLine( new string( '=', 80 ) );
            }
            buf.WriteLine( "LEXICAL ENVIRONMENT" );
            buf.WriteLine( new string( '=', 80 ) );
            DumpDictionary( buf, GetLexicalVariablesDictionary( 0 ) );
            buf.WriteLine( new string( '=', 80 ) );
            buf.WriteLine( "DYNAMIC ENVIRONMENT" );
            buf.WriteLine( new string( '=', 80 ) );
            DumpDictionary( buf, GetDynamicVariablesDictionary( 0 ) );
            buf.WriteLine( new string( '=', 80 ) );
            buf.WriteLine( "EVALUATION STACK" );
            buf.WriteLine( new string( '=', 80 ) );
            buf.Write( GetEvaluationStack() );
            return buf.ToString();
        }


        internal static string GetEvaluationStack()
        {
            var index = 0;
            var prefix = "";
            var buf = new StringWriter();
            var saved = SaveStackAndFrame();
            try
            {
                DefDynamic( Symbols.PrintCompact, true );

                foreach ( object item in ToIter( CurrentThreadContext.EvaluationStack ) )
                {
                    // Every function call adds the source code form
                    // Every lambda call adds the outer frame (not null) and specialvariables (maybe null)
                    if ( item is Frame )
                    {
                        ++index;
                        prefix = index.ToString() + ":";
                    }
                    else if ( item is Cons )
                    {
                        var form = ( Cons ) item;
                        var leader = prefix.PadLeft(3,' ');
                        buf.WriteLine( "{0} {1}", leader, ToPrintString( form ).Shorten( GetConsoleWidth() - leader.Length - 1 ) );
                        prefix = "";
                    }
                }
            }
            finally
            {
                RestoreStackAndFrame( saved );
            }
            return buf.ToString();
        }

        internal static Frame GetFrameAt( int pos )
        {
            if ( pos <= 0 )
            {
                return CurrentThreadContext.Frame;
            }

            var index = 0;
            foreach ( object item in ToIter( CurrentThreadContext.EvaluationStack ) )
            {
                if ( item is Frame )
                {
                    ++index;
                    if ( pos == index )
                    {
                        return ( Frame ) item;
                    }
                }
            }
            return null;
        }

        internal static SpecialVariables GetSpecialVariablesAt( int pos )
        {
            if ( pos <= 0 )
            {
                return CurrentThreadContext.SpecialStack;
            }

            var index = 0;
            var done = false;
            foreach ( object item in ToIter( CurrentThreadContext.EvaluationStack ) )
            {
                if ( item is Frame )
                {
                    ++index;
                    if ( pos == index )
                    {
                        done = true;
                    }
                }
                else if ( done )
                {
                    return ( SpecialVariables ) item;
                }
            }
            return null;
        }

        internal static bool IsNotDlrCode( string line )
        {
            return line.IndexOf( "CallSite" ) == -1
                && line.IndexOf( "System.Dynamic" ) == -1
                && line.IndexOf( "Microsoft.Scripting" ) == -1;
        }

        internal static string RemoveDlrReferencesFromException( Exception ex )
        {
            return String.Join( "\n", ex.ToString().Split( '\n' ).Where( IsNotDlrCode ) );
        }
	}
}


