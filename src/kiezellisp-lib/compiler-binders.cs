#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    //InvokeMemberBinderKey
    public class InvokeMemberBinderKey
    {
        #region Fields

        private readonly int _count;
        private readonly string _name;

        #endregion Fields

        #region Constructors

        public InvokeMemberBinderKey(string name, int count)
        {
            _name = name;
            _count = count;
        }

        #endregion Constructors

        #region Public Properties

        public int Count
        {
            get
            {
                return _count;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        #endregion Public Properties

        #region Public Methods

        public override bool Equals(object obj)
        {
            var key = obj as InvokeMemberBinderKey;
            return key != null && key._name == _name && key._count == _count;
        }

        public override int GetHashCode()
        {
            // Stolen from DLR sources when it overrode GetHashCode on binders.
            return 0x28000000 ^ _name.GetHashCode() ^ _count.GetHashCode();
        }

        #endregion Public Methods
    }

    public class KiezelGetIndexBinder : GetIndexBinder
    {
        #region Constructors

        public KiezelGetIndexBinder(int count)
            : base(Runtime.GetCallInfo(count))
        {
        }

        #endregion Constructors

        #region Public Methods

        public override DynamicMetaObject FallbackGetIndex(
            DynamicMetaObject target, DynamicMetaObject[] indexes,
            DynamicMetaObject errorSuggestion)
        {
            var deferArgs = Runtime.CheckDeferArgs(target, indexes);
            if (deferArgs != null)
            {
                return Defer(deferArgs);
            }

            if (target.Value == null)
            {
                var expr = Expression.Constant(null);
                var restrictions2 = BindingRestrictions.GetInstanceRestriction(target.Expression, null);
                return new DynamicMetaObject(expr, restrictions2);
            }

            // Find our own binding.
            //
            // Conversions created in GetIndexExpression must be consistent with
            // restrictions made in GetTargetArgsRestrictions.
            var indexingExpr = Runtime.GetIndexingExpression(target, indexes);

            if (indexingExpr == null)
            {
                return errorSuggestion ??
                Runtime.CreateThrow(
                    target, indexes, BindingRestrictions.Empty,
                    typeof(InvalidOperationException),
                    "No get indexer found: " + target.LimitType.FullName + Runtime.CollectParameterInfo(target, indexes));
            }

            var restrictions = Runtime.GetTargetArgsRestrictions(target, indexes, false);
            return new DynamicMetaObject(Runtime.EnsureObjectResult(indexingExpr), restrictions);
        }

        #endregion Public Methods
    }

    public class KiezelGetMemberBinder : GetMemberBinder
    {
        #region Constructors

        public KiezelGetMemberBinder(string name)
            : base(name, true)
        {
        }

        #endregion Constructors

        #region Public Methods

        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            // Used by (attr obj name)

            if (!target.HasValue)
            {
                return Defer(target);
            }

            var flags = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            var name = Name.LispToPascalCaseName();
            var members = target.LimitType.GetMember(name, flags);

            if (target.Value == null)
            {
                var id = target.LimitType.Name + "." + name;
                return Runtime.CheckTargetNullReference(target, "Cannot get property on null reference:" + id + "(null)");
            }

            if (members.Length == 1 && (members[0] is PropertyInfo || members[0] is FieldInfo))
            {
                var expr = Expression.MakeMemberAccess(Expression.Convert(target.Expression, members[0].DeclaringType), members[0]);

                return new DynamicMetaObject(Runtime.EnsureObjectResult(expr),
                    // Don't need restriction test for name since this
                    // rule is only used where binder is used, which is
                    // only used in sites with this binder.Name.
                    BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));
            }
            else {
                return errorSuggestion ??
                Runtime.CreateThrow(
                    target, null,
                    BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType),
                    typeof(MissingMemberException),
                    "Property or field not found: " + target.LimitType.Name + "." + name + Runtime.CollectParameterInfo(target));
            }
        }

        #endregion Public Methods
    }

    public class KiezelInvokeBinder : InvokeBinder
    {
        #region Constructors

        public KiezelInvokeBinder(int count)
            : base(Runtime.GetCallInfo(count))
        {
        }

        #endregion Constructors

        #region Public Methods

        public override DynamicMetaObject FallbackInvoke(
            DynamicMetaObject target, DynamicMetaObject[] args,
            DynamicMetaObject errorSuggestion)
        {
            var deferArgs = Runtime.CheckDeferArgs(target, args);
            if (deferArgs != null)
            {
                return Defer(deferArgs);
            }

            if (target.Value == null)
            {
                return Runtime.CheckTargetNullReference(target, "Not invokable: " + Runtime.CollectParameterInfo(target, args));
            }

            if (Runtime.Prototypep(target.Value))
            {
                var indexingExpr = Runtime.GetIndexingExpression(target, args);
                var restrictions = Runtime.GetTargetArgsRestrictions(target, args, false);

                if (indexingExpr == null)
                {
                    return errorSuggestion ??
                    Runtime.CreateThrow(
                        target, args, restrictions,
                        typeof(InvalidOperationException),
                        "Not invokable: " + Runtime.CollectParameterInfo(target, args));
                }

                return new DynamicMetaObject(Runtime.EnsureObjectResult(indexingExpr), restrictions);
            }

            if (target.LimitType.IsSubclassOf(typeof(Delegate)))
            {
                var parms = target.LimitType.GetMethod("Invoke").GetParameters();

                if (parms.Length == args.Length)
                {
                    // Don't need to check if argument types match parameters.
                    // If they don't, users get an argument conversion error.
                    var callArgs = Runtime.ConvertArguments(args, parms);
                    var expression = Expression.Invoke(
                                         Expression.Convert(target.Expression, target.LimitType),
                                         callArgs);
                    return new DynamicMetaObject(
                        Runtime.EnsureObjectResult(expression),
                        BindingRestrictions.GetTypeRestriction(target.Expression,
                            target.LimitType));
                }
            }

            return errorSuggestion ??
            Runtime.CreateThrow(
                target, args,
                BindingRestrictions.GetTypeRestriction(target.Expression,
                    target.LimitType),
                typeof(InvalidOperationException),
                "Not invokable: " + Runtime.CollectParameterInfo(target, args));
        }

        #endregion Public Methods
    }

    public class KiezelInvokeMemberBinder : InvokeMemberBinder
    {
        #region Constructors

        public KiezelInvokeMemberBinder(string name, int count)
            : base(name, true, Runtime.GetCallInfo(count))
        {
            // true = ignoreCase
        }

        #endregion Constructors

        #region Public Methods

        public override DynamicMetaObject FallbackInvoke(
            DynamicMetaObject target, DynamicMetaObject[] args,
            DynamicMetaObject errorSuggestion)
        {
            var argexprs = new Expression[args.Length + 1];
            for (var i = 0; i < args.Length; i++)
            {
                argexprs[i + 1] = args[i].Expression;
            }
            argexprs[0] = target.Expression;

            return new DynamicMetaObject(
                Runtime.CompileDynamicExpression(
                    Runtime.GetInvokeBinder(args.Length),
                    typeof(object),
                    argexprs),
                target.Restrictions.Merge(
                    BindingRestrictions.Combine(args)));
        }

        public override DynamicMetaObject FallbackInvokeMember(
            DynamicMetaObject target, DynamicMetaObject[] args,
            DynamicMetaObject errorSuggestion)
        {
            var deferArgs = Runtime.CheckDeferArgs(target, args);
            if (deferArgs != null)
            {
                return Defer(deferArgs);
            }

            var limitType = target.LimitType;
            var name = Name.LispToPascalCaseName();

            if (target.Value == null)
            {
                return Runtime.CheckTargetNullReference(target,
                    "Cannot invoke a method on a null reference:"
                    + limitType.FullName + "." + name + Runtime.CollectParameterInfo(target, args));
            }

            var builtin = Runtime.FindImportedFunction(limitType, Name);

            if (builtin != null)
            {
                DynamicMetaObject result;
                if (builtin.TryBindInvokeBestInstanceMethod(false, target, target, args, out result))
                {
                    return result;
                }
            }

            var flags = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            var methods = new List<CandidateMethod<MethodInfo>>();
            bool createdParamArray;

            foreach (var m in limitType.GetMember(name, flags))
            {
                MethodInfo mi;

                if (m is PropertyInfo)
                {
                    mi = ((PropertyInfo)m).GetGetMethod();
                }
                else {
                    mi = m as MethodInfo;
                }

                if (mi != null && Runtime.ParametersMatchArguments(mi.GetParameters(), args, out createdParamArray))
                {
                    Runtime.InsertInMostSpecificOrder(methods, mi, createdParamArray);
                }
            }

            if (methods.Count == 0 && (Name.StartsWith("set-") || Name.StartsWith("get-")))
            {
                // Fallback to special handling to change .set-bla to .set_bla if the former has failed.
                name = Name.Left(3) + "_" + Name.Substring(4).LispToPascalCaseName();

                foreach (var m in limitType.GetMember(name, flags))
                {
                    MethodInfo mi;

                    if (m is PropertyInfo)
                    {
                        mi = ((PropertyInfo)m).GetGetMethod();
                    }
                    else {
                        mi = m as MethodInfo;
                    }

                    if (mi != null && Runtime.ParametersMatchArguments(mi.GetParameters(), args, out createdParamArray))
                    {
                        Runtime.InsertInMostSpecificOrder(methods, mi, createdParamArray);
                    }
                }
            }

            var restrictions = Runtime.GetTargetArgsRestrictions(target, args, false);

            if (methods.Count == 0)
            {
                return errorSuggestion ?? Runtime.CreateThrow(target, args, restrictions, typeof(MissingMemberException),
                    "No (suitable) method found: " + limitType.FullName + "." + name + Runtime.CollectParameterInfo(target, args));
            }
            else {
                var method = methods[0].Method;
                var callArgs2 = Runtime.ConvertArguments(args, method.GetParameters());
                Expression expr;

                if (method.IsStatic)
                {
                    // NOT REACHED
                    // static
                    // extension with target as extra parameter
                    expr = Expression.Call(method, callArgs2);
                }
                else {
                    expr = Expression.Call(Expression.Convert(target.Expression, limitType), method, callArgs2);
                }

                return new DynamicMetaObject(Runtime.EnsureObjectResult(expr), restrictions);
            }
        }

        #endregion Public Methods
    }

    public class KiezelSetIndexBinder : SetIndexBinder
    {
        #region Constructors

        public KiezelSetIndexBinder(int count)
            : base(Runtime.GetCallInfo(count))
        {
        }

        #endregion Constructors

        #region Public Methods

        public override DynamicMetaObject FallbackSetIndex(
            DynamicMetaObject target, DynamicMetaObject[] indexes,
            DynamicMetaObject value, DynamicMetaObject errorSuggestion)
        {
            var deferArgs = Runtime.CheckDeferArgs(target, indexes);
            if (deferArgs != null)
            {
                return Defer(deferArgs);
            }

            if (target.Value == null)
            {
                return Runtime.CheckTargetNullReference(target, "Cannot set index on null reference: "
                + target.LimitType.FullName + Runtime.CollectParameterInfo(target, indexes));
            }

            Debug.Assert(target.HasValue && target.LimitType != typeof(Array));
            Type valueType;
            var indexingExpr = Runtime.GetIndexingExpression(target, indexes, out valueType);

            if (indexingExpr == null)
            {
                return errorSuggestion ??
                Runtime.CreateThrow(
                    target, indexes, BindingRestrictions.Empty,
                    typeof(InvalidOperationException),
                    "No set indexer found: " + target.LimitType.FullName + Runtime.CollectParameterInfo(target, indexes));
            }

            var setIndexExpr = Expression.Assign(indexingExpr, Runtime.ConvertArgument(value, valueType));

            BindingRestrictions restrictions = Runtime.GetTargetArgsRestrictions(target, indexes, false);

            return new DynamicMetaObject(Runtime.EnsureObjectResult(setIndexExpr), restrictions);
        }

        #endregion Public Methods
    }

    public class KiezelSetMemberBinder : SetMemberBinder
    {
        #region Constructors

        public KiezelSetMemberBinder(string name)
            : base(name, true)
        {
        }

        #endregion Constructors

        #region Public Methods

        public override DynamicMetaObject FallbackSetMember(
            DynamicMetaObject target, DynamicMetaObject value,
            DynamicMetaObject errorSuggestion)
        {
            // Used by (set-attr obj name value)

            if (!target.HasValue)
            {
                return Defer(target);
            }

            var name = Name.LispToPascalCaseName();

            if (target.Value == null)
            {
                var id = target.LimitType.Name + "." + name;
                return Runtime.CheckTargetNullReference(target, "Cannot set property on null reference:" + id + "(null)");
            }

            var flags = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            var members = target.LimitType.GetMember(name, flags);

            if (members.Length == 1 && members[0] is PropertyInfo)
            {
                var prop = (PropertyInfo)members[0];
                var val = Runtime.ConvertArgument(value, prop.PropertyType);
                var expr = Expression.Assign(Expression.MakeMemberAccess(Expression.Convert(target.Expression, prop.DeclaringType), prop), val);
                return new DynamicMetaObject(Runtime.EnsureObjectResult(expr),
                    BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));
            }
            else if (members.Length == 1 && members[0] is FieldInfo)
            {
                var field = (FieldInfo)members[0];
                var val = Runtime.ConvertArgument(value, field.FieldType);
                var expr = Expression.Assign(Expression.MakeMemberAccess(Expression.Convert(target.Expression, field.DeclaringType), field), val);
                return new DynamicMetaObject(Runtime.EnsureObjectResult(expr),
                    BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));
            }
            else if (members.Length == 1 && members[0] is EventInfo)
            {
                //public static void AddEventHandler( System.Reflection.EventInfo eventinfo, object target, object func )

                var evt = (EventInfo)members[0];
                var expr = Expression.Call(Runtime.AddEventHandlerMethod,
                               Expression.Constant(evt, typeof(EventInfo)),
                               Expression.Convert(target.Expression, evt.DeclaringType),
                               Expression.Convert(value.Expression, typeof(IApply)));
                return new DynamicMetaObject(Runtime.EnsureObjectResult(expr),
                    BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));
            }
            else {
                return errorSuggestion ??
                Runtime.CreateThrow(
                    target, null,
                    BindingRestrictions.GetTypeRestriction(target.Expression,
                        target.LimitType),
                    typeof(MissingMemberException),
                    "Property or field not found: " + target.LimitType.Name + "." + name + Runtime.CollectParameterInfo(target));
            }
        }

        #endregion Public Methods
    }

    public partial class Runtime
    {
        #region Static Fields

        public static ConcurrentDictionary<int, CallInfo> _getCallInfo;
        public static ConcurrentDictionary<int, CallSiteBinder> _getIndexBinders;
        public static ConcurrentDictionary<string, CallSiteBinder> _getMemberBinders;
        public static ConcurrentDictionary<int, CallSiteBinder> _invokeBinders;
        public static ConcurrentDictionary<InvokeMemberBinderKey, CallSiteBinder> _invokeMemberBinders;
        public static ConcurrentDictionary<int, CallSiteBinder> _setIndexBinders;
        public static ConcurrentDictionary<string, CallSiteBinder> _setMemberBinders;

        #endregion Static Fields

        #region Public Methods

        public static CallInfo GetCallInfo(int count)
        {
            return _getCallInfo.GetOrAdd(count, GetCallInfoFactory);
        }

        public static CallInfo GetCallInfoFactory(int n)
        {
            return new CallInfo(n);
        }

        public static CallSiteBinder GetCallSiteBinderFactory(string name)
        {
            return new KiezelGetMemberBinder(name);
        }

        public static CallSiteBinder GetGetIndexBinder(int count)
        {
            return _getIndexBinders.GetOrAdd(count, GetGetIndexBinderFactory);
        }

        public static CallSiteBinder GetGetIndexBinderFactory(int count)
        {
            return new KiezelGetIndexBinder(count);
        }

        public static CallSiteBinder GetGetInvokeBinderFactory(int count)
        {
            return new KiezelInvokeBinder(count);
        }

        public static CallSiteBinder GetGetInvokeMemberBinderFactory(InvokeMemberBinderKey info)
        {
            return new KiezelInvokeMemberBinder(info.Name, info.Count);
        }

        public static CallSiteBinder GetGetMemberBinder(string name)
        {
            return _getMemberBinders.GetOrAdd(name, GetCallSiteBinderFactory);
        }

        public static CallSiteBinder GetInvokeBinder(int count)
        {
            return _invokeBinders.GetOrAdd(count, GetGetInvokeBinderFactory);
        }

        public static CallSiteBinder GetInvokeMemberBinder(InvokeMemberBinderKey info)
        {
            return _invokeMemberBinders.GetOrAdd(info, GetGetInvokeMemberBinderFactory);
        }

        public static CallSiteBinder GetSetIndexBinder(int count)
        {
            return _setIndexBinders.GetOrAdd(count, GetSetIndexBinderFactory);
        }

        public static CallSiteBinder GetSetIndexBinderFactory(int count)
        {
            return new KiezelSetIndexBinder(count);
        }

        public static CallSiteBinder GetSetMemberBinder(string name)
        {
            return _setMemberBinders.GetOrAdd(name, GetSetMemberBinderFactory);
        }

        public static CallSiteBinder GetSetMemberBinderFactory(string name)
        {
            return new KiezelSetMemberBinder(name);
        }

        public static void RestartBinders()
        {
            _getCallInfo = new ConcurrentDictionary<int, CallInfo>();
            _getMemberBinders = new ConcurrentDictionary<string, CallSiteBinder>();
            _setMemberBinders = new ConcurrentDictionary<string, CallSiteBinder>();
            _getIndexBinders = new ConcurrentDictionary<int, CallSiteBinder>();
            _setIndexBinders = new ConcurrentDictionary<int, CallSiteBinder>();
            _invokeBinders = new ConcurrentDictionary<int, CallSiteBinder>();
            _invokeMemberBinders = new ConcurrentDictionary<InvokeMemberBinderKey, CallSiteBinder>();
        }

        #endregion Public Methods
    }
}