// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using Numerics;

using KeyFunc = System.Func<object, object>;
using PredicateFunc = System.Func<object, bool>;
using TestFunc = System.Func<object, object, bool>;
using ActionFunc = System.Action<object>;
using ReduceFunc = System.Func<object, object, object>;
using ThreadFunc = System.Func<object>;


namespace Kiezel
{

	public partial class Runtime
	{

        [Lisp( "as-int", "as-int32" )]
        public static int AsInt( object a )
        {
            if ( a is int )
            {
                return (int) a;
            }
            else if ( a is BigInteger )
            {
                var n = ( BigInteger ) a;
                return ( int ) n;
            }
            else if ( a is BigRational )
            {
                var n = ( BigRational ) a;
                return ( int ) n;
            }
            else
            {
                return Convert.ToInt32( a );
            }
        }

        [Lisp( "as-long", "as-int64" )]
        public static long AsLong( object a )
        {
            if ( a is long )
            {
                return ( long ) a;
            }
            else if ( a is BigInteger )
            {
                var n = ( BigInteger ) a;
                return ( long ) n;
            }
            else if ( a is BigRational )
            {
                var n = ( BigRational ) a;
                return ( long ) n;
            }
            else
            {
                return Convert.ToInt64( a );
            }
        }

        [Lisp( "as-decimal" )]
        public static decimal AsDecimal( object a )
        {
            if ( a is decimal )
            {
                return ( decimal ) a;
            }
            else if ( a is BigInteger )
            {
                var n = ( BigInteger ) a;
                return ( decimal ) n;
            }
            else if ( a is BigRational )
            {
                var n = ( BigRational ) a;
                return ( decimal ) n;
            }
            else
            {
                return Convert.ToDecimal( a );
            }
        }

        [Lisp( "as-double" )]
        public static double AsDouble( object a )
        {
            if ( a is double )
            {
                return ( double ) a;
            }
            else if ( a is BigInteger )
            {
                var n = ( BigInteger ) a;
                return ( double ) n;
            }
            else if ( a is BigRational )
            {
                var n = ( BigRational ) a;
                return ( double ) n;
            }
            else
            {
                return Convert.ToDouble( a );
            }
        }

        [Lisp( "as-big-integer" )]
        public static BigInteger AsBigInteger( object a )
        {

            if ( a is BigInteger )
            {
                return ( BigInteger ) a;
            }
            else if ( a is BigRational )
            {
                var n = ( BigRational ) a;
                return n.GetWholePart();
            }
            else if (a is int)
            {
                return new BigInteger( ( int ) a );
            }
            else if ( a is long )
            {
                return new BigInteger( ( long ) a );
            }
            else if ( a is double )
            {
                return new BigInteger( ( double ) a );
            }
            else if ( a is decimal )
            {
                return new BigInteger( ( decimal ) a );
            }
            else
            {
                return ( BigInteger ) a;
            }
        }

        [Lisp( "as-big-rational" )]
        public static BigRational AsBigRational( object a )
        {
            if ( a is BigRational )
            {
                return ( BigRational ) a;
            }
            else
            {
                return new BigRational( AsBigInteger( a ) );
            }
        }

        [Lisp( "as-complex" )]
        public static Complex AsComplex( object a )
        {
            if ( a is Complex )
            {
                return ( Complex ) a;
            }
            else
            {
                return new Complex( AsDouble( a ), 0 );
            }
        }
		//
		// typecasts
		//

        internal static Cons ToCons( object obj )
        {
            if ( obj != null && !( obj is Cons ) )
            {
                throw new LispException( "Cannot cast to Cons: {0}", ToPrintString( obj ) );
            }

            return ( Cons ) obj;
        }

        internal static IList ToIList( object obj )
		{
			if ( obj == null )
			{
				// avoids crash
				return new object[0];
			}
			else if ( obj is IList )
			{
				return (IList) obj;
			}
			else
			{
				throw new LispException( "Cannot cast to IList: {0}", ToPrintString( obj ) );
			}
		}


