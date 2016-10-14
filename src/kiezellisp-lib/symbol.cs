// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Text;

namespace Kiezel
{
    public enum SymbolUsage
    {
        None,
        Variable,
        Constant,
        ReadonlyVariable,
        Function,
        SpecialForm,
        Macro,
        SymbolMacro,
        CompilerMacro
    }

    public partial class Runtime
    {
        [Lisp("get-designated-string")]
        public static string GetDesignatedString(object target)
        {
            if (target == null)
            {
                return "";
            }
            else if (target is string)
            {
                return (string)target;
            }
            else
            {
                return SymbolName(target);
            }
        }

        public static Symbol CheckReadVariable(object target)
        {
            var sym = CheckSymbol(target);
            if (sym.Usage == SymbolUsage.None)
            {
                ThrowError("Undefined variable: ", sym.ContextualName);
            }
            return sym;
        }

        public static Symbol CheckWriteVariable(object target)
        {
            var sym = CheckReadVariable(target);
            if (sym.Usage != SymbolUsage.Variable)
            {
                switch (sym.Usage)
                {
                    case SymbolUsage.Constant:
                    {
                        ThrowError("Cannot assign to constant: ", sym.ContextualName);
                        break;
                    }
                    case SymbolUsage.Function:
                    {
                        ThrowError("Cannot assign to function: ", sym.ContextualName);
                        break;
                    }
                    case SymbolUsage.ReadonlyVariable:
                    {
                        ThrowError("Cannot assign to readonly variable: ", sym.ContextualName);
                        break;
                    }

                }
            }
            return sym;
        }

        [Lisp("undef")]
        public static void Undef(object target)
        {
            var sym = CheckSymbol(target);
            EraseCompilerValue(sym);
            EraseVariable(sym);
        }

        [Lisp("set")]
        public static object Set(object var, object val)
        {
            var sym = CheckWriteVariable(var);
            if (sym.IsDynamic)
            {
                SetDynamic(sym, val);
            }
            else
            {
                EraseCompilerValue(sym);
                sym.CheckedValue = val;
            }
            return val;
        }

        [Lisp("set-symbol-value")]
        public static object SetSymbolValue(object target, object value)
        {
            var sym = CheckWriteVariable(target);
            EraseCompilerValue(sym);
            sym.Value = value;
            return value;
        }

        [Lisp("symbol-name")]
        public static string SymbolName(object target)
        {
            var sym = CheckSymbol(target);
            return sym.Name;
        }

        [Lisp("symbol-package")]
        public static Package SymbolPackage(object target)
        {
            var sym = CheckSymbol(target);
            return sym.Package;
        }

        [Lisp("symbol-value")]
        public static object SymbolValue(object target)
        {
            var sym = CheckReadVariable(target);
            return sym.CheckedValue;
        }

        public static object DefineConstant(Symbol sym, object value, string doc)
        {
            EraseCompilerValue(sym);
            sym.ConstantValue = value;
            sym.Documentation = doc;
            return sym;
        }

        public static void EraseCompilerValue(Symbol sym)
        {
            sym.CompilerMacroValue = null;
            sym.CompilerDocumentation = null;
            sym.CompilerUsage = SymbolUsage.None;
        }

        public static void EraseVariable(Symbol sym)
        {
            sym.Value = null;
            sym.Documentation = null;
            sym.Usage = SymbolUsage.None;
        }

        public static object DefineFunction(Symbol sym, object value, string doc)
        {
            EraseCompilerValue(sym);
            sym.FunctionValue = value;
            sym.Documentation = doc;
            return sym;
        }

        public static object DefineCompilerMacro(Symbol sym, LambdaClosure value, string doc)
        {
            if (!Functionp(sym.Value))
            {
                ThrowError("Cannot define compiler macro for non-function {0}", sym);
            }
            sym.CompilerMacroValue = value;
            sym.CompilerDocumentation = doc;
            return sym;
        }

        public static object DefineSymbolMacro(Symbol sym, SymbolMacro value, string doc)
        {
            EraseVariable(sym);
            sym.SymbolMacroValue = value;
            sym.CompilerDocumentation = doc;
            return sym;
        }

