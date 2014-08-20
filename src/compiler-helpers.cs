// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Dynamic;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.IO;

namespace Kiezel
{

    public static partial class RuntimeHelpers
    {

        internal static T GetMostSpecific<T>( List<T> methods ) where T: MethodBase
        {
            methods.Sort( CompareMethodBase );
            return methods[ 0 ];
        }

        internal static int CompareMethodBase( MethodBase m1, MethodBase m2 )
        {
            var p1 = m1.GetParameters();
            var p2 = m2.GetParameters();
            return CompareParameterInfo( p1, p2 );
        }

        internal static PropertyInfo GetMostSpecific( List<PropertyInfo> methods ) 
        {
            methods.Sort( ComparePropertyInfo );
            return methods[ 0 ];
        }

        internal static int ComparePropertyInfo( PropertyInfo m1, PropertyInfo m2 )
        {
            var p1 = m1.GetIndexParameters();
            var p2 = m2.GetIndexParameters();
            return CompareParameterInfo( p1, p2 );
        }

        internal static int CompareParameterInfo( ParameterInfo[] p1, ParameterInfo[] p2)
        {
            var i = 0;
            for ( ; i < p1.Length && i < p2.Length; ++i )
            {
                var t1 = p1[ i ].ParameterType;
                var t2 = p2[ i ].ParameterType;
                var cmp = 0;

                if ( t1 == t2 )
                {
                    cmp = 0;
                }
                else if ( t1.IsAssignableFrom( t2 ) )
                {
                    cmp = 1;
                }
                else if ( t2.IsAssignableFrom( t1 ) )
                {
                    cmp = -1;
                }
                else 
                {
                    var param1 = p1[ i ].IsDefined( typeof( ParamArrayAttribute ), false );
                    var param2 = p2[ i ].IsDefined( typeof( ParamArrayAttribute ), false );
                    if ( param1 && !param2 )
                    {
                        cmp = 1;
                    }
                    else if ( !param1 && param2 )
                    {
                        cmp = -1;
                    }
                    else
                    {
                        //throw new LispException( "Cannot resolve parameter type comparison" );
                        cmp = t1.GetHashCode().CompareTo( t2.GetHashCode() );
                    }
                }

                if ( cmp != 0 )
                {
                    return cmp;
                }
            }
            if ( i == p1.Length && i == p2.Length )
            {
                return 0;
            }
            else if ( i == p1.Length )
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }

        internal static DynamicMetaObject CheckTargetNullReference( DynamicMetaObject target, string context )
        {
            return CreateThrow(
                    target, null,
                    BindingRestrictions.GetExpressionRestriction( Expression.Equal( Expression.Constant( null, typeof( object ) ), target.Expression ) ),
                    typeof( NullReferenceException ),
                    context );
        }

        internal static bool ParametersMatchArguments( ParameterInfo[] parameters, object[] args )
        {
            int len = parameters.Length;

            if ( len == 0 )
            {
                return args.Length == 0;
            }

            bool hasParamArray = len >= 1 && parameters[ len - 1 ].IsDefined( typeof( ParamArrayAttribute ), false );

            if ( !hasParamArray )
            {
                if ( args.Length != len )
                {
                    return false;
                }

                for ( int i = 0; i < len; i++ )
                {
                    if ( !ParameterMatchArgument( parameters[ i ].ParameterType, args[ i ] ) )
                    {
                        return false;
                    }
                }

                return true;

            }
            else
            {
                int last = len - 1;

                if ( args.Length < last )
                {
                    return false;
                }

                for ( int i = 0; i < last; i++ )
                {
                    if ( !ParameterMatchArgument( parameters[ i ].ParameterType, args[ i ] ) )
                    {
                        return false;
                    }
                }

                if ( args.Length == last )
                {
                    // One argument short is ok for param array
                    return true;
                }

                if ( ParameterArrayMatchArguments( parameters[ last ], args, last, args.Length - last ) )
                {
                    return true;
                }

                return false;
            }
        }

        internal static bool ParameterArrayMatchArguments( ParameterInfo parameter, object[] args, int offset, int count )
        {
            var elementType = parameter.ParameterType.GetElementType();

            if ( elementType == null )
            {
                elementType = typeof( object );
            }

            for ( int i = offset; i < offset + count; i++ )
            {
                if ( !ParameterMatchArgument( elementType, args[ i ] ) )
                {
                    return false;
                }
            }

            return true;
        }

