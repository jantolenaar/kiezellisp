// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Dynamic;
using System.Text;
using System.Reflection;

namespace Kiezel
{
    internal delegate Expression CompilerHelper(Cons form, AnalysisScope scope);

    internal class SpecialForm
    {
        internal CompilerHelper Helper;

        internal SpecialForm( CompilerHelper helper )
        {
            Helper = helper;
        }

        public override string ToString()
        {
            return String.Format( "SpecialForm Name=\"{0}.{1}\"", Helper.Method.DeclaringType, Helper.Method.Name );
        }
    }


    public partial class Runtime
    {
        internal static void RestartCompiler()
        {
            Symbols.Declare.FunctionValue = new SpecialForm( CompileDeclare );
            Symbols.Def.FunctionValue = new SpecialForm( CompileDef );
            Symbols.DefConstant.FunctionValue = new SpecialForm( CompileDefConstant );
            Symbols.Setq.FunctionValue = new SpecialForm( CompileSetqVariable );
            Symbols.Lambda.FunctionValue = new SpecialForm( CompileLambda );
            Symbols.GreekLambda.FunctionValue = new SpecialForm( CompileLambda );
            Symbols.Return.FunctionValue = new SpecialForm( CompileReturn );
            Symbols.DefMacro.FunctionValue = new SpecialForm( CompileDefMacro );
            Symbols.Defun.FunctionValue = new SpecialForm( CompileDefun );
            Symbols.DefMulti.FunctionValue = new SpecialForm( CompileDefMulti );
            Symbols.DefMethod.FunctionValue = new SpecialForm( CompileDefMethod );
            Symbols.If.FunctionValue = new SpecialForm( CompileIf );
            Symbols.And.FunctionValue = new SpecialForm( CompileAnd );
            Symbols.Or.FunctionValue = new SpecialForm( CompileOr );
            Symbols.Do.FunctionValue = new SpecialForm( CompileDo );
            Symbols.MergeWithOuterDo.FunctionValue = new SpecialForm( CompileMergeWithOuterDo );
            Symbols.TagBody.FunctionValue = new SpecialForm( CompileTagBody );
            Symbols.Goto.FunctionValue = new SpecialForm( CompileGoto );
            Symbols.Quote.FunctionValue = new SpecialForm( CompileQuote );
            Symbols.bqQuote.FunctionValue = new SpecialForm( CompileQuote );
            Symbols.GetAttr.FunctionValue = new SpecialForm( CompileGetMember );
            Symbols.SetAttr.FunctionValue = new SpecialForm( CompileSetMember );
            Symbols.GetElt.FunctionValue = new SpecialForm( CompileGetElt );
            Symbols.SetElt.FunctionValue = new SpecialForm( CompileSetElt );
            Symbols.Var.FunctionValue = new SpecialForm( CompileVar );
            Symbols.HiddenVar.FunctionValue = new SpecialForm( CompileHiddenVar );
            Symbols.Throw.FunctionValue = new SpecialForm( CompileThrow );
            Symbols.TryAndCatch.FunctionValue = new SpecialForm( CompileTryAndCatch );
            Symbols.TryFinally.FunctionValue = new SpecialForm( CompileTryFinally );
        }

        internal static void CheckLength( Cons form, int length )
        {
            if ( Length( form ) != length )
            {
                throw new LispException( "{0}: expected list with length equal to {1}", form, length );
            }
        }

        internal static void CheckMinLength( Cons form, int length )
        {
            if ( Length( form ) < length )
            {
                throw new LispException( "{0}: expected list with length greater than {1}", form, length );
            }
        }

        internal static void CheckMaxLength( Cons form, int length )
        {
            if ( Length( form ) > length )
            {
                throw new LispException( "{0}: expected list with length less than {1}", form, length );
            }
        }

        internal static Delegate CompileToDelegate( Expression expr )
        {
            var lambda = expr as LambdaExpression;

            if ( lambda == null )
            {
                lambda = Expression.Lambda( expr );
            }

            if ( AdaptiveCompilation )
            {
                return Microsoft.Scripting.Generation.CompilerHelpers.LightCompile( lambda, CompilationThreshold );
            }
            else
            {
                return lambda.Compile();
            }
        }

        internal static Func<object> CompileToFunction( Expression expr )
        {
            return ( Func<object> ) CompileToDelegate( expr );
        }

        internal static Func<object, object[],object> CompileToFunction2( Expression expr, params ParameterExpression[] parameters )
        {
            return ( Func<object, object[],object> ) CompileToDelegate( expr, parameters );
        }

        internal static Delegate CompileToDelegate( Expression expr, ParameterExpression[] parameters )
        {
            var lambda = expr as LambdaExpression;

            if ( lambda == null )
            {
                lambda = Expression.Lambda( expr, parameters );
            }

            if ( AdaptiveCompilation )
            {
                return Microsoft.Scripting.Generation.CompilerHelpers.LightCompile( lambda, CompilationThreshold );
            }
            else
            {
                return lambda.Compile();
            }
        }

        internal static Func<Cons, object, object[], object> CompileToFunction3( Expression expr, params ParameterExpression[] parameters )
        {
            return ( Func<Cons, object, object[], object> ) CompileToDelegate( expr, parameters );
        }

        internal static bool TryOptimize( ref object expr )
        {
            var expr2 = Optimizer( expr );
            var optimized = expr2 != expr;
            expr = expr2;
            return optimized;
        }

        [Lisp("system.optimizer")]
        public static object Optimizer( object expr )
        {
            if ( !(expr is Cons) )
            {
                return expr;
            }
            var forms = (Cons)expr;
            var head = First( expr ) as Symbol;
            if ( head != null && head.Package == LispPackage )
            {
                var proc = head.Value as ImportedFunction;
                if ( proc != null && proc.Pure )
                {
                    if ( head == Symbols.Quote )
                    {
                        expr = Second( forms );
                    }
                    else
                    {
                        var tail = Map( Optimizer, Cdr( forms ) );
                        bool simple = Every( Literalp, tail );
                        if ( simple )
                        {
                            expr = Apply( proc, tail );
                        }
                    }
                }
            }
            return expr;
        }

        internal static Expression Compile( object expr, AnalysisScope scope )
        {
            if ( DebugMode )
            {
                var context = CurrentThreadContext;
                var saved = context.SaveStackAndFrame( null, MakeList( Symbols.Compiling, expr ) );
                var result = CompileWrapped( expr, scope );
                context.RestoreStackAndFrame( saved );
                return result;
            }
            else
            {
                var result = CompileWrapped( expr, scope );
                return result;
            }
        }