        public static object DefineMacro(Symbol sym, LambdaClosure value, string doc)
        {
            EraseVariable(sym);
            sym.MacroValue = value;
            sym.CompilerDocumentation = doc;
            return sym;
        }

        public static object DefineVariable(Symbol sym, object value, string doc)
        {
            EraseCompilerValue(sym);
            sym.VariableValue = value;
            sym.Documentation = doc;
            return sym;
        }

        public static void WarnWhenShadowing(Symbol sym)
        {
            var package = CurrentPackage();
            if (sym.Package != package && sym.Package != UserPackage)
            {
                PrintWarning("defining symbol ", sym.Name, " shadows ", sym.LongName);
            }
        }
    }

    public class Symbol : IPrintsValue, IApply
    {
        public object _value;
        public string Documentation;
        public string Name;
        public Package Package;
        public Cons PropList;
        public SymbolUsage Usage;

        public object _compilerValue;
        public string CompilerDocumentation;
        public SymbolUsage CompilerUsage;

        public Symbol(string name, Package package = null)
        {
            Name = name;
            if (Name[0] == '$')
            {
                IsDynamic = true;
            }
            else if (Name.Length >= 3 && Name[0] == '*' && Name[Name.Length - 1] == '*')
            {
                IsDynamic = true;
            }
            else
            {
                IsDynamic = false;
            }

            PropList = null;
            Package = package;

            if (package == Runtime.KeywordPackage)
            {
                _value = this;
                Usage = SymbolUsage.Constant;
            }
            else
            {
                _value = null;
                Usage = SymbolUsage.None;
            }
        }

        public string ContextualName
        {
            get
            {
                if (Package == null)
                {
                    return "#:" + Name;
                }
                else if (Package == Runtime.KeywordPackage)
                {
                    return ":" + Name;
                }
                else if (Runtime.ToBool(Runtime.GetDynamic(Symbols.PrintShortSymbolNames)))
                {
                    return Name;
                }
                else if (Package == Runtime.LispPackage || Package == Runtime.LispDocPackage)
                {
                    return Name;
                }
                else if (Runtime.CurrentPackage().Find(Name) == this)
                {
                    return Name;
                }
                else
                {
                    return LongName;
                }
            }
        }

        public object CheckedValue
        {
            get
            {
                Runtime.CheckReadVariable(this);
                return _value;
            }

            set
            {
                Runtime.CheckWriteVariable(this);
                _value = value;
            }
        }

        public object ConstantValue
        {
            set
            {
                _value = value;
                Usage = SymbolUsage.Constant;
            }
        }

        public string DiagnosticsName
        {
            get
            {
                return ContextualName;
            }
        }

        public object FunctionValue
        {
            set
            {
                _value = value;
                Usage = SymbolUsage.Function;
            }
        }

        public bool SuppressWarnings
        {
            get
            {
                return this.Package == Runtime.TempPackage
                || Name.StartsWith("_")
                || Name.StartsWith("~")
                || Name.StartsWith("%");
            }
        }

        public bool IsConstant
        {
            get
            {
                return Usage == SymbolUsage.Constant;
            }
        }

        public bool IsDynamic
        {
            get;
            internal set;
        }

        public bool IsFunction
        {
            get
            {
                return Usage == SymbolUsage.Function;
            }
        }

        public bool IsReadonlyVariable
        {
            get
            {
                return Usage == SymbolUsage.ReadonlyVariable;
            }
        }

        public bool IsUndefined
        {
            get
            {
                return Usage == SymbolUsage.None;
            }
        }

        public bool IsVariable
        {
            get
            {
                return Usage == SymbolUsage.Variable;
            }
        }

        public object LessCheckedValue
        {
            get
            {
                Runtime.CheckReadVariable(this);
                return _value;
            }

            set
            {
                if (IsUndefined)
                {
                    Usage = SymbolUsage.Variable;
                }
                Runtime.CheckWriteVariable(this);
                _value = value;
            }
        }

        public string LongName
        {
            get
            {
                if (Package == null)
                {
                    return "#:" + Name;
                }
                else if (Package.FindExported(Name) != null)
                {
                    return Package.Name + ":" + Name;
                }
                else
                {
                    return Package.Name + "::" + Name;
                }
            }
        }

