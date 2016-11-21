#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System.Globalization;
    using System.Numerics;

    using Numerics;

    [RestrictedImport]
    public abstract class Number
    {
        #region Static Fields

        private static int a = 'a';
        private static int A = 'A';
        private static int nine = '9';
        private static int z = 'z';
        private static int Z = 'Z';
        private static int zero = '0';

        #endregion Static Fields

        #region Public Methods

        public static bool CanShrink(BigRational d)
        {
            return (d.Denominator == 1);
        }

        public static bool CanShrink(long d)
        {
            if (int.MinValue <= d && d <= int.MaxValue)
            {
                return true;
            }
            else {
                return false;
            }
        }

        public static bool CanShrink(BigInteger d)
        {
            if (long.MinValue <= d && d <= long.MaxValue)
            {
                return true;
            }
            else {
                return false;
            }
        }

        public static string ConvertToString(BigInteger n, bool escape, int radix)
        {
            if (radix == -1)
            {
                radix = (int)Runtime.GetDynamic(Symbols.PrintBase);
            }

            if (radix == -1)
            {
                radix = 10;
            }
            else if (radix < 2 || radix > 36)
            {
                throw new LispException("Invalid number base: {0}", radix);
            }

            if (n == 0)
            {
                return "0";
            }

            var sign = (n >= 0) ? "" : "-";
            n = (n >= 0) ? n : -n;
            var stk = new Vector();
            while (n != 0)
            {
                var d = (int)(n % radix);
                if (d <= 9)
                {
                    stk.Add((char)(d + '0'));
                }
                else {
                    stk.Add((char)(d - 10 + 'a'));
                }
                n = n / radix;
            }
            stk.Reverse();
            if (escape)
            {
                switch (radix)
                {
                    case 10:
                        return sign + Runtime.MakeString(stk.ToArray());
                    case 16:
                        return sign + "0x" + Runtime.MakeString(stk.ToArray());
                    case 8:
                        return sign + "0" + Runtime.MakeString(stk.ToArray());
                    case 2:
                        return "#b" + sign + Runtime.MakeString(stk.ToArray());
                    default:
                        return "#" + radix + "r" + sign + Runtime.MakeString(stk.ToArray());
                }
            }
            else {
                return sign + Runtime.MakeString(stk.ToArray());
            }
        }

        public static int ParseNumberBase(string token, int numberBase)
        {
            BigInteger value;
            if (!TryParseNumberBase(token, true, numberBase, out value))
            {
                throw new LispException("invalid base {0} number: {1}", numberBase, token);
            }
            return (int)value;
        }

        public static object Shrink(BigRational d)
        {
            if (CanShrink(d))
            {
                return Shrink(d.Numerator);
            }
            else {
                return d;
            }
        }

        public static object Shrink(long d)
        {
            if (CanShrink(d))
            {
                return (int)d;
            }
            else {
                return d;
            }
        }

        public static object Shrink(BigInteger d)
        {
            if (CanShrink(d))
            {
                return Shrink((long)d);
            }
            else {
                return d;
            }
        }

        public static object TryParse(string str, CultureInfo culture, int numberBase, bool decimalPointIsComma)
        {
            string s = str;
            BigInteger result;

            if (str == ".")
            {
                // Mono parses this as a zero.
                return null;
            }

            var point = decimalPointIsComma ? "," : ".";

            if (numberBase != 0 && numberBase != 10)
            {
                if (TryParseNumberBase(s, true, numberBase, out result))
                {
                    return Shrink(result);
                }
                else {
                    return null;
                }
            }

            if (numberBase == 0 && s.Length >= 3 && s[0] == '0' && s[1] == 'x')
            {
                if (TryParseNumberBase(s.Substring(2), false, 16, out result))
                {
                    return Shrink(result);
                }
            }
            else if (numberBase == 0 && s.Length >= 4 && s[0] == '-' && s[1] == '0' && s[2] == 'x')
            {
                if (TryParseNumberBase(s.Substring(3), false, 16, out result))
                {
                    return Shrink(-result);
                }
            }
            else if (numberBase == 0 && s.Length >= 2 && s[0] == '0' && s[1] != '.')
            {
                if (TryParseNumberBase(s.Substring(1), false, 8, out result))
                {
                    return Shrink(result);
                }
            }
            else if (numberBase == 0 && s.Length >= 3 && s[0] == '-' && s[1] == '0' && s[2] != '.')
            {
                if (TryParseNumberBase(s.Substring(2), false, 8, out result))
                {
                    return Shrink(-result);
                }
            }
            else if (s.IndexOf(point) == -1)
            {
                int pos = s.IndexOf('/');

                if (0 < pos && pos + 1 < s.Length)
                {
                    BigInteger numerator;
                    BigInteger denominator;

                    if (TryParseNumberBase(s.Substring(0, pos), true, 10, out numerator)
                        && TryParseNumberBase(s.Substring(pos + 1), false, 10, out denominator))
                    {
                        return Shrink(new BigRational(numerator, denominator));
                    }
                }
                else {
                    if (TryParseNumberBase(s, true, 10, out result))
                    {
                        return Shrink(result);
                    }
                }
            }
            else {
                decimal result2;
                double result3;

                s = s.Replace("_", "");

                if (decimalPointIsComma)
                {
                    s = s.Replace(".", "").Replace(",", ".");
                }
                else {
                    s = s.Replace(",", "");
                }

                if (Runtime.ReadDecimalNumbers && decimal.TryParse(s, NumberStyles.Any, culture ?? CultureInfo.InvariantCulture, out result2))
                {
                    return result2;
                }
                else if (double.TryParse(s, NumberStyles.Any, culture ?? CultureInfo.InvariantCulture, out result3))
                {
                    return result3;
                }
            }

            return null;
        }

        public static bool TryParseHexNumber(string token, out int result)
        {
            BigInteger number;
            if (TryParseNumberBase(token, false, 16, out number))
            {
                result = (int)number;
                return true;
            }
            result = 0;
            return false;
        }

        public static bool TryParseNumberBase(string token, bool negAllowed, int numberBase, out BigInteger result)
        {
            var negative = false;
            var digits = 0;
            result = 0;

            foreach (char ch in token)
            {
                if (ch == '_')
                {
                    continue;
                }

                var digitCode = (int)ch;
                var digitValue = numberBase;

                if (digits == 0 && negAllowed && ch == '-')
                {
                    negative = !negative;
                    continue;
                }
                else if (zero <= digitCode && digitCode <= nine)
                {
                    digitValue = digitCode - zero;
                }
                else if (a <= digitCode && digitCode <= z)
                {
                    digitValue = digitCode - a + 10;
                }
                else if (A <= digitCode && digitCode <= Z)
                {
                    digitValue = digitCode - A + 10;
                }

                if (digitValue >= numberBase)
                {
                    return false;
                }

                result = numberBase * result + digitValue;
                ++digits;
            }

            if (digits == 0)
            {
                return false;
            }

            if (negative)
            {
                result = -result;
            }

            return true;
        }

        #endregion Public Methods
    }
}