        internal static object ArgumentValue( object arg )
        {
            return arg is DynamicMetaObject ? ( ( DynamicMetaObject ) arg ).Value : arg;
        }

        internal static bool ParameterMatchArgument( Type parameterType, object arg )
        {
            Type type = arg is DynamicMetaObject ? ( ( DynamicMetaObject ) arg ).LimitType : arg == null ? null : arg.GetType();
            object value = arg is DynamicMetaObject ? ( ( DynamicMetaObject ) arg ).Value : arg;

            return ( parameterType == typeof( Type ) && type == typeof( Type ) )
                    || ( IsSpecificDelegate( parameterType ) && ( typeof( IApply ).IsAssignableFrom( type ) || type == typeof( Symbol ) ) )
                    || ( value == null && !parameterType.IsValueType )
                    || parameterType.IsAssignableFrom( type )
                    || CanMaybeConvertToEnumType( parameterType, type )
                    || CanMaybeCastToEnumerableT( parameterType, type )
                    || CanConvertFrom( type, parameterType );
        }

        internal static bool CanMaybeCastToEnumerableT( Type parameterType, Type argType )
        {
            // Ignore the source element type.
            var b = parameterType.Name == "IEnumerable`1" && typeof( IEnumerable ).IsAssignableFrom( argType );
            return b;
        }

        internal static bool CanMaybeConvertToEnumType( Type parameterType, Type argType )
        {
            var b = parameterType.IsEnum && ( argType == typeof( int ) || argType == typeof( Symbol ) );
            return b;
        }

        internal static Expression[] ConvertArguments( object[] args, ParameterInfo[] parameters )
        {
            // Arguments are already checked!!
            var len = parameters.Length;
            var callArgs = new Expression[ len ];

            if ( len == 0 )
            {
                return callArgs;
            }

            bool hasParamArray = len >= 1 && parameters[ len - 1 ].IsDefined( typeof( ParamArrayAttribute ), false );
            var last = len - ( hasParamArray ? 1 : 0 );

            for ( int i = 0; i < last; i++ )
            {
                callArgs[ i ] = ConvertArgument( args[ i ], parameters[ i ].ParameterType );
            }

            if ( !hasParamArray )
            {
                return callArgs;
            }

            // Param array creation
            var tail = new List<Expression>();
            var elementType = parameters[ last ].ParameterType.GetElementType();
            if ( elementType == null )
            {
                elementType = typeof( object );
            }
            for ( int i = last; i < args.Length; ++i )
            {
                tail.Add( ConvertArgument( args[ i ], elementType ) );
            }
            callArgs[ last ] = Expression.NewArrayInit( elementType, tail );
            return callArgs;
        }

        internal static bool IsSpecificDelegate( Type type )
        {
            if ( typeof( Delegate ).IsAssignableFrom( type ) )
            {
                return typeof( Delegate ) != type;
            }
            else
            {
                return false;
            }
        }