        public object ReadonlyValue
        {
            set
            {
                _value = value;
                Usage = SymbolUsage.ReadonlyVariable;
            }
        }

        public SpecialForm SpecialFormValue
        {
            get
            {
                return _compilerValue as SpecialForm;
            }

            set
            {
                _compilerValue = value;
                CompilerUsage = SymbolUsage.SpecialForm;
            }
        }

        public LambdaClosure CompilerMacroValue
        {
            set
            {
                _compilerValue = value;
                CompilerUsage = SymbolUsage.CompilerMacro;
            }
        }

        internal object CompilerValue
        {
            get{ return _compilerValue; }
        }

        public LambdaClosure MacroValue
        {
            get
            {
                return _compilerValue as LambdaClosure;
            }

            set
            {
                _compilerValue = value;
                CompilerUsage = SymbolUsage.Macro;
            }
        }

        public SymbolMacro SymbolMacroValue
        {
            get
            {
                return _compilerValue as SymbolMacro;
            }

            set
            {
                _compilerValue = value;
                CompilerUsage = SymbolUsage.SymbolMacro;
            }
        }

        public object Value
        {
            get
            {
                return _value;
            }

            set
            {
                if (IsUndefined)
                {
                    Usage = SymbolUsage.Variable;
                }

                _value = value;
            }
        }

        public object VariableValue
        {
            set
            {
                _value = value;
                Usage = SymbolUsage.Variable;
            }
        }

        public string ToString(bool escape)
        {
            var s = ContextualName;
            if (escape && Package != null)
            {
                var buf = new StringBuilder();
                foreach (var ch in s)
                {
                    if (Runtime.MustEscapeChar(ch))
                    {
                        buf.Append("\\");
                    }
                    buf.Append(ch);
                }
                s = buf.ToString();
            }
            return s;
        }

        public override string ToString()
        {
            return ContextualName;
        }


        object IApply.Apply(object[] args)
        {
            var value = CheckedValue as IApply;
            if (value == null)
            {
                throw new LispException("The value of the global variable {0} is not a function.", ContextualName);
            }
            return Runtime.Apply(value, args);
        }
    }

    public partial class Symbols
    {
        public static Symbol And;

        public static Symbol Append;

        public static Symbol Apply;

        public static Symbol Args;

        public static Symbol Array;

        public static Symbol AsVector;

        public static Symbol Base;

        public static Symbol BitAnd;

        public static Symbol BitNot;

        public static Symbol BitOr;

        public static Symbol BitShiftLeft;

        public static Symbol BitShiftRight;

        public static Symbol BitXor;

        public static Symbol Body;

        public static Symbol Bool;

        public static Symbol bqAppend;

        public static Symbol bqForce;

        public static Symbol bqList;

        public static Symbol bqQuote;
        
        public static Symbol BuiltinConstructor;

        public static Symbol BuiltinFunction;

        public static Symbol Case;

        public static Symbol Catch;

        public static Symbol CommandLineArguments;

        public static Symbol CommandLineScriptName;

        public static Symbol CompilerMacro;
        
        public static Symbol CompileTimeBranch;

        public static Symbol Compiling;

        public static Symbol Constant;

        public static Symbol CreateTask;

        public static Symbol CreateDelayedExpression;

        public static Symbol DebugMode;

        public static Symbol Declare;

        public static Symbol Def;

        public static Symbol DefConstant;

        public static Symbol DefineCompilerMacro;

        public static Symbol DefineSymbolMacro;

        public static Symbol DefMacro;

        public static Symbol DefMethod;

        public static Symbol DefMulti;

        public static Symbol DefSpecialForm;

        public static Symbol Defun;
        
        public static Symbol Do;

        public static Symbol Documentation;

        public static Symbol Dot;

        public static Symbol Dynamic;

        public static Symbol[] DynamicVariables;

        public static Symbol E;

        public static Symbol EnableExternalDocumentation;

        public static Symbol EnableWarnings;

        public static Symbol Environment;

        public static Symbol Equality;

        public static Symbol Escape;

        public static Symbol Eval;

        public static Symbol Exception;