        internal static Expression CompileWrapped( object expr, AnalysisScope scope )
        {
            if ( expr is Symbol )
            {
                var sym = ( Symbol ) expr;
                // This optimization does not help much, I think.
                //if ( sym.IsDynamic || scope.HasLocalVariable( sym, int.MaxValue ) )
                //{
                //    return CompileGetVariable( sym, scope );
                //}
                //else if ( sym.IsFunction && ( sym.Value is ImportedFunction || sym.Value is ImportedConstructor ) )
                //{
                //    return Expression.Constant( sym.Value, typeof( object ) );
                //}
                //else
                {
                    return CompileGetVariable( sym, scope );
                }
            }
            else if ( expr is Cons )
            {
                var form = ( Cons ) expr;
                var head = First( form ) as Symbol;

                if ( head != null && !scope.HasLocalVariable( head, int.MaxValue ) )
                {
                    if ( OptimizerEnabled && TryOptimize( ref expr ) )
                    {
                        return Expression.Constant( expr, typeof( object ) );
                    }

                    if ( head.SpecialFormValue != null )
                    {
                        return head.SpecialFormValue.Helper( form, scope );
                    }

                    if ( Macrop( head.Value ) )
                    {
                        var expansion = MacroExpand1( form );
                        return Compile( expansion, scope );
                    }
                }

                return CompileFunctionCall( form, scope );
            }
            else
            {
                // anything else is a literal
                return CompileLiteral( expr );
            }
        }

        internal static Expression CompileLiteral( object expr )
        {
            return Expression.Constant( expr, typeof( object ) );
        }

        internal static object Execute( Expression expr )
        {
            var proc = CompileToFunction( expr );
            return proc();
        }

        internal static Cons RewriteAsBinaryExpression( object oper, Cons args )
        {
            var result = First( args );

            while ( true )
            {
                args = Cdr( args );

                if ( args == null )
                {
                    break;
                }

                result = MakeList( oper, result, First( args ) );

            }

            return (Cons) result;
        }


        internal static Expression CompileQuote( Cons form, AnalysisScope scope )
        {
            CheckMinLength( form, 2 );
            return CompileLiteral( Second( form ) );
        }

