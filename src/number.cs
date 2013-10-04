// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Globalization;
using System.Numerics;
using Numerics;

namespace Kiezel
{
    abstract class Number
    {

        static int zero = ( int ) '0';
        static int nine = ( int ) '9';
        static int a = ( int ) 'a';
        static int z = ( int ) 'z';
        static int A = ( int ) 'A';
        static int Z = ( int ) 'Z';

        internal static string ConvertToString( BigInteger n, bool escape, int radix )
        {
            if ( radix == -1 )
            {
                radix = ( int ) Runtime.GetDynamic( Symbols.PrintBase );
            }

            if ( radix == -1 )
            {
                radix = 10;
            }
            else if ( radix < 2 || radix > 36 )
            {
                throw new LispException( "Invalid number base: {0}", radix );
            }

            if ( n == 0 )
            {
                return "0";
            }

            var sign = ( n >= 0 ) ? "" : "-";
            n = ( n >= 0 ) ? n : -n;
            var stk = new Vector();
            while ( n != 0 )
            {
                var d = ( int ) ( n % radix );
                if ( d <= 9 )
                {
                    stk.Add( ( char ) ( d + '0' ) );
                }
                else
                {
                    stk.Add( ( char ) ( d - 10 + 'a' ) );
                }
                n = n / radix;
            }
            stk.Reverse();
            if ( escape )
            {
                if ( radix == 10 )
                {
                    return sign + Runtime.MakeString( stk.ToArray() );
                }
                else if ( radix == 16 )
                {
                    return sign + "0x" + Runtime.MakeString( stk.ToArray() );
                }
                else if ( radix == 8 )
                {
                    return sign + "0" + Runtime.MakeString( stk.ToArray() );
                }
                else if ( radix == 2 )
                {
                    return "#b" + sign + Runtime.MakeString( stk.ToArray() );
                }
                else
                {
                    return "#" + radix.ToString() + "r" + sign + Runtime.MakeString( stk.ToArray() );
                }
            }
            else
            {
                return sign + Runtime.MakeString( stk.ToArray() );
            }
        }

        internal static int ParseNumberBase( string token, int numberBase )
        {
            BigInteger value;
            if ( !TryParseNumberBase( token, true, numberBase, out value ) )
            {
                throw new LispException( "invalid base {0} number: {1}", numberBase, token );
            }
            return ( int ) value;
        }

        internal static bool TryParseNumberBase( string token, bool negAllowed, int numberBase, out BigInteger result )
        {
            bool negative = false;
            int digits = 0;
            result = 0;

            foreach ( char ch in token )
            {
                if ( ch == '_' || ch == ',' )
                {
                    continue;
                }

                int digitCode = ( int ) ch;
                int digitValue = numberBase;

                if ( digits == 0 && negAllowed && ch == '-' )
                {
                    negative = !negative;
                    continue;
                }
                else if ( zero <= digitCode && digitCode <= nine )
                {
                    digitValue = digitCode - zero;
                }
                else if ( a <= digitCode && digitCode <= z )
                {
                    digitValue = digitCode - a + 10;
                }
                else if ( A <= digitCode && digitCode <= Z )
                {
                    digitValue = digitCode - A + 10;
                }

                if ( digitValue >= numberBase )
                {
                    return false;
                }

                result = numberBase * result + digitValue;
                ++digits;
            }

            if ( digits == 0 )
            {
                return false;
            }

            if ( negative )
            {
                result = -result;
            }

            return true;
        }


        internal static object TryParse( string str, CultureInfo culture, int numberBase )
        {
            string s = str;
            BigInteger result;

            if ( str == "." )
            {
                // Mono parses this as a zero.
                return null;
            }

            if ( numberBase != 0 && numberBase != 10 )
            {
                if ( TryParseNumberBase( s, true, numberBase, out result ) )
                {
                    return Shrink( result );
                }
                else
                {
                    return null;
                }
            }

            if ( numberBase == 0 && s.Length >= 3 && s[ 0 ] == '0' && s[ 1 ] == 'x' )
            {
                if ( TryParseNumberBase( s.Substring( 2 ), false, 16, out result ) )
                {
                    return Shrink( result );
                }
            }
            else if ( numberBase == 0 && s.Length >= 4 && s[ 0 ] == '-' && s[ 1 ] == '0' && s[ 2 ] == 'x' )
            {
                if ( TryParseNumberBase( s.Substring( 3 ), false, 16, out result ) )
                {
                    return Shrink( -result );
                }
            }
            else if ( numberBase == 0 && s.Length >= 2 && s[ 0 ] == '0' && s[ 1 ] != '.' )
            {
                if ( TryParseNumberBase( s.Substring( 1 ), false, 8, out result ) )
                {
                    return Shrink( result );
                }
            }
            else if ( numberBase == 0 && s.Length >= 3 && s[ 0 ] == '-' && s[ 1 ] == '0' && s[ 2 ] != '.' )
            {
                if ( TryParseNumberBase( s.Substring( 2 ), false, 8, out result ) )
                {
                    return Shrink( -result );
                }
            }
            else if ( s.IndexOf( '.' ) == -1 )
            {
                int pos = s.IndexOf( '/' );

                if ( 0 < pos && pos + 1 < s.Length )
                {
                    BigInteger numerator;
                    BigInteger denominator;

                    if ( TryParseNumberBase( s.Substring( 0, pos ), true, 10, out numerator )
                        && TryParseNumberBase( s.Substring( pos + 1 ), false, 10, out denominator ) )
                    {
                        return Shrink( new BigRational( numerator, denominator ) );
                    }
                }
                else
                {
                    if ( TryParseNumberBase( s, true, 10, out result ) )
                    {
                        return Shrink( result );
                    }
                }
            }
            else
            {
                decimal result2;
                double result3;

                if ( Runtime.ReadDecimalNumbers && decimal.TryParse( s, NumberStyles.Any, culture ?? CultureInfo.InvariantCulture, out result2 ) )
                {
                    return result2;
                }
                else if ( double.TryParse( s, NumberStyles.Any, culture ?? CultureInfo.InvariantCulture, out result3 ) )
                {
                    return result3;
                }
            }

            return null;
        }

        static internal bool CanShrink( BigRational d )
        {
            return ( d.Denominator == 1 );
        }

        internal static object Shrink( BigRational d )
        {
            if ( CanShrink( d ) )
            {
                return Shrink( d.Numerator );
            }
            else
            {
                return d;
            }
        }

        static internal bool CanShrink( Int64 d )
        {
            if ( Int32.MinValue <= d && d <= Int32.MaxValue )
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        internal static object Shrink( Int64 d )
        {
            if ( CanShrink( d ) )
            {
                return ( int ) d;
            }
            else
            {
                return d;
            }
        }

        static internal bool CanShrink( BigInteger d )
        {
            if ( Int64.MinValue <= d && d <= Int64.MaxValue )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static object Shrink( BigInteger d )
        {
            if ( CanShrink( d ) )
            {
                return Shrink( ( Int64 ) d );
            }
            else
            {
                return d;
            }
        }


    }

    abstract class Rational : Number
    {

    }

    abstract class Integer : Number
    {

    }

    abstract class Atom
    {

    }

    abstract class List
    {

    }

    abstract class Enumerable
    {
    }

    abstract class Keyword
    {

    }
}
