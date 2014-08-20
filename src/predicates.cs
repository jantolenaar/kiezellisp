// Copyright (C) Jan Tolenaar. See the file LICENSE for details.


using System;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Numerics;
using Numerics;

namespace Kiezel
{
    public partial class Runtime
    {
        [Lisp( "subtype?" )]
        public static bool Subtypep( Symbol subtype, Symbol supertype )
        {
            return IsSubtype( GetType( subtype ), GetType( supertype ), false );
        }

        internal static bool IsSubtype( object subtype, object type, bool strict )
        {
            if ( Equal( subtype, type ) )
            {
                return !strict;
            }

            if ( type == null )
            {
                return true;
            }

            if ( type is bool && ( bool ) type )
            {
                return true;
            }

            if ( type == (object) typeof( object ) )
            {
                return true;
            }

            if ( subtype == null )
            {
                return false;
            }

            if ( subtype is bool && ( bool ) subtype )
            {
                return false;
            }

            if ( subtype == (object) typeof( object ) )
            {
                return false;
            }

            if ( subtype is EqlSpecializer )
            {
                var sub = ( ( EqlSpecializer ) subtype ).Value;
                if ( type is EqlSpecializer )
                {
                    var super = ( ( EqlSpecializer ) type ).Value;
                    return strict ? false : Eql( sub, super );
                }
                else if ( type is Prototype )
                {
                    return false;
                }
                else
                {
                    var super = ( Type ) type;
                    return IsInstanceOf( sub, super );
                }
            }
            else if ( subtype is Prototype )
            {
                var sub = ( Prototype ) subtype;
                if ( type is Prototype )
                {
                    var super = ( Prototype ) type;
                    return sub.IsSubTypeOf( super );
                }
                else
                {
                    return false;
                }
            }
            else
            {
                var sub = ( Type ) subtype;
                if ( type is Type )
                {
                    var super = ( Type ) type;
                    List<Type> subtypes;
                    if ( AbstractTypes.TryGetValue( super, out subtypes ) )
                    {
                        return subtypes.Contains(sub);
                    }
                    else
                    {
                        return super.IsAssignableFrom( sub );
                    }
                }
                else
                {
                    return false;
                }
            }
           
        }

        [Pure, Lisp( "type?" )]
        public static bool Typep( object target, Symbol type )
        {
            var t = GetType( type );
            return IsInstanceOf( target, t );
        }

        internal static bool IsInstanceOf( object target, object type )
        {
            if ( type == null )
            {
                return true;
            }

            if ( type is bool && ( bool ) type )
            {
                return true;
            }

            if ( type is EqlSpecializer )
            {
                var value = ( ( EqlSpecializer ) type ).Value;
                return Eql( target, value );
            }
            else if ( type is Prototype )
            {
                var inst = target as Prototype;
                return inst != null && ( inst == type || inst.IsSubTypeOf( ( Prototype ) type ) );
            }
            else if ( type is Type )
            {
                var t = ( Type ) type;
                
                if ( t == typeof( Number ) )
                {
                    return Numberp( target );
                }
                else if ( t == typeof( Integer ) )
                {
                    return Integerp( target );
                }
                else if ( t == typeof( Rational ) )
                {
                    return Rationalp( target );
                }
                else if ( t == typeof( List ) )
                {
                    return Listp( target );
                }
                else if ( t == typeof( Atom ) )
                {
                    return Atomp( target );
                }
                else if ( t == typeof( Sequence ) )
                {
                    return Sequencep( target );
                }
                else if ( t == typeof( Enumerable ) )
                {
                    return Enumerablep( target );
                }
                else if ( t == typeof( KeywordClass ) )
                {
                    return Keywordp( target );
                }
                else
                {
                    return t.IsInstanceOfType( target );
                }
            }
            else
            {
                throw new LispException( "Not a type: {0}", ToPrintString( type ) );
            }
        }

        [Pure, Lisp( "prototype?" )]
        public static bool Prototypep( object expr )
        {
            return expr is Prototype;
        }

        [Pure, Lisp( "exception?" )]
        public static bool Exceptionp( object expr )
        {
            return expr is Exception;
        }

        [Pure, Lisp( "defined?" )]
        public static bool Definedp( Symbol sym )
        {
            return !sym.IsUndefined;
        }

        [Pure, Lisp( "string?" )]
        public static bool Stringp( object expr )
        {
            return expr is string;
        }

        [Pure, Lisp( "date?" )]
        public static bool Datep( object expr )
        {
            return expr is DateTime;
        }

        [Pure, Lisp( "char?" )]
        public static bool Charp( object expr )
        {
            return expr is char;
        }

        [Pure, Lisp( "number?" )]
        public static bool Numberp( object expr )
        {
            return Integerp( expr ) || expr is decimal || expr is double || Rationalp( expr ) || Complexp( expr );
        }

        [Pure, Lisp( "complex?" )]
        public static bool Complexp( object expr )
        {
            return expr is Complex;
        }

        [Pure, Lisp( "integer?" )]
        public static bool Integerp( object expr )
        {
            return expr is int || expr is Int64 || expr is BigInteger;
        }

        [Pure, Lisp( "ratio?" )]
        public static bool Ratiop( object expr )
        {
            return expr is BigRational;
        }

