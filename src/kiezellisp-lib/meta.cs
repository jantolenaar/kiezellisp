// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Threading;

namespace Kiezel
{
    public partial class Runtime
    {
        [Lisp( "code-walk" )]
        public static object CodeWalk( object form, Func<object, object> transform )
        {
            Func<object, AnalysisScope, object> wrapper = ( x, y ) => transform( x );
            return CodeWalk( form, wrapper, null );
        }

        [Lisp( "code-walk" )]
        public static object CodeWalk( object form, Func<object, AnalysisScope, object> transform, AnalysisScope env )
        {
            if ( env == null )
            {
                env = new AnalysisScope( null, "codewalk" );
            }

            form = transform( form, env );

            if ( !Consp( form ) )
            {
                return form;
            }

            var forms = ( Cons ) form;
            var head = First( forms ) as Symbol;

            if ( head != null && head.SpecialFormValue != null )
            {
                var tag = head.Name;
                switch ( tag )
                {
                    case "declare":
                    case "goto":
                    case "quote":
                    {
                        break;
                    }
                    case "def":
                    case "var":
                    case "hidden-var":
                    case "let":
                    case "defconstant":
                    case "lazy":
                    case "future":
                    case "setq":
                    {
                        forms = CodeWalkListAt( 2, forms, transform, env );
                        var variables = Second( forms );
                        if ( variables is Cons )
                        {
                            foreach ( var sym in ToIter( variables ) )
                            {
                                env.DefineFrameLocal( ( Symbol ) sym, ScopeFlags.All );
                            }
                        }
                        else
                        {
                            env.DefineFrameLocal( ( Symbol ) variables, ScopeFlags.All );
                        }
                        break;
                    }
                    case "lambda":
                    {
                        return form;
                        throw new LispException( "lambda not supported by code walker" );
                    }
                    case "letfun":
                    {
                        return form;
                        throw new LispException( "letfun not supported by code walker" );
                    }
                    case "try":
                    {
                        forms = MakeCons( Symbols.Try, CodeWalkListTry( Cdr( forms ), transform, env ) );
                        break;
                    }
                    case "defmacro":
                    case "defun":
                    case "defmulti":
                    case "defmethod":
                    {
                        return form;
                        throw new LispException( "defun, defmacro, defmulti and defmethod not supported by code walker" );
                    }
                    case "do":
                    {
                        var env2 = new AnalysisScope( env, "do" );
                        forms = CodeWalkListAt( 1, forms, transform, env2 );
                        break;
                    }
                    default:
                    {
                        forms = CodeWalkListAt( 1, forms, transform, env );
                        break;
                    }
                }
            }
            else
            {
                forms = CodeWalkListAt( 0, forms, transform, env );
            }

            return forms;
        }

        [Lisp( "code-walk-list" )]
        public static Cons CodeWalkList( Cons forms, Func<object, object> transform )
        {
            Func<object, AnalysisScope, object> wrapper = ( x, y ) => transform( x );
            return CodeWalkListAt( 0, forms, wrapper, null );
        }

        [Lisp( "code-walk-list" )]
        public static Cons CodeWalkList( Cons forms, Func<object, AnalysisScope, object> transform, AnalysisScope env )
        {
            return CodeWalkListAt( 0, forms, transform, env );
        }