        public static Symbol False;

        public static Symbol Features;

        public static Symbol Finally;

        public static Symbol Force;

        public static Symbol Funcall;
        
        public static Symbol Function;

        public static Symbol FunctionExitLabel;

        public static Symbol FunctionKeyword;

        public static Symbol FutureVar;

        public static Symbol GenericFunction;

        public static Symbol GetArgumentOrDefault;

        public static Symbol GetAttr;

        public static Symbol GetElt;

        public static Symbol Goto;

        public static Symbol HashElif;

        public static Symbol HashElse;

        public static Symbol HashEndif;

        public static Symbol HelpHook;

        public static Symbol HiddenVar;

        public static Symbol I;

        public static Symbol If;

        public static Symbol IfLet;

        public static Symbol Ignore;

        public static Symbol ImportedConstructor;

        public static Symbol ImportedFunction;

        public static Symbol InfoColor;

        public static Symbol InitialValue;

        public static Symbol InteractiveMode;

        public static Symbol It;

        public static Symbol Key;

        public static Symbol kwForce;

        public static Symbol Label;

        public static Symbol Lambda;
        
        public static Symbol LambdaList;

        public static Symbol LazyImport;

        public static Symbol LazyVar;

        public static Symbol Left;

        public static Symbol Let;

        public static Symbol LetFun;

        public static Symbol LetMacro;

        public static Symbol LetSymbolMacro;

        public static Symbol List;

        public static Symbol ListStar;

        public static Symbol LoadPath;

        public static Symbol LoadPrint;

        public static Symbol LoadPrintKeyword;

        public static Symbol LoadVerbose;

        public static Symbol LoadVerboseKeyword;

        public static Symbol Macro;

        public static Symbol MacroexpandHook;

        public static Symbol Macroexpand1;

        public static Symbol MacroKeyword;

        public static Symbol Main;

        public static Symbol Math;

        public static Symbol MaxElements;

        public static Symbol MergingDo;

        public static Symbol Method;

        public static Symbol MethodKeyword;

        public static Symbol MissingValue;

        public static Symbol Modules;

        public static Symbol New;

        public static Symbol Not;

        public static Symbol Nth;

        public static Symbol Null;

        public static Symbol NullableDot;

        public static Symbol[] NumberedVariables;

        public static Symbol Optional;

        public static Symbol OptionalKeyword;

        public static Symbol Or;

        public static Symbol Package;

        public static Symbol PackageNamePrefix;

        public static Symbol Padding;

        public static Symbol Params;

        public static Symbol PI;

        public static Symbol Pow;

        public static Symbol Pretty;

        public static Symbol PrettyPrintHook;

        public static Symbol PrintBase;

        public static Symbol PrintCompact;

        public static Symbol PrintEscape;

        public static Symbol PrintForce;

        public static Symbol PrintPrototypeWithBraces;
        
        public static Symbol PrintShortSymbolNames;

        public static Symbol PrintVectorWithBrackets;

        public static Symbol Prog;

        public static Symbol QuasiQuote;

        public static Symbol Quote;

        public static Symbol RawParams;

        public static Symbol ReadEval;

        public static Symbol ReadonlyVariable;

        public static Symbol Readtable;

        public static Symbol Recur;

        public static Symbol RecursionLabel;

        public static Symbol ReplForceIt;

        public static Symbol ReplListenerPort;

        public static Symbol[] ReservedVariables;

        public static Symbol Rest;

        public static Symbol Return;

        public static Symbol ReturnFrom;

        public static Symbol ReturnFromLoad;

        public static Symbol Returns;

        public static Symbol Right;

        public static Symbol ScriptDirectory;

        public static Symbol ScriptName;

        public static Symbol Self;

        public static Symbol Set;

        public static Symbol SetAttr;

        public static Symbol SetElt;

        public static Symbol Setf;

        public static Symbol Setq;

        public static Symbol[] ShortLambdaVariables;

        public static Symbol SpecialConstant;

        public static Symbol SpecialForm;

        public static Symbol SpecialReadonlyVariable;

        public static Symbol SpecialVariable;

        public static Symbol StdErr;

        public static Symbol StdIn;