        internal static MethodInfo RuntimeMethod( string name )
        {
            var methods = typeof( Runtime ).GetMethods( BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
            return methods.First( x => x.Name == name );
        }

        internal static Expression CallRuntime( MethodInfo method, params Expression[] exprs )
        {
            return Expression.Call( method, exprs );
        }

        internal static MethodInfo SetLexicalMethod = RuntimeMethod( "SetLexical" );
        internal static MethodInfo UnwindExceptionIntoNewExceptionMethod = RuntimeMethod( "UnwindExceptionIntoNewException" );
        internal static MethodInfo SaveStackAndFrameMethod = RuntimeMethod( "SaveStackAndFrame" );
        internal static MethodInfo SaveStackAndFrameWithMethod = RuntimeMethod( "SaveStackAndFrameWith" );
        internal static MethodInfo RestoreStackAndFrameMethod = RuntimeMethod( "RestoreStackAndFrame" );
        internal static MethodInfo RestoreFrameMethod = RuntimeMethod( "RestoreFrame" );
        internal static MethodInfo ToBoolMethod = RuntimeMethod( "ToBool" );
        internal static MethodInfo LogBeginCallMethod = RuntimeMethod( "LogBeginCall" );
        internal static MethodInfo LogEndCallMethod = RuntimeMethod( "LogEndCall" );
        internal static MethodInfo DefineMethodMethod = RuntimeMethod( "DefineMethod" );
        internal static MethodInfo DefineMultiMethodMethod = RuntimeMethod( "DefineMultiMethod" );
        internal static MethodInfo GetDynamicMethod = RuntimeMethod( "GetDynamic" );
        internal static MethodInfo SetDynamicMethod = RuntimeMethod( "SetDynamic" );
        internal static MethodInfo GetLexicalMethod = RuntimeMethod( "GetLexical" );
        internal static MethodInfo DefDynamicMethod = RuntimeMethod( "DefDynamic" );
        internal static MethodInfo DefineVariableMethod = RuntimeMethod( "DefineVariable" );
        internal static MethodInfo DefineConstantMethod = RuntimeMethod( "DefineConstant" );
        internal static MethodInfo DefineFunctionMethod = RuntimeMethod( "DefineFunction" );
        internal static MethodInfo NullOperationMethod = RuntimeMethod( "NullOperation" );
        internal static MethodInfo CastMethod = typeof( System.Linq.Enumerable ).GetMethod( "Cast" );
        internal static MethodInfo AddEventHandlerMethod = RuntimeMethod( "AddEventHandler" );
        internal static MethodInfo ConvertToEnumTypeMethod = RuntimeMethod( "ConvertToEnumType" );
        internal static MethodInfo AsListMethod = RuntimeMethod( "AsList" );
        internal static MethodInfo AsVectorMethod = RuntimeMethod( "AsVector" );
        internal static MethodInfo EqualMethod = RuntimeMethod( "Equal" );
        internal static MethodInfo IsInstanceOfMethod = RuntimeMethod( "IsInstanceOf" );
        internal static MethodInfo NotMethod = RuntimeMethod( "Not" );
        internal static Type GenericListType = GetTypeForImport( "System.Collections.Generic.List`1", null );

        internal static object NullOperation( object a )
        {
            return null;
        }

        internal static Expression WrapEvaluationStack( Expression code, Frame frame = null, Cons form = null )
        {
            // For function, try and loop: anything that has a non-local exit method.
            var saved = Expression.Parameter( typeof( ThreadContextState ), "saved" );
            var result = Expression.Parameter( typeof( object ), "result" );
            var index = Expression.Parameter( typeof( int ), "index" );
            var exprs = new List<Expression>();

            exprs.Add( Expression.Assign( saved, CallRuntime( SaveStackAndFrameWithMethod,
                                                                  Expression.Constant( frame, typeof( Frame ) ),
                                                                  Expression.Constant( form, typeof( Cons ) ) ) ) );

            if ( form != null )
            {
                //exprs.Add( Expression.Assign( index, CallRuntime( LogBeginCallMethod, Expression.Constant( form ) ) ) );
            }

            exprs.Add( Expression.Assign( result, code ) );

            if ( form != null )
            {
                //exprs.Add( CallRuntime( LogEndCallMethod, index ) );
            }

            exprs.Add( CallRuntime( RestoreStackAndFrameMethod, saved ) );
            exprs.Add( result );

            return Expression.Block
                    (
                        typeof( object ),
                        new[] { saved, result, index },
                        exprs
                    );
        }

        internal static Expression CompileDynamicExpression( System.Runtime.CompilerServices.CallSiteBinder binder, Type returnType, IEnumerable<Expression> args )
        {
            if ( AdaptiveCompilation )
            {
                return Microsoft.Scripting.Ast.Utils.LightDynamic( binder, returnType, new System.Runtime.CompilerServices.ReadOnlyCollectionBuilder<Expression>( args ) );
            }
            else
            {
                return Expression.Dynamic( binder, returnType, args );
            }
        }

        internal static Expression CompileFunctionCall( Cons form, AnalysisScope scope )
        {
            CheckMinLength( form, 1 );
            var formFunc = First( form );
            var formArgs = Cdr( form );
            var member = formFunc as Cons;

            if ( member != null && First( member ) == Symbols.Accessor && Second(member) is string )
            {
                var name = (string) Second( member );

                if ( name != "" )
                {
                    List<Expression> args = new List<Expression>();
                    foreach ( object a in ToIter( formArgs ) )
                    {
                        args.Add( Compile( a, scope ) );
                    }

                    var binder = GetInvokeMemberBinder( new InvokeMemberBinderKey( name, Length( formArgs ) ) );
                    var code = CompileDynamicExpression( binder, typeof( object ), args );
                    if ( !DebugMode )
                    {
                        return code;
                    }
                    else
                    {
                        return WrapEvaluationStack( code, null, form );
                    }
                }

                // fall thru
            }

            // other cases and cases that haven't returned yet.
            {
                var func = Compile( formFunc, scope );
                List<Expression> args = new List<Expression>();
                args.Add( func );
                foreach ( object a in ToIter( formArgs ) )
                {
                    args.Add( Compile( a, scope ) );
                }
                var binder = GetInvokeBinder( Length( formArgs ) );
                var code = CompileDynamicExpression( binder, typeof( object ), args );
                if ( !DebugMode )
                {
                    return code;
                }
                else
                {
                    return WrapEvaluationStack( code, null, form );
                }
            }
        }


        internal static Expression CompileGetMember( Cons form, AnalysisScope scope )
        {
            // (attr target property)
            CheckLength( form, 3 );
            var member = Third( form );

            if ( member is string || Keywordp( member ) )
            {
                var name = GetDesignatedString( member );
                var target = Compile( Second( form ), scope );
                var binder = GetGetMemberBinder( name );
                return Expression.Dynamic( binder, typeof( object ), new Expression[] { target } );
            }
            else if ( member is Cons && First( member ) == Symbols.Quote && Second( member ) is Symbol )
            {
                var name = ( ( Symbol ) Second( member ) ).Name;
                var target = Compile( Second( form ), scope );
                var binder = GetGetMemberBinder( name );
                return Expression.Dynamic( binder, typeof( object ), new Expression[] { target } );
            }
            else
            {
                // not a constant name
                var newform = MakeCons( Symbols.GetAttrFunc, Cdr( form ) );
                return CompileFunctionCall( newform, scope );
            }
        }


        internal static Expression CompileSetMember( Cons form,  AnalysisScope scope )
        {
            // (set-attr target property value)
            CheckLength( form, 4 );
            var member = Third( form );

            if ( member is string || Keywordp( member ) )
            {
                var name = GetDesignatedString( member );
                var target = Compile( Second( form ), scope );
                var value = Compile( Fourth( form ), scope );
                var binder = GetSetMemberBinder( name );
                return CompileDynamicExpression( binder, typeof( object ), new Expression[] { target, value } );
            }
            else if ( member is Cons && First( member ) == Symbols.Quote && Second( member ) is Symbol )
            {
                var name = ( ( Symbol ) Second( member ) ).Name;
                var target = Compile( Second( form ), scope );
                var value = Compile( Fourth( form ), scope );
                var binder = GetSetMemberBinder( name );
                return CompileDynamicExpression( binder, typeof( object ), new Expression[] { target, value } );
            }
            else
            {
                // not a constant name
                return CompileFunctionCall( MakeCons( Symbols.SetAttrFunc, Cdr( form ) ), scope );
            }
        }

        internal static Expression CompileGetElt( Cons form,  AnalysisScope scope )
        {
            // (elt target indexes)
            CheckMinLength( form, 3 );
            var args = new List<Expression>( ConvertToEnumerableObject( form.Cdr ).Select( x => Compile( x, scope ) ) );
            var binder = GetGetIndexBinder( args.Count - 1 );
            return CompileDynamicExpression( binder, typeof( object ), args );
        }

        internal static Expression CompileSetElt( Cons form,  AnalysisScope scope )
        {
            // (set-elt target indexes value)
            CheckMinLength( form, 4 );
            var args = new List<Expression>( ConvertToEnumerableObject( form.Cdr ).Select( x => Compile( x, scope ) ) );
            var binder = GetSetIndexBinder( args.Count - 1  );
            return CompileDynamicExpression( binder, typeof( object ), args );
        }

        internal static LambdaSignature CompileFormalArgs( Cons args, AnalysisScope scope, LambdaKind kind )
        {
            var signature = new LambdaSignature( kind );
            signature.ArgModifier = null;

            foreach ( object item in ToIter( args ) )
            {
                if ( item is Symbol )
                {
                    var sym = ( Symbol ) item;

                    if ( sym == Symbols.Optional || sym == Symbols.Key || sym == Symbols.Rest || sym == Symbols.Body || sym == Symbols.Params || sym == Symbols.Vector )
                    {
                        if ( signature.ArgModifier != null )
                        {
                            throw new LispException( "Only one modifier can be used: &key, &optional, &rest, &body, &vector or &params" );
                        }
                        signature.ArgModifier = sym;
                        signature.RequiredArgsCount = signature.Parameters.Count;
                        continue;
                    }
                    else
                    {
                        ParameterDef arg = new ParameterDef( sym );
                        signature.Parameters.Add( arg );
                        signature.Names.Add( sym );
                    }
                }
                else if ( item is Cons )
                {
                    var list = ( Cons ) item;

                    if ( signature.ArgModifier == Symbols.Key || signature.ArgModifier == Symbols.Optional )
                    {
                        Symbol sym = ( Symbol ) First( list );
                        var initForm = Second( list );
                        if ( initForm == null )
                        {
                            signature.Parameters.Add( new ParameterDef( sym ) );
                            signature.Names.Add( sym );
                        }
                        else
                        {
                            if ( initForm is Vector || initForm is Prototype || ( initForm is Cons && Runtime.First( initForm ) == Symbols.Quote ) )
                            {
                                Runtime.PrintWarning( "Bad style: using literals of type vector, prototype or list as default value." );
                            }
                            var initForm2 = Compile( initForm, scope );
                            signature.Parameters.Add( new ParameterDef( sym, initForm: initForm2 ) );
                            signature.Names.Add( sym );
                        }
                    }
                    else if ( signature.ArgModifier == null && kind == LambdaKind.Macro )
                    {
                        var nestedArgs = CompileFormalArgs( list, scope, kind );
                        ParameterDef arg = new ParameterDef( null, nestedParameters: nestedArgs );
                        signature.Parameters.Add( arg );
                        signature.Names.AddRange( nestedArgs.Names );
                    }
                    else if ( signature.ArgModifier == null && kind == LambdaKind.Method )
                    {
                        var sym = ( Symbol ) First( list );
                        var type = Second( list );
                        if ( type == null )
                        {
                            ParameterDef arg = new ParameterDef( sym );
                            signature.Parameters.Add( arg );
                            signature.Names.Add( sym );
                        }
                        else if ( type is Cons && First( type ) == MakeSymbol( "eql", LispPackage ) )
                        {
                            var expr = Compile( Second( type ), scope );
                            ParameterDef arg = new ParameterDef( sym, specializer: new EqlSpecializer( expr ));
                            signature.Parameters.Add( arg );
                            signature.Names.Add( sym );
                        }
                        else
                        {
                            if ( !Symbolp( type ) || Keywordp( type ) )
                            {
                                throw new LispException( "Invalid type specifier: {0}", type );
                            }
                            ParameterDef arg = new ParameterDef( sym, specializer: type );
                            signature.Parameters.Add( arg );
                            signature.Names.Add( sym );
                        }
                    }
                    else
                    {
                        throw new LispException( "Invalid CONS in lambda parameter list" );
                    }
                }
            }

            if ( signature.ArgModifier == null )
            {
                signature.RequiredArgsCount = signature.Parameters.Count;
            }

            if ( kind == LambdaKind.Function )
            {
                var sym = signature.ArgModifier;
                if ( sym == Symbols.Rest || sym == Symbols.Body || sym == Symbols.Params || sym == Symbols.Vector )
                {
                    if ( signature.RequiredArgsCount + 1 != signature.Parameters.Count )
                    {
                        throw new LispException( "Invalid placement of &rest or similar modifier." );
                    }
                }
            }

            return signature;
        }

        internal static Expression CompileDefMacro( Cons form,  AnalysisScope scope )
        {
            // defmacro name args body
            CheckMinLength( form, 3 );
            var sym = CheckSymbol( Second( form ) );
            if ( sym.IsDynamic )
            {
                throw new LispException( "Invalid macro name: {0}", sym );
            }
            string doc;
            var lambda = CompileLambdaDef( sym, Cddr( form ), scope, LambdaKind.Macro, out doc );
            return Expression.Call( DefineFunctionMethod, Expression.Constant( sym ), lambda, Expression.Constant( doc, typeof(string) ) );
        }


        internal static Expression CompileDefun( Cons form, AnalysisScope scope )
        {
            // defun name args body
            CheckMinLength( form, 3 );
            var sym = CheckSymbol( Second( form ) );
            if ( sym.IsDynamic )
            {
                throw new LispException( "Invalid function name: {0}", sym );
            }
            string doc;
            var lambda = CompileLambdaDef( sym, Cddr( form ), scope, LambdaKind.Function, out doc );
            return Expression.Call( DefineFunctionMethod, Expression.Constant( sym ), lambda, Expression.Constant( doc, typeof( string ) ) );
        }

        internal static Expression CompileDefMethod( Cons form, AnalysisScope scope )
        {
            // defmethod name args body
            CheckMinLength( form, 3 );
            var sym = CheckSymbol( Second( form ) );
            if ( sym.IsDynamic )
            {
                throw new LispException( "Invalid method name: {0}", sym );
            }
            string doc;
            var lambda = CompileLambdaDef( sym, Cddr( form ), scope, LambdaKind.Method, out doc );
            return CallRuntime( DefineMethodMethod, Expression.Constant(sym), lambda );
        }

        internal static Expression CompileDefMulti( Cons form, AnalysisScope scope )
        {
            // defmulti name args [doc] body
            CheckMinLength( form, 3 );
            var sym = CheckSymbol( Second( form ) );
            if ( sym.IsDynamic )
            {
                throw new LispException( "Invalid method name: {0}", sym );
            }
            var args = ( Cons ) Third( form );
            var body = Cdr( Cddr ( form ) );
            var syntax = MakeListStar( sym, args );
            var lispParams = CompileFormalArgs( args, new AnalysisScope(), LambdaKind.Function );
            string doc = "";
            if ( Length( body ) >= 1 && body.Car is string )
            {
                doc = ( string ) body.Car;
            }
            return CallRuntime( DefineMultiMethodMethod, Expression.Constant( sym ), Expression.Constant( lispParams ), Expression.Constant( doc, typeof( string ) ) );
        }

        internal static Expression CompileReturn( Cons form,  AnalysisScope scope )
        {
            // return [expr]
            CheckMaxLength( form, 2 );
            var funscope = FindFirstLambda( scope );

            if ( funscope == null )
            {
                throw new LispException( "Invalid use of RETURN." );
            }

            funscope.UsesReturn = true;

            Expression value;

            if ( funscope.IsFileScope )
            {
                value = Expression.Constant( new ReturnFromLoadException() );
                return Expression.Goto( funscope.ReturnLabel, value, typeof( object ) );
            }
            else if ( Second( form ) == null )
            {
                value = Expression.Constant( null, typeof( object ) );
                return Expression.Return( funscope.ReturnLabel, value, typeof( object ) );
            }
            else
            {
                value = Compile( Second( form ), scope );
                return Expression.Return( funscope.ReturnLabel, value, typeof( object ) );
            }

        }


        internal static Expression CompileLambda( Cons form, AnalysisScope scope )
        {
            // lambda args body
            CheckMinLength( form, 2 );

            string doc;
            return CompileLambdaDef( null, Cdr( form ), scope, LambdaKind.Function, out doc );
        }

        internal static Expression CompileLambdaDef( Symbol name, Cons forms, AnalysisScope scope, LambdaKind kind, out string doc )
        {
            CheckMinLength( forms, 0 );

            if ( Length( forms ) == 1 )
            {
                PrintWarning( "function body contains no forms." );
            }

            var maybeArgs = First(forms);
            var args = Listp( maybeArgs ) ? ( Cons ) maybeArgs : MakeList( maybeArgs );
            var body = Cdr( forms );

            var funscope = new AnalysisScope( scope, name == null ? null : name.Name );
            funscope.IsLambda = true;
            funscope.ReturnLabel = Expression.Label( typeof( object ), "return-label" );

            var template = new Lambda();

            template.Name = name;
            template.Signature = CompileFormalArgs( args, scope, kind );

            if ( kind == LambdaKind.Method )
            {
                var container = name.Value as MultiMethod;
                if ( container != null )
                {
                    var m = template.Signature;
                    var g = container.Signature;
                    var m1 = m.RequiredArgsCount;
                    var m2 = m.Parameters.Count;
                    var m3 = m.ArgModifier;
                    var g1 = g.RequiredArgsCount;
                    var g2 = g.Parameters.Count;
                    var g3 = g.ArgModifier;

                    if ( m1 != g1 )
                    {
                        throw new LispException( "Method does not match multi-method: number of required arguments" );
                    }
                    if ( m3 != g3 )
                    {
                        throw new LispException( "Method does not match multi-method: different argument modifiers" );
                    }
                    if ( g3 != Symbols.Key && m2 != g2 )
                    {
                        throw new LispException( "Method does not match multi-method: number of arguments" );
                    }
                    if ( g3 == Symbols.Key )
                    {
                        // Replace keyword parameters with the full list from the generic definition, but keep the defaults
                        var usedKeys = template.Signature.Parameters.GetRange( m1, m2 - m1 );
                        var replacementKeys = container.Signature.Parameters.GetRange( g1, g2 - g1 );
                        template.Signature.Parameters.RemoveRange( m1, m2 - m1 );
                        template.Signature.Names.RemoveRange( m1, m2 - m1 );
                        foreach ( var par in replacementKeys )
                        {
                            var oldpar = usedKeys.FirstOrDefault( x => x.Sym == par.Sym );
                            if ( oldpar != null )
                            {
                                var newpar = new ParameterDef( par.Sym, initForm: oldpar.InitForm );
                                template.Signature.Parameters.Add( newpar );
                                template.Signature.Names.Add( par.Sym );
                            }
                            else
                            {
                                // Insert place holder 
                                var newpar = new ParameterDef( par.Sym, hidden: true );
                                template.Signature.Parameters.Add( newpar );
                                template.Signature.Names.Add( newpar.Sym );
                            }
                        }
                    }
                }
            }
            if ( name != null )
            {
                template.Syntax = MakeListStar( template.Name, args );
            }

#if !DEBUG
            template.Source = MakeListStar( Symbols.Lambda, args, body );
#endif
            doc = "";
            if ( Length( body ) > 1 && body.Car is string )
            {
                doc = ( string ) body.Car;
            }

            if ( args != null )
            {
                var binding = GetLambdaParameterBindingCode( template.Signature );
                var body2 = MakeList( MakeCons( Symbols.Do, body ) );
                body = AsList( Append( binding, body2 ) );
            }
#if DEBUG
            template.Source = MakeListStar( Symbols.Lambda, MakeList( Symbols.Params, Symbols.Args ), body );
#endif
            var lambdaListNative = funscope.DefineNativeLocal( Symbols.LambdaList, ScopeFlags.All );
            var recurNative = funscope.DefineNativeLocal( Symbols.Recur, ScopeFlags.All );
            var argsNative = funscope.DefineNativeLocal( Symbols.Args, ScopeFlags.All );

            Expression code = CompileBody( body, funscope );

            if ( funscope.UsesReturn )
            {
                var temp = Expression.Parameter( typeof( object ), "temp" );

                code = Expression.Block( typeof( object ),
                                                    new ParameterExpression[] { temp },
                                                    Expression.Assign( temp, code ),
                                                    Expression.Label( funscope.ReturnLabel, temp ) );

                if ( funscope.UsesFramedVariables || funscope.UsesDynamicVariables )
                {
                    // Without RETURN, every DO block cleans up after itself.
                    code = WrapEvaluationStack( code, null, null );
                }
            }

            template.Proc = CompileToFunction3( code, lambdaListNative, recurNative, argsNative );

            return Expression.New( typeof( Lambda ).GetConstructor( new Type[] { typeof( Lambda ) } ), Expression.Constant( template, typeof( Lambda ) ) );

        }


        internal static Cons GetLambdaParameterBindingCode( LambdaSignature signature )
        {
            var temp = GenTemp( "temp" );
            var code = new Vector();
            for ( int i = 0; i < signature.Names.Count; ++i )
            {
                if ( signature.Names[ i ] != Symbols.Underscore )
                {
                    if ( i >= signature.Parameters.Count || !signature.Parameters[ i ].Hidden )
                    {
                        code.Add( MakeList( Symbols.Var, signature.Names[ i ], MakeList( Symbols.GetElt, Symbols.Args, i ) ) );
                    }
                }
            }
            return AsList( code );
        }

        internal static Cons GetMultipleVarBindingCode( Vector names, object expr )
        {
            var temp = GenTemp( "temp" );
            var code = new Vector();
            code.Add( Symbols.MergeWithOuterDo );
            code.Add( MakeList( Symbols.HiddenVar, temp, MakeList( Symbols.AsTuple, expr, names.Count ) ) );

            for ( int i = 0; i < names.Count; ++i )
            {
                if ( names[ i ] != Symbols.Underscore )
                {
                    code.Add( MakeList( Symbols.Var, names[ i ], MakeList( Symbols.GetElt, temp, i ) ) );
                }
            }

            return AsList( code );

        }

        internal static Expression CompileTopLevelExpressionFromLoadFile( object expr, AnalysisScope scope )
        {
            var code = Compile( expr, scope );

            if ( code == null )
            {
                return null;
            }

            if ( scope.UsesReturn )
            {
                var temp = Expression.Parameter( typeof( object ), "temp" );
                code = Expression.Block( typeof( object ), new ParameterExpression[] { temp },
                                                           Expression.Assign( temp, RuntimeHelpers.EnsureObjectResult( code ) ),
                                                           Expression.Label( scope.ReturnLabel, temp ) );
            }

            return code;
        }

        internal static Expression CompileGetVariable( Symbol sym, AnalysisScope scope )
        {
 
            if ( sym.IsDynamic )
            {
                return CallRuntime( GetDynamicMethod, Expression.Constant( sym ) );
            }
            else
            {
                int index;
                int depth;
                ParameterExpression parameter;

                if ( scope.FindLocal( sym, ScopeFlags.Referenced, out depth, out index, out parameter ) )
                {
                    if ( parameter != null )
                    {
                        return parameter;
                    }
                    else
                    {
                        return CallRuntime( GetLexicalMethod, Expression.Constant( depth ), Expression.Constant( index ) );
                    }
                }
                else
                {
                    if ( ToBool( GetDynamic( Symbols.Strict ) ) )
                    {
                        if ( sym.IsUndefined )
                        {
                            PrintWarning( "Symbol ", sym, " is not yet defined" );
                        }
                    }
                    return Expression.PropertyOrField( Expression.Constant( sym ), "CheckedValue" );
                }
            }
        }

        internal static Expression CompileSetqVariable( Cons form, AnalysisScope scope )
        {
            // setq sym expr
            // setq (sym...) expr
            CheckLength( form, 3 );

            if ( Second( form ) is Cons )
            {
                return CompileMultipleSetqVariable( form, scope );
            }

            var sym = CheckSymbol( Second( form ) );
            var value = Compile( Third( form ), scope );

            if ( sym.IsDynamic )
            {
                if ( scope.Parent == null )
                {
                    return Expression.Assign( Expression.PropertyOrField( Expression.Constant( sym ), "LessCheckedValue" ), value );
                }
                else
                {
                    return CallRuntime( SetDynamicMethod, Expression.Constant( sym ), value );
                }
            }
            else
            {
                int depth;
                int index;
                ParameterExpression parameter;

                if ( scope.FindLocal( sym, ScopeFlags.Assigned, out depth, out index, out parameter ) )
                {
                    if ( parameter != null )
                    {
                        return Expression.Assign( parameter, value );
                    }
                    else
                    {
                        return CallRuntime( SetLexicalMethod, Expression.Constant( depth ), Expression.Constant( index ), value );
                    }
                }
                else if ( scope.Parent == null )
                {
                    return Expression.Assign( Expression.PropertyOrField( Expression.Constant( sym ), "LessCheckedValue" ), value );
                }
                else
                {
                    if ( ToBool( GetDynamic( Symbols.Strict ) ) )
                    {
                        if ( sym.IsUndefined )
                        {
                            PrintWarning( "Symbol ", sym, " is not yet defined" );
                        }
                    }
                    return Expression.Assign( Expression.PropertyOrField( Expression.Constant( sym ), "CheckedValue" ), value );
                }
            }
        }


        internal static Expression CompileMultipleSetqVariable( Cons form,  AnalysisScope scope )
        {
            // multiple-setq (sym...) expr
            CheckLength( form, 3 );
            var temp1 = GenTemp( "temp" );
            var temp2 = GenTemp( "temp" );
            var symbols = AsVector( ( Cons ) Second( form ) );
            var code = new Vector();
            code.Add( Symbols.Do );
            code.Add( MakeList( Symbols.Var, temp1, Third( form ) ) );
            code.Add( MakeList( Symbols.Var, temp2, MakeList( Symbols.AsTuple, temp1, symbols.Count ) ) );
            for (int i = 0; i < symbols.Count; ++i)
            {
                code.Add( MakeList( Symbols.Setq, symbols[ i ], MakeList( Symbols.GetElt, temp2, i ) ) );
            }
            code.Add( temp1 );
            return Compile( AsList( code ), scope );
        }

        internal static Expression CompileDef( Cons form,  AnalysisScope scope )
        {
            // def sym expr [doc]
            CheckMinLength( form, 3 );
            CheckMaxLength( form, 4 );
            var sym = CheckSymbol( Second( form ) );
            var value = Compile( Third( form ), scope );
            var doc = ( string ) Fourth( form );
            return Expression.Call( DefineVariableMethod, Expression.Constant( sym ), value, Expression.Constant( doc, typeof(string) ) );
        }

        internal static Expression CompileDefConstant( Cons form, AnalysisScope scope )
        {
            // defconstant sym expr [doc]
            CheckMinLength( form, 3 );
            CheckMaxLength( form, 4 );
            var sym = CheckSymbol( Second( form ) );
            var value = Compile( Third( form ), scope );
            var doc = ( string ) Fourth( form );
            return Expression.Call( DefineConstantMethod, Expression.Constant( sym ), value, Expression.Constant( doc, typeof( string ) ) );
        }

        internal static Expression CompileIf( Cons form,  AnalysisScope scope )
        {
            // if expr expr [expr]
            CheckMinLength( form, 3 );
            CheckMaxLength( form, 4 );
            var testExpr = Compile( Second( form ), scope );
            var thenExpr = Compile( Third( form ), scope );
            var elseExpr = Compile( Fourth( form ), scope );
            return Expression.Condition( WrapBooleanTest( testExpr ), thenExpr, elseExpr );
        }

        internal static Expression CompileAnd( Cons form, AnalysisScope scope )
        {
            // AND forms
            return CompileAndExpression( Cdr( form ), scope );
        }

        internal static Expression CompileAndExpression( Cons forms, AnalysisScope scope )
        {
            if ( forms == null )
            {
                return CompileLiteral( true );
            }
            else if ( Cdr( forms ) == null )
            {
                return Compile( First( forms ), scope );
            }
            else
            {
                return Expression.Condition( WrapBooleanTest( Compile( First( forms ), scope ) ),
                                             CompileAndExpression( Cdr( forms ), scope ),
                                             CompileLiteral( null ) );
            }
        }

        internal static Expression CompileOr( Cons form, AnalysisScope scope )
        {
            // OR forms
            return CompileOrExpression( Cdr( form ), scope );
        }

        internal static Expression CompileOrExpression( Cons forms, AnalysisScope scope )
        {
            if ( forms == null )
            {
                return CompileLiteral( false );
            }
            else if ( Cdr( forms ) == null )
            {
                return Compile( First( forms ), scope );
            }
            else
            {
                var expr1 = Compile( First( forms ), scope );
                var expr2 = CompileOrExpression( Cdr( forms ), scope );
                var temp = Expression.Variable( typeof( object ), "temp" );
                var tempAssign = Expression.Assign( temp, expr1 );
                var result = Expression.Condition( WrapBooleanTest( temp ), temp, expr2 );

                return Expression.Block( typeof( object ), new ParameterExpression[] { temp }, tempAssign, result );
            }
        }

        internal static Expression CompileDo( Cons form,  AnalysisScope scope )
        {
            return CompileBody( Cdr(form), scope );
        }

        internal static Expression CompileMergeWithOuterDo( Cons forms, AnalysisScope scope )
        {
            if ( !scope.IsBlockScope )
            {
                throw new LispException( "Statement requires block scope: {0}", forms );
            }
            var expressions = CompileBodyExpressions( forms, scope );
            Expression block = Expression.Block( typeof( object ), expressions );
            return block;
        }

        internal static Expression CompileBody( Cons forms, AnalysisScope scope )
        {
            if ( forms == null )
            {
                return CompileLiteral( null );
            }

            var bodyScope = new AnalysisScope( scope, "body" );
            bodyScope.IsBlockScope = true;
            bodyScope.Variables = new List<ScopeEntry>();
            if ( !DebugMode )
            {
                bodyScope.FreeVariables = new HashSet<Symbol>();
            }
            else
            {
                bodyScope.Names = new List<Symbol>();
            }

            var bodyExprs = CompileBodyExpressions( forms, bodyScope );

            if ( bodyScope.Variables.Count == 0 && !bodyScope.UsesTilde )
            {
                if ( bodyExprs.Count == 0 )
                {
                    return CompileLiteral( null );
                }

                if ( bodyExprs.Count == 1 )
                {
                    return Compile( First( forms ), scope );
                }
            }

            bool recompile = false;

            if ( bodyScope.UsesTilde )
            {
                recompile = true;
            }

            if ( !DebugMode && bodyScope.FreeVariables.Count != 0 )
            {
                recompile = true;
            }

            if ( recompile )
            {
                bodyScope.Variables = new List<ScopeEntry>();

                if ( !DebugMode )
                {
                    if ( bodyScope.FreeVariables.Count != 0 )
                    {
                        // Recompile with the free variables moved from native to frame
                        bodyScope.Names = new List<Symbol>();
                    }

                    if ( bodyScope.UsesTilde )
                    {
                        bodyScope.DefineNativeLocal( Symbols.Tilde, ScopeFlags.All );
                    }
                }
                else
                {
                    bodyScope.Names = new List<Symbol>();
                    bodyScope.DefineFrameLocal( Symbols.Tilde, ScopeFlags.All );
                }

                bodyExprs = CompileBodyExpressions( forms, bodyScope );
            }

            Expression block = Expression.Block( typeof( object ), bodyScope.Parameters, bodyExprs );

            if ( bodyScope.Names != null || bodyScope.UsesFramedVariables )
            {
                var lambda = FindFirstLambda( bodyScope );
                if ( lambda != null )
                {
                    lambda.UsesFramedVariables = true;
                }
                block = WrapEvaluationStack( block, new Frame( bodyScope.Names ) );
            }
            else if ( bodyScope.UsesDynamicVariables )
            {
                var lambda = FindFirstLambda( bodyScope );
                if ( lambda != null )
                {
                    lambda.UsesDynamicVariables = true;
                }
                block = WrapEvaluationStack( block, null );
            }

            if ( DebugMode )
            {
                bodyScope.CheckVariables();
            }

            return block;

        }

        internal static List<Expression> CompileBodyExpressions( Cons forms, AnalysisScope bodyScope )
        {
            var bodyExprs = new List<Expression>();
            var usesTilde = bodyScope.UsesTilde;
            var countTilde = 0;

            for ( var list = forms; list != null; list = list.Cdr )
            {
                if ( bodyScope.UsesTilde )
                {
                    ++countTilde;
                    var tilde = MakeSymbol( String.Format( "~{0}", countTilde ), LispPackage );
                    var value = CompileVar( MakeList( Symbols.Var, tilde, list.Car ), bodyScope );
                    bodyExprs.Add( value );
                }
                else
                {
                    var value = Compile( list.Car, bodyScope );
                    bodyExprs.Add( value );
                }
            }

            return bodyExprs;

        }


        internal static Expression CompileGoto( Cons form, AnalysisScope scope )
        {
            // goto symbol
            CheckLength( form, 2 );
            var tag = CheckSymbol( Second(form) );
            var label = FindTagbodyLabel( scope, tag );
            if ( label == null )
            {
                throw new LispException( "Label {0} not found", tag );
            }

            var code = Expression.Goto( label );
            return RuntimeHelpers.EnsureObjectResult( code );
        }

        internal static Expression CompileTagBody( Cons form, AnalysisScope scope )
        {
            // tagbody (tag|statement)*
            var tagScope = new AnalysisScope( scope, "tagbody" );
            tagScope.IsTagBodyScope = true;

            var saved = Expression.Parameter( typeof( ThreadContextState ), "saved" );
            var result = Expression.Parameter( typeof( object ), "result" );
            var exprs = new List<Expression>();

            exprs.Add( Expression.Assign( saved, CallRuntime( SaveStackAndFrameMethod ) ) );

            foreach ( var stmt in Cdr( form ) )
            {
                if ( stmt is Symbol )
                {
                    var label = Expression.Label( ( ( Symbol ) stmt ).Name );
                    tagScope.Tags.Add( label );
                }
            }

            foreach ( var stmt in Cdr( form ) )
            {
                if ( stmt is Symbol )
                {
                    var label = FindTagbodyLabel( tagScope, ( Symbol ) stmt );
                    exprs.Add( Expression.Label( label ) );
                    exprs.Add( CallRuntime( RestoreStackAndFrameMethod, saved ) );
                }
                else
                {
                    exprs.Add( Compile( stmt, tagScope ) );
                }
            }

            return Expression.Block
                    (
                        typeof( object ),
                        new ParameterExpression[] { saved, result },
                        exprs
                    );

        }

        internal static Expression CompileVar( Cons form, AnalysisScope scope )
        {
            if ( Listp( Second( form ) ) )
            {
                var names = AsVector( ( Cons ) Second( form ) );
                var expr = Third( form );
                var code = GetMultipleVarBindingCode( names, expr );
                return Compile( code, scope );
            }
            else
            {
                return CompileVarInScope( form, scope, false );
            }
        }

        internal static Expression CompileHiddenVar( Cons form, AnalysisScope scope )
        {
            return CompileVarInScope( form, scope, true );
        }

        internal static Expression CompileDeclare( Cons form, AnalysisScope scope )
        {
            CheckLength( form, 2 );
            if ( !scope.IsBlockScope )
            {
                throw new LispException( "Statement requires block scope: {0}", form );
            }
            foreach ( Cons declaration in Cdr( form ) )
            {
                var declare = ( Symbol ) First( declaration );
                switch ( declare.Name )
                {
                    case "ignore":
                    {
                        foreach ( Symbol sym in Cdr( declaration ) )
                        {
                            scope.FindLocal( sym, ScopeFlags.Ignore );                      
                        }
                        break;
                    }
                    case "ignorable":
                    {
                        foreach ( Symbol sym in Cdr( declaration ) )
                        {
                            scope.FindLocal( sym, ScopeFlags.Ignorable );
                        }
                        break;
                    }
                    default:
                    {
                        break;
                    }
                }
            }
            return Expression.Constant( null, typeof( object ) );
        }


        internal static Expression CompileVarInScope( Cons form, AnalysisScope scope, bool native )
        {
            // var sym [expr]
            CheckMinLength( form, 2 );
            CheckMaxLength( form, 3 );
            if ( !scope.IsBlockScope )
            {
                throw new LispException( "Statement requires block scope: {0}", form );
            }
            var sym = CheckSymbol( Second( form ) );
            if ( sym == Symbols.Tilde )
            {
                throw new LispException( "\"~\" is a reserved symbol name" );
            }
            if ( scope.HasLocalVariable( sym, 0 ) )
            {
                throw new LispException( "Duplicate declaration of variable: {0}", sym );
            }

            // Initializer must be compiled before adding the variable
            // since it may already exist. Works like nested LET forms.
            var flags = Length( form ) == 2 ? 0 : ScopeFlags.Initialized;
            var val = Length( form ) == 2 ? CompileLiteral( null ) : Compile( Third( form ), scope );

            if ( sym.IsDynamic )
            {
                scope.UsesDynamicVariables = true;
                return CallRuntime( DefDynamicMethod, Expression.Constant( sym ), val );
            }
            else if ( native )
            {
                var parameter = scope.DefineNativeLocal( sym, flags );
                return Expression.Assign( parameter, val );
            }
            else if ( !DebugMode && scope.FreeVariables != null && !scope.FreeVariables.Contains( sym ) )
            {
                var parameter = scope.DefineNativeLocal( sym, flags );
                return Expression.Assign( parameter, val );
            }
            else
            {
                int index = scope.DefineFrameLocal( sym, flags );
                return CallRuntime( SetLexicalMethod, Expression.Constant( 0 ), Expression.Constant( index ), val );
            }
        }


        internal static Expression CompileThrow( Cons form, AnalysisScope scope )
        {
            CheckMaxLength( form, 2 );
            return Expression.Block( typeof( object ), Expression.Throw( Compile( Second( form ), scope ) ), CompileLiteral( null ) );
        }

        internal static bool EnableCatch = true;

        internal static Expression CompileTryAndCatch( Cons form, AnalysisScope scope )
        {
            // 'not catching an exception' is to the REPL like 'catching it when it is thrown' is to the VS debugger.
            // It makes the exception visible to the programmer.
            CheckMinLength( form, 2 );
            var tryExpr = CompileBody( Cdr( form ), scope );

            if ( EnableCatch )
            {
                var result = Expression.Parameter( typeof( object ), "result" );
                var exception = Expression.Parameter( typeof( Exception ), "exception" );
                return WrapEvaluationStack(
                    Expression.Block( new ParameterExpression[] { result, exception },
                    Expression.TryCatch( Expression.Assign( result, tryExpr ),
                                         Expression.Catch( exception,
                                            Expression.Assign( result, CallRuntime( UnwindExceptionIntoNewExceptionMethod, exception ) ) ) ) ) );
            }
            else
            {
                return tryExpr;
            }
        }

        internal static Expression CompileTryFinally( Cons form, AnalysisScope scope )
        {
            CheckMinLength( form, 2 );
            var tryExpr = Compile( Second( form ), scope );

            if ( Length( form ) == 2 )
            {
                return tryExpr;
            }

            var finallyExpr = CompileBody( Cddr( form ), scope );

            var saved = Expression.Parameter( typeof( ThreadContextState ), "saved" );
            var saved2 = Expression.Parameter( typeof( ThreadContextState ), "saved2" );

            return Expression.Block
                    (
                        typeof( object ),
                        new ParameterExpression[] { saved },
                        Expression.Assign( saved, CallRuntime( SaveStackAndFrameMethod ) ),
                        Expression.TryFinally
                        (
                            tryExpr,
                            Expression.Block
                            (
                                typeof(object),
                                new ParameterExpression[] { saved2 },
                                Expression.Assign( saved2, CallRuntime( SaveStackAndFrameMethod ) ),
                                CallRuntime( RestoreFrameMethod, saved ),
                                finallyExpr,
                                CallRuntime( RestoreFrameMethod, saved2 )
                            )
                        )
                    );
        }

        internal static LabelTarget FindTagbodyLabel( AnalysisScope scope, Symbol sym )
        {
            var curscope = scope;
            while ( curscope != null )
            {
                if ( curscope.IsTagBodyScope )
                {
                    var tags = curscope.Tags.Where( x => x.Name == sym.Name ).ToList();
                    if ( tags.Count != 0 )
                    {
                        return tags[ 0 ];
                    }
                    curscope = curscope.Parent;
                }
                else if ( curscope.IsLambda )
                {
                    break;
                }
                else
                {
                    curscope = curscope.Parent;
                }
            }

            return null;
        }


        internal static AnalysisScope FindFirstLambda( AnalysisScope scope )
        {
            var curscope = scope;
            while ( curscope != null )
            {
                if ( curscope.IsLambda )
                {
                    return curscope;
                }
                else
                {
                    curscope = curscope.Parent;
                }
            }
            return null;
        }

        internal static Expression WrapBooleanTest( Expression expr )
        {
            return CallRuntime( ToBoolMethod, expr );
        }
    }


}
