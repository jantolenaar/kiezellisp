#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.Numerics;

    using Numerics;

    using TestFunc = System.Func<object, object, bool>;

    public partial class Runtime
    {
        #region Public Methods

        [Pure,
        Lisp("+")]
        public static object Add(params object[] args)
        {
            if (args.Length == 0)
            {
                return 0;
            }
            else if (args.Length == 1)
            {
                return Add2(args[0], 0);
            }
            else
            {
                var result = args[0];
                for (var i = 1; i < args.Length; ++i)
                {
                    result = Add2(result, args[i]);
                }
                return result;
            }
        }

        public static object Add2(object a1, object a2)
        {
            // commonest case first
            if (a1 is int && a2 is int)
            {
                var i1 = (int)a1;
                var i2 = (int)a2;

                try
                {
                    checked
                    {
                        return i1 + i2;
                    }
                }
                catch
                {
                    return (long)i1 + (long)i2;
                }
            }
            else if (a1 is Complex || a2 is Complex)
            {
                return AsComplex(a1) + AsComplex(a2);
            }
            else if (a1 is double || a2 is double)
            {
                return AsDouble(a1) + AsDouble(a2);
            }
            else if (a1 is decimal || a2 is decimal)
            {
                decimal d1 = AsDecimal(a1);
                decimal d2 = AsDecimal(a2);

                try
                {
                    checked
                    {
                        return d1 + d2;
                    }
                }
                catch
                {
                    return (double)d1 + (double)d2;
                }
            }
            else if (a1 is float || a2 is float)
            {
                return AsSingle(a1) + AsSingle(a2);
            }
            else if (a1 is BigRational || a2 is BigRational)
            {
                return Number.Shrink(AsBigRational(a1) + AsBigRational(a2));
            }
            else if (a1 is BigInteger || a2 is BigInteger)
            {
                return Number.Shrink(AsBigInteger(a1) + AsBigInteger(a2));
            }
            else if (a1 is long || a2 is long)
            {
                long i1 = Convert.ToInt64(a1);
                long i2 = Convert.ToInt64(a2);

                try
                {
                    checked
                    {
                        return Number.Shrink(i1 + i2);
                    }
                }
                catch
                {
                    return Number.Shrink(new BigInteger(i1) + new BigInteger(i2));
                }
            }
            else if (a1 is DateTime && a2 is TimeSpan)
            {
                var a = (DateTime)a1;
                var b = (TimeSpan)a2;
                return a + b;
            }
            else if (a1 is TimeSpan && a2 is TimeSpan)
            {
                var a = (TimeSpan)a1;
                var b = (TimeSpan)a2;
                return a + b;
            }
            else if (a1 is TimeSpan && a2 is int)
            {
                var a = (TimeSpan)a1;
                var b = (int)a2;
                return a + new TimeSpan(b, 0, 0, 0);
            }
            else
            {
                return Add2(Convert.ToInt32(a1), Convert.ToInt32(a2));
            }
        }

        [Pure,
        Lisp("bit-and")]
        public static object BitAnd(params object[] args)
        {
            var result = args[0];

            for (var i = 1; i < args.Length; ++i)
            {
                result = BitAnd(result, args[i]);
            }

            return result;
        }

        public static object BitAnd(object a1, object a2)
        {
            if (a1 is BigInteger || a2 is BigInteger)
            {
                var d1 = AsBigInteger(a1);
                var d2 = AsBigInteger(a2);
                return Number.Shrink(d1 & d2);
            }
            else if (a1 is long || a2 is long)
            {
                var d1 = Convert.ToInt64(a1);
                var d2 = Convert.ToInt64(a2);
                return Number.Shrink(d1 & d2);
            }
            else if (a1 is int || a2 is int)
            {
                var d1 = Convert.ToInt32(a1);
                var d2 = Convert.ToInt32(a2);
                return d1 & d2;
            }
            else
            {
                //return CallOperatorMethod( "op_BitwiseAnd", a1, a2 );
                throw new LispException("BitAnd - not implemented");
            }
        }

        [Pure,
        Lisp("bit-not")]
        public static object BitNot(object a1)
        {
            if (a1 is BigInteger)
            {
                var d1 = AsBigInteger(a1);
                return Number.Shrink(~d1);
            }
            else if (a1 is long)
            {
                var d1 = Convert.ToInt64(a1);
                return Number.Shrink(~d1);
            }
            else if (a1 is int)
            {
                var d1 = Convert.ToInt32(a1);
                return ~d1;
            }
            else
            {
                //return CallOperatorMethod( "op_OnesComplement", a1 );
                throw new LispException("BitNot - not implemented");
            }
        }

        [Pure,
        Lisp("bit-or")]
        public static object BitOr(params object[] args)
        {
            var result = args[0];

            for (var i = 1; i < args.Length; ++i)
            {
                result = BitOr(result, args[i]);
            }

            return result;
        }

        public static object BitOr(object a1, object a2)
        {
            if (a1 is BigInteger || a2 is BigInteger)
            {
                var d1 = AsBigInteger(a1);
                var d2 = AsBigInteger(a2);
                return Number.Shrink(d1 | d2);
            }
            else if (a1 is long || a2 is long)
            {
                var d1 = Convert.ToInt64(a1);
                var d2 = Convert.ToInt64(a2);
                return Number.Shrink(d1 | d2);
            }
            else if (a1 is int || a2 is int)
            {
                var d1 = Convert.ToInt32(a1);
                var d2 = Convert.ToInt32(a2);
                return d1 | d2;
            }
            else
            {
                //return CallOperatorMethod( "op_BitwiseOr", a1, a2 );
                throw new LispException("BitOr - not implemented");
            }
        }

        [Pure,
        Lisp("bit-shift-left")]
        public static object BitShiftLeft(object a1, object a2)
        {
            if (a1 is int && a2 is int)
            {
                var i1 = ToInt(a1);
                var i2 = ToInt(a2);
                return i1 << i2;
            }
            else
            {
                //return CallOperatorMethod( "op_LeftShift", a1, a2 );
                throw new LispException("BitLeftShift - not implemented");
            }
        }

        [Pure,
        Lisp("bit-shift-right")]
        public static object BitShiftRight(object a1, object a2)
        {
            if (a1 is int && a2 is int)
            {
                var i1 = ToInt(a1);
                var i2 = ToInt(a2);
                return i1 >> i2;
            }
            else
            {
                //return CallOperatorMethod( "op_RightShift", a1, a2 );
                throw new LispException("BitRightShift - not implemented");
            }
        }

        [Pure,
        Lisp("bit-xor")]
        public static object BitXor(params object[] args)
        {
            var result = args[0];

            for (var i = 1; i < args.Length; ++i)
            {
                result = BitXor(result, args[i]);
            }

            return result;
        }

        public static object BitXor(object a1, object a2)
        {
            if (a1 is BigInteger || a2 is BigInteger)
            {
                var d1 = AsBigInteger(a1);
                var d2 = AsBigInteger(a2);
                return Number.Shrink(d1 ^ d2);
            }
            else if (a1 is long || a2 is long)
            {
                var d1 = Convert.ToInt64(a1);
                var d2 = Convert.ToInt64(a2);
                return Number.Shrink(d1 ^ d2);
            }
            else if (a1 is int || a2 is int)
            {
                var d1 = Convert.ToInt32(a1);
                var d2 = Convert.ToInt32(a2);
                return d1 ^ d2;
            }
            else
            {
                //return CallOperatorMethod( "op_ExclusiveOr", a1, a2 );
                throw new LispException("BitXor - not implemented");
            }
        }

        [Pure,
        Lisp("compare")]
        public static int Compare(object a1, object a2)
        {
            if (a1 == null && a2 == null)
            {
                return 0;
            }
            else if (a1 is int && a2 is int)
            {
                // commonest case first
                return ((int)a1).CompareTo((int)a2);
            }
            else if (a1 is string && a2 is string)
            {
                // otherwise 'A' in 'a'..'z' will be true
                return string.CompareOrdinal((string)a1, (string)a2);
            }
            else if (Numberp(a1) && Numberp(a2))
            {
                if (a1 is double || a2 is double)
                {
                    var d1 = AsDouble(a1);
                    var d2 = AsDouble(a2);
                    return d1.CompareTo(d2);
                }
                else if (a1 is decimal || a2 is decimal)
                {
                    var d1 = AsDecimal(a1);
                    var d2 = AsDecimal(a2);
                    return d1.CompareTo(d2);
                }
                if (a1 is float || a2 is float)
                {
                    var d1 = AsSingle(a1);
                    var d2 = AsSingle(a2);
                    return d1.CompareTo(d2);
                }
                else if (a1 is BigRational || a2 is BigRational)
                {
                    var d1 = AsBigRational(a1);
                    var d2 = AsBigRational(a2);
                    return d1.CompareTo(d2);
                }
                else if (a1 is BigInteger || a2 is BigInteger)
                {
                    var d1 = AsBigInteger(a1);
                    var d2 = AsBigInteger(a2);
                    return d1.CompareTo(d2);
                }
                else if (a1 is long || a2 is long)
                {
                    var d1 = Convert.ToInt64(a1);
                    var d2 = Convert.ToInt64(a2);
                    return d1.CompareTo(d2);
                }
            }
            else if (a1 is char && a2 is char)
            {
                return ((char)a1).CompareTo((char)a2);
            }
            else if (a1 is DateTime && a2 is DateTime)
            {
                return ((DateTime)a1).CompareTo((DateTime)a2);
            }
            else if (a1 is TimeSpan && a2 is TimeSpan)
            {
                return ((TimeSpan)a1).CompareTo((TimeSpan)a2);
            }
            else if (a1 is Symbol && a2 is Symbol)
            {
                return string.CompareOrdinal(((Symbol)a1).ContextualName, ((Symbol)a2).ContextualName);
            }

            throw new LispException("Cannot compare {0} and {1}", ToPrintString(a1), ToPrintString(a2));
        }

        [Pure,
        Lisp("compare-ci")]
        public static int CompareCaseInsensitive(object a, object b)
        {
            if (a is string && b is string)
            {
                return string.Compare((string)a, (string)b, true);
            }
            else if (a is char && b is char)
            {
                return char.ToLower((char)a).CompareTo(char.ToLower((char)b));
            }
            else
            {
                return Compare(a, b);
            }
        }

        [Pure,
        Lisp("dec")]
        public static object Dec(object a1)
        {
            if (a1 is int)
            {
                var i1 = (int)a1;

                try
                {
                    checked
                    {
                        return i1 - 1;
                    }
                }
                catch
                {
                    return (long)i1 - (long)1;
                }
            }
            else
            {
                return Sub(a1, 1);
            }
        }

        public static bool DecrementChar(ref char ch, out bool carry)
        {
            if (Char.IsLower(ch))
            {
                if (ch == 'a')
                {
                    carry = true;
                    ch = 'z';
                    return true;
                }
                else
                {
                    carry = false;
                    --ch;
                    return true;
                }
            }
            else if (Char.IsUpper(ch))
            {
                if (ch == 'A')
                {
                    carry = true;
                    ch = 'Z';
                    return true;
                }
                else
                {
                    carry = false;
                    --ch;
                    return true;
                }
            }
            else if (Char.IsDigit(ch))
            {
                if (ch == '0')
                {
                    carry = true;
                    ch = '9';
                    return true;
                }
                else
                {
                    carry = false;
                    --ch;
                    return true;
                }
            }
            else
            {
                carry = true;
                return false;
            }
        }

        public static string DecrementString(string s)
        {
            // todo: handle underflow

            if (s == "")
            {
                return s;
            }

            var a = s.ToCharArray();
            bool carry;
            for (var i = a.Length - 1; i >= 0; --i)
            {
                DecrementChar(ref a[i], out carry);
                if (!carry)
                {
                    break;
                }
            }
            return new string(a);
        }

        [Pure,
        Lisp("/")]
        public static object Div(params object[] args)
        {
            if (args.Length == 0)
            {
                return 1;
            }

            if (args.Length == 1)
            {
                return Div(1, args[0]);
            }

            var result = args[0];
            for (var i = 1; i < args.Length; ++i)
            {
                result = Div(result, args[i]);
            }
            return result;
        }

        public static object Div(object a1, object a2)
        {
            if (a1 is Complex || a2 is Complex)
            {
                return AsComplex(a1) / AsComplex(a2);
            }
            else if (a1 is double || a2 is double)
            {
                return AsDouble(a1) / AsDouble(a2);
            }
            else if (a1 is decimal || a2 is decimal)
            {
                var d1 = AsDecimal(a1);
                var d2 = AsDecimal(a2);

                try
                {
                    checked
                    {
                        return d1 / d2;
                    }
                }
                catch
                {
                    return (double)d1 / (double)d2;
                }
            }
            else if (a1 is float || a2 is float)
            {
                return AsSingle(a1) / AsSingle(a2);
            }
            else // if ( a1 is BigRational || a2 is BigRational )
            {
                return Number.Shrink(AsBigRational(a1) / AsBigRational(a2));
            }
        }

        [Pure,
        Lisp("divrem")]
        public static Cons Divrem(object a1)
        {
            return Divrem(1, a1);
        }

        [Pure,
        Lisp("divrem")]
        public static Cons Divrem(object a1, object a2)
        {
            // commonest case first
            if (a1 is int && a2 is int)
            {
                var d1 = (int)a1;
                var d2 = (int)a2;
                var rem = d1 % d2;
                var quo = (d1 - rem) / d2;
                return MakeList(quo, rem);
            }
            else if (a1 is double || a2 is double)
            {
                var d1 = AsDouble(a1);
                var d2 = AsDouble(a2);
                var rem = d1 % d2;
                var quo = (d1 - rem) / d2;
                return MakeList(quo, rem);
            }
            else if (a1 is decimal || a2 is decimal)
            {
                var d1 = AsDecimal(a1);
                var d2 = AsDecimal(a2);
                var rem = d1 % d2;
                var quo = (d1 - rem) / d2;
                return MakeList(quo, rem);
            }
            else if (a1 is BigRational || a2 is BigRational)
            {
                var d1 = AsBigRational(a1);
                var d2 = AsBigRational(a2);
                var rem = d1 % d2;
                var quo = (d1 - rem) / d2;
                return MakeList(Number.Shrink(quo), Number.Shrink(rem));
            }
            else if (a1 is float || a2 is float)
            {
                var d1 = AsSingle(a1);
                var d2 = AsSingle(a2);
                var rem = d1 % d2;
                var quo = (d1 - rem) / d2;
                return MakeList(quo, rem);
            }
            else if (a1 is BigInteger || a2 is BigInteger)
            {
                var d1 = AsBigInteger(a1);
                var d2 = AsBigInteger(a2);
                var rem = d1 % d2;
                var quo = (d1 - rem) / d2;
                return MakeList(Number.Shrink(quo), Number.Shrink(rem));
            }
            else if (a1 is long || a2 is long)
            {
                long d1 = Convert.ToInt64(a1);
                long d2 = Convert.ToInt64(a2);
                var rem = d1 % d2;
                var quo = (d1 - rem) / d2;
                return MakeList(Number.Shrink(quo), Number.Shrink(rem));
            }
            else
            {
                return Divrem(Convert.ToInt32(a1), Convert.ToInt32(a2));
            }
        }

        [Pure,
        Lisp("eq")]
        public static bool Eq(object a, object b)
        {
            return Object.ReferenceEquals(a, b);
        }

        [Pure,
        Lisp("eql")]
        public static bool Eql(object a, object b)
        {
            if (Eq(a, b))
            {
                return true;
            }

            if (Object.Equals(a, b))
            {
                return true;
            }

            return false;
        }

        [Pure,
        Lisp("=", "equal")]
        public static bool Equal(object a, object b)
        {
            if (Eql(a, b))
            {
                return true;
            }

            if (a == null || b == null)
            {
                return false;
            }

            if (a is Complex)
            {
                if (b is Complex)
                {
                    return (Complex)a == (Complex)b;
                }
                else if (Numberp(b))
                {
                    return ((Complex)a).Equals(AsComplex(b));
                }
                else
                {
                    return false;
                }
            }

            if (b is Complex)
            {
                if (Numberp(a))
                {
                    return ((Complex)b).Equals(AsComplex(a));
                }
                else
                {
                    return false;
                }
            }

            if (Numberp(a) && Numberp(b))
            {
                return Compare(a, b) == 0;
            }

            if (a is Cons && b is Cons)
            {
                var a1 = (Cons)a;
                var b1 = (Cons)b;
                return Equal(Car(a1), Car(b1)) && Equal(Cdr(a1), Cdr(b1));
            }

            return false;
        }

        [Pure,
        Lisp("=", "equal")]
        public static bool Equal(params object[] args)
        {
            return IterateBinaryTestOperator(Equal, args);
        }

        [Pure,
        Lisp("equal-ci")]
        public static bool EqualCaseInsensitive(object a, object b)
        {
            if (a is string && b is string)
            {
                return String.Compare((string)a, (string)b, true) == 0;
            }
            else if (a is char && b is char)
            {
                return Char.ToLower((char)a) == Char.ToLower((char)b);
            }
            else
            {
                return Equal(a, b);
            }
        }

        public static CultureInfo GetCultureInfo(object ident)
        {
            if (ident == null)
            {
                return CultureInfo.InvariantCulture;
            }
            else
            {
                return ident as CultureInfo ?? CultureInfo.GetCultureInfo(GetDesignatedString(ident));
            }
        }

        [Pure,
        Lisp(">")]
        public static bool Greater(object a1, object a2)
        {
            return Compare(a1, a2) > 0;
        }

        [Pure,
        Lisp(">")]
        public static bool Greater(params object[] args)
        {
            return IterateBinaryTestOperator(Greater, args);
        }

        [Pure,
        Lisp("inc")]
        public static object Inc(object a1)
        {
            if (a1 is int)
            {
                var i1 = (int)a1;

                try
                {
                    checked
                    {
                        return i1 + 1;
                    }
                }
                catch
                {
                    return (long)i1 + (long)1;
                }
            }
            else
            {
                return Add2(a1, 1);
            }
        }

        public static bool IncrementChar(ref char ch, out bool carry)
        {
            if (Char.IsLower(ch))
            {
                if (ch == 'z')
                {
                    carry = true;
                    ch = 'a';
                    return true;
                }
                else
                {
                    carry = false;
                    ++ch;
                    return true;
                }
            }
            else if (Char.IsUpper(ch))
            {
                if (ch == 'Z')
                {
                    carry = true;
                    ch = 'A';
                    return true;
                }
                else
                {
                    carry = false;
                    ++ch;
                    return true;
                }
            }
            else if (Char.IsDigit(ch))
            {
                if (ch == '9')
                {
                    carry = true;
                    ch = '0';
                    return true;
                }
                else
                {
                    carry = false;
                    ++ch;
                    return true;
                }
            }
            else
            {
                carry = true;
                return false;
            }
        }

        public static string IncrementString(string s)
        {
            if (s == "")
            {
                return s;
            }

            var a = s.ToCharArray();
            var carry = false;
            for (var i = a.Length - 1; i >= 0; --i)
            {
                if (IncrementChar(ref a[i], out carry))
                {
                }
                if (!carry)
                {
                    break;
                }
            }
            s = new string(a);
            return s;
        }

        public static bool IterateBinaryTestOperator(TestFunc test, object[] args)
        {
            if (args.Length < 2)
            {
                //throw new LispException("Too few arguments");
                return true;
            }
            for (var i = 0; i + 1 < args.Length; ++i)
            {
                if (!test(args[i], args[i + 1]))
                {
                    return false;
                }
            }
            return true;
        }

        [Pure,
        Lisp("<")]
        public static bool Less(object a1, object a2)
        {
            return Compare(a1, a2) < 0;
        }

        [Pure,
        Lisp("<")]
        public static bool Less(params object[] args)
        {
            return IterateBinaryTestOperator(Less, args);
        }

        [Pure,
        Lisp("and")]
        public static object LogicalAnd(object a1, object a2)
        {
            if (ToBool(a1))
            {
                return a2;
            }
            else
            {
                return a1;
            }
        }

        [Pure,
        Lisp("and")]
        public static object LogicalAnd(params object[] args)
        {
            object result = true;
            foreach (var a in args)
            {
                result = a;
                if (!ToBool(result))
                {
                    break;
                }
            }
            return result;
        }

        [Pure,
        Lisp("if")]
        public static object LogicalIf(object a1, object a2)
        {
            return LogicalIf(a1, a2, null);
        }

        [Pure,
        Lisp("if")]
        public static object LogicalIf(object a1, object a2, object a3)
        {
            if (ToBool(a1))
            {
                return a2;
            }
            else
            {
                return a3;
            }
        }

        [Pure,
        Lisp("or")]
        public static object LogicalOr(object a1, object a2)
        {
            if (ToBool(a1))
            {
                return a1;
            }
            else
            {
                return a2;
            }
        }

        [Pure,
        Lisp("or")]
        public static object LogicalOr(params object[] args)
        {
            object result = false;
            foreach (var a in args)
            {
                result = a;
                if (ToBool(result))
                {
                    break;
                }
            }
            return result;
        }

        [Pure,
        Lisp("complex")]
        public static Complex MakeComplex(object r, object i)
        {
            return new Complex(AsDouble(r), AsDouble(i));
        }

        [Pure,
        Lisp("complex-from-polar-coordinates")]
        public static Complex MakeComplexFromPolarCoordinates(object a, object b)
        {
            return Complex.FromPolarCoordinates(AsDouble(a), AsDouble(b));
        }

        [Pure]
        [Lisp("max")]
        public static object Max(params object[] args)
        {
            if (args.Length == 0)
            {
                return MissingValue;
            }
            else
            {
                var result = args[0];
                for (var i = 1; i < args.Length; ++i)
                {
                    if (Greater(args[i], result))
                    {
                        result = args[i];
                    }
                }
                return result;
            }
        }

        [Pure]
        [Lisp("min")]
        public static object Min(params object[] args)
        {
            if (args.Length == 0)
            {
                return MissingValue;
            }
            else
            {
                var result = args[0];
                for (var i = 1; i < args.Length; ++i)
                {
                    if (Less(args[i], result))
                    {
                        result = args[i];
                    }
                }
                return result;
            }
        }

        [Pure,
        Lisp("*")]
        public static object Mul(params object[] args)
        {
            if (args.Length == 0)
            {
                return 1;
            }

            object result = args[0];
            for (var i = 1; i < args.Length; ++i)
            {
                result = Mul(result, args[i]);
            }
            return result;
        }

        public static object Mul(object a1, object a2)
        {
            // commonest case first
            if (a1 is int && a2 is int)
            {
                var i1 = (int)a1;
                var i2 = (int)a2;

                try
                {
                    checked
                    {
                        return i1 * i2;
                    }
                }
                catch
                {
                    return (long)i1 * (long)i2;
                }
            }
            else if (a1 is Complex || a2 is Complex)
            {
                return AsComplex(a1) * AsComplex(a2);
            }
            else if (a1 is double || a2 is double)
            {
                return AsDouble(a1) * AsDouble(a2);
            }
            else if (a1 is decimal || a2 is decimal)
            {
                var d1 = AsDecimal(a1);
                var d2 = AsDecimal(a2);

                try
                {
                    checked
                    {
                        return d1 * d2;
                    }
                }
                catch
                {
                    return (double)d1 * (double)d2;
                }
            }
            else if (a1 is float || a2 is float)
            {
                return AsSingle(a1) * AsSingle(a2);
            }
            else if (a1 is BigRational || a2 is BigRational)
            {
                return Number.Shrink(AsBigRational(a1) * AsBigRational(a2));
            }
            else if (a1 is BigInteger || a2 is BigInteger)
            {
                return Number.Shrink(AsBigInteger(a1) * AsBigInteger(a2));
            }
            else if (a1 is long || a2 is long)
            {
                var i1 = Convert.ToInt64(a1);
                var i2 = Convert.ToInt64(a2);

                try
                {
                    checked
                    {
                        return i1 * i2;
                    }
                }
                catch
                {
                    return Number.Shrink(new BigInteger(i1) * new BigInteger(i2));
                }
            }
            else
            {
                return Mul(Convert.ToInt32(a1), Convert.ToInt32(a2));
            }
        }

        [Pure,
        Lisp("natural-compare")]
        public static int NaturalCompare(object x, object y, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "ignore-whitespace", "compact-whitespace", "punctuation-is-whitespace", "culture" }, false, true, true, null);
            var ignoreWhitespace = ToBool(kwargs[0]);
            var compactWhitespace = ToBool(kwargs[1]);
            var punctuationIsWhite = ToBool(kwargs[2]);
            var culture = GetCultureInfo(kwargs[3]);
            var ascending = 1;

            Func<char, bool> IsWhiteSpace = ch => Char.IsWhiteSpace(ch) || (punctuationIsWhite && Char.IsPunctuation(ch));

            var s1 = (x == null) ? null : x.ToString();
            var s2 = (y == null) ? null : y.ToString();

            var n1 = (s1 == null) ? 0 : s1.Length;
            var n2 = (s2 == null) ? 0 : s2.Length;

            var i1 = 0;
            var i2 = 0;

            while (i1 < n1 && i2 < n2)
            {
                var c1 = s1[i1];
                var c2 = s2[i2];

                if (ignoreWhitespace)
                {
                    if (IsWhiteSpace(c1))
                    {
                        ++i1;
                        continue;
                    }

                    if (IsWhiteSpace(c2))
                    {
                        ++i2;
                        continue;
                    }
                }
                else if (compactWhitespace)
                {
                    if (IsWhiteSpace(c1))
                    {
                        c1 = ' ';
                        while (i1 + 1 < n1 && IsWhiteSpace(s1[i1 + 1]))
                        {
                            ++i1;
                        }
                    }

                    if (IsWhiteSpace(c2))
                    {
                        c2 = ' ';
                        while (i2 + 1 < n2 && IsWhiteSpace(s2[i2 + 1]))
                        {
                            ++i2;
                        }
                    }
                }

                if (Char.IsDigit(c1) && Char.IsDigit(c2))
                {
                    // digit <-> digit
                    var v1 = 0;
                    var v2 = 0;
                    var l1 = 0;
                    var l2 = 0;

                    while (i1 < n1 && Char.IsDigit(s1[i1]))
                    {
                        v1 = (v1 * 10) + Convert.ToInt32(s1[i1]) - Convert.ToInt32('0');
                        ++i1;
                        ++l1;
                    }

                    while (i2 < n2 && Char.IsDigit(s2[i2]))
                    {
                        v2 = (v2 * 10) + Convert.ToInt32(s2[i2]) - Convert.ToInt32('0');
                        ++i2;
                        ++l2;
                    }

                    if (v1 != v2)
                    {
                        return ascending * Math.Sign(v1 - v2);
                    }
                    else if (l1 != l2)
                    {
                        return ascending * Math.Sign(l2 - l1);
                    }
                }
                else
                {
                    var d = String.Compare(new string(c1, 1), new string(c2, 1), true, culture);
                    if (d != 0)
                    {
                        return ascending * d;
                    }
                    ++i1;
                    ++i2;
                }
            }

            if (i1 == n1 && i2 == n2)
            {
                return 0;
            }
            else if (i1 == n1)
            {
                return ascending * -1;
            }
            else
            {
                return ascending * 1;
            }
        }

        [Lisp("neg")]
        public static object Neg(object a1)
        {
            if (a1 is int)
            {
                var d1 = (int)a1;
                try
                {
                    checked
                    {
                        return -d1;
                    }
                }
                catch
                {
                    return -(long)d1;
                }
            }
            else if (a1 is Complex)
            {
                return -AsComplex(a1);
            }
            else if (a1 is double)
            {
                return -AsDouble(a1);
            }
            else if (a1 is float)
            {
                return -AsSingle(a1);
            }
            else if (a1 is decimal)
            {
                return -AsDecimal(a1);
            }
            else if (a1 is BigRational)
            {
                return -AsBigRational(a1);
            }
            else if (a1 is BigInteger)
            {
                return -AsBigInteger(a1);
            }
            else if (a1 is long)
            {
                long i1 = Convert.ToInt64(a1);

                try
                {
                    checked
                    {
                        return -i1;
                    }
                }
                catch
                {
                    return Number.Shrink(-1 * (new BigInteger(i1)));
                }
            }
            else
            {
                return Neg(Convert.ToInt32(a1));
            }
        }

        [Pure,
        Lisp("not")]
        public static bool Not(object a1)
        {
            return !ToBool(a1);
        }

        [Pure,
        Lisp("/=")]
        public static bool NotEqual(object a, object b)
        {
            return !Equal(a, b);
        }

        [Pure,
        Lisp("/=")]
        public static bool NotEqual(params object[] args)
        {
            if (args.Length < 2)
            {
                throw new LispException("Too few arguments.");
            }
            for (var i = 0; i < args.Length - 1; ++i)
            {
                for (var j = i + 1; j < args.Length; ++j)
                {
                    if (Equal(args[i], args[j]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        [Pure,
        Lisp("<=")]
        public static bool NotGreater(object a1, object a2)
        {
            return Compare(a1, a2) <= 0;
        }

        [Pure,
        Lisp("<=")]
        public static bool NotGreater(params object[] args)
        {
            return IterateBinaryTestOperator(NotGreater, args);
        }

        [Pure,
        Lisp(">=")]
        public static bool NotLess(object a1, object a2)
        {
            return Compare(a1, a2) >= 0;
        }

        [Pure,
        Lisp(">=")]
        public static bool NotLess(params object[] args)
        {
            return IterateBinaryTestOperator(NotLess, args);
        }

        [Pure,
        Lisp("%")]
        public static object Rem(object a1, object a2)
        {
            var m = Divrem(a1, a2);
            return Second(m);
        }

        [Pure,
        Lisp("structurally-equal")]
        public static bool StructurallyEqual(object a, object b)
        {
            if (a is Cons && b is Cons)
            {
                return StructurallyEqual(Car((Cons)a), Car((Cons)b)) && StructurallyEqual(Cdr((Cons)a), Cdr((Cons)b));
            }

            if ((Listp(a) || a is IList) && (Listp(b) || b is IList))
            {
                if (Listp(a))
                {
                    a = AsVector((Cons)a);
                }

                if (Listp(b))
                {
                    b = AsVector((Cons)b);
                }

                var aa = (IList)a;
                var bb = (IList)b;
                if (aa.Count != bb.Count)
                {
                    return false;
                }
                for (var i = 0; i < aa.Count; ++i)
                {
                    if (!StructurallyEqual(aa[i], bb[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            return Equal(a, b);
        }

        [Pure,
        Lisp("-")]
        public static object Sub(params object[] args)
        {
            if (args.Length == 0)
            {
                return 0;
            }

            if (args.Length == 1)
            {
                return Neg(args[0]);
            }

            var result = args[0];
            for (var i = 1; i < args.Length; ++i)
            {
                result = Sub(result, args[i]);
            }
            return result;
        }

        public static object Sub(object a1, object a2)
        {
            // commonest case first
            if (a1 is int && a2 is int)
            {
                var i1 = (int)a1;
                var i2 = (int)a2;

                try
                {
                    checked
                    {
                        return i1 - i2;
                    }
                }
                catch
                {
                    return (long)i1 - (long)i2;
                }
            }
            else if (a1 is Complex || a2 is Complex)
            {
                return AsComplex(a1) - AsComplex(a2);
            }
            else if (a1 is double || a2 is double)
            {
                return AsDouble(a1) - AsDouble(a2);
            }
            else if (a1 is decimal || a2 is decimal)
            {
                var d1 = AsDecimal(a1);
                var d2 = AsDecimal(a2);

                try
                {
                    checked
                    {
                        return d1 - d2;
                    }
                }
                catch
                {
                    return (double)d1 - (double)d2;
                }
            }
            else if (a1 is float || a2 is float)
            {
                return AsSingle(a1) - AsSingle(a2);
            }
            else if (a1 is BigInteger || a2 is BigInteger)
            {
                return Number.Shrink(AsBigInteger(a1) - AsBigInteger(a2));
            }
            else if (a1 is long || a2 is long)
            {
                var i1 = Convert.ToInt64(a1);
                var i2 = Convert.ToInt64(a2);

                try
                {
                    checked
                    {
                        return Number.Shrink(i1 - i2);
                    }
                }
                catch
                {
                    return Number.Shrink(new BigInteger(i1) - new BigInteger(i2));
                }
            }
            else if (a1 is DateTime && a2 is DateTime)
            {
                var a = (DateTime)a1;
                var b = (DateTime)a2;
                return a - b;
            }
            else if (a1 is DateTime && a2 is TimeSpan)
            {
                var a = (DateTime)a1;
                var b = (TimeSpan)a2;
                return a - b;
            }
            else if (a1 is TimeSpan && a2 is TimeSpan)
            {
                var a = (TimeSpan)a1;
                var b = (TimeSpan)a2;
                return a - b;
            }
            else if (a1 is TimeSpan && a2 is int)
            {
                var a = (TimeSpan)a1;
                var b = (int)a2;
                return a - new TimeSpan(b, 0, 0, 0);
            }
            else
            {
                return Sub(Convert.ToInt32(a1), Convert.ToInt32(a2));
            }
        }

        [Pure,
        Lisp("typeof")]
        public static object TypeOf(object a)
        {
            if (a == null)
            {
                return null;
            }
            else if (a is Prototype)
            {
                return a;
            }
            else
            {
                return a.GetType();
            }
        }

        [Pure,
        Lisp("xor")]
        public static object Xor(params object[] args)
        {
            if (args.Length == 0)
            {
                return null;
            }

            var result = args[0];
            for (var i = 1; i < args.Length; ++i)
            {
                result = Xor(result, args[i]);
            }
            return result;
        }

        public static object Xor(object a1, object a2)
        {
            var b1 = ToBool(a1);
            var b2 = ToBool(a2);

            if (b1 && !b2)
            {
                return a1;
            }
            else if (!b1 && b2)
            {
                return a2;
            }
            else
            {
                return null;
            }
        }

        #endregion Public Methods
    }
}
