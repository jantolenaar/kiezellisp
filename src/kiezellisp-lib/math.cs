// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Numerics;
using Numerics;

namespace Kiezel
{
    public partial class Runtime
    {
        internal static Random RandomNumbers = new Random();

        [Lisp( "math:random" )]
        public static int Random()
        {
            return RandomNumbers.Next();
        }

        [Lisp( "math:random" )]
        public static int Random( int maxValue )
        {
            return RandomNumbers.Next( maxValue );
        }

        [Lisp( "math:random" )]
        public static int Random( int minValue, int maxValue )
        {
            return RandomNumbers.Next( minValue, maxValue );
        }

        [Lisp( "math:random-double" )]
        public static double RandomDouble()
        {
            return RandomNumbers.NextDouble();
        }

        [Lisp( "math:init-random" )]
        public static void InitRandom()
        {
            RandomNumbers = new Random();
        }

        [Pure, Lisp( "math:abs" )]
        public static object Abs( object a )
        {
            if ( a is int )
            {
                return Math.Abs( ( int ) a );
            }
            else if ( a is long )
            {
                return Math.Abs( ( long ) a );
            }
            else if ( a is decimal )
            {
                return Math.Abs( ( decimal ) a );
            }
            else if ( a is double )
            {
                return Math.Abs( ( double ) a );
            }
            else if ( a is BigInteger )
            {
                return BigInteger.Abs( ( BigInteger ) a );
            }
            else if ( a is BigRational )
            {
                return BigRational.Abs( ( BigRational ) a );
            }
            else
            {
                return Complex.Abs( AsComplex( a ) );
            }
        }

        [Pure, Lisp( "math:acos" )]
        public static object Acos( object a )
        {
            if ( a is Complex )
            {
                return Complex.Acos( ( Complex ) a );
            }
            else
            {
                return Math.Acos( AsDouble( a ) );
            }
        }

        [Pure, Lisp( "math:asin" )]
        public static object Asin( object a )
        {
            if ( a is Complex )
            {
                return Complex.Asin( ( Complex ) a );
            }
            else
            {
                return Math.Asin( AsDouble( a ) );
            }
        }

        [Pure, Lisp( "math:atan" )]
        public static object Atan( object a )
        {
            if ( a is Complex )
            {
                return Complex.Atan( ( Complex ) a );
            }
            else
            {
                return Math.Atan( AsDouble( a ) );
            }
        }

        [Pure, Lisp( "math:ceiling" )]
        public static object Ceiling( object a )
        {
            if ( a is double )
            {
                return Math.Ceiling( ( double ) a );
            }
            else if ( Integerp( a ) )
            {
                return a;
            }
            else
            {
                return Math.Ceiling( AsDecimal( a ) );
            }
        }

        [Pure, Lisp( "math:conjugate" )]
        public static object Conjugate( object a )
        {
            return Complex.Conjugate( AsComplex( a ) );
        }

        [Pure, Lisp( "math:cos" )]
        public static object Cos( object a )
        {
            if ( a is Complex )
            {
                return Complex.Cos( ( Complex ) a );
            }
            else
            {
                return Math.Cos( AsDouble( a ) );
            }
        }

        [Pure, Lisp( "math:cosh" )]
        public static object Cosh( object a )
        {
            if ( a is Complex )
            {
                return Complex.Cosh( ( Complex ) a );
            }
            else
            {
                return Math.Cosh( AsDouble( a ) );
            }
        }

        [Pure, Lisp( "math:exp" )]
        public static object Exp( object a )
        {
            if ( a is Complex )
            {
                return Complex.Exp( ( Complex ) a );
            }
            else
            {
                return Math.Exp( AsDouble( a ) );
            }
        }

        [Pure, Lisp( "math:floor" )]
        public static object Floor( object a )
        {
            if ( a is double )
            {
                return Math.Floor( ( double ) a );
            }
            else if ( Integerp( a ) )
            {
                return a;
            }
            else
            {
                return Math.Floor( AsDecimal( a ) );
            }
        }

        [Pure, Lisp( "math:log" )]
        public static object Log( object a )
        {
            if ( a is Complex )
            {
                return Complex.Log( ( Complex ) a );
            }
            else if ( NotGreater( a, 0 ) )
            {
                return Complex.Log( AsComplex( a ) );
            }
            else
            {
                return Math.Log( AsDouble( a ) );
            }
        }

        [Pure, Lisp( "math:log10" )]
        public static object Log10( object a )
        {
            if ( a is Complex )
            {
                return Complex.Log10( ( Complex ) a );
            }
            else if ( NotGreater( a, 0 ) )
            {
                return Complex.Log10( AsComplex( a ) );
            }
            else
            {
                return Math.Log10( AsDouble( a ) );
            }
        }