        internal static Expression ConvertArgument( object arg, Type parameterType )
        {
            Type type;
            Expression argExpr;

            if ( arg is DynamicMetaObject )
            {
                var dmo = ( DynamicMetaObject ) arg;
                type = dmo.LimitType;
                argExpr = dmo.Expression;
                if ( ( typeof( IApply ).IsAssignableFrom( type ) || type == typeof( Symbol ) ) && IsSpecificDelegate( parameterType ) )
                {
                    argExpr = GetDelegateExpression( argExpr, parameterType );
                }
                else if ( CanMaybeConvertToEnumType( parameterType, type ) )
                {
                    argExpr = Expression.Call( null, Runtime.ConvertToEnumTypeMethod, Expression.Constant( parameterType, typeof( Type ) ), Expression.Convert( argExpr, typeof( object ) ) );
                }
                else if ( CanMaybeCastToEnumerableT( parameterType, type ) )
                {
                    // e.g. convert (1 2 3) to List<Int32>.
                    var ts = parameterType.GetGenericArguments();
                    var m = Runtime.CastMethod.MakeGenericMethod( ts );
                    argExpr = Expression.Call( null, m, Expression.Convert( argExpr, typeof( IEnumerable ) ) );
                }
                else if ( type != parameterType && typeof( IConvertible ).IsAssignableFrom( type ) && typeof( IConvertible ).IsAssignableFrom( parameterType ) )
                {
                    //argExpr = Expression.Convert( argExpr, typeof( object ) );
                    argExpr = Expression.Call( Runtime.ChangeTypeMethod, argExpr, Expression.Constant( parameterType ) );
                    argExpr = Expression.Convert( argExpr, parameterType );
                }
                else
                {
                    argExpr = Expression.Convert( argExpr, parameterType );
                }
            }
            else
            {
                type = arg == null ? typeof( object ) : arg.GetType();
                if ( ( typeof( IApply ).IsAssignableFrom( type ) || type == typeof( Symbol ) ) && IsSpecificDelegate( parameterType ) )
                {
                    argExpr = GetDelegateExpression( Expression.Constant( arg ), parameterType );
                }
                else if ( CanMaybeConvertToEnumType( parameterType, type ) )
                {
                    argExpr = Expression.Call( null, Runtime.ConvertToEnumTypeMethod, Expression.Constant( parameterType, typeof( Type ) ), Expression.Constant( arg, typeof( object ) ) );
                }
                else if ( CanMaybeCastToEnumerableT( parameterType, type ) )
                {
                    var ts = parameterType.GetGenericArguments();
                    var m = Runtime.CastMethod.MakeGenericMethod( ts );
                    argExpr = Expression.Call( null, m, Expression.Constant( arg, type ) );
                }
                else if ( type != parameterType && typeof( IConvertible ).IsAssignableFrom( type ) && typeof( IConvertible ).IsAssignableFrom( parameterType ) )
                {
                    argExpr = Expression.Constant( arg, typeof( object ) );
                    argExpr = Expression.Call( Runtime.ChangeTypeMethod, argExpr, Expression.Constant( parameterType ) );
                    argExpr = Expression.Convert( argExpr, parameterType );
                }
                else
                {
                    argExpr = Expression.Convert( Expression.Constant( arg, type ), parameterType );
                }
            }

            return argExpr;
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
        internal static BindingRestrictions GetTargetArgsRestrictions(
                DynamicMetaObject target, DynamicMetaObject[] args,
                bool instanceRestrictionOnTarget )
        {
            // Important to add existing restriction first because the
            // DynamicMetaObjects (and possibly values) we're looking at depend
            // on the pre-existing restrictions holding true.
            var restrictions = target.Restrictions.Merge( BindingRestrictions
                                                            .Combine( args ) );
            if ( instanceRestrictionOnTarget )
            {
                restrictions = restrictions.Merge(
                    BindingRestrictions.GetInstanceRestriction(
                        target.Expression,
                        target.Value
                    ) );
            }
            else
            {
                restrictions = restrictions.Merge(
                    BindingRestrictions.GetTypeRestriction(
                        target.Expression,
                        target.LimitType
                    ) );
            }
            for ( int i = 0; i < args.Length; i++ )
            {
                BindingRestrictions r;
                if ( args[ i ].HasValue && args[ i ].Value == null )
                {
                    r = BindingRestrictions.GetInstanceRestriction(
                            args[ i ].Expression, null );
                }
                else
                {
                    r = BindingRestrictions.GetTypeRestriction(
                            args[ i ].Expression, args[ i ].LimitType );
                }
                restrictions = restrictions.Merge( r );
            }
            return restrictions;
        }

        // Return the expression for getting target[indexes]
        //
        // Note, callers must ensure consistent restrictions are added for
        // the conversions on args and target.
        //
        internal static Expression GetIndexingExpression(
                                      DynamicMetaObject target,
                                      DynamicMetaObject[] indexes )
        {
            Debug.Assert( target.HasValue && target.LimitType != typeof( Array ) );

            if ( target.LimitType.IsArray )
            {
                var indexExpressions = indexes.Select( i => Expression.Convert( i.Expression, typeof(int) ) );
                return Expression.ArrayAccess( Expression.Convert( target.Expression, target.LimitType ), indexExpressions );
            }
            else
            {
                var props = target.LimitType.GetProperties();
                var allIndexers = props.Where( idx => idx.GetIndexParameters().Length == indexes.Length ).ToArray();
                var indexers = new List<PropertyInfo>();
                ParameterInfo[] indexerParams = null;

                foreach ( var idxer in allIndexers )
                {
                    indexerParams = idxer.GetIndexParameters();
                    if ( RuntimeHelpers.ParametersMatchArguments( indexerParams, indexes ) )
                    {
                        indexers.Add( idxer );
                    }
                }

                if ( indexers.Count == 0 )
                {
                    //Console.WriteLine( "no match" );
                    return null;
                }

                var indexer = GetMostSpecific( indexers );
                var indexExpressions = RuntimeHelpers.ConvertArguments( indexes, indexer.GetIndexParameters() );

                return Expression.MakeIndex(
                            Expression.Convert( target.Expression, target.LimitType ), indexer, indexExpressions );
            }
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
        internal static DynamicMetaObject CreateThrow
                ( DynamicMetaObject target, DynamicMetaObject[] args,
                 BindingRestrictions moreTests,
                 Type exception, params object[] exceptionArgs )
        {
            Expression[] argExprs = null;
            Type[] argTypes = Type.EmptyTypes;
            int i;
            if ( exceptionArgs != null )
            {
                i = exceptionArgs.Length;
                argExprs = new Expression[ i ];
                argTypes = new Type[ i ];
                i = 0;
                foreach ( object o in exceptionArgs )
                {
                    Expression e = Expression.Constant( o );
                    argExprs[ i ] = e;
                    argTypes[ i ] = e.Type;
                    i += 1;
                }
            }
            ConstructorInfo constructor = exception.GetConstructor( argTypes );
            if ( constructor == null )
            {
                throw new ArgumentException( "Type doesn't have constructor with a given signature" );
            }
            return new DynamicMetaObject(
                Expression.Throw(
                    Expression.New( constructor, argExprs ),
                    typeof( object ) ),
                    target.Restrictions.Merge( BindingRestrictions.Combine( args ) )
                                   .Merge( moreTests ) );
        }

        // EnsureObjectResult wraps expr if necessary so that any binder or
        // DynamicMetaObject result expression returns object.  This is required
        // by CallSites.
        //
        internal static Expression EnsureObjectResult( Expression expr )
        {
            if ( !expr.Type.IsValueType )
            {
                return expr;
            }
            else if ( expr.Type == typeof( void ) )
            {
                return Expression.Block( expr, Expression.Constant( null, typeof( object ) ) );
            }
            else
            {
                return Expression.Convert( expr, typeof( object ) );
            }
        }

        internal static DynamicMetaObject[] GetCombinedTargetArgs( DynamicMetaObject target, DynamicMetaObject[] args )
        {
            var newargs = new DynamicMetaObject[ args.Length + 1 ];
            newargs[ 0 ] = target;
            Array.Copy( args, 0, newargs, 1, args.Length );
            return newargs;
        }


        internal static bool SplitCombinedTargetArgs( DynamicMetaObject[] args, out DynamicMetaObject argsFirst, out DynamicMetaObject[] argsRest )
        {
            if ( args.Length == 0 )
            {
                argsFirst = null;
                argsRest = null;
                return false;
            }
            else
            {
                argsRest = new DynamicMetaObject[ args.Length - 1 ];
                argsFirst = args[ 0 ];
                Array.Copy( args, 1, argsRest, 0, args.Length - 1 );
                return true;
            }
        }

        internal static DynamicMetaObject[] CheckDeferArgs( DynamicMetaObject target, DynamicMetaObject[] args )
        {
            if ( !target.HasValue || args.Any( ( a ) => !a.HasValue ) )
            {
                var deferArgs = new DynamicMetaObject[ args.Length + 1 ];
                for ( int i = 0; i < args.Length; i++ )
                {
                    deferArgs[ i + 1 ] = args[ i ];
                }
                deferArgs[ 0 ] = target;

                return deferArgs;
            }
            else
            {
                return null;
            }
        }

        internal static LambdaExpression GetDelegateExpression( Expression lambda, Type delegateType )
        {
            MethodInfo method = delegateType.GetMethod( "Invoke" );
            var parameters1 = method.GetParameters();
            var parameters2 = parameters1.Select( p => Expression.Parameter( p.ParameterType ) ).ToArray();
            var parameters3 = parameters2.Select( p => EnsureObjectResult( p ) ).ToArray();
            var funcall = typeof( Runtime ).GetMethod( "Funcall" );
            var argumentArray = Expression.NewArrayInit( typeof( object ), parameters3 );
            var lambdaCall = Expression.Call( funcall, lambda, argumentArray );
            Expression returnVal;
            if ( method.ReturnType == typeof( void ) )
            {
                returnVal = Expression.Convert( lambdaCall, typeof( object ) );
            }
            else
            {
                returnVal = Expression.Convert( lambdaCall, method.ReturnType );
            }
            var expression = Expression.Lambda( delegateType, returnVal, parameters2 );
            return expression;
        }

        internal static bool CanConvertFrom( Type type1, Type type2 )
        {
            if ( type1.IsPrimitive && type2.IsPrimitive )
            {
                TypeCode typeCode1 = Type.GetTypeCode( type1 );
                TypeCode typeCode2 = Type.GetTypeCode( type2 );
                // If both type1 and type2 have the same type, return true.
                if ( typeCode1 == typeCode2 )
                    return true;
                // Possible conversions from Char follow.
                if ( typeCode1 == TypeCode.Char )
                    switch ( typeCode2 )
                    {
                        case TypeCode.UInt16:
                        return true;
                        case TypeCode.UInt32:
                        return true;
                        case TypeCode.Int32:
                        return true;
                        case TypeCode.UInt64:
                        return true;
                        case TypeCode.Int64:
                        return true;
                        case TypeCode.Single:
                        return true;
                        case TypeCode.Double:
                        return true;
                        default:
                        return false;
                    }
                // Possible conversions from Byte follow.
                if ( typeCode1 == TypeCode.Byte )
                    switch ( typeCode2 )
                    {
                        case TypeCode.Char:
                        return true;
                        case TypeCode.UInt16:
                        return true;
                        case TypeCode.Int16:
                        return true;
                        case TypeCode.UInt32:
                        return true;
                        case TypeCode.Int32:
                        return true;
                        case TypeCode.UInt64:
                        return true;
                        case TypeCode.Int64:
                        return true;
                        case TypeCode.Single:
                        return true;
                        case TypeCode.Double:
                        return true;
                        default:
                        return false;
                    }
                // Possible conversions from SByte follow.
                if ( typeCode1 == TypeCode.SByte )
                    switch ( typeCode2 )
                    {
                        case TypeCode.Int16:
                        return true;
                        case TypeCode.Int32:
                        return true;
                        case TypeCode.Int64:
                        return true;
                        case TypeCode.Single:
                        return true;
                        case TypeCode.Double:
                        return true;
                        default:
                        return false;
                    }
                // Possible conversions from UInt16 follow.
                if ( typeCode1 == TypeCode.UInt16 )
                    switch ( typeCode2 )
                    {
                        case TypeCode.UInt32:
                        return true;
                        case TypeCode.Int32:
                        return true;
                        case TypeCode.UInt64:
                        return true;
                        case TypeCode.Int64:
                        return true;
                        case TypeCode.Single:
                        return true;
                        case TypeCode.Double:
                        return true;
                        default:
                        return false;
                    }
                // Possible conversions from Int16 follow.
                if ( typeCode1 == TypeCode.Int16 )
                    switch ( typeCode2 )
                    {
                        case TypeCode.Int32:
                        return true;
                        case TypeCode.Int64:
                        return true;
                        case TypeCode.Single:
                        return true;
                        case TypeCode.Double:
                        return true;
                        default:
                        return false;
                    }
                // Possible conversions from UInt32 follow.
                if ( typeCode1 == TypeCode.UInt32 )
                    switch ( typeCode2 )
                    {
                        case TypeCode.UInt64:
                        return true;
                        case TypeCode.Int64:
                        return true;
                        case TypeCode.Single:
                        return true;
                        case TypeCode.Double:
                        return true;
                        default:
                        return false;
                    }
                // Possible conversions from Int32 follow.
                if ( typeCode1 == TypeCode.Int32 )
                    switch ( typeCode2 )
                    {
                        case TypeCode.Int64:
                        return true;
                        case TypeCode.Single:
                        return true;
                        case TypeCode.Double:
                        return true;
                        default:
                        return false;
                    }
                // Possible conversions from UInt64 follow.
                if ( typeCode1 == TypeCode.UInt64 )
                    switch ( typeCode2 )
                    {
                        case TypeCode.Single:
                        return true;
                        case TypeCode.Double:
                        return true;
                        default:
                        return false;
                    }
                // Possible conversions from Int64 follow.
                if ( typeCode1 == TypeCode.Int64 )
                    switch ( typeCode2 )
                    {
                        case TypeCode.Single:
                        return true;
                        case TypeCode.Double:
                        return true;
                        default:
                        return false;
                    }
                // Possible conversions from Single follow.
                if ( typeCode1 == TypeCode.Single )
                    switch ( typeCode2 )
                    {
                        case TypeCode.Double:
                        return true;
                        default:
                        return false;
                    }
            }
            return false;
        }

    }


}
