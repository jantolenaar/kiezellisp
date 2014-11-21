// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Kiezel
{
    public class AccessorLambda : IDynamicMetaObjectProvider, IApply
    {
        internal string Members;
        internal bool Nullable;
        internal Func<object> Proc0;
        internal Func<object, object> Proc1;
        internal Func<object, object, object> Proc2;
        internal Func<object, object, object, object> Proc3;
        internal Func<object, object, object, object, object> Proc4;
        internal Func<object, object, object, object, object, object> Proc5;
        internal Func<object, object, object, object, object, object, object> Proc6;

        public AccessorLambda( bool nullable, string members )
        {
            Members = members;
            Nullable = nullable;
        }

        object IApply.Apply( object[] args )
        {
            if ( args.Length > 6 )
            {
                var args2 = args.Select( x => ( Expression ) Expression.Constant( x ) ).ToArray();
                var expr = AccessorLambdaMetaObject.MakeExpression( Nullable, Members, args2 );
                var proc = Runtime.CompileToFunction( expr );
                var val = proc();
                return val;
            }
            else
            {
                switch ( args.Length )
                {
                    case 0:
                    {
                        if ( Proc0 == null )
                        {
                            Proc0 = ( Func<object> ) MakeExpressionProc( 0 );
                        }
                        return Proc0();
                    }
                    case 1:
                    {
                        if ( Proc1 == null )
                        {
                            Proc1 = ( Func<object, object> ) MakeExpressionProc( 1 );
                        }
                        return Proc1( args[ 0 ] );
                    }
                    case 2:
                    {
                        if ( Proc2 == null )
                        {
                            Proc2 = ( Func<object, object, object> ) MakeExpressionProc( 2 );
                        }
                        return Proc2( args[ 0 ], args[ 1 ] );
                    }
                    case 3:
                    {
                        if ( Proc3 == null )
                        {
                            Proc3 = ( Func<object, object, object, object> ) MakeExpressionProc( 3 );
                        }
                        return Proc3( args[ 0 ], args[ 1 ], args[ 2 ] );
                    }
                    case 4:
                    {
                        if ( Proc4 == null )
                        {
                            Proc4 = ( Func<object, object, object, object, object> ) MakeExpressionProc( 4 );
                        }
                        return Proc4( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ] );
                    }
                    case 5:
                    {
                        if ( Proc5 == null )
                        {
                            Proc5 = ( Func<object, object, object, object, object, object> ) MakeExpressionProc( 1 );
                        }
                        return Proc5( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ] );
                    }
                    case 6:
                    {
                        if ( Proc6 == null )
                        {
                            Proc6 = ( Func<object, object, object, object, object, object, object> ) MakeExpressionProc( 6 );
                        }
                        return Proc6( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ], args[ 5 ] );
                    }
                    default:
                    {
                        throw new NotImplementedException( "Apply supports up to 6 arguments" );
                    }
                }
            }
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject( Expression parameter )
        {
            return new AccessorLambdaMetaObject( parameter, this );
        }

        public override string ToString()
        {
            return String.Format( "AccessorLambda Name=\"{0}\" Nullable=\"{1}\"", Members, Nullable );
        }
        internal Delegate MakeExpressionProc( int argCount )
        {
            var names = Members.Split( new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries );
            var args = new ParameterExpression[ argCount ];
            for ( var i = 0; i < argCount; ++i )
            {
                args[ i ] = Expression.Parameter( typeof( object ) );
            }
            var code = AccessorLambdaMetaObject.MakeExpression( Nullable, Members, args );
            var proc = Runtime.CompileToDelegate( code, args );
            return proc;
        }
    }

    public class AccessorLambdaMetaObject : DynamicMetaObject
    {
        internal AccessorLambda Lambda;

        public AccessorLambdaMetaObject( Expression parameter, AccessorLambda lambda )
            : base( parameter, BindingRestrictions.Empty, lambda )
        {
            this.Lambda = lambda;
        }

        public static Expression MakeExpression( bool nullable, string members, Expression[] args )
        {
            // Warning: modifies args

            if ( args.Length == 0 )
            {
                throw new LispException( "Member accessor invoked without a target: {0}", members );
            }

            var names = members.Split( new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries );
            var code = args[ 0 ];

            if ( nullable )
            {
                var temp = Expression.Parameter( typeof( object ) );

                for ( var i = 0; i < names.Length; ++i )
                {
                    Expression code2;

                    if ( i < names.Length - 1 )
                    {
                        var binder = Runtime.GetInvokeMemberBinder( new InvokeMemberBinderKey( names[ i ], 0 ) );
                        code2 = Runtime.CompileDynamicExpression( binder, typeof( object ), new Expression[] { code } );
                    }
                    else
                    {
                        var binder = Runtime.GetInvokeMemberBinder( new InvokeMemberBinderKey( names[ i ], args.Length - 1 ) );
                        args[ 0 ] = code;
                        code2 = Runtime.CompileDynamicExpression( binder, typeof( object ), args );
                    }

                    code = Expression.Condition( Runtime.WrapBooleanTest( Expression.Assign( temp, code ) ), code2, Expression.Constant( null ) );
                }

                code = Expression.Block( typeof( object ), new ParameterExpression[] { temp }, code );
            }
            else
            {
                for ( var i = 0; i < names.Length; ++i )
                {
                    if ( i < names.Length - 1 )
                    {
                        var binder = Runtime.GetInvokeMemberBinder( new InvokeMemberBinderKey( names[ i ], 0 ) );
                        code = Runtime.CompileDynamicExpression( binder, typeof( object ), new Expression[] { code } );
                    }
                    else
                    {
                        var binder = Runtime.GetInvokeMemberBinder( new InvokeMemberBinderKey( names[ i ], args.Length - 1 ) );
                        args[ 0 ] = code;
                        code = Runtime.CompileDynamicExpression( binder, typeof( object ), args );
                    }
                }
            }

            return code;
        }

        public override DynamicMetaObject BindInvoke( InvokeBinder binder, DynamicMetaObject[] args )
        {
            var args2 = args.Select( x => x.Expression ).ToArray();
            var code = MakeExpression( false, Lambda.Members, args2 );
            var restrictions = BindingRestrictions.GetInstanceRestriction( this.Expression, this.Value );
            return new DynamicMetaObject( code, restrictions );
        }
    }

    public class ImportedConstructor : IDynamicMetaObjectProvider, IApply
    {
        internal ConstructorInfo[] Members;
        internal dynamic Proc0;
        internal dynamic Proc1;
        internal dynamic Proc10;
        internal dynamic Proc11;
        internal dynamic Proc12;
        internal dynamic Proc2;
        internal dynamic Proc3;
        internal dynamic Proc4;
        internal dynamic Proc5;
        internal dynamic Proc6;
        internal dynamic Proc7;
        internal dynamic Proc8;
        internal dynamic Proc9;
        public ImportedConstructor( ConstructorInfo[] members )
        {
            Members = members;
            Proc0 = this;
            Proc1 = this;
            Proc2 = this;
            Proc3 = this;
            Proc4 = this;
            Proc5 = this;
            Proc6 = this;
            Proc7 = this;
            Proc8 = this;
            Proc9 = this;
            Proc10 = this;
            Proc11 = this;
            Proc12 = this;
        }

        public bool HasKiezelMethods
        {
            get
            {
                return Members.Any( x => x.DeclaringType.FullName.IndexOf( "Kiezel" ) != -1 );
            }
        }

        object IApply.Apply( object[] args )
        {
            if ( args.Length > 12 )
            {
                var binder = Runtime.GetInvokeBinder( args.Length );
                var exprs = new List<Expression>();
                exprs.Add( Expression.Constant( this ) );
                exprs.AddRange( args.Select( x => Expression.Constant( x ) ) );
                var code = Runtime.CompileDynamicExpression( binder, typeof( object ), exprs );
                var proc = Runtime.CompileToFunction( code );
                return proc();
            }
            else
            {
                switch ( args.Length )
                {
                    case 0:
                    {
                        return Proc0();
                    }
                    case 1:
                    {
                        return Proc1( args[ 0 ] );
                    }
                    case 2:
                    {
                        return Proc2( args[ 0 ], args[ 1 ] );
                    }
                    case 3:
                    {
                        return Proc3( args[ 0 ], args[ 1 ], args[ 2 ] );
                    }
                    case 4:
                    {
                        return Proc4( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ] );
                    }
                    case 5:
                    {
                        return Proc5( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ] );
                    }
                    case 6:
                    {
                        return Proc6( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ], args[ 5 ] );
                    }
                    case 7:
                    {
                        return Proc7( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ], args[ 5 ], args[ 6 ] );
                    }
                    case 8:
                    {
                        return Proc8( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ], args[ 5 ], args[ 6 ], args[ 7 ] );
                    }
                    case 9:
                    {
                        return Proc9( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ], args[ 5 ], args[ 6 ], args[ 7 ], args[ 8 ] );
                    }
                    case 10:
                    {
                        return Proc9( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ], args[ 5 ], args[ 6 ], args[ 7 ], args[ 8 ], args[ 9 ] );
                    }
                    case 11:
                    {
                        return Proc9( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ], args[ 5 ], args[ 6 ], args[ 7 ], args[ 8 ], args[ 9 ], args[ 10 ] );
                    }
                    case 12:
                    {
                        return Proc9( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ], args[ 5 ], args[ 6 ], args[ 7 ], args[ 8 ], args[ 9 ], args[ 10 ], args[ 11 ] );
                    }
                    default:
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject( Expression parameter )
        {
            return new ImportedConstructorMetaObject( parameter, this );
        }

        public override string ToString()
        {
            return String.Format( "BuiltinConstructor Method=\"{0}.{1}\"", Members[ 0 ].DeclaringType, Members[ 0 ].Name );
        }
    }

    public class ImportedConstructorMetaObject : DynamicMetaObject
    {
        internal ImportedConstructor runtimeModel;

        public ImportedConstructorMetaObject( Expression objParam, ImportedConstructor runtimeModel )
            : base( objParam, BindingRestrictions.Empty, runtimeModel )
        {
            this.runtimeModel = runtimeModel;
        }

        public override DynamicMetaObject BindInvoke( InvokeBinder binder, DynamicMetaObject[] args )
        {
            bool createdParamArray;
            string suitable = "";
            var ctors = new List<CandidateMethod<ConstructorInfo>>();

            foreach ( ConstructorInfo m in runtimeModel.Members )
            {
                if ( m.IsStatic )
                {
                    continue;
                }

                suitable = "suitable ";

                if ( RuntimeHelpers.ParametersMatchArguments( m.GetParameters(), args, out createdParamArray ) )
                {
                    RuntimeHelpers.InsertInMostSpecificOrder( ctors, m, createdParamArray );
                }
            }

            if ( ctors.Count == 0 )
            {
                throw new MissingMemberException( "No " + suitable + "constructor found: " + runtimeModel.Members[ 0 ].Name );
            }

            var ctor = ctors[ 0 ].Method;
            var restrictions = RuntimeHelpers.GetTargetArgsRestrictions( this, args, true );
            var callArgs = RuntimeHelpers.ConvertArguments( args, ctor.GetParameters() );

            return new DynamicMetaObject( RuntimeHelpers.EnsureObjectResult( Expression.New( ctor, callArgs ) ), restrictions );
        }

        //public override DynamicMetaObject BindConvert( ConvertBinder binder )
        //{
        //    var expr = Expression.Constant( RuntimeHelpers.CreateDelegate( runtimeModel.Runtime, runtimeModel.Members[ 0 ] ) );
        //    return new DynamicMetaObject( expr, this.Restrictions );
        //}
    }

    public class ImportedFunction : IDynamicMetaObjectProvider, IApply, ISyntax
    {
        internal MethodInfo[] BuiltinExtensionMembers;
        internal Type DeclaringType;
        internal MethodInfo[] ExternalExtensionMembers;
        internal MethodInfo[] Members;
        internal string Name;
        internal dynamic Proc0;
        internal dynamic Proc1;
        internal dynamic Proc10;
        internal dynamic Proc11;
        internal dynamic Proc12;
        internal dynamic Proc2;
        internal dynamic Proc3;
        internal dynamic Proc4;
        internal dynamic Proc5;
        internal dynamic Proc6;
        internal dynamic Proc7;
        internal dynamic Proc8;
        internal dynamic Proc9;
        internal bool Pure;
        internal ImportedFunction( string name, Type declaringType )
        {
            Init();
            Name = name;
            DeclaringType = declaringType;
            BuiltinExtensionMembers = new MethodInfo[ 0 ];
            Members = new MethodInfo[ 0 ];
            ExternalExtensionMembers = new MethodInfo[ 0 ];
            Pure = false;
        }

        internal ImportedFunction( string name, Type declaringType, MethodInfo[] members, bool pure )
            : this( name, declaringType )
        {
            Members = members;
            Pure = pure;
        }

        public bool HasKiezelMethods
        {
            get
            {
                return Members.Any( x => x.DeclaringType.FullName.IndexOf( "Kiezel" ) != -1 );
            }
        }

        object IApply.Apply( object[] args )
        {
            if ( args.Length > 12 )
            {
                var binder = Runtime.GetInvokeBinder( args.Length );
                var exprs = new List<Expression>();
                exprs.Add( Expression.Constant( this ) );
                exprs.AddRange( args.Select( Expression.Constant ) );
                var code = Runtime.CompileDynamicExpression( binder, typeof( object ), exprs );
                var proc = Runtime.CompileToFunction( code );
                return proc();
            }
            else
            {
                switch ( args.Length )
                {
                    case 0:
                    {
                        return Proc0();
                    }
                    case 1:
                    {
                        return Proc1( args[ 0 ] );
                    }
                    case 2:
                    {
                        return Proc2( args[ 0 ], args[ 1 ] );
                    }
                    case 3:
                    {
                        return Proc3( args[ 0 ], args[ 1 ], args[ 2 ] );
                    }
                    case 4:
                    {
                        return Proc4( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ] );
                    }
                    case 5:
                    {
                        return Proc5( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ] );
                    }
                    case 6:
                    {
                        return Proc6( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ], args[ 5 ] );
                    }
                    case 7:
                    {
                        return Proc7( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ], args[ 5 ], args[ 6 ] );
                    }
                    case 8:
                    {
                        return Proc8( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ], args[ 5 ], args[ 6 ], args[ 7 ] );
                    }
                    case 9:
                    {
                        return Proc9( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ], args[ 5 ], args[ 6 ], args[ 7 ], args[ 8 ] );
                    }
                    case 10:
                    {
                        return Proc9( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ], args[ 5 ], args[ 6 ], args[ 7 ], args[ 8 ], args[ 9 ] );
                    }
                    case 11:
                    {
                        return Proc9( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ], args[ 5 ], args[ 6 ], args[ 7 ], args[ 8 ], args[ 9 ], args[ 10 ] );
                    }
                    case 12:
                    {
                        return Proc9( args[ 0 ], args[ 1 ], args[ 2 ], args[ 3 ], args[ 4 ], args[ 5 ], args[ 6 ], args[ 7 ], args[ 8 ], args[ 9 ], args[ 10 ], args[ 11 ] );
                    }
                    default:
                    {
                        throw new NotImplementedException( "Apply supports up to 12 arguments" );
                    }
                }
            }
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject( Expression parameter )
        {
            return new ImportedFunctionMetaObject( parameter, this );
        }

        Cons ISyntax.GetSyntax( Symbol context )
        {
            var v = new Vector();
            foreach ( var m in Members )
            {
                v.Add( Runtime.GetMethodSyntax( m, context ) );
            }
            return Runtime.AsList( Runtime.SeqBase.Distinct( v, Runtime.StructurallyEqualApply ) );
        }

        public override string ToString()
        {
            return String.Format( "Function Name=\"{0}.{1}\"", DeclaringType, Name );
        }

        public bool TryBindInvokeBestMethod( bool restrictionOnTargetInstance, DynamicMetaObject target, DynamicMetaObject[] args, out DynamicMetaObject result )
        {
            DynamicMetaObject argsFirst = null;
            DynamicMetaObject[] argsRest = null;
            return TryBindInvokeBestMethod( restrictionOnTargetInstance, target, args, argsFirst, argsRest, out result );
        }

        //internal Delegate MakeExpressionProc( int argCount )
        //{
        //    var args = new ParameterExpression[ argCount ];
        //    for ( var i = 0; i < argCount; ++i )
        //    {
        //        args[ i ] = Expression.Parameter( typeof( object ) );
        //    }
        //    var binder = Runtime.GetInvokeBinder( args.Length );
        //    var code = Runtime.CompileDynamicExpression( binder, typeof( object ), args );
        //    var proc = Runtime.CompileToDelegate( code, args );
        //    return proc;
        //}
        public bool TryBindInvokeBestMethod( bool restrictionOnTargetInstance, DynamicMetaObject target, DynamicMetaObject argsFirst, DynamicMetaObject[] argsRest, out DynamicMetaObject result )
        {
            return TryBindInvokeBestMethod( restrictionOnTargetInstance, target, null, argsFirst, argsRest, out result );
        }

        public bool TryBindInvokeBestMethod( bool restrictionOnTargetInstance, DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject argsFirst, DynamicMetaObject[] argsRest, out DynamicMetaObject result )
        {
            bool createdParamArray;
            var candidates = new List<CandidateMethod<MethodInfo>>();
            args = args ?? RuntimeHelpers.GetCombinedTargetArgs( argsFirst, argsRest );

            foreach ( MethodInfo m in BuiltinExtensionMembers )
            {
                if ( m.IsStatic )
                {
                    if ( RuntimeHelpers.ParametersMatchArguments( m.GetParameters(), args, out createdParamArray ) )
                    {
                        RuntimeHelpers.InsertInMostSpecificOrder( candidates, m, createdParamArray );
                    }
                }
                else
                {
                    if ( argsRest == null )
                    {
                        RuntimeHelpers.SplitCombinedTargetArgs( args, out argsFirst, out argsRest );
                    }

                    if ( argsRest != null && RuntimeHelpers.ParametersMatchArguments( m.GetParameters(), argsRest, out createdParamArray ) )
                    {
                        RuntimeHelpers.InsertInMostSpecificOrder( candidates, m, createdParamArray );
                    }
                }
            }

            if ( candidates.Count == 0 )
            {
                foreach ( MethodInfo m in Members )
                {
                    if ( m.IsStatic )
                    {
                        if ( RuntimeHelpers.ParametersMatchArguments( m.GetParameters(), args, out createdParamArray ) )
                        {
                            RuntimeHelpers.InsertInMostSpecificOrder( candidates, m, createdParamArray );
                        }
                    }
                    else
                    {
                        if ( argsRest == null )
                        {
                            RuntimeHelpers.SplitCombinedTargetArgs( args, out argsFirst, out argsRest );
                        }

                        if ( argsRest != null && RuntimeHelpers.ParametersMatchArguments( m.GetParameters(), argsRest, out createdParamArray ) )
                        {
                            RuntimeHelpers.InsertInMostSpecificOrder( candidates, m, createdParamArray );
                        }
                    }
                }
            }

            if ( candidates.Count == 0 )
            {
                foreach ( MethodInfo m in ExternalExtensionMembers )
                {
                    if ( m.IsStatic )
                    {
                        if ( RuntimeHelpers.ParametersMatchArguments( m.GetParameters(), args, out createdParamArray ) )
                        {
                            RuntimeHelpers.InsertInMostSpecificOrder( candidates, m, createdParamArray );
                        }
                    }
                    else
                    {
                        if ( argsRest == null )
                        {
                            RuntimeHelpers.SplitCombinedTargetArgs( args, out argsFirst, out argsRest );
                        }

                        if ( argsRest != null && RuntimeHelpers.ParametersMatchArguments( m.GetParameters(), argsRest, out createdParamArray ) )
                        {
                            RuntimeHelpers.InsertInMostSpecificOrder( candidates, m, createdParamArray );
                        }
                    }
                }
            }

            if ( candidates.Count == 0 )
            {
                result = null;
                return false;
            }

            var method = candidates[ 0 ].Method;

            if ( method.IsStatic )
            {
                var restrictions = RuntimeHelpers.GetTargetArgsRestrictions( target, args, true );
                var callArgs = RuntimeHelpers.ConvertArguments( args, method.GetParameters() );
                result = new DynamicMetaObject( RuntimeHelpers.EnsureObjectResult( Expression.Call( method, callArgs ) ), restrictions );
            }
            else
            {
                if ( argsRest == null )
                {
                    RuntimeHelpers.SplitCombinedTargetArgs( args, out argsFirst, out argsRest );
                }

                // When called from FallbackInvokeMember we want to restrict on the type.
                var restrictions = RuntimeHelpers.GetTargetArgsRestrictions( target, argsRest, restrictionOnTargetInstance );
                var targetInst = Expression.Convert( argsFirst.Expression, method.DeclaringType );
                var callArgs = RuntimeHelpers.ConvertArguments( argsRest, method.GetParameters() );
                result = new DynamicMetaObject( RuntimeHelpers.EnsureObjectResult( Expression.Call( targetInst, method, callArgs ) ), restrictions );
            }

            return true;
        }

        internal void Init()
        {
            Proc0 = this;
            Proc1 = this;
            Proc2 = this;
            Proc3 = this;
            Proc4 = this;
            Proc5 = this;
            Proc6 = this;
            Proc7 = this;
            Proc8 = this;
            Proc9 = this;
            Proc10 = this;
            Proc11 = this;
            Proc12 = this;
        }
    }

    public class ImportedFunctionMetaObject : DynamicMetaObject
    {
        internal ImportedFunction runtimeModel;

        public ImportedFunctionMetaObject( Expression objParam, ImportedFunction runtimeModel )
            : base( objParam, BindingRestrictions.Empty, runtimeModel )
        {
            this.runtimeModel = runtimeModel;
        }

        public override DynamicMetaObject BindInvoke( InvokeBinder binder, DynamicMetaObject[] args )
        {
            DynamicMetaObject result;
            if ( !runtimeModel.TryBindInvokeBestMethod( true, this, args, out result ) )
            {
                throw new MissingMemberException( "No (suitable) method found: " + runtimeModel.Name );
            }
            return result;
        }

        //public override DynamicMetaObject BindConvert( ConvertBinder binder )
        //{
        //    var expr = Expression.Constant( RuntimeHelpers.CreateDelegate( runtimeModel.Runtime, runtimeModel.Members[ 0 ] ) );
        //    return new DynamicMetaObject( expr, this.Restrictions );
        //}
    }
}