        public static Cons CodeWalkListAt( int pos, Cons forms, Func<object, AnalysisScope, object> transform, AnalysisScope env )
        {
            if ( env == null )
            {
                env = new AnalysisScope( null, "codewalk" );
            }

            if ( forms == null )
            {
                return null;
            }
            else if ( pos > 0 )
            {
                return MakeCons( forms.Car, CodeWalkListAt( pos - 1, forms.Cdr, transform, env ) );
            }
            else
            {
                return MakeCons( CodeWalk( forms.Car, transform, env ), CodeWalkListAt( 0, forms.Cdr, transform, env ) );
            }
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

        [Lisp( "find-name-in-environment" )]
        public static bool FindNameInEnvironment( Symbol sym, AnalysisScope env )
        {
            return env != null && env.HasLocalVariable( sym, int.MaxValue );
        }

        [Lisp( "gentemp" )]
        public static Symbol GenTemp()
        {
            return GenTemp( "temp" );
        }

        [Lisp( "gentemp" )]
        public static Symbol GenTemp( object prefix )
        {
            var count = Interlocked.Increment( ref GentempCounter );
            var name = String.Format( "{0}{1}", GetDesignatedString( prefix ), count );
            var sym = MakeSymbol( name, TempPackage, false );
            return sym;
        }

        [Lisp( "interpolate-string" )]
        public static object InterpolateString( string str )
        {
            var expr = Runtime.ParseInterpolateString( str );
            return Eval( expr );
        }

        //[Lisp( "augment-environment" )]
        //public static void AugmentEnvironment( Symbol sym, AnalysisScope env )
        //{
        //    env.DefineFrameLocal( sym, ScopeFlags.All );
        //}
        [Lisp( "macroexpand" )]
        public static object MacroExpand( object expr )
        {
            return MacroExpand( expr, null );
        }

        [Lisp( "macroexpand" )]
        public static object MacroExpand( object expr, AnalysisScope env )
        {
            var form = expr;

            while ( TryMacroExpand( form, env, out form ) )
            {
            }

            return form;
        }

        [Lisp( "macroexpand-1" )]
        public static Cons MacroExpand1( object expr )
        {
            return MacroExpand1( expr, null );
        }

        [Lisp( "macroexpand-1" )]
        public static Cons MacroExpand1( object expr, AnalysisScope env )
        {
            object result;
            var expanded = TryMacroExpand( expr, env, out result );
            return MakeList( Force( result ), expanded );
        }

        [Lisp( "macroexpand-all" )]
        public static object MacroExpandAll( object form )
        {
            return MacroExpandAll( form, null );
        }

        [Lisp( "macroexpand-all" )]
        public static object MacroExpandAll( object form, AnalysisScope env )
        {
            return CodeWalk( form, MacroExpand, env );
        }

        [Lisp( "make-environment" )]
        public static AnalysisScope MakeEnvironment()
        {
            return new AnalysisScope();
        }

        [Lisp( "make-extended-environment" )]
        public static FrameAndScope MakeExtendedEnvironment()
        {
            return new FrameAndScope();
        }

        internal static Cons CodeWalkListTry( Cons forms, Func<object, AnalysisScope, object> transform, AnalysisScope env )
        {
            if ( forms == null )
            {
                return null;
            }
            else
            {
                object result;
                if ( forms.Car is Cons )
                {
                    var list = ( Cons ) forms.Car;
                    var head = First( list );
                    if ( head == Symbols.Catch )
                    {
                        // catch (sym type) expr...
                        result = CodeWalkListAt( 2, list, transform, env );
                    }
                    else if ( head == Symbols.Finally )
                    {
                        // finally expr...
                        result = CodeWalkListAt( 1, list, transform, env );
                    }
                    else
                    {
                        result = CodeWalk( list, transform, env );
                    }
                }
                else
                {
                    result = CodeWalk( forms.Car, transform, env );
                }

                return MakeCons( result, CodeWalkListTry( forms.Cdr, transform, env ) );
            }
        }

        internal static bool TryMacroExpand( object expr, AnalysisScope env, out object result )
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

            if ( FindNameInEnvironment( head, env ) )
            {
                return false;
            }

            var macro = head.MacroValue ?? ( head.Value as LambdaClosure );

            if ( macro == null || macro.Kind != LambdaKind.Macro )
            {
                return false;
            }

            var args = AsArray( Cdr( form ) );

            result = macro.ApplyLambdaBind( null, args, false, new AnalysisScope( env, "try-macroexpand" ), form );

            return result != form;
        }
    }
}