        internal static ICollection ToICollection( object obj )
		{
			if ( obj == null )
			{
				// avoids crash
				return new object[0];
			}
			else if ( obj is ICollection )
			{
				return (ICollection) obj;
			}
			else
			{
				throw new LispException( "Cannot cast to ICollection: {0}", ToPrintString( obj ) );
			}
		}

        internal static IEnumerable ToIter( object obj )
		{
			if ( obj == null )
			{
				// avoids crash
				return new object[0];
			}
			else if ( obj is IEnumerable )
			{
				return (IEnumerable) obj;
			}
			else
			{
				throw new LispException( "Cannot cast to IEnumerable: {0}", ToPrintString( obj ) );
			}
		}


        internal static string ToString( object obj )
		{
			if ( obj == null )
			{
				return "";
			}
			else if ( obj is string )
			{
				return (string) obj;
			}
			else if ( obj is StringBuilder || obj is StringWriter)
			{
				return obj.ToString();
			}
			else
			{
				throw new LispException( "Cannot cast to String: {0}", ToPrintString( obj ) );
			}
		}

        [ Pure, Lisp( "string" )]
        public static string MakeString( params object[] objs )
        {
            return MakeStringFromObj( objs );
        }

        internal static string MakeStringFromObj( object obj )
		{
			if ( obj == null )
			{
				return "";
			}
			else if ( obj is string )
			{
				return (string) obj;
			}
			else if ( obj is DateTime )
			{
                var dt = (DateTime) obj;
                if ( dt.Hour == 0 && dt.Minute == 0 && dt.Second == 0 )
                {
                    return dt.ToString( "dd/MMM/yyyy" );
                }
                else
                {
                    return dt.ToString( "dd/MMM/yyyy HH:mm:ss" );
                }

			}
			else if ( obj is StringBuilder || obj is StringWriter )
			{
				return obj.ToString();
			}
			else if ( obj is bool )
			{
				return obj.ToString().ToLower();
			}
			else if ( obj is ValueType )
			{
				return obj.ToString();
			}
			else if ( obj is Prototype )
			{
				//return (string) VM.CoerceToType( (Instance) obj, VM.sym_string );
				return obj.ToString();
			}
			else if ( obj is IEnumerable )
			{
				var buf = new StringWriter();
				foreach ( object item in ( (IEnumerable) obj ) )
				{
					if ( item is DictionaryEntry )
					{
                        buf.Write( MakeStringFromObj( ( ( DictionaryEntry ) item ).Value ) );
					}
					else
					{
                        buf.Write( MakeStringFromObj( item ) );
					}
				}
				return buf.ToString();
			}
			else
			{
				return obj.ToString();
			}

		}

        internal static double ToDouble( object val )
		{
			if ( val is double )
			{
				return (double) val;
			}
			else
			{
				return Convert.ToDouble( val );
			}
		}

        internal static Symbol CheckSymbol( object val )
        {
            if ( val is Symbol )
            {
                return ( Symbol ) val;
            }

            throw new LispException( "{0} is not a symbol", val );
        }

        internal static Symbol CheckKeyword( object val )
        {
            if ( Keywordp( val ) )
            {
                return ( Symbol ) val;
            }

            throw new LispException( "{0} is not a keyword", val );
        }

        internal static int ToInt( object val )
		{
			if ( val is int || val is Enum )
			{
				return (int) val;
			}
			else
			{
				return Convert.ToInt32( val );
			}
		}

        internal static int ToInt( object val, int defaultValue )
        {
            if ( val is int || val is Enum )
            {
                return ( int ) val;
            }
            else if ( val == null )
            {
                return defaultValue;
            }
            else
            {
                return Convert.ToInt32( val );
            }
        }

        internal static object ChangeType( object value, Type targetType )
        {
            if ( value == null )
            {
                return null;
            }

