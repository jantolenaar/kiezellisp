#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public struct CandidateMethod<T>
        where T : MethodBase
    {
        #region Fields

        public bool CreatedParamArray;
        public T Method;

        #endregion Fields

        #region Constructors

        public CandidateMethod(T method, bool createdParamArray)
        {
            Method = method;
            CreatedParamArray = createdParamArray;
        }

        #endregion Constructors
    }

    public static partial class Runtime
    {
        #region Static Fields

        public static TypeCode[] TypeCodes =
            {
                TypeCode.Int16, TypeCode.UInt32,
                TypeCode.Int16, TypeCode.Int32,
                TypeCode.Int16, TypeCode.Int64,
                TypeCode.Int16, TypeCode.UInt32,
                TypeCode.Int16, TypeCode.UInt64,
                TypeCode.Int16, TypeCode.Single,
                TypeCode.Int16, TypeCode.Double,
                TypeCode.Int16, TypeCode.Decimal,
                TypeCode.Int32, TypeCode.UInt32,
                TypeCode.Int32, TypeCode.Int64,
                TypeCode.Int32, TypeCode.UInt64,
                TypeCode.Int32, TypeCode.Single,
                TypeCode.Int32, TypeCode.Double,
                TypeCode.Int32, TypeCode.Decimal,
                TypeCode.Int64, TypeCode.UInt64,
                TypeCode.Int64, TypeCode.Single,
                TypeCode.Int64, TypeCode.Double,
                TypeCode.Int64, TypeCode.Decimal,
                TypeCode.Single, TypeCode.Double,
                TypeCode.Single, TypeCode.Decimal,
                TypeCode.Decimal, TypeCode.Double,
                TypeCode.UInt16, TypeCode.Int16,
                TypeCode.UInt16, TypeCode.Int32,
                TypeCode.UInt16, TypeCode.Int64,
                TypeCode.UInt16, TypeCode.UInt32,
                TypeCode.UInt16, TypeCode.UInt64,
                TypeCode.UInt16, TypeCode.Single,
                TypeCode.UInt16, TypeCode.Double,
                TypeCode.UInt16, TypeCode.Decimal,
                TypeCode.UInt32, TypeCode.Int32,
                TypeCode.UInt32, TypeCode.Int64,
                TypeCode.UInt32, TypeCode.UInt64,
                TypeCode.UInt32, TypeCode.Single,
                TypeCode.UInt32, TypeCode.Double,
                TypeCode.UInt32, TypeCode.Decimal,
                TypeCode.UInt64, TypeCode.Int64,
                TypeCode.UInt64, TypeCode.Single,
                TypeCode.UInt64, TypeCode.Double,
                TypeCode.UInt64, TypeCode.Decimal
            };

        #endregion Static Fields

        #region Public Methods

        public static object ArgumentValue(object arg)
        {
            return arg is DynamicMetaObject ? ((DynamicMetaObject)arg).Value : arg;
        }

        public static bool CanConvertFrom(Type type1, Type type2)
        {
            if (type1.IsPrimitive && type2.IsPrimitive)
            {
                TypeCode typeCode1 = Type.GetTypeCode(type1);
                TypeCode typeCode2 = Type.GetTypeCode(type2);

                if (typeCode1 == typeCode2)
                {
                    return true;
                }

                for (var i = 0; i < TypeCodes.Length; i += 2)
                {
                    if (typeCode1 == TypeCodes[i] && typeCode2 == TypeCodes[i + 1])
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool CanMaybeCastToEnumerableT(Type parameterType, Type argType)
        {
            // Ignore the source element type.
            var b = parameterType.Name == "IEnumerable`1" && typeof(IEnumerable).IsAssignableFrom(argType);
            return b;
        }

        public static bool CanMaybeConvertToEnumType(Type parameterType, Type argType)
        {
            var b = parameterType.IsEnum && (argType == typeof(int) || argType == typeof(Symbol));
            return b;
        }

        public static DynamicMetaObject[] CheckDeferArgs(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            if (!target.HasValue || args.Any((a) => !a.HasValue))
            {
                var deferArgs = new DynamicMetaObject[args.Length + 1];
                for (var i = 0; i < args.Length; i++)
                {
                    deferArgs[i + 1] = args[i];
                }
                deferArgs[0] = target;

                return deferArgs;
            }
            else
            {
                return null;
            }
        }

        public static DynamicMetaObject CheckTargetNullReference(DynamicMetaObject target, string context)
        {
            return CreateThrow(
                target, null,
                BindingRestrictions.GetExpressionRestriction(Expression.Equal(Expression.Constant(null, typeof(object)), target.Expression)),
                typeof(NullReferenceException),
                context);
        }

        public static string CollectParameterInfo(params DynamicMetaObject[] args)
        {
            return "(" + ", ".Join(args.Select(x => x.LimitType.ToString() + ":" + ToPrintString(x.Value))) + ")";
        }

        public static string CollectParameterInfo(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            var args2 = GetCombinedTargetArgs(target, args);
            return CollectParameterInfo(args2);
        }

        public static int CompareParameterInfo(ParameterInfo[] param1, bool createdParamArray1, ParameterInfo[] param2, bool createdParamArray2)
        {
            var i = 0;
            for (; i < param1.Length && i < param2.Length; ++i)
            {
                var type1 = param1[i].ParameterType;
                var type2 = param2[i].ParameterType;
                var paramArray1 = param1[i].IsDefined(typeof(ParamArrayAttribute), false) && createdParamArray1;
                var paramArray2 = param2[i].IsDefined(typeof(ParamArrayAttribute), false) && createdParamArray2;

                var cmp = 0;

                if (type1 == type2)
                {
                    cmp = 0;
                }
                else if (paramArray1 && !paramArray2)
                {
                    // prefer param2
                    cmp = 1;
                }
                else if (paramArray2 && !paramArray1)
                {
                    // prefer param1
                    cmp = -1;
                }
                else if (type1.IsAssignableFrom(type2))
                {
                    // prefer param2
                    cmp = 1;
                }
                else if (type2.IsAssignableFrom(type1))
                {
                    // prefer param1
                    cmp = -1;
                }
                else if (CanConvertFrom(type1, type2))
                {
                    // prefer param1
                    cmp = -1;
                }
                else if (CanConvertFrom(type2, type1))
                {
                    // prefer param2
                    cmp = 1;
                }
                else
                {
                    //throw new LispException( "Cannot resolve parameter type comparison" );
                    cmp = type1.GetHashCode().CompareTo(type2.GetHashCode());
                }

                if (cmp != 0)
                {
                    return cmp;
                }
            }
            if (i == param1.Length && i == param2.Length)
            {
                return 0;
            }
            else if (i == param1.Length)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        public static Expression ConvertArgument(object arg, Type parameterType)
        {
            Type type;
            Expression argExpr;

            if (arg is DynamicMetaObject)
            {
                var dmo = (DynamicMetaObject)arg;
                type = dmo.LimitType;
                argExpr = dmo.Expression;
                if (typeof(IApply).IsAssignableFrom(type) && IsSpecificDelegate(parameterType))
                {
                    argExpr = GetDelegateExpression(argExpr, parameterType);
                }
                else if (CanMaybeConvertToEnumType(parameterType, type))
                {
                    argExpr = Expression.Call(null, ConvertToEnumTypeMethod, Expression.Constant(parameterType, typeof(Type)), Expression.Convert(argExpr, typeof(object)));
                }
                else if (CanMaybeCastToEnumerableT(parameterType, type))
                {
                    // e.g. convert (1 2 3) to List<Int32>.
                    var ts = parameterType.GetGenericArguments();
                    var m = CastMethod.MakeGenericMethod(ts);
                    argExpr = Expression.Call(null, m, Expression.Convert(argExpr, typeof(IEnumerable)));
                }
                else if (type != parameterType && typeof(IConvertible).IsAssignableFrom(type) && typeof(IConvertible).IsAssignableFrom(parameterType))
                {
                    //argExpr = Expression.Convert( argExpr, typeof( object ) );
                    argExpr = Expression.Call(ChangeTypeMethod, argExpr, Expression.Constant(parameterType));
                    argExpr = Expression.Convert(argExpr, parameterType);
                }
                else
                {
                    argExpr = Expression.Convert(argExpr, parameterType);
                }
            }
            else
            {
                type = arg == null ? typeof(object) : arg.GetType();
                if (typeof(IApply).IsAssignableFrom(type) && IsSpecificDelegate(parameterType))
                {
                    argExpr = GetDelegateExpression(Expression.Constant(arg), parameterType);
                }
                else if (CanMaybeConvertToEnumType(parameterType, type))
                {
                    argExpr = Expression.Call(null, ConvertToEnumTypeMethod, Expression.Constant(parameterType, typeof(Type)), Expression.Constant(arg, typeof(object)));
                }
                else if (CanMaybeCastToEnumerableT(parameterType, type))
                {
                    var ts = parameterType.GetGenericArguments();
                    var m = CastMethod.MakeGenericMethod(ts);
                    argExpr = Expression.Call(null, m, Expression.Constant(arg, type));
                }
                else if (type != parameterType && typeof(IConvertible).IsAssignableFrom(type) && typeof(IConvertible).IsAssignableFrom(parameterType))
                {
                    argExpr = Expression.Constant(arg, typeof(object));
                    argExpr = Expression.Call(ChangeTypeMethod, argExpr, Expression.Constant(parameterType));
                    argExpr = Expression.Convert(argExpr, parameterType);
                }
                else
                {
                    argExpr = Expression.Convert(Expression.Constant(arg, type), parameterType);
                }
            }

            return argExpr;
        }

        public static Expression[] ConvertArguments(object[] args, ParameterInfo[] parameters)
        {
            // Arguments are already checked!!
            var len = parameters.Length;
            var callArgs = new Expression[len];

            if (len == 0)
            {
                return callArgs;
            }

            bool hasParamArray = len >= 1 && parameters[len - 1].IsDefined(typeof(ParamArrayAttribute), false);
            var last = len - (hasParamArray ? 1 : 0);

            for (var i = 0; i < last; i++)
            {
                callArgs[i] = ConvertArgument(args[i], parameters[i].ParameterType);
            }

            if (!hasParamArray)
            {
                return callArgs;
            }

            if (last + 1 == args.Length && ParameterMatchArgumentExact(parameters[last].ParameterType, args[last]))
            {
                // Final argument is the param array
                callArgs[last] = ConvertArgument(args[last], parameters[last].ParameterType);
                return callArgs;
            }

            // Param array creation
            var tail = new List<Expression>();
            var elementType = parameters[last].ParameterType.GetElementType();
            if (elementType == null)
            {
                elementType = typeof(object);
            }
            for (var i = last; i < args.Length; ++i)
            {
                tail.Add(ConvertArgument(args[i], elementType));
            }
            callArgs[last] = Expression.NewArrayInit(elementType, tail);
            return callArgs;
        }

        // CreateThrow is a convenience function for when binders cannot bind.
        // They need to return a DynamicMetaObject with appropriate restrictions
        // that throws.  Binders never just throw due to the protocol since
        // a binder or MO down the line may provide an implementation.
        //
        // It returns a DynamicMetaObject whose expr throws the exception, and
        // ensures the expr's type is object to satisfy the CallSite return type
        // constraint.
        //
        // A couple of calls to CreateThrow already have the args and target
        // restrictions merged in, but BindingRestrictions.Merge doesn't add
        // duplicates.
        //
        public static DynamicMetaObject CreateThrow(DynamicMetaObject target, DynamicMetaObject[] args,
            BindingRestrictions moreTests,
            Type exception, params object[] exceptionArgs)
        {
            Expression[] argExprs = null;
            Type[] argTypes = Type.EmptyTypes;
            int i;
            if (exceptionArgs != null)
            {
                i = exceptionArgs.Length;
                argExprs = new Expression[i];
                argTypes = new Type[i];
                i = 0;
                foreach (object o in exceptionArgs)
                {
                    Expression e = Expression.Constant(o);
                    argExprs[i] = e;
                    argTypes[i] = e.Type;
                    i += 1;
                }
            }
            ConstructorInfo constructor = exception.GetConstructor(argTypes);
            if (constructor == null)
            {
                throw new ArgumentException("Type doesn't have constructor with a given signature");
            }
            return new DynamicMetaObject(
                Expression.Throw(
                    Expression.New(constructor, argExprs),
                    typeof(object)),
                target.Restrictions.Merge(BindingRestrictions.Combine(args))
                                   .Merge(moreTests));
        }

        // EnsureObjectResult wraps expr if necessary so that any binder or
        // DynamicMetaObject result expression returns object.  This is required
        // by CallSites.
        //
        public static Expression EnsureObjectResult(Expression expr)
        {
            if (!expr.Type.IsValueType)
            {
                return expr;
            }
            else if (expr.Type == typeof(void))
            {
                return Expression.Block(expr, Expression.Constant(Void(), typeof(object)));
            }
            else
            {
                return Expression.Convert(expr, typeof(object));
            }
        }

        public static DynamicMetaObject[] GetCombinedTargetArgs(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            var newargs = new DynamicMetaObject[args.Length + 1];
            newargs[0] = target;
            Array.Copy(args, 0, newargs, 1, args.Length);
            return newargs;
        }

        public static LambdaExpression GetDelegateExpression(Expression lambda, Type delegateType)
        {
            MethodInfo method = delegateType.GetMethod("Invoke");
            var parameters1 = method.GetParameters();
            var parameters2 = parameters1.Select(p => Expression.Parameter(p.ParameterType)).ToArray();
            var parameters3 = parameters2.Select(p => EnsureObjectResult(p)).ToArray();
            var funcall = typeof(Runtime).GetMethod("Funcall");
            var argumentArray = Expression.NewArrayInit(typeof(object), parameters3);
            var lambdaCall = Expression.Call(funcall, Expression.Convert(lambda, typeof(IApply)), argumentArray);
            Expression returnVal;
            if (method.ReturnType == typeof(void))
            {
                returnVal = Expression.Convert(lambdaCall, typeof(object));
            }
            else
            {
                returnVal = Expression.Convert(lambdaCall, method.ReturnType);
            }
            var expression = Expression.Lambda(delegateType, returnVal, parameters2);
            return expression;
        }

        // Return the expression for getting target[indexes]
        //
        // Note, callers must ensure consistent restrictions are added for
        // the conversions on args and target.
        //
        public static Expression GetIndexingExpression(
            DynamicMetaObject target,
            DynamicMetaObject[] indexes)
        {
            Type valueType;
            return GetIndexingExpression(target, indexes, out valueType);
        }

        public static Expression GetIndexingExpression(
            DynamicMetaObject target,
            DynamicMetaObject[] indexes, out Type valueType)
        {
            Debug.Assert(target.HasValue && target.LimitType != typeof(Array));
            valueType = null;

            if (target.LimitType.IsArray)
            {
                valueType = target.LimitType.GetElementType();
                var indexExpressions = indexes.Select(i => Expression.Convert(i.Expression, typeof(int)));
                return Expression.ArrayAccess(Expression.Convert(target.Expression, target.LimitType), indexExpressions);
            }
            else
            {
                var props = target.LimitType.GetProperties();
                var allIndexers = props.Where(idx => idx.GetIndexParameters().Length == indexes.Length).ToArray();
                var indexers = new List<CandidateProperty>();
                ParameterInfo[] indexerParams = null;
                bool createdParamArray;

                foreach (var idxer in allIndexers)
                {
                    indexerParams = idxer.GetIndexParameters();
                    if (ParametersMatchArguments(indexerParams, indexes, out createdParamArray))
                    {
                        InsertInMostSpecificOrder(indexers, idxer, createdParamArray);
                    }
                }

                if (indexers.Count == 0)
                {
                    return null;
                }

                var indexer = indexers[0].Property;
                var indexExpressions = ConvertArguments(indexes, indexer.GetIndexParameters());
                valueType = indexer.PropertyType;

                return Expression.MakeIndex(
                    Expression.Convert(target.Expression, target.LimitType), indexer, indexExpressions);
            }
        }

        // GetTargetArgsRestrictions generates the restrictions needed for the
        // MO resulting from binding an operation.  This combines all existing
        // restrictions and adds some for arg conversions.  targetInst indicates
        // whether to restrict the target to an instance (for operations on type
        // objects) or to a type (for operations on an instance of that type).
        //
        // NOTE, this function should only be used when the caller is converting
        // arguments to the same types as these restrictions.
        //
        public static BindingRestrictions GetTargetArgsRestrictions(
            DynamicMetaObject target, DynamicMetaObject[] args,
            bool instanceRestrictionOnTarget)
        {
            // Important to add existing restriction first because the
            // DynamicMetaObjects (and possibly values) we're looking at depend
            // on the pre-existing restrictions holding true.
            var restrictions = target.Restrictions.Merge(BindingRestrictions
                                                            .Combine(args));
            if (instanceRestrictionOnTarget)
            {
                restrictions = restrictions.Merge(
                    BindingRestrictions.GetInstanceRestriction(
                        target.Expression,
                        target.Value
                    ));
            }
            else
            {
                restrictions = restrictions.Merge(
                    BindingRestrictions.GetTypeRestriction(
                        target.Expression,
                        target.LimitType
                    ));
            }
            for (var i = 0; i < args.Length; i++)
            {
                BindingRestrictions r;
                if (args[i].HasValue && args[i].Value == null)
                {
                    r = BindingRestrictions.GetInstanceRestriction(
                        args[i].Expression, null);
                }
                else
                {
                    r = BindingRestrictions.GetTypeRestriction(
                        args[i].Expression, args[i].LimitType);
                }
                restrictions = restrictions.Merge(r);
            }
            return restrictions;
        }

        public static void InsertInMostSpecificOrder<T>(List<CandidateMethod<T>> candidates, T method, bool createdParamArray)
            where T : MethodBase
        {
            var insertPoint = candidates.Count;
            var p1 = method.GetParameters();

            for (var i = 0; i < candidates.Count; ++i)
            {
                var p2 = candidates[i].Method.GetParameters();
                var f2 = candidates[i].CreatedParamArray;
                if (CompareParameterInfo(p1, createdParamArray, p2, f2) < 0)
                {
                    insertPoint = i;
                    break;
                }
            }

            candidates.Insert(insertPoint, new CandidateMethod<T>(method, createdParamArray));
        }

        public static void InsertInMostSpecificOrder(List<CandidateProperty> candidates, PropertyInfo property, bool createdParamArray)
        {
            var insertPoint = candidates.Count;
            var p1 = property.GetIndexParameters();

            for (var i = 0; i < candidates.Count; ++i)
            {
                var p2 = candidates[i].Property.GetIndexParameters();
                var f2 = candidates[i].CreatedParamArray;
                if (CompareParameterInfo(p1, createdParamArray, p2, f2) < 0)
                {
                    insertPoint = i;
                    break;
                }
            }

            candidates.Insert(insertPoint, new CandidateProperty(property, createdParamArray));
        }

        public static bool IsSpecificDelegate(Type type)
        {
            if (typeof(Delegate).IsAssignableFrom(type))
            {
                return typeof(Delegate) != type;
            }
            else
            {
                return false;
            }
        }

        public static bool ParameterArrayMatchArguments(ParameterInfo parameter, object[] args, int offset, int count)
        {
            var elementType = parameter.ParameterType.GetElementType();

            if (elementType == null)
            {
                elementType = typeof(object);
            }

            for (var i = offset; i < offset + count; i++)
            {
                if (!ParameterMatchArgument(elementType, args[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ParameterMatchArgument(Type parameterType, object arg)
        {
            Type type = arg is DynamicMetaObject ? ((DynamicMetaObject)arg).LimitType : arg == null ? null : arg.GetType();
            object value = arg is DynamicMetaObject ? ((DynamicMetaObject)arg).Value : arg;

            return (parameterType == typeof(Type) && type == typeof(Type))
            || (IsSpecificDelegate(parameterType) && (typeof(IApply).IsAssignableFrom(type) || type == typeof(Symbol)))
            || (value == null && !parameterType.IsValueType)
            || parameterType.IsAssignableFrom(type)
            || CanMaybeConvertToEnumType(parameterType, type)
            || CanMaybeCastToEnumerableT(parameterType, type)
            || CanConvertFrom(type, parameterType);
        }

        public static bool ParameterMatchArgumentExact(Type parameterType, object arg)
        {
            object value;

            if (arg is DynamicMetaObject)
            {
                value = ((DynamicMetaObject)arg).Value;
            }
            else
            {
                value = arg;
            }

            if (value == null)
            {
                return false;
            }
            else
            {
                return parameterType == value.GetType();
            }
        }

        public static bool ParametersMatchArguments(ParameterInfo[] parameters, object[] args)
        {
            bool createdParamArray;
            return ParametersMatchArguments(parameters, args, out createdParamArray);
        }

        public static bool ParametersMatchArguments(ParameterInfo[] parameters, object[] args, out bool createdParamArray)
        {
            createdParamArray = false;

            int len = parameters.Length;

            if (len == 0)
            {
                return args.Length == 0;
            }

            bool hasParamArray = len >= 1 && parameters[len - 1].IsDefined(typeof(ParamArrayAttribute), false);

            if (!hasParamArray)
            {
                if (args.Length != len)
                {
                    return false;
                }

                for (var i = 0; i < len; i++)
                {
                    if (!ParameterMatchArgument(parameters[i].ParameterType, args[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                int last = len - 1;

                if (args.Length < last)
                {
                    return false;
                }

                for (var i = 0; i < last; i++)
                {
                    if (!ParameterMatchArgument(parameters[i].ParameterType, args[i]))
                    {
                        return false;
                    }
                }

                if (args.Length == last)
                {
                    // One argument short is ok for param array
                    return true;
                }

                if (args.Length == last + 1)
                {
                    // Final argument is the param array
                    if (ParameterMatchArgumentExact(parameters[last].ParameterType, args[last]))
                    {
                        return true;
                    }
                }

                if (ParameterArrayMatchArguments(parameters[last], args, last, args.Length - last))
                {
                    createdParamArray = true;
                    return true;
                }

                return false;
            }
        }

        public static bool SplitCombinedTargetArgs(DynamicMetaObject[] args, out DynamicMetaObject argsFirst, out DynamicMetaObject[] argsRest)
        {
            if (args.Length == 0)
            {
                argsFirst = null;
                argsRest = null;
                return false;
            }
            else
            {
                argsRest = new DynamicMetaObject[args.Length - 1];
                argsFirst = args[0];
                Array.Copy(args, 1, argsRest, 0, args.Length - 1);
                return true;
            }
        }

        #endregion Public Methods

        #region Other

        public struct CandidateProperty
        {
            #region Fields

            public bool CreatedParamArray;
            public PropertyInfo Property;

            #endregion Fields

            #region Constructors

            public CandidateProperty(PropertyInfo property, bool createdParamArray)
            {
                Property = property;
                CreatedParamArray = createdParamArray;
            }

            #endregion Constructors
        }

        #endregion Other
    }
}