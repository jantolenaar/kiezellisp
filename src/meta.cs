// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Dynamic;
using System.Text;
using System.Threading;

namespace Kiezel
{
    public partial class Runtime
    {
        [Lisp( "make-symbol" )]
        public static Symbol MakeSymbol( string key, string packageName )
        {
            return MakeSymbol( key, GetPackage( packageName ) );
        }

        [Lisp( "make-symbol" )]
        public static Symbol MakeSymbol( string key, Package package )
        {
            return MakeSymbol( key, package, true );
        }

        internal static Symbol MakeSymbol( string key, Package package, bool export )
        {
            if ( String.IsNullOrWhiteSpace( key ) )
            {
                throw new LispException( "Symbol name cannot be null or blank" );
            }

            if ( package == null )
            {
                return new Symbol( key );
            }
            else
            {
                var sym = package.Intern( key, useMissing: true );
                if ( export )
                {
                    sym.Package.Export( key );
                }
                return sym;
            }
        }

        [Lisp( "make-symbol" )]
        public static Symbol MakeSymbol( string key )
        {
            var value = GetDynamic( Symbols.Package );
            return MakeSymbol( key, ( Package ) value, false );
        }

        internal static Symbol MakeInitialSymbol( string key )
        {
            // bootstrapping...
            return MakeSymbol( key, LispPackage, true );
        }

        [Lisp( "gentemp" )]
        public static Symbol GenTemp()
        {
            return GenTemp( null, null );
        }

        [Lisp( "gentemp" )]
        public static Symbol GenTemp( string prefix )
        {
            return GenTemp( prefix, null );
        }

        [Lisp( "gentemp" )]
        public static Symbol GenTemp( string prefix, object packageName )
        {
            var package = packageName == null ? CurrentPackage() : GetPackage( packageName );
            while ( true )
            {
                var count = Interlocked.Increment( ref GentempCounter );
                var name = String.Format( "{0}-{1}", prefix ?? "temp", count );
                if ( package.Find( name ) == null )
                {
                    var sym = MakeSymbol( name, package, false );
                    sym.IsTemp = true;
                    return sym;
                }
            }
        }

        
        [Lisp( "make-environment" )]
        public static FrameAndScope MakeEnvironment()
        {
            return new FrameAndScope();
        }

        [Lisp( "eval" )]
        public static object Eval( object expr, FrameAndScope env )
        {
            //
            // Limited support in TurboMode
            //
            var saveContext = CurrentThreadContext.Frame;
            CurrentThreadContext.Frame = env.Frame;
            var scope = ReconstructAnalysisScope( env.Frame, env.Scope );
            // for <% examples ... %>
            //scope.IsFileScope = true;
            var result = Execute( Compile( expr, scope ) );
            CurrentThreadContext.Frame.Names = scope.Names;
            CurrentThreadContext.Frame = saveContext;
            return result;
        }

        [Lisp( "eval" )]
        public static object Eval( object expr )
        {
            //
            // Empty lexical scope!
            //
            var scope = new AnalysisScope(); 
            return Execute( Compile( expr, scope ) );
        }

        [Lisp( "interpolate-string" )]
        public static object InterpolateString( string str )
        {
            var expr = Parser.InterpolateString( str );
            return Eval( expr );
        }

        [Lisp( "macroexpand" )]
        public static object MacroExpand( object expr )
        {
            object result = expr;
            while ( TryMacroExpand( result, out result ) )
            {
            }
            return Force( result );
        }

        [Lisp( "macroexpand-1" )]
        public static object MacroExpand1( object expr )
        {
            object result;
            TryMacroExpand( expr, out result );
            return Force( result );
        }

        internal static bool TryMacroExpand( object expr, out object result )
        {
            result = expr;

            if ( !( expr is Cons ) )
            {
                return false;
            }

            var form = ( Cons ) expr;
            var head = First( form ) as Symbol;

            if ( head == null )
            {
                return false;
            }

            var macro = head.Value as Lambda;

            if ( macro == null || macro.Kind != LambdaKind.Macro )
            {
                return false;
            }

            var args = AsArray( Cdr( form ) );
            
            result = macro.ApplyLambdaBind( null, args, false );

            return true;
        }

    }
}
