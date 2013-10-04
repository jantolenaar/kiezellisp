// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

using System;
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

    public class AccessorLambda : IDynamicMetaObjectProvider, IApply
    {
        internal string MemberName;

        public AccessorLambda( string memberName )
        {
            MemberName = memberName;
        }

        public override string ToString()
        {
            return String.Format( "AccessorLambda Name=\"{0}\"", MemberName );
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject( Expression parameter )
        {
            return new GenericApplyMetaObject<AccessorLambda>( parameter, this );
        }

        object IApply.Apply( object[] args )
        {
            return Runtime.InvokeMember( MemberName, args );
        }

    }


  
    public class ImportedFunction : IDynamicMetaObjectProvider, IApply, ISyntax
    {
        internal bool Pure;
        internal MethodInfo[] Members;
        internal dynamic Proc0;
        internal dynamic Proc1;
        internal dynamic Proc2;
        internal dynamic Proc3;
        internal dynamic Proc4;
        internal dynamic Proc5;
        internal dynamic Proc6;
        internal dynamic Proc7;
        internal dynamic Proc8;
        internal dynamic Proc9;
        internal dynamic Proc10;
        internal dynamic Proc11;
        internal dynamic Proc12;


        internal ImportedFunction( MethodInfo[] members, bool pure )
        {
            Init();
            Members = members;
            Pure = pure;
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

        Cons ISyntax.GetSyntax( Symbol context )
        {
            var v = new Vector();
            foreach (var m in Members)
            {
                v.Add( Runtime.GetMethodSyntax( m, context ) );
            }
            return Runtime.AsList( Runtime.Distinct( v, Runtime.StructurallyEqual ) );
        }

        public bool HasKiezelMethods
        {
            get
            {
                return Members.Any( x => x.DeclaringType.FullName.IndexOf("Kiezel") != -1 );
            }
        }

        public override string ToString()
        {
            return String.Format( "Function Name=\"{0}.{1}\"", Members[0].DeclaringType, Members[0].Name );
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject( Expression parameter )
        {
            return new ImportedFunctionMetaObject( parameter, this );
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
            string suitable = "";
            DynamicMetaObject argsFirst = null;
            DynamicMetaObject[] argsRest = null;
            MethodInfo method = null;

            foreach ( MethodInfo m in runtimeModel.Members )
            {
                suitable = "suitable ";

                if ( m.IsStatic )
                {
                    if ( RuntimeHelpers.ParametersMatchArguments( m.GetParameters(), args ) )
                    {
                        method = m;
                        break;
                    }
                }
                else
                {
                    if ( argsRest == null )
                    {
                        RuntimeHelpers.SplitCombinedTargetArgs( args, out argsFirst, out argsRest );
                    }

                    if ( argsRest != null && RuntimeHelpers.ParametersMatchArguments( m.GetParameters(), argsRest ) )
                    {
                        method = m;
                        break;
                    }
                }

            }

            if ( method == null )
            {
                throw new MissingMemberException( "No " + suitable + "method found: " + runtimeModel.Members[0].Name );
            }


            var restrictions = RuntimeHelpers.GetTargetArgsRestrictions( this, args, true );

            if ( method.IsStatic )
            {
                var callArgs = RuntimeHelpers.ConvertArguments( args, method.GetParameters() );
                return new DynamicMetaObject( RuntimeHelpers.EnsureObjectResult( Expression.Call( method, callArgs ) ), restrictions );
            }
            else
            {
                var target = Expression.Convert( argsFirst.Expression, method.DeclaringType );
                var callArgs = RuntimeHelpers.ConvertArguments( argsRest, method.GetParameters() );
                return new DynamicMetaObject( RuntimeHelpers.EnsureObjectResult( Expression.Call( target, method, callArgs ) ), restrictions );
            }

        }

        //public override DynamicMetaObject BindConvert( ConvertBinder binder )
        //{
        //    var expr = Expression.Constant( RuntimeHelpers.CreateDelegate( runtimeModel.Runtime, runtimeModel.Members[ 0 ] ) );
        //    return new DynamicMetaObject( expr, this.Restrictions );
        //}
    }


    public class ImportedConstructor : IDynamicMetaObjectProvider, IApply
    {
        internal ConstructorInfo[] Members;
        internal dynamic Proc0;
        internal dynamic Proc1;
        internal dynamic Proc2;
        internal dynamic Proc3;
        internal dynamic Proc4;
        internal dynamic Proc5;
        internal dynamic Proc6;
        internal dynamic Proc7;
        internal dynamic Proc8;
        internal dynamic Proc9;
        internal dynamic Proc10;
        internal dynamic Proc11;
        internal dynamic Proc12;


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

        public override string ToString()
        {
            return String.Format( "BuiltinConstructor Method=\"{0}.{1}\"", Members[ 0 ].DeclaringType, Members[ 0 ].Name );
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject( Expression parameter )
        {
            return new ImportedConstructorMetaObject( parameter, this );
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

        public bool HasKiezelMethods
        {
            get
            {
                return Members.Any( x => x.DeclaringType.FullName.IndexOf( "Kiezel" ) != -1 );
            }
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
            string suitable = "";
            ConstructorInfo ctor = null;

            foreach ( ConstructorInfo m in runtimeModel.Members )
            {
                if ( m.IsStatic )
                {
                    continue;
                }

                suitable = "suitable ";

                if ( RuntimeHelpers.ParametersMatchArguments( m.GetParameters(), args ) )
                {
                    ctor = m;
                    break;
                }
            }

            if ( ctor == null )
            {
                throw new MissingMemberException( "No " + suitable + "constructor found: " + runtimeModel.Members[ 0 ].Name );
            }


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

}

