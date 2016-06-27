// Copyright (C) Jan Tolenaar. See the file LICENSE for details.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Kiezel
{
    public delegate Expression CompilerHelper(Cons form,AnalysisScope scope);
    public partial class Runtime
    {
        public static MethodInfo AddEventHandlerMethod = RuntimeMethod("AddEventHandler");
        public static MethodInfo ApplyMethod = RuntimeMethod("Apply");
        public static MethodInfo AsListMethod = RuntimeMethod("AsList");
        public static MethodInfo AsVectorMethod = RuntimeMethod("AsVector");
        public static MethodInfo CastMethod = typeof(System.Linq.Enumerable).GetMethod("Cast");
        public static MethodInfo ChangeTypeMethod = RuntimeMethod("ChangeType");
        public static MethodInfo ConvertToEnumTypeMethod = RuntimeMethod("ConvertToEnumType");
        public static MethodInfo DefDynamicConstMethod = RuntimeMethod("DefDynamicConst");
        public static MethodInfo DefDynamicMethod = RuntimeMethod("DefDynamic");
        public static MethodInfo DefineCompilerMacroMethod = RuntimeMethod("DefineCompilerMacro");
        public static MethodInfo DefineSymbolMacroMethod = RuntimeMethod("DefineSymbolMacro");
        public static MethodInfo DefineConstantMethod = RuntimeMethod("DefineConstant");
        public static MethodInfo DefineFunctionMethod = RuntimeMethod("DefineFunction");
        public static MethodInfo DefineMacroMethod = RuntimeMethod("DefineMacro");
        public static MethodInfo DefineMethodMethod = RuntimeMethod("DefineMethod");
        public static MethodInfo DefineMultiMethodMethod = RuntimeMethod("DefineMultiMethod");
        public static MethodInfo DefineVariableMethod = RuntimeMethod("DefineVariable");
        public static MethodInfo EqualMethod = RuntimeMethod("Equal");
        public static Type GenericListType = GetTypeForImport("System.Collections.Generic.List`1", null);
        public static MethodInfo GetDelayedExpressionResultMethod = RuntimeMethod("GetDelayedExpressionResult");
        public static MethodInfo GetDynamicMethod = RuntimeMethod("GetDynamic");
        public static MethodInfo GetLexicalMethod = RuntimeMethod("GetLexical");
        public static MethodInfo GetTaskResultMethod = RuntimeMethod("GetTaskResult");
        public static MethodInfo IsInstanceOfMethod = RuntimeMethod("IsInstanceOf");
        public static MethodInfo LogBeginCallMethod = RuntimeMethod("LogBeginCall");
        public static MethodInfo LogEndCallMethod = RuntimeMethod("LogEndCall");
        public static MethodInfo MakeLambdaClosureMethod = RuntimeMethod(typeof(LambdaDefinition), "MakeLambdaClosure");
        public static MethodInfo MakeMultiArityLambdaMethod = RuntimeMethod("MakeMultiArityLambda");
        public static MethodInfo NotMethod = RuntimeMethod("Not");
        public static MethodInfo NullOperationMethod = RuntimeMethod("NullOperation");
        public static MethodInfo RestoreFrameMethod = RuntimeMethod("RestoreFrame");
        public static MethodInfo RestoreStackAndFrameMethod = RuntimeMethod("RestoreStackAndFrame");
        public static MethodInfo SaveStackAndFrameMethod = RuntimeMethod("SaveStackAndFrame");
        public static MethodInfo SaveStackAndFrameWithMethod = RuntimeMethod("SaveStackAndFrameWith");
        public static MethodInfo SetDynamicMethod = RuntimeMethod("SetDynamic");
        public static MethodInfo SetLexicalMethod = RuntimeMethod("SetLexical");
        public static MethodInfo ToBoolMethod = RuntimeMethod("ToBool");
        public static MethodInfo UnwindExceptionMethod = RuntimeMethod("UnwindException");

        [Lisp("system:optimizer")]
        public static object Optimizer(object expr)
        {
            if (!(expr is Cons))
            {
                return expr;
            }
            var forms = (Cons)expr;
            var head = First(expr) as Symbol;
            if (head != null && head.Package == LispPackage)
            {
                var proc = head.Value as ImportedFunction;
                if (proc != null && proc.Pure)
                {
                    if (head == Symbols.Quote)
                    {
                        expr = Second(forms);
                    }
                    else
                    {
                        var tail = Map(Optimizer, Cdr(forms));
                        bool simple = SeqBase.Every(Literalp, tail);
                        if (simple)
                        {
                            expr = ApplyStar(proc, tail);
                        }
                    }
                }
            }
            return expr;
        }

        public static Expression CallRuntime(MethodInfo method, params Expression[] exprs)
        {
            return Expression.Call(method, exprs);
        }

        public static void CheckLength(Cons form, int length)
        {
            if (Length(form) != length)
            {
                throw new LispException("{0}: expected list with length equal to {1}", form, length);
            }
        }

        public static void CheckMaxLength(Cons form, int length)
        {
            if (Length(form) > length)
            {
                throw new LispException("{0}: expected list with length less than {1}", form, length);
            }
        }

        public static void CheckMinLength(Cons form, int length)
        {
            if (Length(form) < length)
            {
                throw new LispException("{0}: expected list with length greater than {1}", form, length);
            }
        }

        public static Expression Compile(object expr, AnalysisScope scope)
        {
            if (DebugMode)
            {
                var context = CurrentThreadContext;
                var saved = context.SaveStackAndFrame(null, MakeList(Symbols.Compiling, expr));
                var result = CompileWrapped(expr, scope);
                context.RestoreStackAndFrame(saved);
                return result;
            }
            else
            {
                var result = CompileWrapped(expr, scope);
                return result;
            }
        }

        public static Expression CompileAnd(Cons form, AnalysisScope scope)
        {
            // AND forms
            return CompileAndExpression(Cdr(form), scope);
        }

        public static Expression CompileAndExpression(Cons forms, AnalysisScope scope)
        {
            if (forms == null)
            {
                return CompileLiteral(true);
            }
            else if (Cdr(forms) == null)
            {
                return Compile(First(forms), scope);
            }
            else
            {
                return Expression.Condition(WrapBooleanTest(Compile(First(forms), scope)),
                    CompileAndExpression(Cdr(forms), scope),
                    CompileLiteral(null));
            }
        }

        public static Expression CompileBody(Cons forms, AnalysisScope scope)
        {
            if (forms == null)
            {
                return CompileLiteral(null);
            }

            var bodyScope = new AnalysisScope(scope, "body");
            bodyScope.IsBlockScope = true;
            bodyScope.Tilde = bodyScope.DefineNativeLocal(Symbols.Tilde, ScopeFlags.All);

            if (!DebugMode)
            {
                bodyScope.FreeVariables = new HashSet<Symbol>();
            }
            else
            {
                bodyScope.Names = new List<Symbol>();
            }

            foreach (var stmt in forms)
            {
                var name = ExtractLabelName(stmt);

                if (name != null)
                {
                    var label = Expression.Label(typeof(object), name);
                    bodyScope.Tags.Add(label);
                }
            }

            var bodyExprs = CompileBodyExpressions(forms, bodyScope);

            if (bodyScope.Variables.Count == 0 && !bodyScope.UsesTilde && !bodyScope.UsesLabels)
            {
                if (bodyExprs.Count == 0)
                {
                    return CompileLiteral(null);
                }

                if (bodyExprs.Count == 1)
                {
                    return Compile(First(forms), scope);
                }
            }

            bool recompile = false;

            if (!DebugMode && bodyScope.FreeVariables.Count != 0)
            {
                recompile = true;
            }

            if (recompile)
            {
                bodyScope.Variables = new List<ScopeEntry>();
                bodyScope.Tilde = bodyScope.DefineNativeLocal(Symbols.Tilde, ScopeFlags.All);

                if (!DebugMode)
                {
                    if (bodyScope.FreeVariables.Count != 0)
                    {
                        // Recompile with the free variables moved from native to frame
                        bodyScope.Names = new List<Symbol>();
                    }
                }
                else
                {
                    bodyScope.Names = new List<Symbol>();
                }

                bodyExprs = CompileBodyExpressions(forms, bodyScope);
            }

            var parameters = bodyScope.Parameters;

            if (bodyScope.TagBodySaved != null)
            {
                parameters.Add(bodyScope.TagBodySaved);
                bodyExprs.Insert(0, Expression.Assign(bodyScope.TagBodySaved, CallRuntime(SaveStackAndFrameMethod)));
            }

            Expression block = Expression.Block(typeof(object), parameters, bodyExprs);

            if (bodyScope.Names != null || bodyScope.UsesFramedVariables)
            {
                var lambda = FindFirstLambda(bodyScope);
                if (lambda != null)
                {
                    lambda.UsesFramedVariables = true;
                }
                block = WrapEvaluationStack(block, new Frame(bodyScope.Names));
            }
            else if (bodyScope.UsesDynamicVariables)
            {
                var lambda = FindFirstLambda(bodyScope);
                if (lambda != null)
                {
                    lambda.UsesDynamicVariables = true;
                }
                block = WrapEvaluationStack(block, null);
            }

            if (DebugMode)
            {
                bodyScope.CheckVariables();
            }

            return block;
        }

        public static List<Expression> CompileBodyExpressions(Cons forms, AnalysisScope bodyScope)
        {
            var bodyExprs = new List<Expression>();
            var tilde = bodyScope.Tilde;

            for (var list = forms; list != null; list = list.Cdr)
            {
                var expr = list.Car;

                var name = ExtractLabelName(expr);

                if (name != null)
                {
                    AnalysisScope labelScope;
                    LabelTarget label;
                    TryFindTagbodyLabel(bodyScope, name, out labelScope, out label);
                    Expression code = Expression.Label(label, CompileLiteral(null));
                    bodyExprs.Add(code);
                }
                else
                {
                    var code = Compile(expr, bodyScope);
                    if (!(code is GotoExpression) && tilde != null)
                    {
                        code = Expression.Assign(tilde, code);
                    }
                    bodyExprs.Add(code);
                }
            }

            if (bodyExprs[bodyExprs.Count - 1] is LabelExpression)
            {
                bodyExprs.Add(tilde);
            }

            return bodyExprs;
        }

        public static CatchBlock CompileCatchClause(Cons form, AnalysisScope scope, ParameterExpression saved)
        {
            // (sym [type]) forms...
            var sym = (Symbol)First(First(form));
            var typeName = (Symbol)Second(First(form));
            var type = typeName == null ? typeof(Exception) : (Type)GetType(typeName);
            var forms = Cdr(form);
            if (sym == null)
            {
                var code = Expression.Catch(typeof(Exception),
                               Expression.Block(typeof(object),
                                   CallRuntime(RestoreFrameMethod, saved),
                                   CompileBody(forms, scope)));
                return code;
            }
            else
            {
                var catchScope = new AnalysisScope(scope, "catch");
                var var1 = catchScope.DefineNativeLocal(Symbols.Temp, ScopeFlags.All, type);
                var var2 = catchScope.DefineNativeLocal(sym, ScopeFlags.All);
                var code = Expression.Catch(var1,
                               Expression.Block(typeof(object),
                                   new ParameterExpression[] { var2 },
                                   CallRuntime(RestoreFrameMethod, saved),
                                                      //Expression.Assign( var2, var1 ),
                                   Expression.Assign(var2, CallRuntime(UnwindExceptionMethod, var1)),
                                   CompileBody(forms, catchScope)));
                return code;
            }
        }

        public static CatchBlock[] CompileCatchClauses(List<Cons> clauses, AnalysisScope scope, ParameterExpression saved)
        {
            var blocks = new List<CatchBlock>();
            foreach (var clause in clauses)
            {
                blocks.Add(CompileCatchClause(clause, scope, saved));
            }
            return blocks.ToArray();
        }

        public static Expression CompileDeclare(Cons form, AnalysisScope scope)
        {
            CheckLength(form, 2);
            foreach (var item in Cdr( form ))
            {
                if (item is Cons)
                {
                    var declaration = (Cons)item;
                    var declare = (Symbol)First(declaration);
                    switch (declare.Name)
                    {
                        case "ignore":
                        {
                            if (!scope.IsBlockScope)
                            {
                                throw new LispException("(declare (ignore ...)) statement requires block scope: {0}", form);
                            }
                            foreach (Symbol sym in Cdr( declaration ))
                            {
                                scope.FindLocal(sym, ScopeFlags.Ignore);
                            }
                            break;
                        }
                        case "ignorable":
                        {
                            if (!scope.IsBlockScope)
                            {
                                throw new LispException("(declare (ignorable ...)) statement requires block scope: {0}", form);
                            }
                            foreach (Symbol sym in Cdr( declaration ))
                            {
                                scope.FindLocal(sym, ScopeFlags.Ignorable);
                            }
                            break;
                        }
                        case "dynamic":
                        {
                            foreach (Symbol sym in Cdr( declaration ))
                            {
                                sym.IsDynamic = true;
                            }
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                }
            }
            return Expression.Constant(null, typeof(object));
        }

        public static Expression CompileDef(Cons form, AnalysisScope scope)
        {
            // def sym expr [doc]
            CheckMinLength(form, 3);
            CheckMaxLength(form, 4);
            var sym = CheckSymbol(Second(form));
            WarnWhenShadowing(sym);
            var value = Compile(Third(form), scope);
            var doc = (string)Fourth(form);
            return Expression.Call(DefineVariableMethod, Expression.Constant(sym), value, Expression.Constant(doc, typeof(string)));
        }

        public static Expression CompileDefConstant(Cons form, AnalysisScope scope)
        {
            // defconstant sym expr [doc]
            CheckMinLength(form, 3);
            CheckMaxLength(form, 4);
            var sym = CheckSymbol(Second(form));
            WarnWhenShadowing(sym);
            var value = Compile(Third(form), scope);
            var doc = (string)Fourth(form);
            return Expression.Call(DefineConstantMethod, Expression.Constant(sym), value, Expression.Constant(doc, typeof(string)));
        }

        public static Expression CompileDefineCompilerMacro(Cons form, AnalysisScope scope)
        {
            // defmacro name args body
            CheckMinLength(form, 3);
            var sym = CheckSymbol(Second(form));
            if (sym.IsDynamic)
            {
                throw new LispException("Invalid macro name: {0}", sym);
            }
            WarnWhenShadowing(sym);
            string doc;
            var lambda = CompileLambdaDef(sym, Cddr(form), scope, LambdaKind.Macro, out doc);
            return Expression.Call(DefineCompilerMacroMethod, Expression.Constant(sym), lambda, Expression.Constant(doc, typeof(string)));
        }

        public static Expression CompileDefineSymbolMacro(Cons form, AnalysisScope scope)
        {
            // define-compiler name form
            CheckLength(form, 3);
            var sym = CheckSymbol(Second(form));
            if (sym.IsDynamic)
            {
                throw new LispException("Invalid symbol-macro name: {0}", sym);
            }
            WarnWhenShadowing(sym);
            string doc = "";
            var macro = new SymbolMacro(Third(form));
            return Expression.Call(DefineSymbolMacroMethod, Expression.Constant(sym), Expression.Constant(macro), Expression.Constant(doc, typeof(string)));
        }

        public static Expression CompileDefMacro(Cons form, AnalysisScope scope)
        {
            // defmacro name args body
            CheckMinLength(form, 3);
            var sym = CheckSymbol(Second(form));
            if (sym.IsDynamic)
            {
                throw new LispException("Invalid macro name: {0}", sym);
            }
            WarnWhenShadowing(sym);
            string doc;
            var lambda = CompileLambdaDef(sym, Cddr(form), scope, LambdaKind.Macro, out doc);
            return Expression.Call(DefineMacroMethod, Expression.Constant(sym), lambda, Expression.Constant(doc, typeof(string)));
        }

        public static Expression CompileDefMethod(Cons form, AnalysisScope scope)
        {
            // defmethod name args body
            CheckMinLength(form, 3);
            var sym = CheckSymbol(Second(form));
            if (sym.IsDynamic)
            {
                throw new LispException("Invalid method name: {0}", sym);
            }
            WarnWhenShadowing(sym);
            string doc;
            var lambda = CompileLambdaDef(sym, Cddr(form), scope, LambdaKind.Method, out doc);
            return CallRuntime(DefineMethodMethod, Expression.Constant(sym), lambda);
        }

        public static Expression CompileDefMulti(Cons form, AnalysisScope scope)
        {
            // defmulti name args [doc] body
            CheckMinLength(form, 3);
            var sym = CheckSymbol(Second(form));
            if (sym.IsDynamic)
            {
                throw new LispException("Invalid method name: {0}", sym);
            }
            WarnWhenShadowing(sym);
            var args = (Cons)Third(form);
            var body = Cdr(Cddr(form));
            var lispParams = CompileFormalArgs(args, new AnalysisScope(), LambdaKind.Function);
            string doc = "";
            if (Length(body) >= 1 && body.Car is string)
            {
                doc = (string)body.Car;
            }
            return CallRuntime(DefineMultiMethodMethod, Expression.Constant(sym), Expression.Constant(lispParams), Expression.Constant(doc, typeof(string)));
        }

        public static Expression CompileDefun(Cons form, AnalysisScope scope)
        {
            // defun name args body
            CheckMinLength(form, 3);
            var sym = CheckSymbol(Second(form));
            if (sym.IsDynamic)
            {
                throw new LispException("Invalid function name: {0}", sym);
            }
            WarnWhenShadowing(sym);
            string doc;
            var lambda = CompileLambdaDef(sym, Cddr(form), scope, LambdaKind.Function, out doc);
            return CallRuntime(DefineFunctionMethod, Expression.Constant(sym), lambda, Expression.Constant(doc, typeof(string)));
        }

        public static Expression CompileDefunStar(Cons form, AnalysisScope scope)
        {
            // defun* name (args body) ...
            CheckMinLength(form, 3);
            var sym = CheckSymbol(Second(form));
            if (sym.IsDynamic)
            {
                throw new LispException("Invalid function name: {0}", sym);
            }
            WarnWhenShadowing(sym);
            string doc;
            var lambda = CompileMultiArityLambdaDef(sym, Cddr(form), scope, out doc);
            return CallRuntime(DefineFunctionMethod, Expression.Constant(sym), lambda, Expression.Constant(doc, typeof(string)));
        }

        public static Expression CompileDo(Cons form, AnalysisScope scope)
        {
            return CompileBody(Cdr(form), scope);
        }

        public static Expression CompileDynamicExpression(System.Runtime.CompilerServices.CallSiteBinder binder, Type returnType, IEnumerable<Expression> args)
        {
            if (AdaptiveCompilation)
            {
                return Microsoft.Scripting.Ast.Utils.LightDynamic(binder, returnType, new System.Runtime.CompilerServices.ReadOnlyCollectionBuilder<Expression>(args));
            }
            else
            {
                return Expression.Dynamic(binder, returnType, args);
            }
        }

        public static Expression CompileDynamicVarInScope(Cons form, AnalysisScope scope, bool future, bool lazy, bool constant)
        {
            // var sym [expr]
            var sym = (Symbol)Second(form);

            if (!scope.IsBlockScope)
            {
                throw new LispException("Statement requires block or file scope: {0}", form);
            }

            if (lazy || future)
            {
                throw new LispException("Cannot use a dynamic variable as a future or lazy variable: {0}", form);
            }

            // Initializer must be compiled before adding the variable
            // since it may already exist. Works like nested LET forms.
            var flags = (ScopeFlags)0;
            var val = CompileLiteral(null);

            if (Length(form) == 2)
            {
                if (constant)
                {
                    throw new LispException("Constant variable must have an initializer: {0}", sym);
                }
            }
            else
            {
                flags |= ScopeFlags.Initialized | (constant ? ScopeFlags.Constant : 0);
                val = Compile(Third(form), scope);
            }

            scope.UsesDynamicVariables = true;

            if (constant)
            {
                return CallRuntime(DefDynamicConstMethod, Expression.Constant(sym), val);
            }
            else
            {
                return CallRuntime(DefDynamicMethod, Expression.Constant(sym), val);
            }
        }

        public static LambdaSignature CompileFormalArgs(Cons args, AnalysisScope scope, LambdaKind kind)
        {
            var signature = new LambdaSignature(kind == LambdaKind.MultiArityFunction ? LambdaKind.Function : kind);
            signature.ArgModifier = null;
            bool wantWholeArgName = false;
            bool wantEnvArgName = false;

            foreach (object item in ToIter( args ))
            {
                if (wantWholeArgName)
                {
                    signature.WholeArg = (Symbol)item;
                    wantWholeArgName = false;
                }
                else if (wantEnvArgName)
                {
                    signature.EnvArg = (Symbol)item;
                    wantEnvArgName = false;
                }
                else if (item is Symbol)
                {
                    var sym = (Symbol)item;

                    if (sym == Symbols.Whole)
                    {
                        if (kind != LambdaKind.Macro)
                        {
                            throw new LispException("&whole parameter can only be used for a macro");
                        }
                        wantWholeArgName = true;
                    }
                    else if (sym == Symbols.Environment)
                    {
                        if (kind != LambdaKind.Macro)
                        {
                            throw new LispException("&environment parameter can only be used for a macro");
                        }
                        wantEnvArgName = true;
                    }
                    else if (sym == Symbols.Optional || sym == Symbols.Key || sym == Symbols.Rest || sym == Symbols.Body || sym == Symbols.Params || sym == Symbols.Vector)
                    {
                        if (kind == LambdaKind.MultiArityFunction)
                        {
                            throw new LispException("Modifiers not allowed on multi-lambda function");
                        }
                        if (signature.ArgModifier != null)
                        {
                            throw new LispException("Only one modifier can be used: &key, &optional, &rest, &body, &vector or &params");
                        }
                        signature.ArgModifier = sym;
                        signature.RequiredArgsCount = signature.Parameters.Count;
                        continue;
                    }
                    else
                    {
                        ParameterDef arg = new ParameterDef(sym);
                        signature.Parameters.Add(arg);
                        signature.Names.Add(sym);
                    }
                }
                else if (item is Cons)
                {
                    var list = (Cons)item;

                    if (signature.ArgModifier == Symbols.Key || signature.ArgModifier == Symbols.Optional)
                    {
                        Symbol sym = (Symbol)First(list);
                        var initForm = Second(list);
                        if (initForm == null)
                        {
                            signature.Parameters.Add(new ParameterDef(sym));
                            signature.Names.Add(sym);
                        }
                        else
                        {
                            var initForm2 = Compile(initForm, scope);
                            signature.Parameters.Add(new ParameterDef(sym, initForm: initForm2));
                            signature.Names.Add(sym);
                        }
                    }
                    else if (signature.ArgModifier == null && kind == LambdaKind.Macro)
                    {
                        var nestedArgs = CompileFormalArgs(list, scope, kind);
                        ParameterDef arg = new ParameterDef(null, nestedParameters: nestedArgs);
                        signature.Parameters.Add(arg);
                        signature.Names.AddRange(nestedArgs.Names);
                    }
                    else if (signature.ArgModifier == null && kind == LambdaKind.Method)
                    {
                        var sym = (Symbol)First(list);
                        var type = Second(list);
                        if (type == null)
                        {
                            ParameterDef arg = new ParameterDef(sym);
                            signature.Parameters.Add(arg);
                            signature.Names.Add(sym);
                        }
                        else if (type is Cons && First(type) == FindSymbol("lisp:eql"))
                        {
                            // Compile time constants!
                            var expr = Eval(Second(type));
                            ParameterDef arg = new ParameterDef(sym, specializer: new EqlSpecializer(expr));
                            signature.Parameters.Add(arg);
                            signature.Names.Add(sym);
                        }
                        else
                        {
                            if (!Symbolp(type) || Keywordp(type))
                            {
                                throw new LispException("Invalid type specifier: {0}", type);
                            }
                            var realType = GetType((Symbol)type);
                            ParameterDef arg = new ParameterDef(sym, specializer: realType);
                            signature.Parameters.Add(arg);
                            signature.Names.Add(sym);
                        }
                    }
                    else
                    {
                        throw new LispException("Invalid CONS in lambda parameter list");
                    }
                }
            }

            if (signature.ArgModifier == null)
            {
                signature.RequiredArgsCount = signature.Parameters.Count;
            }

            if (kind == LambdaKind.Function)
            {
                var sym = signature.ArgModifier;
                if (sym == Symbols.Rest || sym == Symbols.Body || sym == Symbols.Params || sym == Symbols.Vector)
                {
                    if (signature.RequiredArgsCount + 1 != signature.Parameters.Count)
                    {
                        throw new LispException("Invalid placement of &rest or similar modifier.");
                    }
                }
            }

            return signature;
        }

        public static Expression CompileFunctionCall(Cons form, AnalysisScope scope)
        {
            if (OptimizerEnabled)
            {
                var expr = Optimizer(form);
                if (expr != form)
                {
                    return Expression.Constant(expr, typeof(object));
                }
            }

//            if (head == Symbols.Catch || head == Symbols.Finally)
//            {
//                PrintWarning("Catch/finally used outside of a try block?!");
//            }

            CheckMinLength(form, 1);
            var formFunc = First(form);
            var formArgs = Cdr(form);
            var member = formFunc as Cons;

            if (member != null && First(member) == Symbols.Dot)
            {
                var names = (string)Second(member);
                var args = ToIter(formArgs).Cast<object>().Select(x => Compile(x, scope)).ToArray();
                return AccessorLambdaMetaObject.MakeExpression(false, names, args);
            }
            else if (member != null && First(member) == Symbols.NullableDot)
            {
                var names = (string)Second(member);
                var args = ToIter(formArgs).Cast<object>().Select(x => Compile(x, scope)).ToArray();
                return AccessorLambdaMetaObject.MakeExpression(true, names, args);
            }
            else
            {
                var func = Compile(formFunc, scope);
                List<Expression> args = new List<Expression>();
                args.Add(func);
                foreach (object a in ToIter( formArgs ))
                {
                    args.Add(Compile(a, scope));
                }
                var binder = GetInvokeBinder(Length(formArgs));
                var code = CompileDynamicExpression(binder, typeof(object), args);
                if (!DebugMode)
                {
                    return code;
                }
                else
                {
                    return WrapEvaluationStack(code, null, form);
                }
            }
        }

        public static Expression CompileFutureVar(Cons form, AnalysisScope scope)
        {
            return CompileVarInScope(form, scope, future: true);
        }

        public static Expression CompileGetElt(Cons form, AnalysisScope scope)
        {
            // (elt target indexes)
            CheckMinLength(form, 3);
            var args = new List<Expression>(ConvertToEnumerableObject(form.Cdr).Select(x => Compile(x, scope)));
            var binder = GetGetIndexBinder(args.Count - 1);
            return CompileDynamicExpression(binder, typeof(object), args);
        }

        public static Expression CompileGetMember(Cons form, AnalysisScope scope)
        {
            // (attr target property)
            CheckLength(form, 3);
            var member = Third(form);

            if (member is string || Keywordp(member))
            {
                var name = GetDesignatedString(member);
                var target = Compile(Second(form), scope);
                var binder = GetGetMemberBinder(name);
                return Expression.Dynamic(binder, typeof(object), new Expression[] { target });
            }
            else if (member is Cons && First(member) == Symbols.Quote && Second(member) is Symbol)
            {
                var name = ((Symbol)Second(member)).Name;
                var target = Compile(Second(form), scope);
                var binder = GetGetMemberBinder(name);
                return Expression.Dynamic(binder, typeof(object), new Expression[] { target });
            }
            else
            {
                // not a constant name
                var newform = MakeCons(Symbols.Funcall, form);
                return CompileFunctionCall(newform, scope);
            }
        }

        public static Expression CompileGetVariable(Symbol sym, AnalysisScope scope, bool useEnvironment)
        {
            if (sym.IsDynamic)
            {
                return CallRuntime(GetDynamicMethod, Expression.Constant(sym));
            }
            else if (Keywordp(sym))
            {
                return Expression.Constant(sym, typeof(Symbol));
            }
            else
            {
                int depth;
                ScopeEntry entry;

                if (scope.FindLocal(sym, ScopeFlags.Referenced, out depth, out entry))
                {
                    Expression code;

                    if (entry.MacroValue != null || entry.SymbolMacroValue != null)
                    {
                        throw new LispException("{0}: cannot compile reference to (symbol)macro variable", sym);
                    }
                    else if (entry.Parameter != null)
                    {
                        code = entry.Parameter;
                    }
                    else
                    {
                        code = CallRuntime(GetLexicalMethod, Expression.Constant(depth), Expression.Constant(entry.Index));
                    }

                    if ((entry.Flags & ScopeFlags.Future) != 0)
                    {
                        // This also handles the lazy-future case.
                        code = CallRuntime(GetTaskResultMethod, Expression.Convert(code, typeof(ThreadContext)));
                    }
                    else if ((entry.Flags & ScopeFlags.Lazy) != 0)
                    {
                        code = CallRuntime(GetDelayedExpressionResultMethod, Expression.Convert(code, typeof(DelayedExpression)));
                    }

                    return code;
                }
                else if (useEnvironment)
                {
                    return Expression.PropertyOrField(Expression.Constant(sym), "CheckedOrEnvironmentValue");
                }
                else
                {
                    return Expression.PropertyOrField(Expression.Constant(sym), "CheckedValue");
                }
            }
        }

        public static Expression CompileGoto(Cons form, AnalysisScope scope)
        {
            // goto symbol [value]
            CheckMinLength(form, 2);
            CheckMaxLength(form, 3);
            var tag = CheckSymbol(Second(form));
            var value = Compile(Third(form), scope);
            AnalysisScope labelScope;
            LabelTarget label;
            if (!TryFindTagbodyLabel(scope, tag.LongName, out labelScope, out label))
            {
                throw new LispException("Label {0} not found", tag);
            }

            if (labelScope == scope)
            {
                var code = Expression.Block(typeof(object),
                               Expression.Assign(labelScope.Tilde, value),
                               RuntimeHelpers.EnsureObjectResult(Expression.Goto(label, CompileLiteral(null))));
                return code;
            }
            else
            {
                if (labelScope.TagBodySaved == null)
                {
                    labelScope.TagBodySaved = Expression.Parameter(typeof(ThreadContextState), "saved-for-goto");
                }

                var code = Expression.Block(typeof(object),
                               Expression.Assign(labelScope.Tilde, value),
                               CallRuntime(RestoreStackAndFrameMethod, labelScope.TagBodySaved),
                               RuntimeHelpers.EnsureObjectResult(Expression.Goto(label, CompileLiteral(null))));
                return code;
            }
        }

        public static Expression CompileHiddenVar(Cons form, AnalysisScope scope)
        {
            return CompileVarInScope(form, scope, native: true);
        }

        public static Expression CompileIf(Cons form, AnalysisScope scope)
        {
            // if expr expr [expr]
            CheckMinLength(form, 3);
            CheckMaxLength(form, 4);
            var testExpr = Compile(Second(form), scope);
            var thenExpr = Compile(Third(form), scope);
            var elseExpr = Compile(Fourth(form), scope);
            var test = WrapBooleanTest(testExpr);
            return Expression.Condition(test, thenExpr, elseExpr);
        }

        public static Expression CompileLabel(Cons form, AnalysisScope scope)
        {
            throw new LispException("Labels must be placed at the top level of a implicit or explicit DO block");
        }

        public static Expression CompileLambda(Cons form, AnalysisScope scope)
        {
            // lambda [name] args body

            string doc;
            if (Symbolp(Second(form)))
            {
                CheckMinLength(form, 3);
                return CompileLambdaDef((Symbol)Second(form), Cddr(form), scope, LambdaKind.Function, out doc);
            }
            else
            {
                CheckMinLength(form, 2);
                return CompileLambdaDef(null, Cdr(form), scope, LambdaKind.Function, out doc);
            }
        }

        public static Expression CompileLambdaStar(Cons form, AnalysisScope scope)
        {
            // lambda [name] args body

            string doc;
            if (Symbolp(Second(form)))
            {
                CheckMinLength(form, 3);
                return CompileMultiArityLambdaDef((Symbol)Second(form), Cddr(form), scope, out doc);
            }
            else
            {
                CheckMinLength(form, 2);
                return CompileMultiArityLambdaDef(null, Cdr(form), scope, out doc);
            }
        }

        public static Expression CompileMultiArityLambdaDef(Symbol name, Cons forms, AnalysisScope scope, out string doc)
        {
            doc = "";
            var lambdas = new List<Expression>();
            foreach (Cons form in ToIter( forms ))
            {
                string doc2;
                lambdas.Add(CompileLambdaDef(name, form, scope, LambdaKind.MultiArityFunction, out doc2));
                if (!String.IsNullOrWhiteSpace(doc2))
                {
                    doc = doc + doc2 + "\n";
                }
            }
            var expr = CallRuntime(MakeMultiArityLambdaMethod, Expression.NewArrayInit(typeof(LambdaClosure), lambdas));
            return expr;
        }

        public static Expression CompileLambdaDef(Symbol name, Cons forms, AnalysisScope scope, LambdaKind kind, out string doc)
        {
            CheckMinLength(forms, 0);

            if (Length(forms) == 1)
            {
                PrintWarning("Function body contains no forms.");
            }

            var args = (Cons)First(forms);

            //if ( kind == LambdaKind.Function && args != null && Listp( First(args) ) )
            //{
            //    return CompileMultiArityLambdaDef( name, forms, scope, out doc );
            //}
            
            var body = Cdr(forms);

            var funscope = new AnalysisScope(scope, name == null ? null : name.Name);
            funscope.IsLambda = true;

            var template = new LambdaDefinition();

            template.Name = name;
            template.Signature = CompileFormalArgs(args, scope, kind);

            if (kind == LambdaKind.Method)
            {
                var container = name.Value as MultiMethod;
                if (container != null)
                {
                    var m = template.Signature;
                    var g = container.Signature;
                    var m1 = m.RequiredArgsCount;
                    var m2 = m.Parameters.Count;
                    var m3 = m.ArgModifier;
                    var g1 = g.RequiredArgsCount;
                    var g2 = g.Parameters.Count;
                    var g3 = g.ArgModifier;

                    if (m1 != g1)
                    {
                        throw new LispException("Method does not match multi-method: number of required arguments");
                    }
                    if (m3 != g3)
                    {
                        throw new LispException("Method does not match multi-method: different argument modifiers");
                    }
                    if (g3 != Symbols.Key && m2 != g2)
                    {
                        throw new LispException("Method does not match multi-method: number of arguments");
                    }
                    if (g3 == Symbols.Key)
                    {
                        // Replace keyword parameters with the full list from the generic definition, but keep the defaults
                        var usedKeys = template.Signature.Parameters.GetRange(m1, m2 - m1);
                        var replacementKeys = container.Signature.Parameters.GetRange(g1, g2 - g1);
                        template.Signature.Parameters.RemoveRange(m1, m2 - m1);
                        template.Signature.Names.RemoveRange(m1, m2 - m1);
                        foreach (var par in replacementKeys)
                        {
                            var oldpar = usedKeys.FirstOrDefault(x => x.Sym == par.Sym);
                            if (oldpar != null)
                            {
                                var newpar = new ParameterDef(par.Sym, initForm: oldpar.InitForm);
                                template.Signature.Parameters.Add(newpar);
                                template.Signature.Names.Add(par.Sym);
                            }
                            else
                            {
                                // Insert place holder
                                var newpar = new ParameterDef(par.Sym, hidden: true);
                                template.Signature.Parameters.Add(newpar);
                                template.Signature.Names.Add(newpar.Sym);
                            }
                        }
                    }
                }
            }
            if (name != null)
            {
                template.Syntax = MakeListStar(template.Name, args);
            }

#if !DEBUG
            template.Source = MakeListStar(Symbols.Lambda, args, body);
#endif
            doc = "";
            if (body != null)
            {
                if (body.Car is string)
                {
                    doc = (string)body.Car;
                    if (body.Cdr != null)
                    {
                        body = body.Cdr;
                    }
                }
            }

            var v = new Vector();

            if (args == null)
            {
                v.AddRange(body);
                v.Add(MakeList(Symbols.Label, Symbols.FunctionExitLabel));
            }
            else
            {
                GetLambdaParameterBindingCode(v, template.Signature);
                v.Add(MakeListStar(Symbols.Do, body));
                v.Add(MakeList(Symbols.Label, Symbols.FunctionExitLabel));
            }

            body = AsList(v);

#if DEBUG
            template.Source = MakeListStar(Symbols.Lambda, MakeList(Symbols.Params, Symbols.Args), body);
#endif
            var lambdaListNative = funscope.DefineNativeLocal(Symbols.LambdaList, ScopeFlags.All);
            var recurNative = funscope.DefineNativeLocal(Symbols.Recur, ScopeFlags.All);
            var argsNative = funscope.DefineNativeLocal(Symbols.Args, ScopeFlags.All);

            Expression code = CompileBody(body, funscope);

            template.Proc = CompileToFunction3(code, lambdaListNative, recurNative, argsNative);

            return CallRuntime(MakeLambdaClosureMethod, Expression.Constant(template, typeof(LambdaDefinition)));
        }

        public static Expression CompileLazyVar(Cons form, AnalysisScope scope)
        {
            return CompileVarInScope(form, scope, lazy: true, constant: true);
        }

        public static Expression CompileLet(Cons form, AnalysisScope scope)
        {
            return CompileVarInScope(form, scope, constant: true);
        }

        public static Expression CompileLetFun(Cons form, AnalysisScope scope)
        {
            CheckMinLength(form, 3);
            var name = CheckSymbol(Second(form));
            var lambdaForm = MakeCons(Symbols.Lambda, Cdr(form));
            var letForm = MakeList(Symbols.Let, name, lambdaForm);
            return CompileLet(letForm, scope);
        }

        public static Expression CompileLetMacro(Cons form, AnalysisScope scope)
        {
            // letmacro name args body
            CheckMinLength(form, 3);
            var sym = CheckSymbol(Second(form));
            if (sym.IsDynamic)
            {
                throw new LispException("Invalid macro name: {0}", sym);
            }
            if (!scope.IsBlockScope)
            {
                throw new LispException("Statement requires block or file scope: {0}", form);
            }
            if (sym == Symbols.Tilde)
            {
                throw new LispException("\"~\" is a reserved symbol name");
            }
            if (scope.FindLocal(sym, 0) != null)
            {
                throw new LispException("Duplicate declaration of variable: {0}", sym);
            }
            string doc;
            var lambda = CompileLambdaDef(sym, Cddr(form), scope, LambdaKind.Macro, out doc);
            var closure = (LambdaClosure)Execute(lambda);
            scope.DefineMacro(sym, closure, ScopeFlags.Macro | ScopeFlags.All);
            return Expression.Constant(null);
        }

        public static Expression CompileLetSymbolMacro(Cons form, AnalysisScope scope)
        {
            // let-symbol-macro name form
            CheckLength(form, 3);
            CheckMinLength(form, 3);
            var sym = CheckSymbol(Second(form));
            if (sym.IsDynamic)
            {
                throw new LispException("Invalid symbol-macro name: {0}", sym);
            }
            if (!scope.IsBlockScope)
            {
                throw new LispException("Statement requires block or file scope: {0}", form);
            }
            if (sym == Symbols.Tilde)
            {
                throw new LispException("\"~\" is a reserved symbol name");
            }
            if (scope.FindLocal(sym, 0) != null)
            {
                throw new LispException("Duplicate declaration of variable: {0}", sym);
            }
            var macro = new SymbolMacro(Third(form));
            scope.DefineMacro(sym, macro, ScopeFlags.SymbolMacro | ScopeFlags.All);
            return Expression.Constant(null);

        }

        public static Expression CompileLexicalVarInScope(Cons form, AnalysisScope scope, bool native, bool future, bool lazy, bool constant)
        {
            // var sym [expr]
            var sym = (Symbol)Second(form);
            var initForm = Third(form);

            if (!scope.IsBlockScope)
            {
                throw new LispException("Statement requires block or file scope: {0}", form);
            }
            if (sym == Symbols.Tilde)
            {
                throw new LispException("\"~\" is a reserved symbol name");
            }
            if (scope.FindLocal(sym, 0) != null)
            {
                throw new LispException("Duplicate declaration of variable: {0}", sym);
            }

            constant |= future | lazy;

            // Initializer must be compiled before adding the variable
            // since it may already exist. Works like nested LET forms.
            var flags = (ScopeFlags)0;
            Expression val;

            if (Length(form) == 2)
            {
                if (constant)
                {
                    throw new LispException("Constant, future or lazy variable must have an initializer: {0}", form);
                }

                val = CompileLiteral(null);
            }
            else
            {
                flags |= ScopeFlags.Initialized | (constant ? ScopeFlags.Constant : 0);
                if (lazy && future)
                {
                    // Wrap initializer in TASK macro and wrap references to the variable in a
                    // GetTaskResult call (see CompileGetVariable).
                    flags |= ScopeFlags.Lazy | ScopeFlags.Future;
                    val = Compile(MakeList(Symbols.CreateTask, MakeList(Symbols.Lambda, null, initForm), false), scope);
                }
                else if (lazy)
                {
                    // Wrap initializer in DELAY macro and wrap references to the variable in a
                    // FORCE call.
                    flags |= ScopeFlags.Lazy;
                    val = Compile(MakeList(MakeSymbol("system:create-delayed-expression"), MakeList(Symbols.Lambda, null, initForm)), scope);
                }
                else if (future)
                {
                    // Wrap initializer in TASK macro and wrap references to the variable in a
                    // GetTaskResult call (see CompileGetVariable).
                    flags |= ScopeFlags.Future;
                    val = Compile(MakeList(Symbols.CreateTask, MakeList(Symbols.Lambda, null, initForm), true), scope);
                }
                else
                {
                    val = Compile(initForm, scope);
                }
            }

            if (scope.IsFileScope)
            {
                int index = scope.DefineFrameLocal(sym, flags);
                return CallRuntime(SetLexicalMethod, Expression.Constant(0), Expression.Constant(index), val);
            }
            else if (native)
            {
                var parameter = scope.DefineNativeLocal(sym, flags);
                return Expression.Assign(parameter, val);
            }
            else if (!DebugMode && scope.FreeVariables != null && !scope.FreeVariables.Contains(sym))
            {
                var parameter = scope.DefineNativeLocal(sym, flags);
                return Expression.Assign(parameter, val);
            }
            else
            {
                int index = scope.DefineFrameLocal(sym, flags);
                return CallRuntime(SetLexicalMethod, Expression.Constant(0), Expression.Constant(index), val);
            }
        }

        public static Expression CompileLiteral(object expr)
        {
            return Expression.Constant(expr, typeof(object));
        }

        public static Expression CompileMergingDo(Cons forms, AnalysisScope scope)
        {
            if (scope.IsBlockScope)
            {
                // merge
                var expressions = CompileBodyExpressions(Cdr(forms), scope);
                Expression block = Expression.Block(typeof(object), expressions);
                return block;
            }
            else
            {
                // compile as do block
                return CompileBody(Cdr(forms), scope);
            }
        }

        public static Expression CompileOr(Cons form, AnalysisScope scope)
        {
            // OR forms
            return CompileOrExpression(Cdr(form), scope);
        }

        public static Expression CompileOrExpression(Cons forms, AnalysisScope scope)
        {
            if (forms == null)
            {
                return CompileLiteral(false);
            }
            else if (Cdr(forms) == null)
            {
                return Compile(First(forms), scope);
            }
            else
            {
                var expr1 = Compile(First(forms), scope);
                var expr2 = CompileOrExpression(Cdr(forms), scope);
                var temp = Expression.Variable(typeof(object), "temp");
                var tempAssign = Expression.Assign(temp, expr1);
                var result = Expression.Condition(WrapBooleanTest(temp), temp, expr2);

                return Expression.Block(typeof(object), new ParameterExpression[] { temp }, tempAssign, result);
            }
        }

        public static Expression CompileQuote(Cons form, AnalysisScope scope)
        {
            CheckMinLength(form, 2);
            return CompileLiteral(Second(form));
        }

        public static Expression CompileReturn(Cons form, AnalysisScope scope)
        {
            // return [expr]
            CheckMaxLength(form, 2);

            var funscope = FindFirstLambda(scope);

            if (funscope == null)
            {
                throw new LispException("Invalid use of RETURN.");
            }

            if (funscope.IsFileScope)
            {
                return Compile(MakeList(Symbols.ReturnFromLoad), scope);
            }
            else
            {
                var value = Cadr(form);
                return Compile(MakeList(Symbols.Goto, Symbols.FunctionExitLabel, value), scope);
            }
        }

        public static Expression CompileSetElt(Cons form, AnalysisScope scope)
        {
            // (set-elt target indexes value)
            CheckMinLength(form, 4);
            var args = new List<Expression>(ConvertToEnumerableObject(form.Cdr).Select(x => Compile(x, scope)));
            var binder = GetSetIndexBinder(args.Count - 1);
            return CompileDynamicExpression(binder, typeof(object), args);
        }

        public static Expression CompileSetMember(Cons form, AnalysisScope scope)
        {
            // (set-attr target property value)
            CheckLength(form, 4);
            var member = Third(form);

            if (member is string || Keywordp(member))
            {
                var name = GetDesignatedString(member);
                var target = Compile(Second(form), scope);
                var value = Compile(Fourth(form), scope);
                var binder = GetSetMemberBinder(name);
                return CompileDynamicExpression(binder, typeof(object), new Expression[] { target, value });
            }
            else if (member is Cons && First(member) == Symbols.Quote && Second(member) is Symbol)
            {
                var name = ((Symbol)Second(member)).Name;
                var target = Compile(Second(form), scope);
                var value = Compile(Fourth(form), scope);
                var binder = GetSetMemberBinder(name);
                return CompileDynamicExpression(binder, typeof(object), new Expression[] { target, value });
            }
            else
            {
                // not a constant name
                var newform = MakeCons(Symbols.Funcall, form);
                return CompileFunctionCall(newform, scope);
            }
        }

        public static Expression CompileSetq(Cons form, AnalysisScope scope)
        {
            // setq sym expr
            CheckLength(form, 3);

            var sym = CheckSymbol(Second(form));
            var form2 = MacroExpand(sym, scope);
            if (form2 != sym)
            {
                // symbol macro - change setq to setf
                var expr = MakeListStar(Symbols.Setf, form2, Cddr(form));
                return Compile(expr, scope);
            }

            var value = Compile(Third(form), scope);

            if (sym.IsDynamic)
            {
                if (scope.Parent == null && !scope.IsFileScope)
                {
                    // REPL does not require def before set.
                    return Expression.Assign(Expression.PropertyOrField(Expression.Constant(sym), "LessCheckedValue"), value);
                }
                else
                {
                    return CallRuntime(SetDynamicMethod, Expression.Constant(sym), value);
                }
            }
            else
            {
                int depth;
                ScopeEntry entry;

                if (scope.FindLocal(sym, ScopeFlags.Assigned, out depth, out entry))
                {
                    if (entry.MacroValue != null || entry.SymbolMacroValue != null)
                    {
                        throw new LispException("{0}: cannot compile reference to (symbol)macro variable", sym);
                    }

                    var constant = (entry.Flags & ScopeFlags.Constant) != 0;

                    if (constant)
                    {
                        throw new LispException("Cannot assign to a constant, future or lazy variable: {0}", sym);
                    }

                    if (entry.Parameter != null)
                    {
                        return Expression.Assign(entry.Parameter, value);
                    }
                    else
                    {
                        return CallRuntime(SetLexicalMethod, Expression.Constant(depth), Expression.Constant(entry.Index), value);
                    }
                }
                else if (scope.Parent == null)
                {
                    // REPL assigments
                    return Expression.Assign(Expression.PropertyOrField(Expression.Constant(sym), "LessCheckedValue"), value);
                }
                else
                {
                    return Expression.Assign(Expression.PropertyOrField(Expression.Constant(sym), "CheckedValue"), value);
                }
            }
        }

        public static Expression CompileThrow(Cons form, AnalysisScope scope)
        {
            CheckMaxLength(form, 2);
            return Expression.Block(typeof(object), Expression.Throw(Compile(Second(form), scope)), CompileLiteral(null));
        }

        public static Delegate CompileToDelegate(Expression expr)
        {
            var lambda = expr as LambdaExpression;

            if (lambda == null)
            {
                lambda = Expression.Lambda(expr);
            }

            if (AdaptiveCompilation)
            {
                return Microsoft.Scripting.Generation.CompilerHelpers.LightCompile(lambda, CompilationThreshold);
            }
            else
            {
                return lambda.Compile();
            }
        }

        public static Delegate CompileToDelegate(Expression expr, ParameterExpression[] parameters)
        {
            var lambda = expr as LambdaExpression;

            if (lambda == null)
            {
                lambda = Expression.Lambda(expr, parameters);
            }

            if (AdaptiveCompilation)
            {
                return Microsoft.Scripting.Generation.CompilerHelpers.LightCompile(lambda, CompilationThreshold);
            }
            else
            {
                return lambda.Compile();
            }
        }

        public static Func<object> CompileToFunction(Expression expr)
        {
            return (Func<object>)CompileToDelegate(expr);
        }

        public static Func<object, object[], object> CompileToFunction2(Expression expr, params ParameterExpression[] parameters)
        {
            return (Func<object, object[], object>)CompileToDelegate(expr, parameters);
        }

        public static Func<Cons, object, object[], object> CompileToFunction3(Expression expr, params ParameterExpression[] parameters)
        {
            return (Func<Cons, object, object[], object>)CompileToDelegate(expr, parameters);
        }

        public static Expression CompileTry(Cons form, AnalysisScope scope)
        {
            var tryForms = new Vector();
            var catchForms = new List<Cons>();
            var cleanupForms = new Vector();
            foreach (var expr in Cdr( form ))
            {
                if (expr is Cons)
                {
                    var list = (Cons)expr;
                    var head = First(list);
                    if (head == Symbols.Catch)
                    {
                        catchForms.Add(Cdr(list));
                        continue;
                    }
                    else if (head == Symbols.Finally)
                    {
                        cleanupForms.AddRange(Cdr(list));
                        continue;
                    }
                }
                tryForms.Add(expr);
            }

            if (cleanupForms.Count != 0)
            {
                return CompileTryCatchFinally(AsList(tryForms), catchForms, AsList(cleanupForms), scope);
            }
            else if (catchForms.Count != 0)
            {
                return CompileTryCatch(AsList(tryForms), catchForms, scope);
            }
            else
            {
                return CompileBody(AsList(tryForms), scope);
            }
        }

        public static Expression CompileTryCatch(Cons trying, List<Cons> catching, AnalysisScope scope)
        {
            var saved = Expression.Parameter(typeof(ThreadContextState), "saved");

            var tryExpr = CompileBody(trying, scope);
            var catchExprs = CompileCatchClauses(catching, scope, saved);

            return Expression.Block
                    (
                typeof(object),
                new ParameterExpression[] { saved },
                Expression.Assign(saved, CallRuntime(SaveStackAndFrameMethod)),
                Expression.TryCatch
                        (
                    tryExpr,
                    catchExprs
                )
            );
        }

        public static Expression CompileTryCatchFinally(Cons trying, List<Cons> catching, Cons cleaning, AnalysisScope scope)
        {
            var saved = Expression.Parameter(typeof(ThreadContextState), "saved");
            var saved2 = Expression.Parameter(typeof(ThreadContextState), "saved2");

            var tryExpr = CompileBody(trying, scope);
            var cleanupExpr = CompileBody(cleaning, scope);
            var catchExprs = CompileCatchClauses(catching, scope, saved);

            return Expression.Block
                    (
                typeof(object),
                new ParameterExpression[] { saved },
                Expression.Assign(saved, CallRuntime(SaveStackAndFrameMethod)),
                Expression.TryCatchFinally
                        (
                    tryExpr,
                    Expression.Block
                            (
                        typeof(object),
                        new ParameterExpression[] { saved2 },
                        Expression.Assign(saved2, CallRuntime(SaveStackAndFrameMethod)),
                        CallRuntime(RestoreFrameMethod, saved),
                        cleanupExpr,
                        CallRuntime(RestoreFrameMethod, saved2)
                    ),
                    catchExprs
                )
            );
        }

        public static Expression CompileVar(Cons form, AnalysisScope scope)
        {
            return CompileVarInScope(form, scope);
        }

        public static Expression CompileVarInScope(Cons form, AnalysisScope scope, bool native = false, bool future = false, bool lazy = false, bool constant = false)
        {
            // var sym [expr]
            CheckMinLength(form, 2);
            CheckMaxLength(form, 3);
            var sym = CheckSymbol(Second(form));
            if (sym.IsDynamic)
            {
                return CompileDynamicVarInScope(form, scope, future, lazy, constant);
            }
            else
            {
                return CompileLexicalVarInScope(form, scope, native, future, lazy, constant);
            }
        }

        public static Expression CompileWrapped(object expr1, AnalysisScope scope)
        {
            var expr = MacroExpand(expr1, scope);

            // At this point expr is either not a macro call, or it is a macro call 
            // that refused to expand by returning the &whole parameter.

            if (expr is Symbol)
            {
                var sym = (Symbol)expr;
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
                    return CompileGetVariable(sym, scope, false);
                }
            }
            else if (expr is Cons)
            {
                var form = (Cons)expr;

                if (First(form) is Symbol)
                {
                    var head = (Symbol)First(form);
                    var shadowed = scope.FindLocal(head) != null;
                
                    if (shadowed)
                    {
                        return CompileFunctionCall(form, scope);
                    }
                    else if (head.SpecialFormValue != null)
                    {
                        return head.SpecialFormValue.Helper(form, scope);
                    }
                    else
                    {
                        return CompileFunctionCall(form, scope);
                    }
                }
                else
                {
                    return CompileFunctionCall(form, scope);
                }
            }
            else
            {
                // anything else is a literal
                return CompileLiteral(expr);
            }
        }

        public static object Execute(Expression expr)
        {
            var proc = CompileToFunction(expr);
            return proc();
        }

        public static string ExtractLabelName(object form)
        {
            if (form is Cons && First(form) == Symbols.Label && Symbolp(Second(form)))
            {
                if (Keywordp(Second(form)))
                {
                    return null;
                }
                return ((Symbol)Second(form)).LongName;
            }
            else
            {
                return null;
            }
        }

        public static AnalysisScope FindFirstLambda(AnalysisScope scope)
        {
            var curscope = scope;
            while (curscope != null)
            {
                if (curscope.IsLambda)
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

        public static void GetLambdaParameterBindingCode(Vector code, LambdaSignature signature)
        {
            for (int i = 0; i < signature.Names.Count; ++i)
            {
                if (signature.Names[i] != Symbols.Underscore)
                {
                    if (i >= signature.Parameters.Count || !signature.Parameters[i].Hidden)
                    {
                        code.Add(MakeList(Symbols.Var, signature.Names[i], MakeList(Symbols.GetElt, Symbols.Args, i)));
                    }
                }
            }
            if (signature.WholeArg != null)
            {
                code.Add(MakeList(Symbols.Var, signature.WholeArg, MakeList(Symbols.GetElt, Symbols.Args, signature.Names.Count)));
            }
            if (signature.EnvArg != null)
            {
                code.Add(MakeList(Symbols.Var, signature.EnvArg, MakeList(Symbols.GetElt, Symbols.Args, signature.Names.Count + (signature.WholeArg != null ? 1 : 0))));
            }
        }

        public static object NullOperation(object a)
        {
            return null;
        }

        public static ImportedFunction GetBuiltinFunction(string name)
        {
            var sym = FindSymbol(name);
            return (ImportedFunction)sym.Value;
        }

        public static void RestartCompiler()
        {
            Symbols.And.SpecialFormValue = new SpecialForm(CompileAnd);
            Symbols.Quote.SpecialFormValue = new SpecialForm(CompileQuote);
            Symbols.Declare.SpecialFormValue = new SpecialForm(CompileDeclare);
            Symbols.Def.SpecialFormValue = new SpecialForm(CompileDef);
            Symbols.DefConstant.SpecialFormValue = new SpecialForm(CompileDefConstant);
            Symbols.DefineCompilerMacro.SpecialFormValue = new SpecialForm(CompileDefineCompilerMacro);
            Symbols.DefineSymbolMacro.SpecialFormValue = new SpecialForm(CompileDefineSymbolMacro);
            Symbols.DefMacro.SpecialFormValue = new SpecialForm(CompileDefMacro);
            Symbols.DefMethod.SpecialFormValue = new SpecialForm(CompileDefMethod);
            Symbols.DefMulti.SpecialFormValue = new SpecialForm(CompileDefMulti);
            Symbols.Defun.SpecialFormValue = new SpecialForm(CompileDefun);
            Symbols.DefunStar.SpecialFormValue = new SpecialForm(CompileDefunStar);
            Symbols.Do.SpecialFormValue = new SpecialForm(CompileDo);
            Symbols.FutureVar.SpecialFormValue = new SpecialForm(CompileFutureVar);
            Symbols.GetAttr.SpecialFormValue = new SpecialForm(CompileGetMember);
            Symbols.GetElt.SpecialFormValue = new SpecialForm(CompileGetElt);
            Symbols.Goto.SpecialFormValue = new SpecialForm(CompileGoto);
            Symbols.GreekLambda.SpecialFormValue = new SpecialForm(CompileLambda);
            Symbols.HiddenVar.SpecialFormValue = new SpecialForm(CompileHiddenVar);
            Symbols.If.SpecialFormValue = new SpecialForm(CompileIf);
            Symbols.Label.SpecialFormValue = new SpecialForm(CompileLabel);
            Symbols.Lambda.SpecialFormValue = new SpecialForm(CompileLambda);
            Symbols.LambdaStar.SpecialFormValue = new SpecialForm(CompileLambdaStar);
            Symbols.LazyVar.SpecialFormValue = new SpecialForm(CompileLazyVar);
            Symbols.Let.SpecialFormValue = new SpecialForm(CompileLet);
            Symbols.LetFun.SpecialFormValue = new SpecialForm(CompileLetFun);
            Symbols.LetMacro.SpecialFormValue = new SpecialForm(CompileLetMacro);
            Symbols.LetSymbolMacro.SpecialFormValue = new SpecialForm(CompileLetSymbolMacro);
            Symbols.MergingDo.SpecialFormValue = new SpecialForm(CompileMergingDo);
            Symbols.Or.SpecialFormValue = new SpecialForm(CompileOr);
            Symbols.Quote.SpecialFormValue = new SpecialForm(CompileQuote);
            Symbols.bqQuote.SpecialFormValue = new SpecialForm(CompileQuote);
            Symbols.Return.SpecialFormValue = new SpecialForm(CompileReturn);
            Symbols.SetAttr.SpecialFormValue = new SpecialForm(CompileSetMember);
            Symbols.SetElt.SpecialFormValue = new SpecialForm(CompileSetElt);
            Symbols.Setq.SpecialFormValue = new SpecialForm(CompileSetq);
            Symbols.Throw.SpecialFormValue = new SpecialForm(CompileThrow);
            Symbols.Try.SpecialFormValue = new SpecialForm(CompileTry);
            Symbols.Var.SpecialFormValue = new SpecialForm(CompileVar);
        }

        public static MethodInfo RuntimeMethod(string name)
        {
            return RuntimeMethod(typeof(Runtime), name);
        }

        public static MethodInfo RuntimeMethod(Type type, string name)
        {
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return methods.First(x => x.Name == name);
        }

        public static bool TryFindTagbodyLabel(AnalysisScope scope, string labelName, out AnalysisScope labelScope, out LabelTarget label)
        {
            var curscope = scope;
            while (curscope != null)
            {
                if (curscope.IsBlockScope)
                {
                    var tag = curscope.Tags.FirstOrDefault(x => x.Name == labelName);
                    if (tag != null)
                    {
                        labelScope = curscope;
                        label = tag;
                        return true;
                    }
                    curscope = curscope.Parent;
                }
                else if (curscope.IsLambda)
                {
                    break;
                }
                else
                {
                    curscope = curscope.Parent;
                }
            }

            labelScope = null;
            label = null;

            return false;
        }

        public static Expression WrapBooleanTest(Expression expr)
        {
            return CallRuntime(ToBoolMethod, expr);
        }

        public static Expression WrapEvaluationStack(Expression code, Frame frame = null, Cons form = null)
        {
            // For function, try and loop: anything that has a non-local exit method.
            var saved = Expression.Parameter(typeof(ThreadContextState), "saved");
            var result = Expression.Parameter(typeof(object), "result");
            var index = Expression.Parameter(typeof(int), "index");
            var exprs = new List<Expression>();

            exprs.Add(Expression.Assign(saved, CallRuntime(SaveStackAndFrameWithMethod,
                Expression.Constant(frame, typeof(Frame)),
                Expression.Constant(form, typeof(Cons)))));

            //if ( form != null )
            //{
            //    exprs.Add( Expression.Assign( index, CallRuntime( LogBeginCallMethod, Expression.Constant( form ) ) ) );
            //}

            exprs.Add(Expression.Assign(result, code));

            //if ( form != null )
            //{
            //    exprs.Add( CallRuntime( LogEndCallMethod, index ) );
            //}

            exprs.Add(CallRuntime(RestoreStackAndFrameMethod, saved));
            exprs.Add(result);

            return Expression.Block
                    (
                typeof(object),
                new[] { saved, result, index },
                exprs
            );
        }
    }

    public class SpecialForm
    {
        public CompilerHelper Helper;

        public SpecialForm(CompilerHelper helper)
        {
            Helper = helper;
        }

        public override string ToString()
        {
            return String.Format("SpecialForm Name=\"{0}.{1}\"", Helper.Method.DeclaringType, Helper.Method.Name);
        }
    }
}