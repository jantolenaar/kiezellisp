#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Diagnostics;

    public struct CpuTime
    {
        public double User;
        public double System;

        public CpuTime(double u, double s)
        {
            User = u;
            System = s;
        }

        public static CpuTime operator -(CpuTime t2, CpuTime t1)
        {
            return new CpuTime(Math.Round(t2.User - t1.User, 3), Math.Round(t2.System - t1.System, 3));
        }
    }

    public partial class Runtime
    {
        #region Public Methods

        public static CpuTime GetCpuTime()
        {
            var p = Process.GetCurrentProcess();
            return new CpuTime(p.UserProcessorTime.TotalSeconds, p.PrivilegedProcessorTime.TotalSeconds);
        }

        [Lisp("as-prototype")]
        public static Prototype AsPrototype(object obj)
        {
            return AsPrototype(obj, false);
        }

        public static Prototype AsPrototype(object obj, bool caseInsensitive)
        {
            if (obj is Prototype)
            {
                var dict = ConvertToLispDictionary(((Prototype)obj).Dict, caseInsensitive);
                return Prototype.FromDictionary(dict);
            }
            else if (obj is IDictionary)
            {
                var dict = ConvertToLispDictionary((IDictionary)obj, caseInsensitive);
                return Prototype.FromDictionary(dict);
            }
            else if (obj is Type)
            {
                var dict = ConvertToDictionary((Type)obj, null, false);
                return Prototype.FromDictionary(dict);
            }
            else
            {
                var dict = ConvertToDictionary(obj.GetType(), obj, false);
                return Prototype.FromDictionary(dict);
            }
        }

        [Lisp("as-prototype-ci")]
        public static Prototype AsPrototypeIgnoreCase(object obj)
        {
            return AsPrototype(obj, true);
        }

        public static void Assert(bool testResult, params object[] args)
        {
            if (!testResult)
            {
                var text = MakeString(args);
                throw new AssertFailedException(text);
            }
        }

        public static PrototypeDictionary ConvertToDictionary(Type type, object obj, bool showNonPublic)
        {
            var flags = BindingFlags.Public | (obj == null ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.FlattenHierarchy;

            if (showNonPublic)
            {
                flags |= BindingFlags.NonPublic;
            }
            var members = type.GetMembers(flags);
            var dict = new PrototypeDictionary();

            foreach (var m in members)
            {
                var name = m.Name;
                object value = null;

                try
                {
                    if (m is PropertyInfo)
                    {
                        var p = (PropertyInfo)m;
                        if (p.GetGetMethod() != null && p.GetGetMethod().GetParameters().Length == 0)
                        {
                            value = p.GetValue(obj, new object[0]);
                            dict[name.LispName()] = value;
                        }
                    }
                    else if (m is FieldInfo)
                    {
                        var f = (FieldInfo)m;
                        value = f.GetValue(obj);
                        dict[name.LispName()] = value;
                    }
                }
                catch
                {
                }
            }

            return dict;
        }

        public static PrototypeDictionary ConvertToLispDictionary(IDictionary obj, bool caseInsensitive)
        {
            var dict = new PrototypeDictionary(caseInsensitive);
            foreach (DictionaryEntry item in obj)
            {
                dict[item.Key] = item.Value;
            }
            return dict;
        }

        [Lisp("describe")]
        public static void Describe(object obj)
        {
            Describe(obj, false);
        }

        [Lisp("describe")]
        public static void Describe(object obj, bool showNonPublic)
        {
            var description = GetDescription(obj, showNonPublic);
            WriteLine(description, Symbols.Pretty, true);
        }

        public static void DumpDictionary(object stream, Prototype prototype)
        {
            if (prototype != null)
            {
                DumpDictionary(stream, prototype.Dict);
            }
        }

        public static void DumpDictionary(object stream, PrototypeDictionary dict)
        {
            if (dict != null)
            {
                foreach (var key in ToIter(SeqBase.Sort(dict.Keys, CompareApply, IdentityApply)))
                {
                    object val = dict[key];
                    string line = string.Format("{0} => ", key);
                    Write(line, Symbols.Escape, false, Symbols.Stream, stream);
                    PrettyPrintLine(stream, line.Length, null, val);
                }
            }
        }

        [Lisp("get-description")]
        public static Prototype GetDescription(object obj)
        {
            return GetDescription(obj, false);
        }

        [Lisp("get-description")]
        public static Prototype GetDescription(object obj, bool showNonPublic)
        {
            var result = new Prototype();
            var z = result.Dict;
            var sym = obj as Symbol;
            var isVariable = false;
            object runtimeValue;

            if (sym == null)
            {
                runtimeValue = obj;
            }
            else
            {
                runtimeValue = sym.Value;

                z["symbol"] = sym;
                z["name"] = sym.Name;
                z["package"] = sym.Package == null ? null : sym.Package.Name;

                if (sym.Package != null)
                {
                    z["public"] = sym.IsPublic;
                }

                switch (sym.CompilerUsage)
                {
                    case SymbolUsage.CompilerMacro:
                        {
                            z["compiler-usage"] = Symbols.CompilerMacro;
                            z["compiler-value"] = sym.MacroValue;
                            var b = ((ISyntax)sym.MacroValue).GetSyntax(sym);
                            if (b != null)
                            {
                                z["compiler-syntax"] = b;
                            }
                            break;
                        }
                    case SymbolUsage.Macro:
                        {
                            z["compiler-usage"] = Symbols.Macro;
                            z["compiler-value"] = sym.MacroValue;
                            var b = ((ISyntax)sym.MacroValue).GetSyntax(sym);
                            if (b != null)
                            {
                                z["compiler-syntax"] = b;
                            }
                            break;
                        }
                    case SymbolUsage.SymbolMacro:
                        {
                            z["compiler-usage"] = Symbols.SymbolMacro;
                            z["compiler-value"] = sym.SymbolMacroValue;
                            break;
                        }
                    case SymbolUsage.SpecialForm:
                        {
                            z["compiler-usage"] = Symbols.SpecialForm;
                            z["compiler-value"] = sym.SpecialFormValue;
                            break;
                        }
                }

                if (!string.IsNullOrWhiteSpace(sym.CompilerDocumentation))
                {
                    z["compiler-documentation"] = sym.CompilerDocumentation;
                }

                switch (sym.Usage)
                {
                    case SymbolUsage.None:
                        {
                            z["usage"] = Symbols.Undefined;
                            return result;
                        }
                    case SymbolUsage.Constant:
                        {
                            if (sym.IsDynamic)
                            {
                                z["usage"] = Symbols.SpecialConstant;
                                isVariable = true;
                            }
                            else
                            {
                                z["usage"] = Symbols.Constant;
                                isVariable = true;
                            }
                            break;
                        }
                    case SymbolUsage.ReadonlyVariable:
                        {
                            if (sym.IsDynamic)
                            {
                                z["usage"] = Symbols.SpecialReadonlyVariable;
                                isVariable = true;
                            }
                            else
                            {
                                z["usage"] = Symbols.ReadonlyVariable;
                                isVariable = true;
                            }
                            break;
                        }
                    case SymbolUsage.Variable:
                        {
                            if (sym.IsDynamic)
                            {
                                z["usage"] = Symbols.SpecialVariable;
                                isVariable = true;
                            }
                            else
                            {
                                z["usage"] = Symbols.Variable;
                                isVariable = true;
                            }
                            break;
                        }
                    case SymbolUsage.Function:
                        {
                            z["usage"] = Symbols.Function;
                            break;
                        }
                }

                if (!string.IsNullOrWhiteSpace(sym.Documentation))
                {
                    z["documentation"] = sym.Documentation;
                }

            }

            if (!(runtimeValue is ICollection) || (runtimeValue is IList))
            {
                z["value"] = runtimeValue;
            }

            if (Nullp(runtimeValue))
            {
                return result;
            }
            else if (!(runtimeValue is ICollection) || (runtimeValue is IList))
            {
                z["type"] = runtimeValue.GetType().ToString();
            }

            Symbol usage = null;

            if (!isVariable)
            {
                if (runtimeValue is ISyntax)
                {
                    var b = ((ISyntax)runtimeValue).GetSyntax(obj as Symbol);
                    if (b != null)
                    {
                        z["function-syntax"] = b;
                    }
                }

                if (runtimeValue is MultiMethod)
                {
                    usage = Symbols.GenericFunction;
                }
                else if (runtimeValue is ImportedConstructor)
                {
                    var kiezel = ((ImportedConstructor)runtimeValue).HasKiezelMethods;
                    usage = kiezel ? Symbols.BuiltinConstructor : Symbols.ImportedConstructor;
                }
                else if (runtimeValue is ImportedFunction)
                {
                    var kiezel = ((ImportedFunction)runtimeValue).HasKiezelMethods;
                    usage = kiezel ? Symbols.BuiltinFunction : Symbols.ImportedFunction;
                }
                else if (runtimeValue is LambdaClosure)
                {
                    var l = (LambdaClosure)runtimeValue;

                    //z[ "function-source" ] = l.Definition.Source;

                    switch (l.Kind)
                    {
                        case LambdaKind.Method:
                            {
                                usage = Symbols.Method;
                                break;
                            }
                        default:
                            {
                                usage = Symbols.Function;
                                break;
                            }
                    }
                }
            }

            if (obj is Symbol && usage != null)
            {
                z["usage"] = usage;
            }

            if (runtimeValue is Prototype)
            {
                var p = (Prototype)runtimeValue;
                z["type-specifier"] = p.GetTypeSpecifier();
            }
            else
            {
                var dict = AsPrototype(runtimeValue);

                if (dict.Dict.Count != 0)
                {
                    z["members"] = dict;
                }
            }

            return result;
        }

        [Lisp("get-diagnostics")]
        public static string GetDiagnostics(Exception exception)
        {
            var buf = new StringWriter();
            if (exception != null)
            {
                var ex2 = UnwindException(exception);
                buf.WriteLine("EXCEPTION");
                buf.WriteLine(new string('=', 80));
                buf.WriteLine(ex2.Message);
                buf.WriteLine(new string('=', 80));
                buf.WriteLine(exception);
                buf.WriteLine(new string('=', 80));
            }
            buf.WriteLine("LEXICAL ENVIRONMENT");
            buf.WriteLine(new string('=', 80));
            for (var i = 0; ; ++i)
            {
                var obj = GetLexicalVariablesDictionary(i);
                if (obj == null)
                {
                    break;
                }
                DumpDictionary(buf, obj);
                buf.WriteLine(new string('=', 80));
            }
            buf.WriteLine("DYNAMIC ENVIRONMENT");
            buf.WriteLine(new string('=', 80));
            DumpDictionary(buf, GetDynamicVariablesDictionary(0));
            buf.WriteLine(new string('=', 80));
            buf.WriteLine("EVALUATION STACK");
            buf.WriteLine(new string('=', 80));
            buf.Write(GetEvaluationStack());
            return buf.ToString();
        }

        //[Lisp( "get-dynamic-variables-dictionary" )]
        public static Prototype GetDynamicVariablesDictionary(int pos)
        {
            var env = new PrototypeDictionary();

            for (var entry = GetSpecialVariablesAt(pos); entry != null; entry = entry.Link)
            {
                var key = entry.Sym.DiagnosticsName;
                if (!env.ContainsKey(key))
                {
                    env[key] = entry.Value;
                }
            }

            return Prototype.FromDictionary(env);
        }

        public static string GetEvaluationStack()
        {
            var index = 0;
            var prefix = "";
            var buf = new StringWriter();
            var saved = SaveStackAndFrame();
            try
            {
                DefDynamic(Symbols.PrintCompact, true);

                foreach (object item in ToIter(CurrentThreadContext.EvaluationStack))
                {
                    // Every function call adds the source code form
                    // Every lambda call adds the outer frame (not null) and specialvariables (maybe null)
                    if (item is Frame)
                    {
                        ++index;
                        prefix = index + ":";
                    }
                    else if (item is Cons)
                    {
                        var form = (Cons)item;
                        var leader = prefix.PadLeft(3, ' ');
                        buf.WriteLine("{0} {1}", leader, ToPrintString(form).Shorten(80 - leader.Length - 1));
                        prefix = "";
                    }
                }
            }
            finally
            {
                RestoreStackAndFrame(saved);
            }
            return buf.ToString();
        }

        public static Frame GetFrameAt(int pos)
        {
            if (pos <= 0)
            {
                return CurrentThreadContext.Frame;
            }

            var index = 0;
            foreach (object item in ToIter(CurrentThreadContext.EvaluationStack))
            {
                if (item is Frame)
                {
                    ++index;
                    if (pos == index)
                    {
                        return (Frame)item;
                    }
                }
            }
            return null;
        }

        [Lisp("get-global-symbols")]
        public static Cons GetGlobalSymbols()
        {
            var env = new HashSet<Symbol>();
            foreach (var package in Packages.Values)
            {
                if (package.Name != "")
                {
                    foreach (Symbol sym in package.Dict.Values)
                    {
                        var name = sym.DiagnosticsName;

                        if (!sym.IsUndefined)
                        {
                            env.Add(sym);
                        }
                    }
                }
            }

            return AsList(env.ToArray());
        }

        //[Lisp( "get-global-variables-dictionary" )]
        public static Prototype GetGlobalVariablesDictionary()
        {
            return GetGlobalVariablesDictionary("");
        }

        //[Lisp( "get-global-variables-dictionary" )]
        public static Prototype GetGlobalVariablesDictionary(string pattern)
        {
            var env = new PrototypeDictionary();
            var pat = (pattern ?? "").ToLower();

            foreach (var package in Packages.Values)
            {
                if (package.Name != "")
                {
                    foreach (Symbol sym in package.Dict.Values)
                    {
                        var name = sym.DiagnosticsName;

                        if (!sym.IsUndefined && (pattern == null || name.ToLower().IndexOf(pat) != -1))
                        {
                            env[name] = sym.Value;
                        }
                    }
                }
            }

            return Prototype.FromDictionary(env);
        }

        public static PrototypeDictionary GetLexicalVariablesDictionary(int pos)
        {
            Frame frame = GetFrameAt(pos);
            return (frame == null) ? new PrototypeDictionary() : frame.GetDictionary();
        }

        public static SpecialVariables GetSpecialVariablesAt(int pos)
        {
            if (pos <= 0)
            {
                return CurrentThreadContext.SpecialStack;
            }

            var index = 0;
            var done = false;
            foreach (object item in ToIter(CurrentThreadContext.EvaluationStack))
            {
                if (item is Frame)
                {
                    ++index;
                    if (pos == index)
                    {
                        done = true;
                    }
                }
                else if (done)
                {
                    return (SpecialVariables)item;
                }
            }
            return null;
        }

        public static object GetSyntax(object a)
        {
            var z = GetDescription(a);
            return z.GetValue("function-syntax");
        }

        [Lisp("print-error")]
        public static void PrintError(params object[] args)
        {
            var stream = GetDynamic(Symbols.StdErr);
            PrintStream(stream, "error", MakeString(args) + "\n");
        }

        public static void PrintStream(object stream, string style, string msg)
        {
            Write(msg, Symbols.Stream, stream, Symbols.Escape, false);
        }

        [Lisp("print-trace")]
        public static void PrintTrace(params object[] args)
        {
            var stream = GetDynamic(Symbols.StdLog);
            PrintStream(stream, "info", MakeString(args) + "\n");
        }

        [Lisp("print-warning")]
        public static void PrintWarning(params object[] args)
        {
            if (ToBool(GetDynamic(Symbols.EnableWarnings)))
            {
                var stream = GetDynamic(Symbols.StdErr);
                var text = "Warning: " + MakeString(args) + "\n";
                PrintStream(stream, "warning", text);
            }
        }

        [Lisp("set-debug-level")]
        public static void SetDebugLevel(int level)
        {
            DebugLevel = Math.Max(0, Math.Min(level, 2));
            Symbols.Debugging.ConstantValue = (DebugLevel == 2);
        }

        [Lisp("throw-error")]
        public static void ThrowError(params object[] args)
        {
            var text = MakeString(args);
            throw new LispException(text);
        }

        public static Exception UnwindException(Exception ex)
        {
            while (ex.InnerException != null && ex is TargetInvocationException)
            {
                ex = ex.InnerException;
            }
            return ex;
        }

        public static Exception UnwindExceptionIntoNewException(Exception ex)
        {
            var ex2 = UnwindException(ex);
            string str = GetDiagnostics(ex2).Indent(">>> ");
            var ex3 = new LispException(str, ex2);
            return ex3;
        }

        #endregion Public Methods
    }
}