            if ( value is Vector && targetType.IsArray )
            {
                Type t = targetType.GetElementType();
                Vector v = ( Vector ) value;
                foreach ( object item in ( Vector ) value )
                {
                    if ( item != null && t != item.GetType() )
                    {
                        return null;
                    }
                }
                // all items are of the correct type
                Array a = System.Array.CreateInstance( t, v.Count );
                v.CopyTo( ( object[] ) a, 0 );
                return a;
            }

            if ( value is string && targetType == typeof( char ) )
            {
                string s = ( string ) value;
                if ( s.Length == 1 )
                {
                    return s[ 0 ];
                }
                else
                {
                    return null;
                }
            }

            if ( targetType == typeof( BigInteger ) )
            {
                if ( value is Int32 )
                {
                    return ( BigInteger ) ( Int32 ) value;
                }

                if ( value is Int64 )
                {
                    return ( BigInteger ) ( Int64 ) value;
                }
            }

            if ( targetType == typeof( BigRational ) )
            {
                if ( value is Int32 )
                {
                    return ( BigRational ) ( Int32 ) value;
                }

                if ( value is Int64 )
                {
                    return ( BigRational ) ( Int64 ) value;
                }

                if ( value is BigInteger )
                {
                    return ( BigRational ) ( BigInteger ) value;
                }

            }

            if ( value is string && targetType.IsArray && targetType.GetElementType() == typeof( char ) )
            {
                string s = ( String ) value;
                return s.ToCharArray();
            }

            if ( value is IApply )
            {
                if ( targetType == typeof( EventHandler ) )
                {
                    return new EventHandler( new DelegateWrapper( value as IApply ).Obj_Evt_Void );
                }
                else if ( targetType == typeof( Predicate<object> ) )
                {
                    return new Predicate<object>( new DelegateWrapper( value as IApply ).Obj_Bool );
                }
                else if ( targetType == typeof( Func<object, bool> ) )
                {
                    return new Func<object, bool>( new DelegateWrapper( value as IApply ).Obj_Bool );
                }
                else if ( targetType == typeof( Func<object, object, bool> ) )
                {
                    return new Func<object, object, bool>( new DelegateWrapper( value as IApply ).Obj_Obj_Bool );
                }
                else if ( targetType == typeof( Action ) )
                {
                    return new Action( new DelegateWrapper( value as IApply ).Void );
                }
                else if ( targetType == typeof( Action<object> ) )
                {
                    return new Action<object>( new DelegateWrapper( value as IApply ).Obj_Void );
                }
                else if ( targetType == typeof( Comparison<object> ) )
                {
                    return new Comparison<object>( new DelegateWrapper( value as IApply ).Compare );
                }
                else if ( targetType == typeof( IComparer<object> ) )
                {
                    return new DelegateWrapper( value as IApply );
                }
                else if ( targetType == typeof( IComparer ) )
                {
                    return new DelegateWrapper( value as IApply );
                }
                else if ( targetType == typeof( IEqualityComparer ) )
                {
                    return new DelegateWrapper( value as IApply );
                }
                else if ( targetType == typeof( IEqualityComparer<object> ) )
                {
                    return new DelegateWrapper( value as IApply );
                }
                else if ( targetType == typeof( Converter<object, object> ) )
                {
                    return new Converter<object, object>( new DelegateWrapper( value as IApply ).Obj_Obj );
                }
                else if ( targetType == typeof( Func<object> ) )
                {
                    return new Func<object>( new DelegateWrapper( value as IApply ).Obj );
                }
                else if ( targetType == typeof( Func<object, object> ) )
                {
                    return new Func<object, object>( new DelegateWrapper( value as IApply ).Obj_Obj );
                }
                else if ( targetType == typeof( Func<object, object, object> ) )
                {
                    return new Func<object, object, object>( new DelegateWrapper( value as IApply ).Obj_Obj_Obj );
                }
                else if ( targetType == typeof( Func<object, int, object> ) )
                {
                    return new Func<object, int, object>( new DelegateWrapper( value as IApply ).Obj_Int_Obj );
                }
                else if ( targetType == typeof( Func<object, int, bool> ) )
                {
                    return new Func<object, int, bool>( new DelegateWrapper( value as IApply ).Obj_Int_Bool );
                }
                else if ( targetType == typeof( Func<object, IEnumerable<object>> ) )
                {
                    return new Func<object, IEnumerable<object>>( new DelegateWrapper( value as IApply ).Obj_Enum );
                }
                else if ( targetType == typeof( Func<object, int, IEnumerable<object>> ) )
                {
                    return new Func<object, int, IEnumerable<object>>( new DelegateWrapper( value as IApply ).Obj_Int_Enum );
                }
            }

