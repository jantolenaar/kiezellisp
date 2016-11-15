#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;

    public class DelayedExpression
    {
        #region Fields

        public IApply Recipe;
        public object Result;

        #endregion Fields

        #region Constructors

        public DelayedExpression(IApply code)
        {
            Recipe = code;
            Result = null;
        }

        #endregion Constructors

        #region Methods

        public object GetValue()
        {
            if (Recipe != null)
            {
                Result = Runtime.Funcall(Recipe);
                Recipe = null;
            }
            return Result;
        }

        public override string ToString()
        {
            return System.String.Format("DelayedExpr Result={0}", Runtime.ToPrintString(Result));
        }

        #endregion Methods
    }

    public partial class Runtime
    {
        #region Methods

        [Lisp("system:create-delayed-expression")]
        public static DelayedExpression CreateDelayedExpression(IApply func)
        {
            return new DelayedExpression(func);
        }

        [Lisp("force", "bq:force")]
        public static object Force(object expr)
        {
            if (expr is DelayedExpression)
            {
                return ((DelayedExpression)expr).GetValue();
            }
            else if (expr is Cons)
            {
                foreach (var obj in ( Cons ) expr)
                {
                    Force(obj);
                }
                return expr;
            }
            else
            {
                return expr;
            }
        }

        [Lisp("forced?")]
        public static object Forced(object expr)
        {
            if (expr is DelayedExpression)
            {
                return false;
            }
            else if (expr is Cons)
            {
                return ((Cons)expr).Forced;
            }
            else
            {
                return true;
            }
        }

        [Lisp("system:get-delayed-expression-result")]
        public static object GetDelayedExpressionResult(DelayedExpression expr)
        {
            return expr.GetValue();
        }

        #endregion Methods
    }
}