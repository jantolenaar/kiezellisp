#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    #region Delegates

    public delegate Expression CompilerHelper(Cons form, AnalysisScope scope);

    #endregion Delegates

    public partial class Runtime
    {
        #region Static Fields

        // RuntimeMethod only works for methods without overloads.
        public static MethodInfo MakeFrameMethod = RuntimeMethod(typeof(Frame), nameof(Frame.MakeFrame));
        public static MethodInfo AddEventHandlerMethod = RuntimeMethod(nameof(AddEventHandler));
        public static MethodInfo ApplyMethod = RuntimeMethod(nameof(Apply));
        public static MethodInfo AsListMethod = RuntimeMethod(nameof(AsList));
        public static MethodInfo AsVectorMethod = RuntimeMethod(nameof(AsVector));
        public static MethodInfo CastMethod = RuntimeMethod(typeof(System.Linq.Enumerable), nameof(System.Linq.Enumerable.Cast));
        public static MethodInfo ChangeTypeMethod = RuntimeMethod(nameof(ChangeType));
        public static MethodInfo ConvertToEnumTypeMethod = RuntimeMethod(nameof(ConvertToEnumType));
        public static MethodInfo DefDynamicConstMethod = RuntimeMethod(nameof(DefDynamicConst));
        public static MethodInfo DefDynamicMethod = RuntimeMethod(nameof(DefDynamic));
        public static MethodInfo DefineCompilerMacroMethod = RuntimeMethod(nameof(DefineCompilerMacro));
        public static MethodInfo DefineConstantMethod = RuntimeMethod(nameof(DefineConstant));
        public static MethodInfo DefineFunctionMethod = RuntimeMethod(nameof(DefineFunction));
        public static MethodInfo DefineMacroMethod = RuntimeMethod(nameof(DefineMacro));
        public static MethodInfo DefineMethodMethod = RuntimeMethod(nameof(DefineMethod));
        public static MethodInfo DefineMultiMethodMethod = RuntimeMethod(nameof(DefineMultiMethod));
        public static MethodInfo DefineSymbolMacroMethod = RuntimeMethod(nameof(DefineSymbolMacro));
        public static MethodInfo DefineVariableMethod = RuntimeMethod(nameof(DefineVariable));
        public static MethodInfo EqualMethod = RuntimeMethod(nameof(Equal));
        public static Type GenericListType = GetTypeForImport("System.Collections.Generic.List`1", null);
        public static MethodInfo GetDelayedExpressionResultMethod = RuntimeMethod(nameof(GetDelayedExpressionResult));
        public static MethodInfo GetDynamicMethod = RuntimeMethod(nameof(GetDynamic));
        public static MethodInfo GetLexicalMethod = RuntimeMethod(nameof(GetLexical));
        public static MethodInfo GetTaskResultMethod = RuntimeMethod(nameof(GetTaskResult));
        public static MethodInfo IsInstanceOfMethod = RuntimeMethod(nameof(IsInstanceOf));
        public static Cons LambdaTemplate;
        public static Cons ProgTemplate;
        public static MethodInfo LogBeginCallMethod = RuntimeMethod(nameof(LogBeginCall));
        public static MethodInfo LogEndCallMethod = RuntimeMethod(nameof(LogEndCall));
        public static MethodInfo MakeLambdaClosureMethod = RuntimeMethod(typeof(LambdaDefinition), nameof(LambdaDefinition.MakeLambdaClosure));
        public static MethodInfo NotMethod = RuntimeMethod(nameof(Not));
        public static MethodInfo RestoreFrameMethod = RuntimeMethod(nameof(RestoreFrame));
        public static MethodInfo RestoreStackAndFrameMethod = RuntimeMethod(nameof(RestoreStackAndFrame));
        public static MethodInfo SaveStackAndFrameMethod = RuntimeMethod(nameof(SaveStackAndFrame));
        public static MethodInfo SaveStackAndFrameWithMethod = RuntimeMethod(nameof(SaveStackAndFrameWith));
        public static MethodInfo SetDynamicMethod = RuntimeMethod(nameof(SetDynamic));
        public static MethodInfo SetLexicalMethod = RuntimeMethod(nameof(SetLexical));
        public static MethodInfo ToBoolMethod = RuntimeMethod(nameof(ToBool));
        public static MethodInfo UnwindExceptionMethod = RuntimeMethod(nameof(UnwindException));

        #endregion Static Fields

        #region Public Methods

        public static Expression CallRuntime(ConstructorInfo method, params Expression[] exprs)
        {
            return Expression.New(method, exprs);
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
            if (DebugLevel >= 1)
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

        public static Expression CompileBlock(Cons form, AnalysisScope scope)
        {
            CheckMinLength(form, 2);
            var sym = CheckSymbol(Second(form));
            if (sym.IsDynamic)
            {
                throw new LispException("Invalid block name: {0}", sym);
            }
            return CompileBody(sym, Cddr(form), scope);
        }

        public static bool ContainsFrameDeclarations(Cons forms)
        {
            foreach (var item in forms)
            {
                if (item is Cons)
                {
                    var form = (Cons)item;
                    var sym = form.Car as Symbol;
                    if (sym != null)
                    {
                        switch (sym.Name)
                        {
                            case "var":
                            case "let":
                            case "letfun":
                            case "future":
                            case "lazy":
                                return true;
                            //case "letmacro":
                            //case "let-symbol-macro":
                            default:
                                break;
                        }
                    }
                }
            }

            return false;
        }

        public static Expression CompileBody(Cons forms, AnalysisScope scope)
        {
            return CompileBody(null, forms, scope);
        }

        public static Expression CompileBody(Symbol blockName, Cons forms, AnalysisScope scope)
        {
            //if (forms == null)
            //{
            //    PrintWarning(scope.Name, ": no forms in statement body, using null.");
            //    return CompileLiteral(null);
            //}

            var bodyScope = new AnalysisScope(scope);
            bodyScope.IsBlockScope = true;
            bodyScope.BlockName = blockName;
            if (blockName != null)
            {
                bodyScope.RedoLabel = Expression.Label("redo-label");
                bodyScope.LeaveLabel = Expression.Label("leave-label");
            }
            bodyScope.ResultVar = Expression.Parameter(typeof(object), "%result");

            var bodyExprs = CompileBodyExpressions(forms, bodyScope);

            if (bodyExprs.Count == 0)
            {
                return CompileLiteral(null);
            }

            bodyScope.CheckVariables();
            var code = WrapEvaluationStack(bodyExprs, bodyScope);
            return code;
        }

        public static List<Expression> CompileBodyExpressions(Cons forms, AnalysisScope bodyScope)
        {
            var bodyExprs = new List<Expression>();

            for (var list = forms; list != null; list = list.Cdr)
            {
                var expr = list.Car;
                var code = Compile(expr, bodyScope);
                bodyExprs.Add(code);
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
                var catchScope = new AnalysisScope(scope);
                var var1 = catchScope.DefineVariable(Symbols.Temp, ScopeFlags.All, type);
                var var2 = catchScope.DefineVariable(sym, ScopeFlags.All);
                var code = Expression.Catch(var1,
                               Expression.Block(typeof(object),
                                   new ParameterExpression[] { var2 },
                                   CallRuntime(RestoreFrameMethod, saved),
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
            foreach (var item in Cdr(form))
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
                                foreach (Symbol sym in Cdr(declaration))
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
                                foreach (Symbol sym in Cdr(declaration))
                                {
                                    scope.FindLocal(sym, ScopeFlags.Ignorable);
                                }
                                break;
                            }
                        case "dynamic":
                            {
                                foreach (Symbol sym in Cdr(declaration))
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
            WarnWhenShadowing("def", sym);
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
            WarnWhenShadowing("defconstant", sym);
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
            WarnWhenShadowing("define-compiler-macro", sym);
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
            WarnWhenShadowing("define-symbol-macro", sym);
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
            WarnWhenShadowing("defmacro", sym);
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
            WarnWhenShadowing("defmethod", sym);
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
            WarnWhenShadowing("defmulti", sym);
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
            WarnWhenShadowing("defun", sym);
            string doc;
            var lambda = CompileLambdaDef(sym, Cddr(form), scope, LambdaKind.Function, out doc);
            return CallRuntime(DefineFunctionMethod, Expression.Constant(sym), lambda, Expression.Constant(doc, typeof(string)));
        }

        public static Expression CompileDo(Cons form, AnalysisScope scope)
        {
            return CompileBody(Cdr(form), scope);
        }

        public static Expression CompileDynamicExpression(CallSiteBinder binder, Type returnType, IEnumerable<Expression> args)
        {
            if (AdaptiveCompilation)
            {
                return Microsoft.Scripting.Ast.Utils.LightDynamic(binder, returnType, new ReadOnlyCollectionBuilder<Expression>(args));
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

            if (scope.AnyLabelsCreated)
            {
                throw new LispException("Statement not allowed when labels are already declared: {0}", form);
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
            var signature = new LambdaSignature(kind);
            signature.ArgModifier = null;
            bool wantWholeArgName = false;
            bool wantEnvArgName = false;

            foreach (object item in ToIter(args))
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
                    else if (sym == Symbols.Optional || sym == Symbols.Key || sym == Symbols.Rest
                             || sym == Symbols.Body || sym == Symbols.Params || sym == Symbols.Vector || sym == Symbols.RawParams)
                    {
                        if (signature.ArgModifier != null)
                        {
                            throw new LispException("Only one modifier can be used: &key, &optional, &rest, &body, &vector, &params or &rawparams");
                        }
                        signature.ArgModifier = sym;
                        signature.RequiredArgsCount = signature.Parameters.Count;
                        continue;
                    }
                    else
                    {
                        var arg = new ParameterDef(sym);
                        signature.Parameters.Add(arg);
                        signature.Names.Add(sym);
                    }
                }
                else if (item is Cons)
                {
                    var list = (Cons)item;

                    if (signature.ArgModifier == Symbols.Key || signature.ArgModifier == Symbols.Optional)
                    {
                        var sym = (Symbol)First(list);
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
                        var arg = new ParameterDef(null, nestedParameters: nestedArgs);
                        signature.Parameters.Add(arg);
                        signature.Names.AddRange(nestedArgs.Names);
                    }
                    else if (signature.ArgModifier == null && kind == LambdaKind.Method)
                    {
                        var sym = (Symbol)First(list);
                        var type = Second(list);
                        if (type == null)
                        {
                            var arg = new ParameterDef(sym);
                            signature.Parameters.Add(arg);
                            signature.Names.Add(sym);
                        }
                        else if (type is Cons && First(type) == FindSymbol("lisp:eql"))
                        {
                            // Compile time constants!
                            var expr = Eval(Second(type));
                            var arg = new ParameterDef(sym, specializer: new EqlSpecializer(expr));
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
                            var arg = new ParameterDef(sym, specializer: realType);
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

            //if (kind == LambdaKind.Function)
            {
                var sym = signature.ArgModifier;
                if (sym == Symbols.RawParams)
                {
                    if (signature.Parameters.Count != 1)
                    {
                        throw new LispException("&rawparams: parameter count must be one.");
                    }
                }
                else if (sym == Symbols.Rest || sym == Symbols.Body || sym == Symbols.Params || sym == Symbols.Vector)
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
            //if (DebugLevel == 0)
            {
                var expr = Optimizer(form);
                if (expr != form)
                {
                    return Expression.Constant(expr, typeof(object));
                }
            }

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
                var args = new List<Expression>();
                args.Add(func);
                foreach (object a in ToIter(formArgs))
                {
                    args.Add(Compile(a, scope));
                }
                var binder = GetInvokeBinder(Length(formArgs));
                var code = CompileDynamicExpression(binder, typeof(object), args);

                if (DebugLevel >= 1)
                {
                    return WrapEvaluationCallStack(code, form);
                }
                else
                {
                    return code;
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
                return CompileDynamicExpression(binder, typeof(object), new Expression[] { target });
            }
            else if (member is Cons && First(member) == Symbols.Quote && Second(member) is Symbol)
            {
                var name = ((Symbol)Second(member)).Name;
                var target = Compile(Second(form), scope);
                var binder = GetGetMemberBinder(name);
                return CompileDynamicExpression(binder, typeof(object), new Expression[] { target });
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
                var entry = scope.FindLocal(sym, ScopeFlags.Referenced);
                if (entry != null)
                {
                    Expression code;

                    if (entry.Parameter == null)
                    {
                        throw new LispException("{0}: cannot compile reference to (symbol)macro variable", sym);
                    }

                    if (entry.HoistIndex != -1)
                    {
                        code = CallRuntime(GetLexicalMethod, scope.LambdaParent.HoistedArgs, Expression.Constant(entry.HoistIndex));
                    }
                    else
                    {
                        code = entry.Parameter;
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

        public static Expression CompileIf(Cons form, AnalysisScope scope)
        {
            // if expr expr [expr]
            CheckMinLength(form, 3);
            CheckMaxLength(form, 4);
            var testExpr = Compile(Second(form), scope);
            var thenExpr = EnsureObjectResult(Compile(Third(form), scope));
            var elseExpr = EnsureObjectResult(Compile(Fourth(form), scope));
            var test = WrapBooleanTest(testExpr);
            return Expression.Condition(test, thenExpr, elseExpr);
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

        public static Expression CompileLambdaDef(Symbol name, Cons forms, AnalysisScope scope, LambdaKind kind, out string doc)
        {
            name = name ?? Symbols.Anonymous;

            CheckMinLength(forms, 1);

            var args = (Cons)First(forms);
            var body = Cdr(forms);
            var funscope = new AnalysisScope(scope);
            funscope.LambdaParent = funscope;
            funscope.Name = name;
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

            template.Source = MakeListStar(Symbols.Lambda, args, body);

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

            var lambdaListNative = funscope.DefineVariable(Symbols.LambdaList, ScopeFlags.All);
            var selfNative = funscope.DefineVariable(Symbols.Self, ScopeFlags.All);
            var argsNative = funscope.DefineVariable(Symbols.Args, ScopeFlags.All);
            var hoistedArgsNative = funscope.HoistedArgs = funscope.DefineVariable(Symbols.HoistedArgs, ScopeFlags.All);
            Expression code;

            var rawparams = template.Signature.ArgModifier == Symbols.RawParams;
            var names = GetLambdaArgumentNames(template.Signature);
            var decls = GetDeclarations(rawparams, names, Symbols.Args);
            var body4 = FormatCode(LambdaTemplate, decls, body);
            code = Compile(body4, funscope);

            template.Code = code;
            template.Proc = CompileToFunction4(code, lambdaListNative, selfNative, argsNative, hoistedArgsNative);
            template.HoistNames = funscope.HoistNames;

            var hoisted = Expression.RuntimeVariables(funscope.HoistParameters);
            return CallRuntime(MakeLambdaClosureMethod, Expression.Constant(template, typeof(LambdaDefinition)), hoisted);
        }

        public static Expression CompileLazyVar(Cons form, AnalysisScope scope)
        {
            return CompileVarInScope(form, scope, lazy: true, constant: true);
        }

        public static Expression CompileLeave(Cons form, AnalysisScope scope)
        {
            // leave symbol [value]
            CheckMinLength(form, 2);
            CheckMaxLength(form, 3);
            var tag = CheckSymbol(Second(form));
            var blockScope = FindBlockScope(scope, tag);
            if (blockScope == null)
            {
                throw new LispException("Block {0} not found", tag);
            }
            blockScope.LeaveLabelUsed = true;
            var value = Compile(Third(form), scope);
            var code = Expression.Block(
                Expression.Assign(blockScope.ResultVar, value),
                CallRuntime(RestoreStackAndFrameMethod, blockScope.SavedState),
                Expression.Goto(blockScope.LeaveLabel, value, typeof(object)));
            return code;
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
            if (scope.FindDuplicate(sym))
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
            if (scope.FindDuplicate(sym))
            {
                throw new LispException("Duplicate declaration of variable: {0}", sym);
            }
            var macro = new SymbolMacro(Third(form));
            scope.DefineMacro(sym, macro, ScopeFlags.SymbolMacro | ScopeFlags.All);
            return Expression.Constant(null);
        }

        public static Expression CompileLexicalVarInScope(Cons form, AnalysisScope scope, bool future, bool lazy, bool constant)
        {
            // var sym [expr]
            var sym = (Symbol)Second(form);
            var initForm = Third(form);

            if (!scope.IsBlockScope)
            {
                throw new LispException("Statement requires block scope: {0}", form);
            }
            if (scope.FindDuplicate(sym))
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

            var parameter = scope.DefineVariable(sym, flags);
            return Expression.Assign(parameter, val);
        }

        public static Expression CompileLiteral(object expr)
        {
            return Expression.Constant(expr, typeof(object));
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

        public static Expression CompileMergingDo(Cons forms, AnalysisScope scope)
        {
            var body = Cdr(forms);
            if (body == null)
            {
                throw new LispException("Merging-do body cannot be empty");
            }
            if (scope.IsBlockScope && body != null)
            {
                // compile into outer block scope
                var expressions = CompileBodyExpressions(body, scope);
                Expression block = Expression.Block(typeof(object), expressions);
                return block;
            }
            else
            {
                // compile into new block scope
                return CompileBody(body, scope);
            }
        }

        public static Expression CompileProg(Cons form, AnalysisScope scope)
        {
            CheckMinLength(form, 2);

            if (Length(form) == 2)
            {
                PrintWarning("Prog body contains no forms.");
            }

            var bindings = Second(form);
            var body = Cddr(form);
            var names = new Vector();
            var values = new Vector();

            foreach (var binding in ToIter(bindings))
            {
                object name;
                object value;

                if (Listp(binding))
                {
                    name = First(binding);
                    value = Second(binding);
                }
                else
                {
                    name = binding;
                    value = null;
                }

                if (!Symbolp(name))
                {
                    throw new LispException("prog: invalid binding list: {0}", bindings);
                }

                names.Add(name);
                values.Add(value);
            }
            var args = MakeListStar(Symbols.Array, values);
            var decls = GetDeclarations(false, names, Symbols.RecursionArgs);
            var body4 = FormatCode(ProgTemplate, args, decls, body);
            return Compile(body4, scope);
        }

        public static Expression CompileQuote(Cons form, AnalysisScope scope)
        {
            CheckMinLength(form, 2);
            return CompileLiteral(Second(form));
        }

        public static Expression CompileRecur(Cons form, AnalysisScope scope)
        {
            // recur value*
            var progScope = FindBlockScope(scope, Symbols.Lambda);
            if (progScope == null)
            {
                throw new LispException("Block LAMBDA not found");
            }
            var values = Cdr(form);
            var code = MakeList(Symbols.Do,
                                MakeList(Symbols.Setq, Symbols.RecursionArgs, MakeListStar(MakeSymbol("array"), values)),
                                MakeList(Symbols.Redo, Symbols.Lambda));
            return Compile(code, scope);
        }

        public static Expression CompileRedo(Cons form, AnalysisScope scope)
        {
            // redo symbol
            CheckLength(form, 2);
            var tag = CheckSymbol(Second(form));
            var blockScope = FindBlockScope(scope, tag);
            if (blockScope == null)
            {
                throw new LispException("Block {0} not found", tag);
            }
            blockScope.RedoLabelUsed = true;
            var value = CompileLiteral(null);
            var code = Expression.Block(
               CallRuntime(RestoreStackAndFrameMethod, blockScope.SavedState),
               Expression.Goto(blockScope.RedoLabel, value, typeof(object)));
            return code;
        }

        public static Expression CompileReturn(Cons form, AnalysisScope scope)
        {
            // return [expr]
            CheckMaxLength(form, 2);

            var returnScope = FindReturnScope(scope);

            if (returnScope == null)
            {
                throw new LispException("Invalid use of RETURN.");
            }

            if (returnScope.IsFileScope)
            {
                return Compile(MakeList(Symbols.ReturnFromLoad), scope);
            }
            else
            {
                var value = Cadr(form);
                return Compile(MakeList(Symbols.Leave, Symbols.Lambda, value), scope);
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
                var entry = scope.FindLocal(sym, ScopeFlags.Assigned);
                if (entry != null)
                {
                    if (entry.Parameter == null)
                    {
                        throw new LispException("{0}: cannot compile reference to (symbol)macro variable", sym);
                    }

                    var constant = (entry.Flags & ScopeFlags.Constant) != 0;

                    if (constant)
                    {
                        throw new LispException("Cannot assign to a constant, future or lazy variable: {0}", sym);
                    }

                    if (entry.HoistIndex != -1)
                    {
                        return CallRuntime(SetLexicalMethod, scope.LambdaParent.HoistedArgs, Expression.Constant(entry.HoistIndex), value);
                    }
                    else
                    {
                        return Expression.Assign(entry.Parameter, value);
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

        public static Delegate CompileToDelegate(Expression expr, params ParameterExpression[] parameters)
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

        public static Func<Cons, object, object[], object, object> CompileToFunction4(Expression expr, params ParameterExpression[] parameters)
        {
            return (Func<Cons, object, object[], object, object>)CompileToDelegate(expr, parameters);
        }

        public static Expression CompileTry(Cons form, AnalysisScope scope)
        {
            var tryForms = new Vector();
            var catchForms = new List<Cons>();
            var cleanupForms = new Vector();
            foreach (var expr in Cdr(form))
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
            var saved = Expression.Parameter(typeof(ThreadContextState), "%saved");

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
            var saved = Expression.Parameter(typeof(ThreadContextState), "%saved");
            var saved2 = Expression.Parameter(typeof(ThreadContextState), "%saved2");

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

        public static Expression CompileVarInScope(Cons form, AnalysisScope scope, bool future = false, bool lazy = false, bool constant = false)
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
                return CompileLexicalVarInScope(form, scope, future, lazy, constant);
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

        public static AnalysisScope FindReturnScope(AnalysisScope scope)
        {
            var curscope = scope;
            while (curscope != null)
            {
                if (curscope.IsLambda || curscope.IsFileScope)
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

        public static Cons FormatCode(string template, params object[] args)
        {
            var form = (Cons)ReadFromString(template);
            return FormatCode(form, args);
        }

        public static Cons FormatCode(Cons template, params object[] args)
        {
            var result = new Vector();
            foreach (var item in template)
            {
                if (item is Symbol)
                {
                    var sym = (Symbol)item;
                    if (sym.Name.StartsWith("%%"))
                    {
                        int index;
                        if (int.TryParse(sym.Name.Substring(2), out index))
                        {
                            result.AddRange((Cons)args[index - 1]);
                            continue;
                        }
                    }
                    else if (sym.Name.StartsWith("%"))
                    {
                        int index;
                        if (int.TryParse(sym.Name.Substring(1), out index))
                        {
                            result.Add(args[index - 1]);
                            continue;
                        }
                    }
                }

                if (item is Cons)
                {
                    result.Add(FormatCode((Cons)item, args));
                    continue;
                }
                result.Add(item);
            }
            return AsList(result);
        }

        public static ImportedFunction GetBuiltinFunction(string name)
        {
            var sym = FindSymbol(name);
            return (ImportedFunction)sym.Value;
        }

        public static Cons GetDeclarations(bool rawparams, Vector names, object values)
        {
            var i = 0;
            var v = new Vector();

            if (rawparams)
            {
                v.Add(MakeList(Symbols.Var, names[0], values));
            }
            else
            {
                foreach (var name in names)
                {
                    v.Add(MakeList(Symbols.Var, name, MakeList(Symbols.GetElt, values, i)));
                    ++i;
                }
            }
            return AsList(v);
        }

        public static Vector GetLambdaArgumentNames(LambdaSignature signature)
        {
            var v = new Vector();
            for (var i = 0; i < signature.Names.Count; ++i)
            {
                if (signature.Names[i] != Symbols.Underscore)
                {
                    if (i >= signature.Parameters.Count || !signature.Parameters[i].Hidden)
                    {
                        v.Add(signature.Names[i]);
                    }
                }
            }
            if (signature.WholeArg != null)
            {
                v.Add(signature.WholeArg);
            }
            if (signature.EnvArg != null)
            {
                v.Add(signature.EnvArg);
            }
            return v;
        }

        public static void InitLambdaTemplates()
        {
            LambdaTemplate = (Cons)ReadFromString(@"
					(block lambda
                        %%1
                        (do %%2))");

            ProgTemplate = (Cons)ReadFromString(@"
                    (block lambda
                        (var %recursion-args %1)
                        %%2
                        (do %%3))");

        }

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

        [Lisp("system:parse-parameter-list")]
        public static Cons ParseParameterList(Cons args)
        {
            var signature = CompileFormalArgs(args, null, LambdaKind.Macro);
            return AsList(signature.Names);
        }

        public static void RestartCompiler()
        {
            Symbols.And.SpecialFormValue = new SpecialForm(CompileAnd);
            Symbols.Block.SpecialFormValue = new SpecialForm(CompileBlock);
            Symbols.CompileTimeBranch.SpecialFormValue = new SpecialForm(CompileMergingDo);
            Symbols.Declare.SpecialFormValue = new SpecialForm(CompileDeclare);
            Symbols.Def.SpecialFormValue = new SpecialForm(CompileDef);
            Symbols.DefConstant.SpecialFormValue = new SpecialForm(CompileDefConstant);
            Symbols.DefineCompilerMacro.SpecialFormValue = new SpecialForm(CompileDefineCompilerMacro);
            Symbols.DefineSymbolMacro.SpecialFormValue = new SpecialForm(CompileDefineSymbolMacro);
            Symbols.DefMacro.SpecialFormValue = new SpecialForm(CompileDefMacro);
            Symbols.DefMethod.SpecialFormValue = new SpecialForm(CompileDefMethod);
            Symbols.DefMulti.SpecialFormValue = new SpecialForm(CompileDefMulti);
            Symbols.Defun.SpecialFormValue = new SpecialForm(CompileDefun);
            Symbols.Do.SpecialFormValue = new SpecialForm(CompileDo);
            Symbols.FutureVar.SpecialFormValue = new SpecialForm(CompileFutureVar);
            Symbols.GetAttr.SpecialFormValue = new SpecialForm(CompileGetMember);
            Symbols.GetElt.SpecialFormValue = new SpecialForm(CompileGetElt);
            Symbols.If.SpecialFormValue = new SpecialForm(CompileIf);
            Symbols.Lambda.SpecialFormValue = new SpecialForm(CompileLambda);
            Symbols.LazyVar.SpecialFormValue = new SpecialForm(CompileLazyVar);
            Symbols.Let.SpecialFormValue = new SpecialForm(CompileLet);
            Symbols.LetFun.SpecialFormValue = new SpecialForm(CompileLetFun);
            Symbols.LetMacro.SpecialFormValue = new SpecialForm(CompileLetMacro);
            Symbols.LetSymbolMacro.SpecialFormValue = new SpecialForm(CompileLetSymbolMacro);
            Symbols.Or.SpecialFormValue = new SpecialForm(CompileOr);
            Symbols.Prog.SpecialFormValue = new SpecialForm(CompileProg);
            Symbols.Quote.SpecialFormValue = new SpecialForm(CompileQuote);
            Symbols.bqQuote.SpecialFormValue = new SpecialForm(CompileQuote);
            Symbols.Recur.SpecialFormValue = new SpecialForm(CompileRecur);
            Symbols.Return.SpecialFormValue = new SpecialForm(CompileReturn);
            Symbols.SetAttr.SpecialFormValue = new SpecialForm(CompileSetMember);
            Symbols.SetElt.SpecialFormValue = new SpecialForm(CompileSetElt);
            Symbols.Setq.SpecialFormValue = new SpecialForm(CompileSetq);
            Symbols.Throw.SpecialFormValue = new SpecialForm(CompileThrow);
            Symbols.Try.SpecialFormValue = new SpecialForm(CompileTry);
            Symbols.Var.SpecialFormValue = new SpecialForm(CompileVar);
            Symbols.Redo.SpecialFormValue = new SpecialForm(CompileRedo);
            Symbols.Leave.SpecialFormValue = new SpecialForm(CompileLeave);

            InitLambdaTemplates();
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

        public static ConstructorInfo RuntimeConstructor(Type type)
        {
            var methods = type.GetConstructors(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return methods.First();
        }

        public static AnalysisScope FindBlockScope(AnalysisScope scope, Symbol blockName)
        {
            var curscope = scope;
            while (curscope != null)
            {
                if (curscope.IsBlockScope)
                {
                    if (curscope.BlockName == blockName)
                    {
                        return curscope;
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
            return null;
        }

        public static Expression WrapBooleanTest(Expression expr)
        {
            return CallRuntime(ToBoolMethod, expr);
        }

        public static Expression WrapEvaluationStack(List<Expression> body, AnalysisScope scope)
        {
            var parameters = scope.Parameters;
            var fullWrap = DebugLevel >= 2 && parameters.Count != 0;
            var partialWrap = scope.UsesDynamicVariables || scope.UsesLabels;

            if (fullWrap || partialWrap)
            {
                // For function, try and loop: anything that has a non-local exit method.
                var saved = scope.SavedState;
                var result = scope.ResultVar;
                var index = Expression.Parameter(typeof(int), "%index");
                var exprs = new List<Expression>();
                var form = Expression.Constant(null, typeof(Cons));
                Expression frame;
                if (fullWrap)
                {
                    frame = CallRuntime(MakeFrameMethod, Expression.Constant(scope.Names), Expression.RuntimeVariables(scope.Parameters));
                }
                else
                {
                    frame = Expression.Constant(null, typeof(Frame));
                }
                body.Insert(0, Expression.Assign(saved, CallRuntime(SaveStackAndFrameWithMethod, frame, form)));
                if (scope.RedoLabelUsed)
                {
                    exprs.Add(Expression.Label(scope.RedoLabel));
                }
                var code = Expression.Block(typeof(object), parameters, body);
                exprs.Add(Expression.Assign(result, code));
                exprs.Add(CallRuntime(RestoreStackAndFrameMethod, saved));
                if (scope.LeaveLabelUsed)
                {
                    exprs.Add(Expression.Label(scope.LeaveLabel));
                }
                exprs.Add(result);

                return Expression.Block
                        (
                    typeof(object),
                    new[] { saved, result, index },
                    exprs
                );
            }
            else
            {
                var code = Expression.Block(typeof(object), parameters, body);
                return code;
            }
        }

        public static Expression WrapEvaluationCallStack(Expression code, Cons form)
        {
            // For function calls
            var saved = Expression.Parameter(typeof(ThreadContextState), "%saved-state");
            var result = Expression.Parameter(typeof(object), "%result");
            var exprs = new List<Expression>();
            Expression frame = Expression.Constant(null, typeof(Frame));
            exprs.Add(Expression.Assign(saved, CallRuntime(SaveStackAndFrameWithMethod, frame, Expression.Constant(form, typeof(Cons)))));
            exprs.Add(Expression.Assign(result, code));
            exprs.Add(CallRuntime(RestoreStackAndFrameMethod, saved));
            exprs.Add(result);

            return Expression.Block
                    (
                typeof(object),
                new[] { saved, result },
                exprs
            );
        }

        #endregion Public Methods
    }

    public class SpecialForm
    {
        #region Fields

        public CompilerHelper Helper;

        #endregion Fields

        #region Constructors

        public SpecialForm(CompilerHelper helper)
        {
            Helper = helper;
        }

        #endregion Constructors

        #region Public Methods

        public override string ToString()
        {
            return string.Format("SpecialForm Name=\"{0}.{1}\"", Helper.Method.DeclaringType, Helper.Method.Name);
        }

        #endregion Public Methods
    }
}