        [Pure, Lisp( "math:pow" )]
        public static object Pow( object a1, object a2 )
        {
            if ( a1 is Complex || a2 is Complex )
            {
                return Complex.Pow( AsComplex( a1 ), AsComplex( a2 ) );
            }
            else if ( a1 is Double || a1 is decimal || a2 is double || a2 is decimal || a2 is BigRational )
            {
                var d1 = AsDouble( a1 );
                var d2 = AsDouble( a2 );

                if ( d1 < 0 )
                {
                    return Complex.Pow( new Complex( d1, 0 ), d2 );
                }
                else
                {
                    return Math.Pow( d1, d2 );
                }
            }
            else if ( a1 is BigRational )
            {
                var d1 = AsBigRational( a1 );
                var d2 = AsBigInteger( a2 );
                return Number.Shrink( BigRational.Pow( d1, d2 ) );
            }
            else
            {
                var d1 = AsBigInteger( a1 );
                var d2 = ( int ) AsBigInteger( a2 );
                if ( d2 < 0 )
                {
                    return Number.Shrink( BigRational.Pow( d1, d2 ) );
                }
                else
                {
                    return Number.Shrink( BigInteger.Pow( d1, d2 ) );
                }
            }
        }

        [Pure, Lisp( "math:round" )]
        public static object Round( object a )
        {
            if ( a is double )
            {
                return Math.Round( ( double ) a );
            }
            else if ( Integerp( a ) )
            {
                return a;
            }
            else
            {
                return Math.Round( AsDecimal( a ) );
            }
        }

        [Pure, Lisp( "math:round" )]
        public static object Round( object a, int decimals )
        {
            if ( a is double )
            {
                return Math.Round( ( double ) a, decimals );
            }
            else if ( Integerp( a ) )
            {
                return a;
            }
            else
            {
                return Math.Round( AsDecimal( a ), decimals );
            }
        }

        [Pure, Lisp( "math:sign" )]
        public static int Sign( object a )
        {
            if ( a is int )
            {
                return Math.Sign( ( int ) a );
            }
            else if ( a is long )
            {
                return Math.Sign( ( long ) a );
            }
            else if ( a is decimal )
            {
                return Math.Sign( ( decimal ) a );
            }
            else if ( a is double )
            {
                return Math.Sign( ( double ) a );
            }
            else if ( a is BigInteger )
            {
                var b = ( BigInteger ) a;
                return b < 0 ? -1 : b == 0 ? 0 : 1;
            }
            else if ( a is BigRational )
            {
                var b = ( BigRational ) a;
                return b < 0 ? -1 : b == 0 ? 0 : 1;
            }
            else
            {
                return Math.Sign( AsDouble( a ) );
            }
        }

        [Pure, Lisp( "math:sin" )]
        public static object Sin( object a )
        {
            if ( a is Complex )
            {
                return Complex.Sin( ( Complex ) a );
            }
            else
            {
                return Math.Sin( AsDouble( a ) );
            }
        }

        [Pure, Lisp( "math:sinh" )]
        public static object Sinh( object a )
        {
            if ( a is Complex )
            {
                return Complex.Sinh( ( Complex ) a );
            }
            else
            {
                return Math.Sinh( AsDouble( a ) );
            }
        }

        [Pure, Lisp( "math:sqrt" )]
        public static object Sqrt( object a )
        {
            if ( a is Complex )
            {
                return Complex.Sqrt( ( Complex ) a );
            }
            else if ( Less( a, 0 ) )
            {
                return Complex.Sqrt( AsComplex( a ) );
            }
            else
            {
                return Math.Sqrt( AsDouble( a ) );
            }
        }

        [Pure, Lisp( "math:tan" )]
        public static object Tan( object a )
        {
            if ( a is Complex )
            {
                return Complex.Tan( ( Complex ) a );
            }
            else
            {
                return Math.Tan( AsDouble( a ) );
            }
        }

        [Pure, Lisp( "math:tanh" )]
        public static object Tanh( object a )
        {
            if ( a is Complex )
            {
                return Complex.Tanh( ( Complex ) a );
            }
            else
            {
                return Math.Tanh( AsDouble( a ) );
            }
        }

        [Pure, Lisp( "math:truncate" )]
        public static object Truncate( object a )
        {
            if ( a is double )
            {
                return Math.Truncate( ( double ) a );
            }
            else if ( Integerp( a ) )
            {
                return a;
            }
            else
            {
                return Math.Truncate( AsDecimal( a ) );
            }
        }
    }
}