        [Pure, Lisp( "rational?" )]
        public static bool Rationalp( object expr )
        {
            return expr is BigRational || Integerp( expr );
        }

        [Pure, Lisp( "list?" )]
        public static bool Listp( object expr )
        {
            return expr == null || expr is Cons;
        }

        [Pure, Lisp( "cons?" )]
        public static bool Consp( object expr )
        {
            return expr is Cons;
        }

        [Pure, Lisp( "empty?" )]
        public static bool Emptyp( object expr )
        {
            return expr == null || Zerop( Length( ToIter( expr ) ) );
        }

        [Pure, Lisp( "atom?" )]
        public static bool Atomp( object expr )
        {
            return Literalp( expr ) || Symbolp( expr );
        }

        [Pure, Lisp( "literal?" )]
        public static bool Literalp( object expr )
        {
            return expr == null || expr is ValueType || Numberp( expr ) || Stringp( expr );
        }

        internal static bool Quotedp( object expr )
        {
            return expr is Cons && First( expr ) == Symbols.Quote;
        }

        [Pure, Lisp( "vector?" )]
        public static bool Vectorp( object expr )
        {
            return expr is Vector;
        }

        [Pure, Lisp( "sequence?" )]
        public static bool Sequencep( object expr )
        {
            return Listp( expr ) || Vectorp( expr );
        }

        [Pure, Lisp( "enumerable?" )]
        public static bool Enumerablep( object expr )
        {
            return expr == null || expr is IEnumerable;
        }

        [Pure, Lisp( "ilist?" )]
        public static bool IListp( object expr )
        {
            return expr is IList;
        }

        [Pure, Lisp( "symbol?" )]
        public static bool Symbolp( object expr )
        {
            return expr is Symbol;
        }

        //[Pure, Lisp( "pattern-variable?" )]
        public static bool PatternVariablep( object expr )
        {
            var sym = expr as Symbol;
            return sym != null && sym.Package != KeywordPackage && sym.Name.StartsWith( "?" );
        }

        [Pure, Lisp( "keyword?" )]
        public static bool Keywordp( object expr )
        {
            var sym = expr as Symbol;
            return sym != null && sym.Package == KeywordPackage;
        }

        [Pure, Lisp( "special-symbol?" )]
        public static bool SpecialSymbolp( object expr )
        {
            var sym = expr as Symbol;
            return sym != null && sym.IsDynamic;
        }

        [Pure, Lisp( "null?" )]
        public static bool Nullp( object expr )
        {
            return expr == null;
        }

        [Pure, Lisp( "void?" )]
        public static bool Voidp( object expr )
        {
            return expr is VOID;
        }

        [Pure, Lisp( "lambda?" )]
        public static bool Lambdap( object expr )
        {
            var func = expr as LambdaClosure;
            return func != null && func.Kind == LambdaKind.Function;
        }

        [Pure, Lisp( "macro?" )]
        public static bool Macrop( object expr )
        {
            var func = expr as LambdaClosure;
            return func != null && func.Kind == LambdaKind.Macro;
        }

        [Pure, Lisp( "multi-method?" )]
        public static bool MultiMethodp( object expr )
        {
            return expr is MultiMethod;
        }

        [Pure, Lisp( "special-form?" )]
        public static bool SpecialFormp( object expr )
        {
            return expr is SpecialForm;
        }

        [Pure, Lisp( "function?" )]
        public static bool Functionp( object expr )
        {
            return expr is IApply;
        }

        [Pure, Lisp( "zero?" )]
        public static bool Zerop( object a1 )
        {
            if ( !Numberp( a1 ) )
            {
                throw new LispException( "Not a number" );
            }
            return Equal( a1, 0 );
        }

        [Pure, Lisp( "plus?" )]
        public static bool Plusp( object a1 )
        {
            if ( !Numberp( a1 ) )
            {
                throw new LispException( "Not a number" );
            }
            return Greater( a1, 0 );
        }

        [Pure, Lisp( "minus?" )]
        public static bool Minusp( object a1 )
        {
            if ( !Numberp( a1 ) )
            {
                throw new LispException( "Not a number" );
            }
            return Less( a1, 0 );
        }

        [Pure, Lisp( "even?" )]
        public static bool Evenp( object expr )
        {
            if ( !Integerp( expr ) )
            {
                throw new LispException( "Not an integer" );
            }
            return Equal( BitAnd( expr, 1 ), 0 );
        }

        [Pure, Lisp( "odd?" )]
        public static bool Oddp( object expr )
        {
            if ( !Integerp( expr ) )
            {
                throw new LispException( "Not an integer" );
            }
            return Equal( BitAnd( expr, 1 ), 1 );
        }

        [Pure, Lisp( "boolean" )]
        public static bool ToBool( object a1 )
        {
            if ( a1 is bool )
            {
                return ( bool ) a1;
            }
            else if ( a1 == null )
            {
                return false;
            }
            else if ( a1 is string )
            {
                return ( string ) a1 != "";
            }
            else if ( a1 is ICollection )
            {
                return ( ( ICollection ) a1 ).Count != 0;
            }
            else if ( a1 is IEnumerable )
            {
                return AsLazyList( ( IEnumerable ) a1 ) != null;
            }
            else if ( a1 is VOID )
            {
                return false;
            }
            else
            {
                return true;
            }

        }

    }
}
