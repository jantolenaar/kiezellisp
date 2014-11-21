// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Kiezel
{
    public class InvokeMemberBinderKey
    {
        private int _count;
        private string _name;
        public InvokeMemberBinderKey( string name, int count )
        {
            _name = name;
            _count = count;
        }

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
        public override bool Equals( object obj )
        {
            InvokeMemberBinderKey key = obj as InvokeMemberBinderKey;
            return key != null && key._name == _name && key._count == _count;
        }

        public override int GetHashCode()
        {
            // Stolen from DLR sources when it overrode GetHashCode on binders.
            return 0x28000000 ^ _name.GetHashCode() ^ _count.GetHashCode();
        }
    }

    public class KiezelGetIndexBinder : GetIndexBinder
    {
        public KiezelGetIndexBinder( int count )
            : base( Runtime.GetCallInfo( count ) )
        {
        }

        public override DynamicMetaObject FallbackGetIndex(
                     DynamicMetaObject target, DynamicMetaObject[] indexes,
                     DynamicMetaObject errorSuggestion )
        {
            var deferArgs = RuntimeHelpers.CheckDeferArgs( target, indexes );
            if ( deferArgs != null )
            {
                return Defer( deferArgs );
            }

            if ( target.Value == null )
            {
                var expr = Expression.Constant( null );
                var restrictions2 = BindingRestrictions.GetInstanceRestriction( target.Expression, null );
                return new DynamicMetaObject( expr, restrictions2 );
            }

            // Find our own binding.
            //
            // Conversions created in GetIndexExpression must be consistent with
            // restrictions made in GetTargetArgsRestrictions.
            var indexingExpr = RuntimeHelpers.GetIndexingExpression( target, indexes );

            if ( indexingExpr == null )
            {
                return errorSuggestion ??
                        RuntimeHelpers.CreateThrow(
                             target, indexes, BindingRestrictions.Empty,
                             typeof( InvalidOperationException ),
                             "No get indexer available on: " + target.LimitType.Name + " " + Runtime.ToPrintString( target.Value ) );
            }

            var restrictions = RuntimeHelpers.GetTargetArgsRestrictions( target, indexes, false );
            return new DynamicMetaObject( RuntimeHelpers.EnsureObjectResult( indexingExpr ), restrictions );
        }
    }

    public class KiezelGetMemberBinder : GetMemberBinder
    {
        public KiezelGetMemberBinder( string name )
            : base( name, true )
        {
        }

        public override DynamicMetaObject FallbackGetMember( DynamicMetaObject target, DynamicMetaObject errorSuggestion )
        {
            // Used by (attr obj name)

            if ( !target.HasValue )
            {
                return Defer( target );
            }

            if ( target.Value == null )
            {
                return RuntimeHelpers.CheckTargetNullReference( target, "Cannot get property on a null reference" );
            }

            var flags = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            var name = this.Name.LispToPascalCaseName();
            var members = target.LimitType.GetMember( name, flags );

            if ( members.Length == 1 && ( members[ 0 ] is PropertyInfo || members[ 0 ] is FieldInfo ) )
            {
                var expr = Expression.MakeMemberAccess( Expression.Convert( target.Expression, members[ 0 ].DeclaringType ), members[ 0 ] );

                return new DynamicMetaObject( RuntimeHelpers.EnsureObjectResult( expr ),
                    // Don't need restriction test for name since this
                    // rule is only used where binder is used, which is
                    // only used in sites with this binder.Name.
                    BindingRestrictions.GetTypeRestriction( target.Expression, target.LimitType ) );
            }
            else
            {
                return errorSuggestion ??
                    RuntimeHelpers.CreateThrow(
                        target, null,
                        BindingRestrictions.GetTypeRestriction( target.Expression, target.LimitType ),
                        typeof( MissingMemberException ),
                        "Property or field not found: " + target.LimitType.Name + "." + name );
            }
        }
    }

    public class KiezelInvokeBinder : InvokeBinder
    {
        public KiezelInvokeBinder( int count )
            : base( Runtime.GetCallInfo( count ) )
        {
        }

        public override DynamicMetaObject FallbackInvoke(
                DynamicMetaObject target, DynamicMetaObject[] argMOs,
                DynamicMetaObject errorSuggestion )
        {
            var deferArgs = RuntimeHelpers.CheckDeferArgs( target, argMOs );
            if ( deferArgs != null )
            {
                return Defer( deferArgs );
            }

            if ( target.Value == null )
            {
                return RuntimeHelpers.CheckTargetNullReference( target, "Cannot invoke a null function" );
            }

            //if ( Runtime.EltWrappable( target.Value ) )
            //{
            //    var indexingExpr = RuntimeHelpers.GetIndexingExpression( target, argMOs );

            //    if ( indexingExpr == null )
            //    {
            //        return errorSuggestion ??
            //                RuntimeHelpers.CreateThrow(
            //                     target, argMOs, BindingRestrictions.Empty,
            //                     typeof( InvalidOperationException ),
            //                     "No get indexer available on: " + target.LimitType.Name + " " + Runtime.ToPrintString( target.Value ) );
            //    }

            //    var restrictions = RuntimeHelpers.GetTargetArgsRestrictions( target, argMOs, false );
            //    return new DynamicMetaObject( RuntimeHelpers.EnsureObjectResult( indexingExpr ), restrictions );
            //}

            if ( target.LimitType.IsSubclassOf( typeof( Delegate ) ) )
            {
                var parms = target.LimitType.GetMethod( "Invoke" ).GetParameters();

                if ( parms.Length == argMOs.Length )
                {
                    // Don't need to check if argument types match parameters.
                    // If they don't, users get an argument conversion error.
                    var callArgs = RuntimeHelpers.ConvertArguments( argMOs, parms );
                    var expression = Expression.Invoke(
                        Expression.Convert( target.Expression, target.LimitType ),
                        callArgs );
                    return new DynamicMetaObject(
                        RuntimeHelpers.EnsureObjectResult( expression ),
                        BindingRestrictions.GetTypeRestriction( target.Expression,
                                                               target.LimitType ) );
                }
            }

            return errorSuggestion ??
                RuntimeHelpers.CreateThrow(
                    target, argMOs,
                    BindingRestrictions.GetTypeRestriction( target.Expression,
                                                           target.LimitType ),
                    typeof( InvalidOperationException ),
                    "Not invokable" );
        }
    }

    public class KiezelInvokeMemberBinder : InvokeMemberBinder
    {
        public KiezelInvokeMemberBinder( string name, int count )
            : base( name, true, Runtime.GetCallInfo( count ) )
        { // true = ignoreCase
        }

        public override DynamicMetaObject FallbackInvoke(
                DynamicMetaObject target, DynamicMetaObject[] args,
                DynamicMetaObject errorSuggestion )
        {
            var argexprs = new Expression[ args.Length + 1 ];
            for ( int i = 0; i < args.Length; i++ )
            {
                argexprs[ i + 1 ] = args[ i ].Expression;
            }
            argexprs[ 0 ] = target.Expression;

            return new DynamicMetaObject(
                           Runtime.CompileDynamicExpression(
                                Runtime.GetInvokeBinder( args.Length ),
                                typeof( object ),
                                argexprs ),
                           target.Restrictions.Merge(
                                BindingRestrictions.Combine( args ) ) );
        }

        public override DynamicMetaObject FallbackInvokeMember(
                DynamicMetaObject target, DynamicMetaObject[] args,
                DynamicMetaObject errorSuggestion )
        {
            var deferArgs = RuntimeHelpers.CheckDeferArgs( target, args );
            if ( deferArgs != null )
            {
                return Defer( deferArgs );
            }

            var limitType = target.LimitType;

            if ( target.Value == null )
            {
                return RuntimeHelpers.CheckTargetNullReference( target, "Cannot invoke a method on a null reference" );
            }

            var builtin = Runtime.FindImportedFunction( limitType, this.Name );

            if ( builtin != null )
            {
                DynamicMetaObject result;
                if ( builtin.TryBindInvokeBestMethod( false, target, target, args, out result ) )
                {
                    return result;
                }
            }

            var name = this.Name.LispToPascalCaseName();
            var flags = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            var methods = new List<CandidateMethod<MethodInfo>>();
            bool createdParamArray;

            foreach ( var m in limitType.GetMember( name, flags ) )
            {
                MethodInfo mi;

                if ( m is PropertyInfo )
                {
                    mi = ( ( PropertyInfo ) m ).GetGetMethod();
                }
                else
                {
                    mi = m as MethodInfo;
                }

                if ( mi != null && RuntimeHelpers.ParametersMatchArguments( mi.GetParameters(), args, out createdParamArray ) )
                {
                    RuntimeHelpers.InsertInMostSpecificOrder( methods, mi, createdParamArray );
                }
            }

            if ( methods.Count == 0 && ( this.Name.StartsWith( "set-" ) || this.Name.StartsWith( "get-" ) ) )
            {
                // Fallback to special handling to change .set-bla to .set_bla if the former has failed.
                name = this.Name.Left( 3 ) + "_" + this.Name.Substring( 4 ).LispToPascalCaseName();

                foreach ( var m in limitType.GetMember( name, flags ) )
                {
                    MethodInfo mi;

                    if ( m is PropertyInfo )
                    {
                        mi = ( ( PropertyInfo ) m ).GetGetMethod();
                    }
                    else
                    {
                        mi = m as MethodInfo;
                    }

                    if ( mi != null && RuntimeHelpers.ParametersMatchArguments( mi.GetParameters(), args, out createdParamArray ) )
                    {
                        RuntimeHelpers.InsertInMostSpecificOrder( methods, mi, createdParamArray );
                    }
                }
            }

            var restrictions = RuntimeHelpers.GetTargetArgsRestrictions( target, args, false );

            if ( methods.Count == 0 )
            {
                return errorSuggestion ?? RuntimeHelpers.CreateThrow( target, args, restrictions, typeof( MissingMemberException ),
                        "No (suitable) method found: " + limitType.Name + "." + name );
            }
            else
            {
                var method = methods[ 0 ].Method;
                var callArgs2 = RuntimeHelpers.ConvertArguments( args, method.GetParameters() );
                Expression expr;

                if ( method.IsStatic )
                {
                    // NOT REACHED
                    // static
                    // extension with target as extra parameter
                    expr = Expression.Call( method, callArgs2 );
                }
                else
                {
                    expr = Expression.Call( Expression.Convert( target.Expression, limitType ), method, callArgs2 );
                }

                return new DynamicMetaObject( RuntimeHelpers.EnsureObjectResult( expr ), restrictions );
            }
        }
    }

    public class KiezelSetIndexBinder : SetIndexBinder
    {
        public KiezelSetIndexBinder( int count )
            : base( Runtime.GetCallInfo( count ) )
        {
        }

        public override DynamicMetaObject FallbackSetIndex(
                   DynamicMetaObject target, DynamicMetaObject[] indexes,
                   DynamicMetaObject value, DynamicMetaObject errorSuggestion )
        {
            var deferArgs = RuntimeHelpers.CheckDeferArgs( target, indexes );
            if ( deferArgs != null )
            {
                return Defer( deferArgs );
            }

            if ( target.Value == null )
            {
                return RuntimeHelpers.CheckTargetNullReference( target, "Cannot set index on null reference" );
            }

            Expression valueExpr = value.Expression;
            Debug.Assert( target.HasValue && target.LimitType != typeof( Array ) );
            Type valueType;
            var indexingExpr = RuntimeHelpers.GetIndexingExpression( target, indexes, out valueType );

            if ( indexingExpr == null )
            {
                return errorSuggestion ??
                        RuntimeHelpers.CreateThrow(
                             target, indexes, BindingRestrictions.Empty,
                             typeof( InvalidOperationException ),
                             "No set indexer available on" + target.LimitType.Name );
            }

            var setIndexExpr = Expression.Assign( indexingExpr, Expression.Convert( valueExpr, valueType ) );

            BindingRestrictions restrictions = RuntimeHelpers.GetTargetArgsRestrictions( target, indexes, false );

            return new DynamicMetaObject( RuntimeHelpers.EnsureObjectResult( setIndexExpr ), restrictions );
        }
    }

    public class KiezelSetMemberBinder : SetMemberBinder
    {
        public KiezelSetMemberBinder( string name )
            : base( name, true )
        {
        }

        public override DynamicMetaObject FallbackSetMember(
                DynamicMetaObject target, DynamicMetaObject value,
                DynamicMetaObject errorSuggestion )
        {
            // Used by (set-attr obj name value)

            if ( !target.HasValue )
            {
                return Defer( target );
            }

            if ( target.Value == null )
            {
                return RuntimeHelpers.CheckTargetNullReference( target, "Cannot set property on a null reference" );
            }

            var flags = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            var name = this.Name.LispToPascalCaseName();
            var members = target.LimitType.GetMember( name, flags );

            if ( members.Length == 1 && members[ 0 ] is PropertyInfo )
            {
                var prop = ( PropertyInfo ) members[ 0 ];
                var val = Expression.Convert( value.Expression, prop.PropertyType );
                var expr = Expression.Assign( Expression.MakeMemberAccess( Expression.Convert( target.Expression, prop.DeclaringType ), prop ), val );
                return new DynamicMetaObject( RuntimeHelpers.EnsureObjectResult( expr ),
                                              BindingRestrictions.GetTypeRestriction( target.Expression, target.LimitType ) );
            }
            else if ( members.Length == 1 && members[ 0 ] is FieldInfo )
            {
                var field = ( FieldInfo ) members[ 0 ];
                var val = Expression.Convert( value.Expression, field.FieldType );
                var expr = Expression.Assign( Expression.MakeMemberAccess( Expression.Convert( target.Expression, field.DeclaringType ), field ), val );
                return new DynamicMetaObject( RuntimeHelpers.EnsureObjectResult( expr ),
                                              BindingRestrictions.GetTypeRestriction( target.Expression, target.LimitType ) );
            }
            else if ( members.Length == 1 && members[ 0 ] is EventInfo )
            {
                //public static void AddEventHandler( System.Reflection.EventInfo eventinfo, object target, object func )

                var evt = ( EventInfo ) members[ 0 ];
                var expr = Expression.Call( Runtime.AddEventHandlerMethod,
                                                        Expression.Constant( evt, typeof( EventInfo ) ),
                                                        Expression.Convert( target.Expression, evt.DeclaringType ),
                                                        Expression.Convert( value.Expression, typeof( IApply ) ) );
                return new DynamicMetaObject( RuntimeHelpers.EnsureObjectResult( expr ),
                                              BindingRestrictions.GetTypeRestriction( target.Expression, target.LimitType ) );
            }
            else
            {
                return errorSuggestion ??
                    RuntimeHelpers.CreateThrow(
                        target, null,
                        BindingRestrictions.GetTypeRestriction( target.Expression,
                                                               target.LimitType ),
                        typeof( MissingMemberException ),
                        "Property or field not found: " + target.LimitType.Name + "." + name );
            }
        }
    }

    public partial class Runtime
    {
        internal static ConcurrentDictionary<int, CallInfo> _getCallInfo;
        internal static ConcurrentDictionary<int, CallSiteBinder> _getIndexBinders;
        internal static ConcurrentDictionary<string, CallSiteBinder> _getMemberBinders;
        internal static ConcurrentDictionary<int, CallSiteBinder> _invokeBinders;
        internal static ConcurrentDictionary<InvokeMemberBinderKey, CallSiteBinder> _invokeMemberBinders;
        internal static ConcurrentDictionary<int, CallSiteBinder> _setIndexBinders;
        internal static ConcurrentDictionary<string, CallSiteBinder> _setMemberBinders;
        internal static CallInfo GetCallInfo( int count )
        {
            return _getCallInfo.GetOrAdd( count, GetCallInfoFactory );
        }

        internal static CallInfo GetCallInfoFactory( int n )
        {
            return new CallInfo( n );
        }

        internal static CallSiteBinder GetCallSiteBinderFactory( string name )
        {
            return new KiezelGetMemberBinder( name );
        }

        internal static CallSiteBinder GetGetIndexBinder( int count )
        {
            return _getIndexBinders.GetOrAdd( count, GetGetIndexBinderFactory );
        }

        internal static CallSiteBinder GetGetIndexBinderFactory( int count )
        {
            return new KiezelGetIndexBinder( count );
        }

        internal static CallSiteBinder GetGetInvokeBinderFactory( int count )
        {
            return new KiezelInvokeBinder( count );
        }

        internal static CallSiteBinder GetGetInvokeMemberBinderFactory( InvokeMemberBinderKey info )
        {
            return new KiezelInvokeMemberBinder( info.Name, info.Count );
        }

        internal static CallSiteBinder GetGetMemberBinder( string name )
        {
            return _getMemberBinders.GetOrAdd( name, GetCallSiteBinderFactory );
        }

        internal static CallSiteBinder GetInvokeBinder( int count )
        {
            return _invokeBinders.GetOrAdd( count, GetGetInvokeBinderFactory );
        }

        internal static CallSiteBinder GetInvokeMemberBinder( InvokeMemberBinderKey info )
        {
            return _invokeMemberBinders.GetOrAdd( info, GetGetInvokeMemberBinderFactory );
        }

        internal static CallSiteBinder GetSetIndexBinder( int count )
        {
            return _setIndexBinders.GetOrAdd( count, GetSetIndexBinderFactory );
        }

        internal static CallSiteBinder GetSetIndexBinderFactory( int count )
        {
            return new KiezelSetIndexBinder( count );
        }

        internal static CallSiteBinder GetSetMemberBinder( string name )
        {
            return _setMemberBinders.GetOrAdd( name, GetSetMemberBinderFactory );
        }

        internal static CallSiteBinder GetSetMemberBinderFactory( string name )
        {
            return new KiezelSetMemberBinder( name );
        }

        internal static void RestartBinders()
        {
            _getCallInfo = new ConcurrentDictionary<int, CallInfo>();
            _getMemberBinders = new ConcurrentDictionary<string, CallSiteBinder>();
            _setMemberBinders = new ConcurrentDictionary<string, CallSiteBinder>();
            _getIndexBinders = new ConcurrentDictionary<int, CallSiteBinder>();
            _setIndexBinders = new ConcurrentDictionary<int, CallSiteBinder>();
            _invokeBinders = new ConcurrentDictionary<int, CallSiteBinder>();
            _invokeMemberBinders = new ConcurrentDictionary<InvokeMemberBinderKey, CallSiteBinder>();
        }
    }
     //InvokeMemberBinderKey
}