            return value;
        }

        internal class EltWrapper : IApply
        {
            object element;
            
            internal EltWrapper( object element )
            {
                this.element = element;
            }

            object IApply.Apply( object[] args )
            {
                return Elt( element, args );
            }
        }

        internal static bool EltWrappable( object arg )
        {
            return arg is Prototype || arg is IDictionary;
        }

        internal static IApply GetClosure( object arg )
        {
            if ( arg is Symbol )
            {
                return ( IApply ) ( ( Symbol ) arg ).CheckedValue;
            }
            else if ( EltWrappable( arg ) )
            {
                return new EltWrapper( arg );
            }
            else
            {
                return ( IApply ) arg;
            }
        }

        internal static PredicateFunc GetPredicateFunc( object arg )
        {
            return GetPredicateFunc( arg, ToBool );
        }

        internal static PredicateFunc GetPredicateFunc( object arg, PredicateFunc defaultFunc )
        {
            return GetFunc<PredicateFunc>( arg, defaultFunc );
        }

        internal static T GetFunc<T>( object arg, T defaultFunc )
        {
            if ( arg == null && defaultFunc != null )
            {
                return defaultFunc;
            }
            var test = GetClosure( arg );
            var testf = ( T ) ChangeType( test, typeof( T ) );
            if ( testf != null )
            {
                return testf;
            }
            if ( defaultFunc != null )
            {
                return defaultFunc;
            }
            throw new LispException( "{0} function cannot be null", typeof( T ) );
        }

        internal static TestFunc GetTestFunc( object arg )
        {
            return GetTestFunc( arg, Equal );
        }

        internal static TestFunc GetTestFunc( object arg, TestFunc defaultFunc )
        {
            return GetFunc<TestFunc>( arg, defaultFunc );
        }

        internal static IEqualityComparer<object> GetEqualityComparer( object arg )
        {
            return GetFunc<IEqualityComparer<object>>( arg, EqualityComparer<object>.Default );
        }

        internal static KeyFunc GetKeyFunc( object arg )
        {
            return GetKeyFunc( arg, Identity );
        }

        internal static KeyFunc GetKeyFunc( object arg, KeyFunc defaultFunc )
        {
            return GetFunc<KeyFunc>( arg, defaultFunc );
        }

        internal static ThreadFunc GetThreadFunc( object arg )
        {
            return GetFunc<ThreadFunc>( arg, null );
        }

        internal static ActionFunc GetActionFunc( object arg )
        {
            return GetFunc<ActionFunc>( arg, null );
        }

        internal static ReduceFunc GetReduceFunc( object arg )
        {
            return GetFunc<ReduceFunc>( arg, null );
        }

        internal static Type GetDelegateType( params Type[] types )
        {
            return Expression.GetDelegateType( types );
        }

        internal static Delegate ConvertToDelegate( Type type, object closure )
        {
            var expr = RuntimeHelpers.GetDelegateExpression( Expression.Constant( closure ), type );
            return expr.Compile();
        }

 	}

 }
