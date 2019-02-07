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
        public static object Attr(object target, object attr, params object[] args)
        {
            var name = GetDesignatedString(attr);
            var accessor = new AccessorLambda(false, name);
            var result = ApplyStar(accessor, target, args);
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
        public static object SetAttr(object target, object attr, params object[] args)
        {
            // last args is the value
            var name = GetDesignatedString(attr);
            var binder = GetSetMemberBinder(name);
            var code = CompileDynamicExpression(binder, typeof(object), CompileConstantTargetArgs(target, args));
            var result = Execute(code);
            return result;
        }

        #endregion Public Methods
    }
}