        public static Symbol StdOut;

        public static Symbol StdScr;

        public static Symbol Str;

        public static Symbol Stream;

        public static Symbol StructurallyEqual;

        public static Symbol SymbolMacro;

        public static Symbol Target;

        public static Symbol Temp;

        public static Symbol Throw;

        public static Symbol Tilde;

        public static Symbol Tracing;

        public static Symbol True;

        public static Symbol Try;

        public static Symbol Undefined;

        public static Symbol Underscore;

        public static Symbol Unquote;

        public static Symbol UnquoteSplicing;

        public static Symbol Values;

        public static Symbol Var;

        public static Symbol Variable;

        public static Symbol Vector;

        public static Symbol Verbose;

        public static Symbol Whole;

        public static Symbol Width;

        public static void Create()
        {
            And = MakeSymbol("and");
            Append = MakeSymbol("append");
            Apply = MakeSymbol("apply");
            Args = MakeSymbol("__args__");
            Array = MakeSymbol("array");
            AsVector = MakeSymbol("as-vector");
            Base = MakeSymbol(":base");
            BitAnd = MakeSymbol("bit-and");
            BitNot = MakeSymbol("bit-not");
            BitOr = MakeSymbol("bit-or");
            BitShiftLeft = MakeSymbol("bit-shift-left");
            BitShiftRight = MakeSymbol("bit-shift-right");
            BitXor = MakeSymbol("bit-xor");
            Body = MakeSymbol("&body");
            Bool = MakeSymbol("bool");
            BuiltinConstructor = MakeSymbol("builtin-constructor");
            BuiltinFunction = MakeSymbol("builtin-function");
            Case = MakeSymbol("case");
            Catch = MakeSymbol("catch");
            CommandLineArguments = MakeSymbol("$command-line-arguments");
            CommandLineScriptName = MakeSymbol("$command-line-script-name");
            CompilerMacro = MakeSymbol("compiler-macro");
            CompileTimeBranch = MakeSymbol("compile-time-branch");
            Compiling = MakeSymbol("compiling");
            Constant = MakeSymbol("constant");
            CreateDelayedExpression = MakeSymbol("system:create-delayed-expression");
            CreateTask = MakeSymbol("system:create-task");
            DebugMode = MakeSymbol("$debug-mode");
            Declare = MakeSymbol("declare");
            Def = MakeSymbol("def");
            DefConstant = MakeSymbol("defconstant");
            DefineCompilerMacro = MakeSymbol("define-compiler-macro");
            DefineSymbolMacro = MakeSymbol("define-symbol-macro");
            DefMacro = MakeSymbol("defmacro");
            DefMethod = MakeSymbol("defmethod");
            DefMulti = MakeSymbol("defmulti");
            DefSpecialForm = MakeSymbol("define-special-form");
            Defun = MakeSymbol("defun");
            Do = MakeSymbol("do");
            Documentation = MakeSymbol("documentation");
            Dot = MakeSymbol(".");
            Dynamic = MakeSymbol("dynamic");
            E = MakeSymbol("math:E");
            EnableExternalDocumentation = MakeSymbol("$enable-external-documentation");
            EnableWarnings = MakeSymbol("$enable-warnings");
            Environment = MakeSymbol("&environment");
            Equality = MakeSymbol("=");
            Escape = MakeSymbol(":escape");
            Eval = MakeSymbol("eval");
            Exception = MakeSymbol("$exception");
            False = MakeSymbol("false");
            Features = MakeSymbol("$features");
            Finally = MakeSymbol("finally");
            Force = MakeSymbol("force");
            Funcall = MakeSymbol("funcall");
            Function = MakeSymbol("function");
            FunctionExitLabel = MakeSymbol("%function-exit");
            FunctionKeyword = MakeSymbol(":function");
            FutureVar = MakeSymbol("future");
            GenericFunction = MakeSymbol("generic-function");
            GetArgumentOrDefault = MakeSymbol("get-argument-or-default");
            GetAttr = MakeSymbol("attr");
            GetElt = MakeSymbol("elt");
            Goto = MakeSymbol("goto");
            HashElif = MakeSymbol("#elif");
            HashElse = MakeSymbol("#else");
            HashEndif = MakeSymbol("#endif");
            HelpHook = MakeSymbol("$help-hook");
            HiddenVar = MakeSymbol("hidden-var");
            I = MakeSymbol("math:I");
            If = MakeSymbol("if");
            IfLet = MakeSymbol("if-let");
            Ignore = MakeSymbol("ignore");
            ImportedConstructor = MakeSymbol("imported-constructor");
            ImportedFunction = MakeSymbol("imported-function");
            InfoColor = MakeSymbol("$info-color");
            InitialValue = MakeSymbol(":initial-value");
            InteractiveMode = MakeSymbol("$interactive-mode");
            It = MakeSymbol("it");
            Key = MakeSymbol("&key");
            Label = MakeSymbol("label");
            Lambda = MakeSymbol("lambda");
            LambdaList = MakeSymbol(@"__lambdas__");
            LazyImport = MakeSymbol("$lazy-import");
            LazyVar = MakeSymbol("lazy");
            Left = MakeSymbol(":left");
            Let = MakeSymbol("let");
            LetFun = MakeSymbol("letfun");
            LetMacro = MakeSymbol("letmacro");
            LetSymbolMacro = MakeSymbol("let-symbol-macro");
            List = MakeSymbol("list");
            ListStar = MakeSymbol("list*");
            LoadPath = MakeSymbol("$load-path");
            LoadPrint = MakeSymbol("$load-print");
            LoadPrintKeyword = MakeSymbol(":print");
            LoadVerbose = MakeSymbol("$load-verbose");
            LoadVerboseKeyword = MakeSymbol(":verbose");
            Macro = MakeSymbol("macro");
            MacroexpandHook = MakeSymbol("$macroexpand-hook");
            Macroexpand1 = MakeSymbol("macroexpand-1");
            MacroKeyword = MakeSymbol(":macro");
            Main = MakeSymbol("user:main");
            Math = MakeSymbol("math");
            MaxElements = MakeSymbol(":max-elements");
            MergingDo = MakeSymbol("merging-do");
            Method = MakeSymbol("method");
            MethodKeyword = MakeSymbol(":method");
            MissingValue = MakeSymbol("missing-value");
            Modules = MakeSymbol("$modules");
            New = MakeSymbol("new");
            Not = MakeSymbol("not");
            Nth = MakeSymbol("nth");
            Null = MakeSymbol("null");
            NullableDot = MakeSymbol("?");
            Optional = MakeSymbol("&optional");
            OptionalKeyword = MakeSymbol(":optional");
            Or = MakeSymbol("or");
            PI = MakeSymbol("math:PI");
            Package = MakeSymbol("$package");
            PackageNamePrefix = MakeSymbol("$package-name-prefix");
            Padding = MakeSymbol(":padding");
            Params = MakeSymbol("&params");
            Pow = MakeSymbol("math:pow");
            Pretty = MakeSymbol(":pretty");
            PrettyPrintHook = MakeSymbol("$pprint-hook");
            PrintBase = MakeSymbol("$print-base");
            PrintCompact = MakeSymbol("$print-compact");
            PrintEscape = MakeSymbol("$print-escape");
            PrintForce = MakeSymbol("$print-force");
            PrintPrototypeWithBraces = MakeSymbol("$print-prototype-with-braces");
            PrintShortSymbolNames = MakeSymbol("$print-short-symbol-names");
            PrintVectorWithBrackets = MakeSymbol("$print-vector-with-brackets");
            Prog = MakeSymbol("prog");
            QuasiQuote = MakeSymbol("quasi-quote");
            Quote = MakeSymbol("quote");
            RawParams = MakeSymbol("&rawparams");
            ReadEval = MakeSymbol("$read-eval");
            ReadonlyVariable = MakeSymbol("readonly-variable");
            Readtable = MakeSymbol("$readtable");
            Recur = MakeSymbol("recur");
            RecursionLabel = MakeSymbol("%recursion-label");
            ReplForceIt = MakeSymbol("$repl-force-it");
            ReplListenerPort = MakeSymbol("$repl-listener-port");
            Rest = MakeSymbol("&rest");
            Return = MakeSymbol("return");
            ReturnFrom = MakeSymbol("return-from");
            ReturnFromLoad = MakeSymbol("system:return-from-load");
            Returns = MakeSymbol("&returns");
            Right = MakeSymbol(":right");
            ScriptDirectory = MakeSymbol("$script-directory");
            ScriptName = MakeSymbol("$script-name");
            Self = MakeSymbol("self");
            Set = MakeSymbol("set");
            SetAttr = MakeSymbol("set-attr");
            SetElt = MakeSymbol("set-elt");
            Setf = MakeSymbol("setf");
            Setq = MakeSymbol("setq");
            SpecialConstant = MakeSymbol("special-constant");
            SpecialForm = MakeSymbol("special-form");
            SpecialReadonlyVariable = MakeSymbol("special-readonly-variable");
            SpecialVariable = MakeSymbol("special-variable");
            StdErr = MakeSymbol("$stderr");
            StdIn = MakeSymbol("$stdin");
            StdOut = MakeSymbol("$stdout");
            StdScr = MakeSymbol("$stdscr");
            Str = MakeSymbol("string");
            Stream = MakeSymbol(":stream");
            StructurallyEqual = MakeSymbol("structurally-equal");
            SymbolMacro = MakeSymbol("symbol-macro");
            Target = MakeSymbol("__target__");
            Temp = MakeSymbol(@"__temp__");
            Throw = MakeSymbol("throw");
            Tilde = MakeSymbol("~");
            Tracing = MakeSymbol("$tracing");
            True = MakeSymbol("true");
            Try = MakeSymbol("try");
            Undefined = MakeSymbol("undefined");
            Underscore = MakeSymbol("_");
            Unquote = MakeSymbol("system:unquote");
            UnquoteSplicing = MakeSymbol("system:unquote-splicing");
            Values = MakeSymbol("values");
            Var = MakeSymbol("var");
            Variable = MakeSymbol("variable");
            Vector = MakeSymbol("&vector");
            Verbose = MakeSymbol("$verbose");
            Whole = MakeSymbol("&whole");
            Width = MakeSymbol(":width");
            kwForce = MakeSymbol(":force");

            bqAppend = MakeSymbol("bq:append");
            bqList = MakeSymbol("bq:list");
            bqQuote = MakeSymbol("bq:quote");
            bqForce = MakeSymbol("bq:force");

            NumberedVariables = new Symbol[]
            {
                MakeSymbol(@"\0"),
                MakeSymbol(@"\1"),
                MakeSymbol(@"\2"),
                MakeSymbol(@"\3"),
                MakeSymbol(@"\4"),
                MakeSymbol(@"\5"),
                MakeSymbol(@"\6"),
                MakeSymbol(@"\7"),
                MakeSymbol(@"\8"),
                MakeSymbol(@"\9")
            };

            DynamicVariables = new Symbol[]
            {
                MakeSymbol("$0"),
                MakeSymbol("$1"),
                MakeSymbol("$2"),
                MakeSymbol("$3"),
                MakeSymbol("$4"),
                MakeSymbol("$5"),
                MakeSymbol("$6"),
                MakeSymbol("$7"),
                MakeSymbol("$8"),
                MakeSymbol("$9")
            };

            ReservedVariables = new Symbol[]
            {
                MakeSymbol("{0}"),
                MakeSymbol("{1}"),
                MakeSymbol("{2}"),
                MakeSymbol("{3}"),
                MakeSymbol("{4}"),
                MakeSymbol("{5}"),
                MakeSymbol("{6}"),
                MakeSymbol("{7}"),
                MakeSymbol("{8}"),
                MakeSymbol("{9}")
            };

            ShortLambdaVariables = new Symbol[]
            {
                MakeSymbol("%"),
                MakeSymbol("%1"),
                MakeSymbol("%2"),
                MakeSymbol("%3"),
                MakeSymbol("%4"),
                MakeSymbol("%5"),
                MakeSymbol("%6"),
                MakeSymbol("%7"),
                MakeSymbol("%8"),
                MakeSymbol("%9")
            };
        }

        public static Symbol MakeSymbol(string name)
        {
            return Runtime.MakeSymbol(name);
        }

    }
}