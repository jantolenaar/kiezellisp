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
        Macro
    }

    public class Lambda : IDynamicMetaObjectProvider, IApply, ISyntax
    {
        internal Func<Cons, object, object[], object> Proc;
        internal Symbol Name;
        internal MultiMethod Generic;
        internal BindingRestrictions GenericRestrictions;
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
            // Entrypoint when called via funcall or apply or map etc.
            if ( Kind == LambdaKind.Macro )
            {
                throw new LispException( "Macros must be defined before use." );
            }
            return ApplyLambdaBind( null, args, false  );
        }

        public object ApplyLambdaFast( object[] args )
        {
            // Entrypoint used by compiler after rearranging args.
            return ApplyLambdaBind( null, args, true );
        }

        internal object ApplyLambdaBind( Cons lambdaList, object[] args, bool bound )
        {
            var context = Runtime.CurrentThreadContext;
            var saved = context.Frame;
            context.Frame = Frame;
            object result;

            if ( !bound )
            {
                args = MakeArgumentFrame( args );
            }

            if ( Runtime.DebugMode )
            {
                context.EvaluationStack = Runtime.MakeListStar( saved, context.SpecialStack, context.EvaluationStack );
                result = Proc( lambdaList, this, args );
                context.EvaluationStack = Runtime.Cddr( context.EvaluationStack );
            }
            else
            {
                result = Proc( lambdaList, this, args );
            }
            context.Frame = saved;

            var tailcall = result as TailCall;
            if ( tailcall != null )
            {
                result = tailcall.Run();
            }

            return result;
        }

        internal object[] MakeArgumentFrame( object[] input )
        {
            if ( Signature.Kind != LambdaKind.Macro && Signature.RequiredArgsCount == input.Length && Signature.Names.Count == input.Length )
            {
                // fast track if all arguments (no nested parameters) are accounted for.
                return input;
            }

            var output = new object[ Signature.Names.Count ];
            FillDataFrame( Signature, input, output, 0 );
            return output;
        }

        internal Exception FillDataFrame( LambdaSignature signature, object[] input, object[] output, int offsetOutput )
        {
            var offset = 0;
            var firstKey = -1;
            var usedKeys = 0;
            var haveAll = false;
            var firstArg = 0;

            if ( signature.Kind != LambdaKind.Macro && signature.RequiredArgsCount > 0 )
            {
                // This does not work for nested parameters.
                var n = signature.RequiredArgsCount;

                if ( input.Length < n )
                {
                    throw new LispException( "Missing required parameters" );
                }
                Array.Copy( input, 0, output, offsetOutput, n );
                offsetOutput += n;
                firstArg = n;
                offset = n;
            }

            for ( int iArg = firstArg; !haveAll && iArg < signature.Parameters.Count; ++iArg )
            {
                var mod = ( iArg < signature.RequiredArgsCount ) ? null : signature.ArgModifier;
                var arg = signature.Parameters[ iArg ];
                object val;

                if ( mod == Symbols.Params )
                {
                    var buf = new object[ input.Length - offset ];
                    Array.Copy( input, offset, buf, 0, buf.Length );
                    val = buf;
                    haveAll = true;
                }
                else if ( mod == Symbols.Vector )
                {
                    var v = new Vector( input.Length - offset );
                    for ( int i = offset; i < input.Length; ++i )
                    {
                        v.Add( input[ i ] );
                    }
                    val = v;
                    haveAll = true;
                }
                else if ( mod == Symbols.Rest || mod == Symbols.Body )
                {
                    Cons list = null;
                    for ( int i = input.Length - 1; i >= offset; --i )
                    {
                        list = new Cons( input[ i ], list );
                    }
                    val = list;
                    haveAll = true;
                }
                else if ( mod == Symbols.Key )
                {
                    if ( firstKey == -1 )
                    {
                        firstKey = offset;
                        for ( int i = firstKey; i < input.Length; i += 2 )
                        {
                            if ( !Runtime.Keywordp( input[ i ] ) || i + 1 == input.Length )
                            {
                                throw new LispException( "Invalid keyword/value list" );
                            }
                        }
                    }

                    val = DefaultValue.Value;

                    for ( int i = firstKey; i + 1 < input.Length; i += 2 )
                    {
                        if ( arg.Sym.Name == ((Symbol)input[ i ]).Name )
                        {
                            val = input[ i + 1 ];
                            ++usedKeys;
                            break;
                        }
                    }

                }
                else if ( offset < input.Length )
                {
                    val = input[ offset ];
                    ++offset;
                }
                else if ( mod == Symbols.Optional )
                {
                    val = DefaultValue.Value;
                }
                else
                {
                    throw new LispException( "Missing required argument: {0}", arg.Sym );
                }

                if ( val == DefaultValue.Value )
                {
                    if ( arg.InitFormProc != null )
                    {
                        val = arg.InitFormProc();
                    }
                    else
                    {
                        val = null;
                    }
                }

                if ( arg.NestedParameters != null )
                {
                    // required macro parameter
                    var nestedInput = Runtime.AsArray( ( IEnumerable ) val );
                    FillDataFrame( arg.NestedParameters, nestedInput, output, offsetOutput );
                    offsetOutput += arg.NestedParameters.Names.Count;
                }
                else
                {
                    output[ offsetOutput++ ] = val;
                }
            }

            if ( offset < input.Length && !haveAll && firstKey == -1 )
            {
                throw new LispException( "Too many parameters supplied" );
            }

            return null;
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
            return new LambdaApplyMetaObject( parameter, this );
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

    class LambdaApplyMetaObject : DynamicMetaObject
    {
        internal Lambda lambda;

        public LambdaApplyMetaObject( Expression objParam, Lambda lambda )
            : base( objParam, BindingRestrictions.Empty, lambda )
        {
            this.lambda = lambda;
        }

        public override DynamicMetaObject BindInvoke( InvokeBinder binder, DynamicMetaObject[] args )
        {
            var restrictions = BindingRestrictions.Empty;
            var callArgs = LambdaHelpers.FillDataFrame( lambda.Signature, args, ref restrictions );
            MethodInfo method = typeof( Lambda ).GetMethod( "ApplyLambdaFast", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance );
            var expr = Expression.Call( Expression.Convert( this.Expression, typeof( Lambda ) ), method, callArgs );
            restrictions = BindingRestrictions.GetInstanceRestriction( this.Expression, this.Value ).Merge( restrictions );
            return new DynamicMetaObject( RuntimeHelpers.EnsureObjectResult( expr ), restrictions );
        }

    }

    class LambdaHelpers
    {

        internal static BindingRestrictions GetGenericRestrictions( Lambda method, DynamicMetaObject[] args )
        {
            var methodList = method.Generic.Lambdas;
            var restrictions = BindingRestrictions.Empty;

            //
            // Restrictions for this method
            //

            for ( int i = 0; i < method.Signature.RequiredArgsCount; ++i )
            {
                var par = method.Signature.Parameters[ i ];
                if ( par.Specializer != null )
                {
                    var restr = BindingRestrictions.GetExpressionRestriction( Expression.Call( Runtime.IsInstanceOfMethod, args[ i ].Expression, Expression.Constant( par.Specializer ) ) );
                    restrictions = restrictions.Merge( restr );
                }
            }

            //
            // Additional NOT restrictions for lambdas that come before the method and fully subtype the method.
            //

            foreach ( Lambda lambda in methodList )
            {
                if ( lambda == method )
                {
                    break;
                }

                bool lambdaSubtypesMethod = true;

                for ( int i = 0; i < method.Signature.RequiredArgsCount; ++i )
                {
                    var par = method.Signature.Parameters[ i ];
                    var par2 = lambda.Signature.Parameters[ i ];

                    if ( !Runtime.IsSubtype( par2.Specializer, par.Specializer, false ) )
                    {
                        lambdaSubtypesMethod = false;
                        break;
                    }
                }

                if ( !lambdaSubtypesMethod )
                {
                    continue;
                }

                Expression tests = null;

                for ( int i = 0; i < method.Signature.RequiredArgsCount; ++i )
                {
                    var par = method.Signature.Parameters[ i ];
                    var par2 = lambda.Signature.Parameters[ i ];

                    if ( Runtime.IsSubtype( par2.Specializer, par.Specializer, true ) )
                    {
                        var test = Expression.Not( Expression.Call( Runtime.IsInstanceOfMethod, args[ i ].Expression,
                                                        Expression.Constant( par2.Specializer ) ) );
                        if ( tests == null)
                        {
                            tests = test;
                        }
                        else
                        {
                            tests = Expression.Or(tests,test);
                        }
                    }
                }

                if ( tests != null )
                {
                    var restr = BindingRestrictions.GetExpressionRestriction( tests );
                    restrictions = restrictions.Merge( restr );
                }
            }

            return restrictions;
        }

        internal static Expression FillDataFrame( LambdaSignature signature, DynamicMetaObject[] input, ref BindingRestrictions restrictions )
        {
            var elementType = typeof( object );
            var offset = 0;
            var output = new List<Expression>();

            for ( offset = 0; offset < signature.RequiredArgsCount; ++offset )
            {
                if ( input.Length < offset )
                {
                    throw new LispException( "Missing required parameters" );
                }
                output.Add( Expression.Convert( input[ offset ].Expression, elementType ) );
            }

            if ( offset != signature.Parameters.Count )
            {
                var mod = signature.ArgModifier;

                if ( mod == Symbols.Rest || mod == Symbols.Body || mod == Symbols.Params || mod == Symbols.Vector )
                {
                    var tail = new List<Expression>();
                    for ( int i = offset; i < input.Length; ++i )
                    {
                        tail.Add( Expression.Convert( input[ i ].Expression, elementType ) );
                    }
                    var tailExpr = Expression.NewArrayInit( elementType, tail );
                    if ( mod == Symbols.Rest || mod == Symbols.Body )
                    {
                        var conversion = Expression.Call( Runtime.AsListMethod, tailExpr );
                        output.Add( conversion );
                    }
                    else if ( mod == Symbols.Params )
                    {
                        output.Add( tailExpr );
                    }
                    else if ( mod == Symbols.Vector )
                    {
                        var conversion = Expression.Call( Runtime.AsVectorMethod, tailExpr );
                        output.Add( conversion );
                    }
                }
                else if ( mod == Symbols.Optional )
                {
                    for ( int i = offset; i < input.Length && i < signature.Parameters.Count; ++i )
                    {
                        output.Add( input[ i ].Expression );
                    }
                    for ( int i = input.Length; i < signature.Parameters.Count; ++i )
                    {
                        var expr = signature.Parameters[ i ].InitForm ?? Expression.Constant( null );
                        output.Add( expr );
                    }
                    if ( input.Length > signature.Parameters.Count )
                    {
                        throw new LispException( "Too many arguments supplied" );
                    }
                }
                else if ( mod == Symbols.Key )
                {
                    var firstKey = offset;
                    var usedKeys = 0;

                    for ( int i = firstKey; i < input.Length; i += 2 )
                    {
                        if ( !Runtime.Keywordp( input[ i ].Value ) || i + 1 == input.Length )
                        {
                            throw new LispException( "Invalid keyword/value list" );
                        }
                        var keywordRestriction = BindingRestrictions.GetExpressionRestriction( Expression.Equal( input[ i ].Expression, Expression.Constant( input[ i ].Value ) ) );
                        restrictions = restrictions.Merge( keywordRestriction );
                    }

                    for ( int i = offset; i < signature.Parameters.Count; ++i )
                    {
                        Expression val = null;

                        for ( int j = firstKey; j + 1 < input.Length; j += 2 )
                        {
                            if ( signature.Parameters[ i ].Sym.Name == ( ( Symbol ) input[ j ].Value ).Name )
                            {
                                val = input[ j + 1 ].Expression;
                                ++usedKeys;
                                break;
                            }
                        }

                        if ( val == null )
                        {
                            output.Add( signature.Parameters[ i ].InitForm ?? Expression.Constant( null ) );
                        }
                        else
                        {
                            output.Add( val );
                        }
                    }
                }
            }

            return Expression.NewArrayInit( elementType, output );
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
            // TODO: optimize lambda calls
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


  