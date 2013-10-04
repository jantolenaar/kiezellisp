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

        internal static Delegate CompileToDelegate2( Expression expr, ParameterExpression[] parameters )
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

        internal static Func<object, object[],object> CompileToFunction2( Expression expr, params ParameterExpression[] parameters )
        {
            return ( Func<object, object[],object> ) CompileToDelegate2( expr, parameters );
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
                return Expression.Constant( expr, typeof( object ) );
            }
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


        internal static Expression CompileBinary( CompilerHelper helper, object seed, object oper, Cons args, AnalysisScope scope )
        {
            int count = Length( args );

            if ( count == 0 )
            {
                return Expression.Constant( seed, typeof( object ) );
            }
            else if ( count == 1 )
            {
                return Compile( First( args ), scope );
            }
            else
            {
                Cons newForm = RewriteAsBinaryExpression( oper, args );
                return helper( newForm, scope );
            }
        }

        internal static Expression CompileAnd( Cons form, AnalysisScope scope )
        {
            return CompileBinary( Compile, true, Symbols.If, Cdr( form ), scope );
        }

        internal static Expression CompileOr( Cons form, AnalysisScope scope )
        {
            return CompileBinary( CompileOr2, false, Symbols.Or, Cdr( form ), scope );
        }

        internal static Expression CompileOr2( Cons form, AnalysisScope scope )
        {
            var Do = Symbols.Do;
            var Var = Symbols.Var;
            var X = Symbols.Temp;
            var If = Symbols.If;
            var code = MakeList( Do, MakeList( Var, X, Second( form ) ), MakeList( If, X, X, Third( form ) ) );
            return Compile( code, scope );
        }

        internal static Expression CompileQuote( Cons form, AnalysisScope scope )
        {
            CheckMinLength( form, 2 );
            return Expression.Constant( Second( form ), typeof( object ) );
        }

        internal static MethodInfo RuntimeMethod( string name )
        {
            return typeof( Runtime ).GetMethod( name, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
        }

        internal static Expression CallRuntime( MethodInfo method, params Expression[] exprs )
        {
            return Expression.Call( method, exprs );
        }

        internal static MethodInfo SetLexicalMethod = RuntimeMethod( "SetLexical" );
        internal static MethodInfo UnwindExceptionIntoNewExceptionMethod = RuntimeMethod( "UnwindExceptionIntoNewException" );
        internal static MethodInfo SaveStackMethod = RuntimeMethod( "SaveStack" );
        internal static MethodInfo RestoreStackMethod = RuntimeMethod( "RestoreStack" );
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
            var FormalSlots = signature.Parameters;

            Modifier nextMod = 0;
            Modifier defaultMod = 0;
            Modifier usedMods = 0;

            bool haveNextMod = false;

            if ( kind == LambdaKind.Optional )
            {
                usedMods |= Modifier.Optional;
                defaultMod = nextMod = Modifier.Optional;
                haveNextMod = false;
            }

            foreach ( object item in ToIter(args) )
            {
                if ( item == Symbols.Returns )
                {
                    // for doc gen
                    // should be last
                    break;
                }
                else if ( item == Symbols.Optional )
                {
                    if ( kind == LambdaKind.Optional )
                    {
                        throw new LispException( "Invalid use of &optional" );
                    }
                    if ( ( usedMods & Modifier.Key ) != 0 )
                    {
                        throw new LispException( "&optional cannot be used with &key" );
                    }
                    usedMods |= Modifier.Optional;
                    defaultMod = nextMod = Modifier.Optional;
                    haveNextMod = true;
                    continue;
                }
                else if ( item == Symbols.Key )
                {
                    if ( kind == LambdaKind.Optional )
                    {
                        throw new LispException( "Invalid use of &key" );
                    }
                    if ( ( usedMods & ( Modifier.Optional | Modifier.Rest | Modifier.Params | Modifier.Vector ) ) != 0 )
                    {
                        throw new LispException( "&key cannot be used with &optional, &rest, &vector and &params" );
                    }
                    usedMods |= Modifier.Key;
                    defaultMod = nextMod = Modifier.Key;
                    haveNextMod = true;
                    continue;
                }
                else if ( item == Symbols.Rest || item == Symbols.Body )
                {
                    if ( kind == LambdaKind.Optional )
                    {
                        throw new LispException( "Invalid use of &rest or &body" );
                    }
                    if ( ( usedMods & Modifier.Key ) != 0 )
                    {
                        throw new LispException( "&rest and &body cannot be used with &key" );
                    }
                    usedMods |= Modifier.Rest;
                    nextMod = Modifier.Rest;
                    haveNextMod = true;
                    continue;
                }
                else if ( item == Symbols.Params )
                {
                    if ( kind == LambdaKind.Optional )
                    {
                        throw new LispException( "Invalid use of &params" );
                    }
                    if ( ( usedMods & Modifier.Key ) != 0 )
                    {
                        throw new LispException( "&params cannot be used with &key" );
                    }
                    usedMods |= Modifier.Params;
                    nextMod = Modifier.Params;
                    haveNextMod = true;
                    continue;
                }
                else if ( item == Symbols.Vector )
                {
                    if ( kind == LambdaKind.Optional )
                    {
                        throw new LispException( "Invalid use of &vector" );
                    }

                    if ( ( usedMods & Modifier.Key ) != 0 )
                    {
                        throw new LispException( "&vector cannot be used with &key" );
                    }
                    usedMods |= Modifier.Vector;
                    nextMod = Modifier.Vector;
                    haveNextMod = true;
                    continue;
                }
                else if ( item == Symbols.Whole )
                {
                    if ( kind == LambdaKind.Optional )
                    {
                        throw new LispException( "Invalid use of &whole" );
                    }
                    if ( ( usedMods & Modifier.Key ) != 0 )
                    {
                        //throw new LispException( "&whole cannot be used with &key" );
                    }
                    usedMods |= Modifier.Whole;
                    nextMod = Modifier.Whole;
                    haveNextMod = true;
                    continue;
                }
                else if ( item is Symbol )
                {
                    Modifier mod = haveNextMod ? nextMod : defaultMod;
                    haveNextMod = false;
                    Symbol sym = ( Symbol ) item;
                    ParameterDef arg = new ParameterDef( mod, sym, null, null );
                    signature.Parameters.Add( arg );
                    signature.Names.Add( sym );
                }
                else if ( item is Cons )
                {
                    Modifier mod = haveNextMod ? nextMod : defaultMod;
                    haveNextMod = false;

                    Cons list = ( Cons ) item;

                    if ( kind == LambdaKind.Optional )
                    {
                        throw new LispException( "Invalid CONS in variable list" );
                    }
                    else if ( mod == Modifier.Key || mod == Modifier.Optional )
                    {
                        Symbol sym = ( Symbol ) First( list );
                        var initForm = Second( list );
                        signature.Parameters.Add( new ParameterDef( mod, sym, null, initForm ) );
                        signature.Names.Add( sym );
                    }
                    else if ( kind == LambdaKind.Macro )
                    {
                        if ( mod != 0 )
                        {
                            // error
                        }

                        var nestedArgs = CompileFormalArgs( list, scope, kind );
                        ParameterDef arg = new ParameterDef( 0, nestedArgs );
                        signature.Parameters.Add( arg );
                        signature.Names.AddRange( nestedArgs.Names );
                    }
                    else if ( mod == 0 && kind == LambdaKind.Method )
                    {
                        var sym = ( Symbol ) First( list );
                        var type = Second( list );
                        if ( type == null )
                        {
                            ParameterDef arg = new ParameterDef( mod, sym, null, null );
                            signature.Parameters.Add( arg );
                            signature.Names.Add( sym );
                        }
                        else if ( type is Cons && First( type ) == MakeSymbol( "eql", LispPackage ) )
                        {
                            var expr = Compile( Second( type ), scope );
                            ParameterDef arg = new ParameterDef( mod | Modifier.EqlSpecializer, sym, expr, null );
                            signature.Parameters.Add( arg );
                            signature.Names.Add( sym );
                        }
                        else
                        {
                            if ( !Symbolp( type ) || Keywordp( type ) )
                            {
                                throw new LispException( "Invalid type specifier: {0}", type );
                            }
                            ParameterDef arg = new ParameterDef( mod | Modifier.TypeSpecializer, sym, type, null );
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

            signature.RequiredArgsCount = signature.Parameters.Count( x => (int) x.Modifiers < 4 );

            if ( kind == LambdaKind.Function )
            {
                if ( signature.RequiredArgsCount + 1 == signature.Parameters.Count )
                {
                    var mod = signature.Parameters[ signature.Parameters.Count - 1 ].Modifiers;
                    if ( mod == Modifier.Params || mod == Modifier.Rest || mod == Modifier.Vector )
                    {
                        signature.LastArgModifier = mod;
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
            // defmulti name args body
            CheckMinLength( form, 3 );
            var sym = CheckSymbol( Second( form ) );
            if ( sym.IsDynamic )
            {
                throw new LispException( "Invalid method name: {0}", sym );
            }
            var args = ( Cons ) Third( form );
            var body = Cdr( Cddr ( form ) );
            var syntax = MakeListStar( sym, args );
            var lispParams = CompileFormalArgs( args, new AnalysisScope(), LambdaKind.Method );
            var count = lispParams.RequiredArgsCount;
            string doc = "";
            if ( Length( body ) >= 1 && body.Car is string )
            {
                doc = ( string ) body.Car;
            }
            return CallRuntime( DefineMultiMethodMethod, Expression.Constant( sym ), Expression.Constant( count ), Expression.Constant( doc, typeof( string ) ) );
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

        /*
        internal static Expression CompileBinder( Cons forms, AnalysisScope scope )
        {
            // (binder [type] (arg...) expr)
            CheckMinLength( forms, 3 );
            CheckMaxLength( forms, 4 );

            LambdaKind kind;
            Cons args;
            object expr;

            if ( Length( forms ) == 3 )
            {
                args = ( Cons ) Second( forms );
                expr = Third( forms );
                kind = LambdaKind.Macro;
            }
            else
            {
                args = ( Cons ) Third( forms );
                expr = Fourth( forms );
                kind = TranslateLambdaKind( ( Symbol ) Second( forms ) );
            }

            var template = new LambdaBinder();
            template.Signature = CompileFormalArgs( args, scope, kind );

            return Expression.New( typeof( LambdaBinder ).GetConstructor( new Type[] { typeof( LambdaBinder ), typeof( object ) } ),
                                            Expression.Constant( template, typeof( LambdaBinder ) ),
                                            Compile( expr, scope ) );

        }
        */

        internal static Symbol TranslateLambdaKind( LambdaKind kind )
        {
            switch ( kind )
            {
                case LambdaKind.Optional:
                {
                    return Symbols.OptionalKeyword;
                }
                case LambdaKind.Macro:
                {
                    return Symbols.MacroKeyword;
                }
                case LambdaKind.Method:
                {
                    return Symbols.MethodKeyword;
                }
                default:
                {
                    return Symbols.FunctionKeyword;
                }
            }
        }

        internal static LambdaKind TranslateLambdaKind( Symbol kind )
        {
            if ( kind == Symbols.OptionalKeyword )
            {
                return LambdaKind.Optional;
            }
            else if ( kind == Symbols.MacroKeyword )
            {
                return LambdaKind.Macro;
            }
            else if ( kind == Symbols.MethodKeyword )
            {
                return LambdaKind.Method;
            }
            else if ( kind == Symbols.FunctionKeyword )
            {
                return LambdaKind.Function;
            }
            else
            {
                throw new LispException( "Expected :function :macro or :method keyword" );
            }
        }

        internal static Cons RewriteCode( Cons code, object[] args )
        {
            return ( Cons ) RewriteObject( code, args );
        }

        internal static object RewriteObject( object code, object[] args )
        {
            if ( code is Symbol )
            {
                var sym = ( Symbol ) code;

                if ( sym.Name.Length == 3 && sym.Name[ 0 ] == '{' && char.IsDigit( sym.Name, 1 ) && sym.Name[ 2 ] == '}' )
                {
                    int i = sym.Name[ 1 ] - '0';
                    return args[ i ];
                }
                else
                {
                    return code;
                }
            }
            else if ( code is Cons )
            {
                var list = ( Cons ) code;
                var head = RewriteObject( list.Car,args );
                var tail = RewriteObject( list.Cdr,args );
                if ( head == list.Car && tail == list.Cdr )
                {
                    return code;
                }
                else
                {
                    return MakeCons( head, (Cons)tail );
                }
            }
            else
            {
                return code;
            }
        }

        internal static Cons LambdaTemplate = null;

        internal static Expression CompileLambdaDef( Symbol name, Cons forms, AnalysisScope scope, LambdaKind kind, out string doc )
        {
            CheckMinLength( forms, 2 );

            var maybeArgs = First(forms);
            var args = Listp( maybeArgs ) ? ( Cons ) maybeArgs : MakeList( maybeArgs );
            var body = Cdr( forms );

            var funscope = new AnalysisScope( scope, name == null ? null : name.Name );
            funscope.IsLambda = true;
            funscope.ReturnLabel = Expression.Label( typeof( object ), "return-label" );

            var template = new Lambda();

            template.Name = name;
            template.Signature = CompileFormalArgs( args, scope, kind );

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

            template.Proc = CompileToFunction2( code, recurNative, argsNative );

            return Expression.New( typeof( Lambda ).GetConstructor( new Type[] { typeof( Lambda ) } ), Expression.Constant( template, typeof( Lambda ) ) );

        }


        internal static Cons GetLambdaParameterBindingCode( LambdaSignature signature )
        {
            var temp = GenTemp( "temp" );
            var code = new Vector();
            code.Add( MakeList( Symbols.HiddenVar, temp, MakeList( Symbols.MakeLambdaParameterBinder, Symbols.Recur, Symbols.Args ) ) );

            for ( int i = 0; i < signature.Names.Count; ++i )
            {
                if ( signature.Names[ i ] != Symbols.Underscore )
                {
                    var initForm = signature.FlattenedParameters[ i ].InitForm;
                    if ( initForm != null )
                    {
                        initForm = Runtime.MakeList( Symbols.Lambda, null, initForm );
                    }
                    code.Add( MakeList( Symbols.Var, signature.Names[ i ], MakeList( MakeList( Symbols.Accessor, Symbols.GetElt.Name ), temp, i, initForm ) ) );
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

            if (scope.UsesReturn)
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
            object testExpr = Second( form );
            object thenExpr = Third( form );
            object elseExpr = Fourth( form );
            Expression alt = Compile( elseExpr, scope );
            return Expression.Condition( WrapBooleanTest( Compile( testExpr, scope ) ),
                                         Expression.Convert( Compile( thenExpr, scope ), typeof( object ) ),
                                         Expression.Convert( alt, typeof( object ) ) );
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
                return Expression.Constant( null, typeof( object ) );
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
                    return Expression.Constant( null, typeof( object ) );
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
            var val = Length( form ) == 2 ? Expression.Constant( null ) : Compile( Third( form ), scope );

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


        internal static Expression CompileThrow( Cons form,  AnalysisScope scope )
        {
            CheckMaxLength( form, 2 );
            return Expression.Block( typeof(object), Expression.Throw( Compile( Second( form ), scope ) ), Expression.Constant(null) );
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
