#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq.Expressions;
    using System.Numerics;
    using System.Text;

    using Numerics;

    using ActionFunc = System.Action<object>;

    using CompareFunc = System.Func<object, object, int>;

    using JustFunc = System.Func<object>;

    using KeyFunc = System.Func<object, object>;

    using PredicateFunc = System.Func<object, bool>;

    using TestFunc = System.Func<object, object, bool>;

    using ThreadFunc = System.Func<object>;

    public partial class Runtime
    {
        #region Methods

        [Lisp("as-big-integer")]
        public static BigInteger AsBigInteger(object a)
        {
            if (a is BigInteger)
            {
                return (BigInteger)a;
            }
            else if (a is BigRational)
            {
                var n = (BigRational)a;
                return n.GetWholePart();
            }
            else if (a is int)
            {
                return new BigInteger((int)a);
            }
            else if (a is long)
            {
                return new BigInteger((long)a);
            }
            else if (a is double)
            {
                return new BigInteger((double)a);
            }
            else if (a is decimal)
            {
                return new BigInteger((decimal)a);
            }
            else
            {
                return (BigInteger)a;
            }
        }

        [Lisp("as-big-rational")]
        public static BigRational AsBigRational(object a)
        {
            if (a is BigRational)
            {
                return (BigRational)a;
            }
            else
            {
                return new BigRational(AsBigInteger(a));
            }
        }

        [Lisp("as-complex")]
        public static Complex AsComplex(object a)
        {
            if (a is Complex)
            {
                return (Complex)a;
            }
            else
            {
                return new Complex(AsDouble(a), 0);
            }
        }

        [Lisp("as-decimal")]
        public static decimal AsDecimal(object a)
        {
            if (a is decimal)
            {
                return (decimal)a;
            }
            else if (a is BigInteger)
            {
                var n = (BigInteger)a;
                return (decimal)n;
            }
            else if (a is BigRational)
            {
                var n = (BigRational)a;
                return (decimal)n;
            }
            else
            {
                return Convert.ToDecimal(a);
            }
        }

        [Lisp("as-double")]
        public static double AsDouble(object a)
        {
            if (a is double)
            {
                return (double)a;
            }
            else if (a is BigInteger)
            {
                var n = (BigInteger)a;
                return (double)n;
            }
            else if (a is BigRational)
            {
                var n = (BigRational)a;
                return (double)n;
            }
            else
            {
                return Convert.ToDouble(a);
            }
        }

        [Lisp("as-int", "as-int32")]
        public static int AsInt(object a)
        {
            if (a is int)
            {
                return (int)a;
            }
            else if (a is long)
            {
                return (int)(long)a;
            }
            else if (a is BigInteger)
            {
                var n = (BigInteger)a;
                return (int)n;
            }
            else if (a is BigRational)
            {
                var n = (BigRational)a;
                return (int)n;
            }
            else
            {
                return Convert.ToInt32(a);
            }
        }

        [Lisp("as-long", "as-int64")]
        public static long AsLong(object a)
        {
            if (a is long)
            {
                return (long)a;
            }
            else if (a is BigInteger)
            {
                var n = (BigInteger)a;
                return (long)n;
            }
            else if (a is BigRational)
            {
                var n = (BigRational)a;
                return (long)n;
            }
            else
            {
                return Convert.ToInt64(a);
            }
        }

        [Lisp("as-single")]
        public static float AsSingle(object a)
        {
            if (a is float)
            {
                return (float)a;
            }
            else if (a is BigInteger)
            {
                var n = (BigInteger)a;
                return (float)n;
            }
            else if (a is BigRational)
            {
                var n = (BigRational)a;
                return (float)n;
            }
            else
            {
                return Convert.ToSingle(a);
            }
        }

        //
        // Used by ChangeTypeMethod
        //
        public static object ChangeType(object value, Type targetType)
        {
            if (value == null)
            {
                return null;
            }

            if (value is Vector && targetType.IsArray)
            {
                Type t = targetType.GetElementType();
                Vector v = (Vector)value;
                foreach (object item in ( Vector ) value)
                {
                    if (item != null && t != item.GetType())
                    {
                        return null;
                    }
                }
                // all items are of the correct type
                Array a = System.Array.CreateInstance(t, v.Count);
                v.CopyTo((object[])a, 0);
                return a;
            }

            if (value is string && targetType == typeof(char))
            {
                string s = (string)value;
                if (s.Length == 1)
                {
                    return s[0];
                }
                else
                {
                    return null;
                }
            }

            if (targetType == typeof(BigInteger))
            {
                if (value is Int32)
                {
                    return (BigInteger)(Int32)value;
                }

                if (value is Int64)
                {
                    return (BigInteger)(Int64)value;
                }
            }

            if (targetType == typeof(BigRational))
            {
                if (value is Int32)
                {
                    return (BigRational)(Int32)value;
                }

                if (value is Int64)
                {
                    return (BigRational)(Int64)value;
                }

                if (value is BigInteger)
                {
                    return (BigRational)(BigInteger)value;
                }
            }

            if (value is string && targetType.IsArray && targetType.GetElementType() == typeof(char))
            {
                string s = (String)value;
                return s.ToCharArray();
            }

            if (value is IApply)
            {
                if (targetType == typeof(EventHandler))
                {
                    return new EventHandler(new DelegateWrapper(value as IApply).Obj_Evt_Void);
                }
                else if (targetType == typeof(Predicate<object>))
                {
                    return new Predicate<object>(new DelegateWrapper(value as IApply).Obj_Bool);
                }
                else if (targetType == typeof(Func<object, bool>))
                {
                    return new Func<object, bool>(new DelegateWrapper(value as IApply).Obj_Bool);
                }
                else if (targetType == typeof(Func<object, object, bool>))
                {
                    return new Func<object, object, bool>(new DelegateWrapper(value as IApply).Obj_Obj_Bool);
                }
                else if (targetType == typeof(Action))
                {
                    return new Action(new DelegateWrapper(value as IApply).Void);
                }
                else if (targetType == typeof(Action<object>))
                {
                    return new Action<object>(new DelegateWrapper(value as IApply).Obj_Void);
                }
                else if (targetType == typeof(Comparison<object>))
                {
                    return new Comparison<object>(new DelegateWrapper(value as IApply).Compare);
                }
                else if (targetType == typeof(IComparer<object>))
                {
                    return new DelegateWrapper(value as IApply);
                }
                else if (targetType == typeof(IComparer))
                {
                    return new DelegateWrapper(value as IApply);
                }
                else if (targetType == typeof(IEqualityComparer))
                {
                    return new DelegateWrapper(value as IApply);
                }
                else if (targetType == typeof(IEqualityComparer<object>))
                {
                    return new DelegateWrapper(value as IApply);
                }
                else if (targetType == typeof(Converter<object, object>))
                {
                    return new Converter<object, object>(new DelegateWrapper(value as IApply).Obj_Obj);
                }
                else if (targetType == typeof(Func<object>))
                {
                    return new Func<object>(new DelegateWrapper(value as IApply).Obj);
                }
                else if (targetType == typeof(Func<object, object>))
                {
                    return new Func<object, object>(new DelegateWrapper(value as IApply).Obj_Obj);
                }
                else if (targetType == typeof(Func<object[], object>))
                {
                    return new Func<object[], object>(new DelegateWrapper(value as IApply).ObjA_Obj);
                }
                else if (targetType == typeof(Func<object, object, int>))
                {
                    return new Func<object, object, int>(new DelegateWrapper(value as IApply).Obj_Obj_Int);
                }
                else if (targetType == typeof(Func<object, object, object>))
                {
                    return new Func<object, object, object>(new DelegateWrapper(value as IApply).Obj_Obj_Obj);
                }
                else if (targetType == typeof(Func<object, int, object>))
                {
                    return new Func<object, int, object>(new DelegateWrapper(value as IApply).Obj_Int_Obj);
                }
                else if (targetType == typeof(Func<object, int, bool>))
                {
                    return new Func<object, int, bool>(new DelegateWrapper(value as IApply).Obj_Int_Bool);
                }
                else if (targetType == typeof(Func<object, IEnumerable<object>>))
                {
                    return new Func<object, IEnumerable<object>>(new DelegateWrapper(value as IApply).Obj_Enum);
                }
                else if (targetType == typeof(Func<object, int, IEnumerable<object>>))
                {
                    return new Func<object, int, IEnumerable<object>>(new DelegateWrapper(value as IApply).Obj_Int_Enum);
                }
            }

            if (typeof(IConvertible).IsAssignableFrom(value.GetType()) && typeof(IConvertible).IsAssignableFrom(targetType))
            {
                return Convert.ChangeType(value, targetType);
            }

            return value;
        }

        public static Symbol CheckKeyword(object val)
        {
            if (Keywordp(val))
            {
                return (Symbol)val;
            }

            throw new LispException("{0} is not a keyword", val);
        }

        public static Symbol CheckSymbol(object val)
        {
            if (val is Symbol)
            {
                return (Symbol)val;
            }

            throw new LispException("{0} is not a symbol", val);
        }

        public static Delegate ConvertToDelegate(Type type, IApply closure)
        {
            var expr = GetDelegateExpression(Expression.Constant(closure), type);
            return expr.Compile();
        }

        public static IApply GetClosure(object arg, IApply defaultValue = null)
        {
            return arg == null ? defaultValue : (IApply)arg;
        }

        //
        // typecasts
        //
        [Pure,
        Lisp("string")]
        public static string MakeString(params object[] objs)
        {
            return MakeStringFromObj(false, objs);
        }

        public static string MakeStringFromObj(bool insertSpaces, object obj)
        {
            if (obj == null)
            {
                return "";
            }
            else if (obj is string)
            {
                return (string)obj;
            }
            else if (obj is DateTime)
            {
                var dt = (DateTime)obj;
                if (dt.Hour == 0 && dt.Minute == 0 && dt.Second == 0)
                {
                    return dt.ToString("yyyy-MM-dd");
                }
                else
                {
                    return dt.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            else if (obj is StringBuilder || obj is StringWriter)
            {
                return obj.ToString();
            }
            else if (obj is bool)
            {
                return obj.ToString().ToLower();
            }
            else if (obj is ValueType)
            {
                return obj.ToString();
            }
            else if (obj is Prototype)
            {
                //return (string) VM.CoerceToType( (Instance) obj, VM.sym_string );
                return obj.ToString();
            }
            else if (obj is IEnumerable)
            {
                var buf = new StringWriter();
                var space = "";
                foreach (object item in ( ( IEnumerable ) obj ))
                {
                    buf.Write(space);
                    space = insertSpaces ? " " : "";

                    if (item is DictionaryEntry)
                    {
                        buf.Write(MakeStringFromObj(false, ((DictionaryEntry)item).Value));
                    }
                    else
                    {
                        buf.Write(MakeStringFromObj(false, item));
                    }
                }
                return buf.ToString();
            }
            else
            {
                return obj.ToString();
            }
        }

        public static Cons ToCons(object obj)
        {
            if (obj != null && !(obj is Cons))
            {
                throw new LispException("Cannot cast to Cons: {0}", ToPrintString(obj));
            }

            return (Cons)obj;
        }

        public static double ToDouble(object val)
        {
            if (val is double)
            {
                return (double)val;
            }
            else
            {
                return Convert.ToDouble(val);
            }
        }

        public static ICollection ToICollection(object obj)
        {
            if (obj == null)
            {
                // avoids crash
                return new object[ 0 ];
            }
            else if (obj is ICollection)
            {
                return (ICollection)obj;
            }
            else
            {
                throw new LispException("Cannot cast to ICollection: {0}", ToPrintString(obj));
            }
        }

        public static IList ToIList(object obj)
        {
            if (obj == null)
            {
                // avoids crash
                return new object[ 0 ];
            }
            else if (obj is IList)
            {
                return (IList)obj;
            }
            else
            {
                throw new LispException("Cannot cast to IList: {0}", ToPrintString(obj));
            }
        }

        public static int ToInt(object val)
        {
            if (val is int || val is SeqBase)
            {
                return (int)val;
            }
            else
            {
                return Convert.ToInt32(val);
            }
        }

        public static int ToInt(object val, int defaultValue)
        {
            if (val is int || val is SeqBase)
            {
                return (int)val;
            }
            else if (val == null)
            {
                return defaultValue;
            }
            else
            {
                return Convert.ToInt32(val);
            }
        }

        public static IEnumerable ToIter(object obj)
        {
            if (obj == null)
            {
                // avoids crash
                return new object[ 0 ];
            }
            else if (obj is IEnumerable)
            {
                return (IEnumerable)obj;
            }
            else
            {
                throw new LispException("Cannot cast to IEnumerable: {0}", ToPrintString(obj));
            }
        }

        public static string ToString(object obj)
        {
            if (obj == null)
            {
                return "";
            }
            else if (obj is string)
            {
                return (string)obj;
            }
            else if (obj is StringBuilder || obj is StringWriter)
            {
                return obj.ToString();
            }
            else
            {
                throw new LispException("Cannot cast to String: {0}", ToPrintString(obj));
            }
        }

        #endregion Methods
    }
}