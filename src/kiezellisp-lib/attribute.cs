#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System.Linq;
    using System.Linq.Expressions;

    public partial class Runtime
    {
        #region Public Methods

        [Lisp("attr")]
        public static object Attr(object target, object attr)
        {
            var name = GetDesignatedString(attr);
            var binder = GetGetMemberBinder(name);
            var arg1 = CompileLiteral(target);
            var code = CompileDynamicExpression(binder, typeof(object), new Expression[] { arg1 });
            var result = Execute(code);
            return result;
        }

        // Handled by compiler if used as function in function call; otherwise
        // accessor creates a lambda.
        [Lisp(".")]
        public static object MemberAccessor(string member)
        {
            return new AccessorLambda(false, member);
        }

        [Lisp("?")]
        public static object NullableMemberAccessor(string member)
        {
            return new AccessorLambda(true, member);
        }

        [Lisp("set-attr")]
        public static object SetAttr(object target, object attr, object value)
        {
            var name = GetDesignatedString(attr);
            var binder = GetSetMemberBinder(name);
            var arg1 = CompileLiteral(target);
            var arg2 = CompileLiteral(value);
            var code = CompileDynamicExpression(binder, typeof(object), new Expression[] { arg1, arg2 });
            var result = Execute(code);
            return result;
        }

        #endregion Public Methods
    }
}