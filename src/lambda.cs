// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace Kiezel
{

    enum LambdaKind
    {
        Function,
        Method,
        Macro,
        Optional
    }

    public class Lambda : IDynamicMetaObjectProvider, IApply, ISyntax
    {
        internal Func<object, object[], object> Proc;
        internal Symbol Name;
        internal MultiMethod Generic;
        internal Cons Syntax;
        internal Cons Source;
        internal LambdaSignature Signature;
        internal Frame Frame;
        internal bool IsGetter;

        public Lambda()
        {
        }

        public Lambda( Lambda template )
        {
            Name = template.Name;
            Proc = template.Proc;
            Signature = template.Signature.EvalSpecializers();
            Syntax = template.Syntax;
            Source = template.Source;
            Frame = Runtime.CurrentThreadContext.Frame;
            IsGetter = Signature.IsGetter();
        }

        internal LambdaKind Kind
        {
            get
            {
                return Signature.Kind;
            }
        }

        Cons ISyntax.GetSyntax( Symbol context )
        {
            if ( Syntax != null )
            {
                return new Cons( Syntax, null );
            }
            else
            {
                return null;
            }
        }

        object IApply.Apply( object[] args )
        {
            if ( Kind == LambdaKind.Macro )
            {
                throw new LispException( "Macros must be defined before use." );
            }
            return ApplyLambda( args );
        }

        internal object ApplyLambda( object[] args )
        {
            var context = Runtime.CurrentThreadContext;
            var saved = context.Frame;
            context.Frame = Frame;
            object result;
            if ( Runtime.DebugMode )
            {
                context.EvaluationStack = Runtime.MakeListStar( saved, context.SpecialStack, context.EvaluationStack );
                result = Proc( this, args );
                context.EvaluationStack = Runtime.Cddr( context.EvaluationStack );
            }
            else
            {
                result = Proc( this, args );
            }
            context.Frame = saved;

            var tailcall = result as TailCall;
            if ( tailcall != null )
            {
                result = tailcall.Run();
            }

            return result;
        }

        public override string ToString()
        {
            if ( Kind == LambdaKind.Macro )
            {
                return String.Format( "Macro Name=\"{0}\"", Name == null ? "" : Name.Name );
            }
            else
            {
                return String.Format( "Lambda Name=\"{0}\"", Name == null ? "" : Name.Name );
            }
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject( Expression parameter )
        {
            return new GenericApplyMetaObject<Lambda>( parameter, this );
        }
    }


    class TailCall
    {
        IApply Proc;
        object[] Args;

        public TailCall( IApply proc, object[] args )
        {
            Proc = proc;
            Args = args;
        }

        public object Run()
        {
            return Proc.Apply( Args );
        }
    }

    class ApplyWrapper : IApply, IDynamicMetaObjectProvider
    {
        IApply Proc;

        public ApplyWrapper( IApply proc )
        {
            Proc = proc;
        }

        object IApply.Apply( object[] args )
        {
            return Runtime.Apply( Proc, args[ 0 ] );
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject( Expression parameter )
        {
            return new GenericApplyMetaObject<ApplyWrapper>( parameter, this );
        }

    }

    class GenericApplyMetaObject<T> : DynamicMetaObject
    {
        internal T lambda;

        public GenericApplyMetaObject( Expression objParam, T lambda )
            : base( objParam, BindingRestrictions.Empty, lambda )
        {
            this.lambda = lambda;
        }

        public override DynamicMetaObject BindInvoke( InvokeBinder binder, DynamicMetaObject[] args )
        {
            MethodInfo method = typeof( IApply ).GetMethod( "Apply" );
            var list = new List<Expression>();
            foreach ( var arg in args )
            {
                list.Add( RuntimeHelpers.ConvertArgument( arg, typeof( object ) ) );
            }
            var callArg = Expression.NewArrayInit( typeof( object ), list );
            var expr = Expression.Call( Expression.Convert( this.Expression, typeof( T ) ), method, callArg );
            var restrictions = BindingRestrictions.GetTypeRestriction( this.Expression, typeof( T ) );
            return new DynamicMetaObject( RuntimeHelpers.EnsureObjectResult( expr ), restrictions );
        }

    }

    public partial class Runtime
    {
        [Lisp( "system.create-tailcall" )]
        public static object CreateTailCall( IApply proc, params object[] args )
        {
            return new TailCall( proc, args );
        }
    }
}


  