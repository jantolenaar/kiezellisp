// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Diagnostics;

namespace Kiezel
{
    public partial class Runtime
    {
        [Lisp("as-prototype")]
        public static Prototype AsPrototype(object obj)
        {
            return AsPrototype(obj, false);
        }

        [Lisp("as-prototype-ci")]
        public static Prototype AsPrototypeIgnoreCase(object obj)
        {
            return AsPrototype(obj, true);
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
                    case SymbolUsage.SpecialForm:
                    {
                        z["compiler-usage"] = Symbols.SpecialForm;
                        z["compiler-value"] = sym.SpecialFormValue;
                        break;
                    }
                }

                if (!String.IsNullOrWhiteSpace(sym.CompilerDocumentation))
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

                if (!String.IsNullOrWhiteSpace(sym.Documentation))
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
                        case LambdaKind.Function:
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

        public static Exception UnwindException(Exception ex)
        {
            while (ex.InnerException != null && ex is System.Reflection.TargetInvocationException)
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
                buf.WriteLine(exception.ToString());
                buf.WriteLine(new string('=', 80));
            }
            buf.WriteLine("LEXICAL ENVIRONMENT");
            buf.WriteLine(new string('=', 80));
            for (int i = 0;; ++i)
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

        //[Lisp( "get-lexical-variables-dictionary" )]
        public static Prototype GetLexicalVariablesDictionary(int pos)
        {
            Frame frame = GetFrameAt(pos);

            if (frame == null)
            {
                return null;
            }

            var env = new PrototypeDictionary();

            for (; frame != null; frame = frame.Link)
            {
                if (frame.Names != null)
                {
                    for (int i = 0; i < frame.Names.Count; ++i)
                    {
                        var key = frame.Names[i].DiagnosticsName;
                        object value = null;
                        if (frame.Values != null && i < frame.Values.Count)
                        {
                            value = frame.Values[i];
                        }
                        if (key == Symbols.Tilde.Name)
                        {
                            if (frame == CurrentThreadContext.Frame)
                            {
                                env[key] = value;
                            }
                            else if (!env.ContainsKey(key))
                            {
                                env[key] = value;
                            }
                        }
                        else if (!env.ContainsKey(key))
                        {
                            env[key] = value;
                        }
                    }
                }
            }

            return Prototype.FromDictionary(env);
        }

        [Lisp("print-warning")]
        public static void PrintWarning(params object[] args)
        {
            if (DebugMode && ToBool(GetDynamic(Symbols.EnableWarnings)))
            {
                var stream = GetDynamic(Symbols.StdErr);
                var text = ";;; Warning: " + MakeString(args);
                PrintLogColor(stream, "warning", text);
            }
        }

        [Lisp("throw-error")]
        public static void ThrowError(params object[] args)
        {
            var text = MakeString(args);
            throw new LispException(text);
        }

        public static void Assert(bool testResult, params object[] args)
        {
            if (!testResult)
            {
                var text = MakeString(args);
                throw new AssertFailedException(text);
            }
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
                            value = p.GetValue(obj, new object[ 0 ]);
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

        public static void DumpDictionary(object stream, Prototype prototype)
        {
            if (prototype == null)
            {
                return;
            }

            var dict = prototype.Dict;

            foreach (string key in ToIter( SeqBase.Sort( dict.Keys, CompareApply, IdentityApply ) ))
            {
                object val = dict[key];
                string line = String.Format("{0} => ", key);
                Write(line, Symbols.Escape, false, Symbols.Stream, stream);
                PrettyPrintLine(stream, line.Length, null, val);
            }
        }

        public static int GetConsoleWidth()
        {
            return 80;
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

                foreach (object item in ToIter( CurrentThreadContext.EvaluationStack ))
                {
                    // Every function call adds the source code form
                    // Every lambda call adds the outer frame (not null) and specialvariables (maybe null)
                    if (item is Frame)
                    {
                        ++index;
                        prefix = index.ToString() + ":";
                    }
                    else if (item is Cons)
                    {
                        var form = (Cons)item;
                        var leader = prefix.PadLeft(3, ' ');
                        buf.WriteLine("{0} {1}", leader, ToPrintString(form).Shorten(GetConsoleWidth() - leader.Length - 1));
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
            foreach (object item in ToIter( CurrentThreadContext.EvaluationStack ))
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

        public static SpecialVariables GetSpecialVariablesAt(int pos)
        {
            if (pos <= 0)
            {
                return CurrentThreadContext.SpecialStack;
            }

            var index = 0;
            var done = false;
            foreach (object item in ToIter( CurrentThreadContext.EvaluationStack ))
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

        public static void PrintLog(params object[] args)
        {
            var stream = GetDynamic(Symbols.StdErr);
            PrintLogColor(stream, "", MakeString(args));
        }

        public static void PrintLogColor(object stream, string color, string msg)
        {
            if (stream is ILogWriter)
            {
                var log = (ILogWriter)stream;
                log.WriteLog(color, msg);
            }
            else
            {
                WriteLine(msg, Symbols.Stream, stream, Symbols.Escape, false);
            }
        }

  
    }
}