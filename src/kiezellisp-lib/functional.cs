#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;

    public partial class Runtime
    {
        #region Fields

        public static IApply CompareApply = new CompareApplyWrapper();
        public static IApply EqualApply = new EqualApplyWrapper();
        public static IApply IdentityApply = new IdentityApplyWrapper();
        public static IApply StructurallyEqualApply = new StructurallyEqualApplyWrapper();

        #endregion Fields

        #region Methods

        public static object Apply(IApply func, object[] args)
        {
            return func.Apply(args);
        }

        [Lisp("apply")]
        public static IApply ApplyStar(IApply func)
        {
            return new ApplyWrapper(func);
        }

        [Lisp("apply")]
        public static object ApplyStar(IApply func, params object[] args)
        {
            return func.Apply(MakeArrayStar(args));
        }

        [Lisp("complement")]
        public static object Complement(IApply func)
        {
            Func<object[], object> c = args =>
            {
                return !ToBool(Apply(func, args));
            };
            return new ApplyWrapper2(c);
        }

        [Lisp("compose")]
        public static IApply Compose(params IApply[] funcs)
        {
            if (funcs.Length == 0)
            {
                // todo return identity
                return (IApply)FindSymbol("lisp:identity").Value;
            }
            else if (funcs.Length == 1)
            {
                return funcs[0];
            }
            else
            {
                Func<object[], object> c = args =>
                {
                    var result = Apply(funcs[funcs.Length - 1], args);
                    for (var i = funcs.Length - 2; i >= 0; --i)
                    {
                        result = Funcall(funcs[i], result);
                    }
                    return result;
                };
                return new ApplyWrapper2(c);
            }
        }

        [Lisp("funcall")]
        public static object Funcall(IApply func, params object[] args)
        {
            if (func == null)
            {
                return Identity(args);
            }
            else
            {
                return func.Apply(args);
            }
        }

        public static bool FuncallBool(IApply func, params object[] args)
        {
            return ToBool(func.Apply(args));
        }

        public static int FuncallInt(IApply func, params object[] args)
        {
            return (int)func.Apply(args);
        }

        [Lisp("identity")]
        public static object Identity(params object[] a)
        {
            if (a.Length == 1)
            {
                return a[0];
            }
            else
            {
                return AsList(a);
            }
        }

        public static object Identity(object a)
        {
            return a;
        }

        #endregion Methods
    }
}