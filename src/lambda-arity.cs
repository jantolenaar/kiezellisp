// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Kiezel
{
    public class MultiArityLambda : IDynamicMetaObjectProvider, IApply, ISyntax
    {
        internal List<LambdaClosure> Lambdas = new List<LambdaClosure>();

        internal MultiArityLambda( params LambdaClosure[] lambdas ) 
        {
            foreach (var lambda in lambdas)
            {
                lambda.Owner = this;
                Lambdas.Add( lambda );
            }
        }

        object IApply.Apply( object[] args )
        {
            var methods = Match( args );
            if ( methods == null )
            {
                throw new LispException( "No matching multi-arity-lambda found" );
            }
            var lambda = ( LambdaClosure ) methods.Car;
            return lambda.ApplyLambdaFast( args );
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject( Expression parameter )
        {
            return new MultiArityLambdaApplyMetaObject( parameter, this );
        }

        Cons ISyntax.GetSyntax( Symbol context )
        {
            return Runtime.AsList( Lambdas.Select( Runtime.GetSyntax ).Distinct() );
        }

        public Cons Match( object[] args )
        {
            return Runtime.AsList( Lambdas.Where( x => x.Definition.Signature.RequiredArgsCount == args.Length ) );
        }

    }

    public partial class Runtime
    {
        internal static MultiArityLambda MakeMultiArityLambda( LambdaClosure[] lambdas)
        {
            var multilambda = new MultiArityLambda( lambdas );
            return multilambda;
        }
    }


    internal class MultiArityLambdaApplyMetaObject : DynamicMetaObject
    {
        internal MultiArityLambda Parent;

        public MultiArityLambdaApplyMetaObject( Expression objParam, MultiArityLambda parent )
            : base( objParam, BindingRestrictions.Empty, parent )
        {
            this.Parent = parent;
        }

        public override DynamicMetaObject BindInvoke( InvokeBinder binder, DynamicMetaObject[] args )
        {
            var methods = Match( args );
            if ( methods == null )
            {
                throw new LispException( "No matching multi-lambda found" );
            }
            var lambda = ( LambdaClosure ) methods.Car;
            var restrictions = BindingRestrictions.Empty;
            MethodInfo method = typeof( LambdaClosure ).GetMethod( "ApplyLambdaFast", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
            var list = new List<Expression>();
            foreach ( var arg in args )
            {
                list.Add( RuntimeHelpers.ConvertArgument( arg, typeof( object ) ) );
            }
            var callArgs = Expression.NewArrayInit( typeof( object ), list );
            var expr = Expression.Call( Expression.Constant( lambda, typeof( LambdaClosure ) ), method, callArgs );
            restrictions = BindingRestrictions.GetInstanceRestriction( this.Expression, this.Value ).Merge( restrictions );
            return new DynamicMetaObject( RuntimeHelpers.EnsureObjectResult( expr ), restrictions );

        }

        public Cons Match( DynamicMetaObject[] args )
        {
            return Runtime.AsList( Parent.Lambdas.Where( x => x.Definition.Signature.RequiredArgsCount == args.Length ) );
        }
    }
}