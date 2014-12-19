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
        Function
    }

    public partial class Runtime
    {
        [Lisp( "get-designated-string" )]
        public static string GetDesignatedString( object target )
        {
            if ( target == null )
            {
                return "";
            }
            else if ( target is string )
            {
                return ( string ) target;
            }
            else
            {
                return SymbolName( target );
            }
        }

        [Lisp( "set" )]
        public static object Set( object var, object val )
        {
            var sym = CheckSymbol( var );
            if ( sym.IsDynamic )
            {
                SetDynamic( sym, val );
            }
            else
            {
                sym.CheckedValue = val;
            }
            return val;
        }

        [Lisp( "set-symbol-documentation" )]
        public static object SetSymbolDocumentation( object target, object value )
        {
            var sym = CheckSymbol( target );
            sym.Documentation = value;
            return value;
        }

        [Lisp( "set-symbol-function-syntax" )]
        public static object SetSymbolFunctionSyntax( object target, object value )
        {
            var sym = CheckSymbol( target );
            sym.FunctionSyntax = value;
            return value;
        }

        [Lisp( "set-symbol-value" )]
        public static object SetSymbolValue( object target, object value )
        {
            var sym = CheckSymbol( target );
            sym.Value = value;
            return value;
        }

        [Lisp( "symbol-documentation" )]
        public static Cons SymbolDocumentation( object target )
        {
            var sym = CheckSymbol( target );
            return AsList( ( IEnumerable ) sym.Documentation );
        }

        [Lisp( "symbol-function-syntax" )]
        public static Cons SymbolFunctionSyntax( object target )
        {
            var sym = CheckSymbol( target );
            return AsList( ( IEnumerable ) sym.FunctionSyntax );
        }

        [Lisp( "symbol-name" )]
        public static string SymbolName( object target )
        {
            var sym = CheckSymbol( target );
            return sym.Name;
        }

        [Lisp( "symbol-package" )]
        public static Package SymbolPackage( object target )
        {
            var sym = CheckSymbol( target );
            return sym.Package;
        }

        [Lisp( "symbol-value" )]
        public static object SymbolValue( object target )
        {
            var sym = CheckSymbol( target );
            return sym.CheckedValue;
        }

        internal static object DefineConstant( Symbol sym, object value, string doc )
        {
            sym.ConstantValue = value;
            sym.Documentation = String.IsNullOrWhiteSpace( doc ) ? null : MakeList( doc );
            return sym;
        }

        internal static object DefineFunction( Symbol sym, object value, string doc )
        {
            sym.FunctionValue = value;
            sym.Documentation = String.IsNullOrWhiteSpace( doc ) ? null : MakeList( doc );
            return sym;
        }

        internal static object DefineVariable( Symbol sym, object value, string doc )
        {
            sym.VariableValue = value;
            sym.Documentation = String.IsNullOrWhiteSpace( doc ) ? null : MakeList( doc );
            return sym;
        }
    }

    public class Symbol : IPrintsValue, IApply
    {
        internal object _value;
        internal object /*Cons*/ Documentation;
        internal object /*Cons*/ FunctionSyntax;
        internal string Name;
        internal Package Package;
        internal Cons PropList;
        internal SymbolUsage Usage;

        internal Symbol( string name, Package package = null )
        {
            Name = name;
            PropList = null;
            Package = package;

            if ( package == Runtime.KeywordPackage )
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
                if ( Package == null )
                {
                    return "#:" + Name;
                }
                else if ( Package == Runtime.KeywordPackage )
                {
                    return ":" + Name;
                }
                else if ( Runtime.ToBool( Runtime.GetDynamic( Symbols.PrintShortSymbolNames ) ) )
                {
                    return Name;
                }
                else if ( Package == Runtime.LispPackage || Package == Runtime.LispDocPackage )
                {
                    return Name;
                }
                else if ( Runtime.CurrentPackage().Find( Name ) == this )
                {
                    return Name;
                }
                else
                {
                    return LongName;
                }
            }
        }

        internal object CheckedValue
        {
            get
            {
                if ( IsUndefined )
                {
                    throw new LispException( "Undefined variable: {0}", LongName );
                }

                return _value;
            }

            set
            {
                if ( IsUndefined )
                {
                    throw new LispException( "Undefined variable: {0}", LongName );
                }

                if ( !IsVariable )
                {
                    throw new LispException( "Cannot assign to constant or function: {0}", LongName );
                }

                _value = value;
            }
        }

        internal object ConstantValue
        {
            set
            {
                _value = value;
                Usage = SymbolUsage.Constant;
            }
        }

        internal string DiagnosticsName
        {
            get
            {
                return ContextualName;
            }
        }

        internal object FunctionValue
        {
            set
            {
                _value = value;
                Usage = SymbolUsage.Function;
            }
        }

        internal bool IsConstant
        {
            get
            {
                return Usage == SymbolUsage.Constant;
            }
        }

        internal bool IsDynamic
        {
            get
            {
                return Name[ 0 ] == '$';
            }
        }

        internal bool IsFunction
        {
            get
            {
                return Usage == SymbolUsage.Function;
            }
        }

        internal bool IsReadonlyVariable
        {
            get
            {
                return Usage == SymbolUsage.ReadonlyVariable;
            }
        }

        internal bool IsUndefined
        {
            get
            {
                return Usage == SymbolUsage.None;
            }
        }

        internal bool IsVariable
        {
            get
            {
                return Usage == SymbolUsage.Variable;
            }
        }
        internal object LessCheckedValue
        {
            get
            {
                if ( IsUndefined )
                {
                    throw new LispException( "Undefined variable: {0}", ContextualName );
                }

                return _value;
            }

            set
            {
                if ( IsUndefined )
                {
                    Usage = SymbolUsage.Variable;
                }
                else if ( !IsVariable )
                {
                    throw new LispException( "Cannot assign to constant or function: {0}", ContextualName );
                }

                _value = value;
            }
        }

        internal string LongName
        {
            get
            {
                if ( Package == null )
                {
                    return "#:" + Name;
                }
                else if ( Package.FindExternal( Name ) != null )
                {
                    return Package.Name + ":" + Name;
                }
                else
                {
                    return Package.Name + "::" + Name;
                }
            }
        }

        internal object ReadonlyValue
        {
            set
            {
                _value = value;
                Usage = SymbolUsage.ReadonlyVariable;
            }
        }

        internal SpecialForm SpecialFormValue
        {
            get
            {
                return _value as SpecialForm;
            }
        }

        internal object Value
        {
            get
            {
                return _value;
            }

            set
            {
                if ( IsUndefined )
                {
                    Usage = SymbolUsage.Variable;
                }

                _value = value;
            }
        }

        internal object VariableValue
        {
            set
            {
                _value = value;
                Usage = SymbolUsage.Variable;
            }
        }
        public string ToString( bool escape )
        {
            var s = ContextualName;
            if ( escape && Package != null )
            {
                var buf = new StringBuilder();
                foreach ( var ch in s )
                {
                    if ( Runtime.MustEscapeChar( ch ) )
                    {
                        buf.Append( "\\" );
                    }
                    buf.Append( ch );
                }
                s = buf.ToString();
            }
            return s;
        }

        public override string ToString()
        {
            return ContextualName;
        }


        object IApply.Apply( object[] args )
        {
            var value = CheckedValue as IApply;
            if ( value == null )
            {
                throw new LispException( "The value of the global variable {0} is not a function.", ContextualName );
            }
            return Runtime.Apply( value, args );
        }
    }

    internal partial class Symbols
    {
        internal static Symbol And;

        internal static Symbol Append;

        internal static Symbol Apply;

        internal static Symbol Args;

        internal static Symbol AsLazyList;

        internal static Symbol AsMultipleElements;

        internal static Symbol AsVector;

        internal static Symbol BackgroundColor;

        internal static Symbol Base;

        internal static Symbol BitAnd;

        internal static Symbol BitNot;

        internal static Symbol BitOr;

        internal static Symbol BitShiftLeft;

        internal static Symbol BitShiftRight;

        internal static Symbol BitXor;

        internal static Symbol Body;

        internal static Symbol Bool;

        internal static Symbol ForceAppend;

        internal static Symbol bqClobberable;

        internal static Symbol BuiltinConstructor;

        internal static Symbol BuiltinFunction;

        internal static Symbol Case;

        internal static Symbol Catch;

        internal static Symbol Color;

        internal static Symbol CommandLineArguments;

        internal static Symbol Compiling;

        internal static Symbol Constant;

        internal static Symbol CreateTask;

        internal static Symbol DebugMode;

        internal static Symbol Declare;

        internal static Symbol Def;

        internal static Symbol Default;

        internal static Symbol DefConstant;

        internal static Symbol DefMacro;

        internal static Symbol DefMethod;

        internal static Symbol DefMulti;

        internal static Symbol DefSpecialForm;

        internal static Symbol Defun;

        internal static Symbol Do;

        internal static Symbol Documentation;

        internal static Symbol Dot;

        internal static Symbol[] DynamicVariables;

        internal static Symbol E;

        internal static Symbol EnableExternalDocumentation;

        internal static Symbol EnableWarnings;

        internal static Symbol Environment;

        internal static Symbol EofValue;

        internal static Symbol Equality;

        internal static Symbol Escape;

        internal static Symbol Eval;

        internal static Symbol Exception;

        internal static Symbol False;

        internal static Symbol Features;

        internal static Symbol Finally;

        internal static Symbol Force;

        internal static Symbol Function;

        internal static Symbol FunctionExitLabel;

        internal static Symbol FunctionKeyword;

        internal static Symbol FutureVar;

        internal static Symbol GenericFunction;

        internal static Symbol GetArgumentOrDefault;

        internal static Symbol GetAttr;

        internal static Symbol GetAttrFunc;

        internal static Symbol GetElt;

        internal static Symbol Goto;

        internal static Symbol GreekLambda;

        internal static Symbol HelpHook;

        internal static Symbol HiddenVar;

        internal static Symbol I;

        internal static Symbol If;

        internal static Symbol IfLet;

        internal static Symbol Ignore;

        internal static Symbol ImportedConstructor;

        internal static Symbol ImportedFunction;

        internal static Symbol InitialValue;

        internal static Symbol InteractiveMode;

        internal static Symbol It;

        internal static Symbol Key;

        internal static Symbol kwForce;

        internal static Symbol Label;

        internal static Symbol Lambda;

        internal static Symbol LambdaList;

        internal static Symbol LazyImport;

        internal static Symbol LazyVar;

        internal static Symbol Left;

        internal static Symbol Let;

        internal static Symbol LetFun;

        internal static Symbol List;

        internal static Symbol ListStar;

        internal static Symbol LoadPath;

        internal static Symbol LoadPrint;

        internal static Symbol LoadVerbose;

        internal static Symbol Macro;

        internal static Symbol Macroexpand1;

        internal static Symbol MacroKeyword;

        internal static Symbol Main;

        internal static Symbol Math;

        internal static Symbol MaxElements;

        internal static Symbol MergingDo;

        internal static Symbol Method;

        internal static Symbol MethodKeyword;

        internal static Symbol Modules;

        internal static Symbol New;

        internal static Symbol Not;

        internal static Symbol Nth;

        internal static Symbol Null;

        internal static Symbol NullableDot;

        internal static Symbol[] NumberedVariables;

        internal static Symbol Optional;

        internal static Symbol OptionalKeyword;

        internal static Symbol Or;

        internal static Symbol Package;

        internal static Symbol PackageNamePrefix;

        internal static Symbol Padding;

        internal static Symbol Params;

        internal static Symbol PI;

        internal static Symbol Pow;

        internal static Symbol Pretty;

        internal static Symbol PrettyPrintHook;

        internal static Symbol PrintBackgroundColor;

        internal static Symbol PrintBase;

        internal static Symbol PrintColor;

        internal static Symbol PrintCompact;

        internal static Symbol PrintEscape;

        internal static Symbol PrintForce;

        internal static Symbol PrintShortSymbolNames;

        internal static Symbol QuasiQuote;

        internal static Symbol Quote;

        internal static Symbol ReadEval;

        internal static Symbol ReadonlyVariable;

        internal static Symbol Readtable;

        internal static Symbol Recur;

        internal static Symbol ReplListenerPort;

        internal static Symbol[] ReservedVariables;

        internal static Symbol Rest;

        internal static Symbol Return;

        internal static Symbol ReturnFrom;

        internal static Symbol ReturnFromLoad;

        internal static Symbol Returns;

        internal static Symbol Right;

        internal static Symbol ScriptDirectory;

        internal static Symbol ScriptName;

        internal static Symbol Set;

        internal static Symbol SetAttr;

        internal static Symbol SetAttrFunc;

        internal static Symbol SetElt;

        internal static Symbol Setf;

        internal static Symbol Setq;

        internal static Symbol PrettyReader;

        internal static Symbol[] ShortLambdaVariables;

        internal static Symbol SpecialConstant;

        internal static Symbol SpecialForm;

        internal static Symbol SpecialReadonlyVariable;

        internal static Symbol SpecialVariable;

        internal static Symbol StandoutBackgroundColor;

        internal static Symbol StandoutColor;

        internal static Symbol StdErr;

        internal static Symbol StdIn;

        internal static Symbol StdLog;

        internal static Symbol StdOut;

        internal static Symbol Str;

        internal static Symbol Stream;

        internal static Symbol StructurallyEqual;

        internal static Symbol TagBody;

        internal static Symbol TailCall;

        internal static Symbol Target;

        internal static Symbol Temp;

        internal static Symbol Throw;

        internal static Symbol Tilde;

        internal static Symbol Tracing;

        internal static Symbol True;

        internal static Symbol Try;

        internal static Symbol Undefined;

        internal static Symbol Underscore;

        internal static Symbol Unquote;

        internal static Symbol UnquoteSplicing;

        internal static Symbol UnquoteNSplicing;

        internal static Symbol Values;

        internal static Symbol Var;

        internal static Symbol Variable;

        internal static Symbol Vector;

        internal static Symbol Verbose;

        internal static Symbol Whole;

        internal static Symbol Width;

        internal static Symbol WriteHook;

        internal static void Create()
        {
            And = MakeSymbol( "and" );
            Append = MakeSymbol( "append" );
            Apply = MakeSymbol( "apply" );
            Args = MakeSymbol( "__args__" );
            AsLazyList = MakeSymbol( "as-lazy-list" );
            AsMultipleElements = MakeSymbol( "as-multiple-elements" );
            AsVector = MakeSymbol( "as-vector" );
            BackgroundColor = Runtime.MakeSymbol( "background-color", Runtime.KeywordPackage );
            Base = Runtime.MakeSymbol( "base", Runtime.KeywordPackage );
            BitAnd = MakeSymbol( "bit-and" );
            BitNot = MakeSymbol( "bit-not" );
            BitOr = MakeSymbol( "bit-or" );
            BitShiftLeft = MakeSymbol( "bit-shift-left" );
            BitShiftRight = MakeSymbol( "bit-shift-right" );
            BitXor = MakeSymbol( "bit-xor" );
            Body = MakeSymbol( "&body" );
            Bool = MakeSymbol( "bool" );
            BuiltinConstructor = MakeSymbol( "builtin-constructor" );
            BuiltinFunction = MakeSymbol( "builtin-function" );
            Case = MakeSymbol( "case" );
            Catch = MakeSymbol( "catch" );
            Color = Runtime.MakeSymbol( "color", Runtime.KeywordPackage );
            CommandLineArguments = MakeSymbol( "$command-line-arguments" );
            Compiling = MakeSymbol( "compiling" );
            Constant = MakeSymbol( "constant" );
            CreateTask = Runtime.MakeSymbol( "create-task", Runtime.SystemPackage );
            DebugMode = MakeSymbol( "$debug-mode" );
            Declare = MakeSymbol( "declare" );
            Def = MakeSymbol( "def" );
            DefConstant = MakeSymbol( "defconstant" );
            DefMacro = MakeSymbol( "defmacro" );
            DefMethod = MakeSymbol( "defmethod" );
            DefMulti = MakeSymbol( "defmulti" );
            DefSpecialForm = MakeSymbol( "define-special-form" );
            Default = MakeSymbol( "default" );
            Defun = MakeSymbol( "defun" );
            Do = MakeSymbol( "do" );
            Documentation = MakeSymbol( "documentation" );
            Dot = MakeSymbol( "." );
            E = Runtime.MakeSymbol( "E", Runtime.MathPackage );
            EnableExternalDocumentation = MakeSymbol( "$enable-external-documentation" );
            EnableWarnings = MakeSymbol( "$enable-warnings" );
            Environment = MakeSymbol( "&environment" );
            EofValue = Runtime.MakeSymbol( "eof-value", Runtime.KeywordPackage );
            Equality = MakeSymbol( "=" );
            Escape = Runtime.MakeSymbol( "escape", Runtime.KeywordPackage );
            Eval = MakeSymbol( "eval" );
            Exception = MakeSymbol( "$exception" );
            False = MakeSymbol( "false" );
            Features = MakeSymbol( "$features" );
            Finally = MakeSymbol( "finally" );
            Force = MakeSymbol( "force" );
            Function = MakeSymbol( "function" );
            FunctionExitLabel = MakeSymbol( "function-exit" );
            FunctionKeyword = Runtime.MakeSymbol( "function", Runtime.KeywordPackage );
            FutureVar = MakeSymbol( "future" );
            GenericFunction = MakeSymbol( "generic-function" );
            GetArgumentOrDefault = MakeSymbol( "get-argument-or-default" );
            GetAttr = MakeSymbol( "attr" );
            GetAttrFunc = MakeSymbol( "%attr" );
            GetElt = MakeSymbol( "elt" );
            Goto = MakeSymbol( "goto" );
            GreekLambda = MakeSymbol( "\u03bb" );
            HelpHook = MakeSymbol( "$help-hook" );
            HiddenVar = MakeSymbol( "hidden-var" );
            I = Runtime.MakeSymbol( "I", Runtime.MathPackage );
            If = MakeSymbol( "if" );
            IfLet = MakeSymbol( "if-let" );
            Ignore = MakeSymbol( "ignore" );
            ImportedConstructor = MakeSymbol( "imported-constructor" );
            ImportedFunction = MakeSymbol( "imported-function" );
            InitialValue = Runtime.MakeSymbol( "initxxxial-value", Runtime.KeywordPackage );
            InteractiveMode = MakeSymbol( "$interactive-mode" );
            It = MakeSymbol( "it" );
            Key = MakeSymbol( "&key" );
            Label = MakeSymbol( "label" );
            Lambda = MakeSymbol( "lambda" );
            LambdaList = MakeSymbol( @"__lambdas__" );
            LazyImport = MakeSymbol( "$lazy-import" );
            LazyVar = MakeSymbol( "lazy" );
            Left = Runtime.MakeSymbol( "left", Runtime.KeywordPackage );
            Let = MakeSymbol( "let" );
            LetFun = MakeSymbol( "letfun" );
            List = MakeSymbol( "list" );
            ListStar = MakeSymbol( "list*" );
            LoadPath = MakeSymbol( "$load-path" );
            LoadPrint = MakeSymbol( "$load-print" );
            LoadVerbose = MakeSymbol( "$load-verbose" );
            Macro = MakeSymbol( "macro" );
            Macroexpand1 = MakeSymbol( "macroexpand-1" );
            MacroKeyword = Runtime.MakeSymbol( "macro", Runtime.KeywordPackage );
            Main = Runtime.MakeSymbol( "main", Runtime.UserPackage );
            Math = MakeSymbol( "math" );
            MaxElements = Runtime.MakeSymbol( "max-elements", Runtime.KeywordPackage );
            MergingDo = MakeSymbol( "merging-do" );
            Method = MakeSymbol( "method" );
            MethodKeyword = Runtime.MakeSymbol( "method", Runtime.KeywordPackage );
            Modules = MakeSymbol( "$modules" );
            New = MakeSymbol( "new" );
            Not = MakeSymbol( "not" );
            Nth = MakeSymbol( "nth" );
            Null = MakeSymbol( "null" );
            NullableDot = MakeSymbol( "?" );
            Optional = MakeSymbol( "&optional" );
            OptionalKeyword = Runtime.MakeSymbol( "optional", Runtime.KeywordPackage );
            Or = MakeSymbol( "or" );
            PI = Runtime.MakeSymbol( "PI", Runtime.MathPackage );
            Package = MakeSymbol( "$package" );
            PackageNamePrefix = MakeSymbol( "$package-name-prefix" );
            Padding = Runtime.MakeSymbol( "padding", Runtime.KeywordPackage );
            Params = MakeSymbol( "&params" );
            Pow = Runtime.MakeSymbol( "pow", Runtime.MathPackage, true );
            Pretty = Runtime.MakeSymbol( "pretty", Runtime.KeywordPackage );
            PrettyPrintHook = MakeSymbol( "$pprint-hook" );
            PrintBackgroundColor = MakeSymbol( "$print-background-color" );
            PrintBase = MakeSymbol( "$print-base" );
            PrintColor = MakeSymbol( "$print-color" );
            PrintCompact = MakeSymbol( "$print-compact" );
            PrintEscape = MakeSymbol( "$print-escape" );
            PrintForce = MakeSymbol( "$print-force" );
            PrintShortSymbolNames = MakeSymbol( "$print-short-symbol-names" );
            QuasiQuote = MakeSymbol( "quasi-quote" );
            Quote = MakeSymbol( "quote" );
            ReadEval = MakeSymbol( "$read-eval" );
            ReadonlyVariable = MakeSymbol( "readonly-variable" );
            Readtable = MakeSymbol( "$readtable" );
            Recur = MakeSymbol( "recur" );
            ReplListenerPort = MakeSymbol( "$repl-listener-port" );
            Rest = MakeSymbol( "&rest" );
            Return = MakeSymbol( "return" );
            ReturnFrom = MakeSymbol( "return-from" );
            ReturnFromLoad = MakeSymbol( "return-from-load" );
            Returns = MakeSymbol( "&returns" );
            Right = Runtime.MakeSymbol( "right", Runtime.KeywordPackage );
            ScriptDirectory = MakeSymbol( "$script-directory" );
            ScriptName = MakeSymbol( "$script-name" );
            Set = MakeSymbol( "set" );
            SetAttr = MakeSymbol( "set-attr" );
            SetAttrFunc = MakeSymbol( "%set-attr" );
            SetElt = MakeSymbol( "set-elt" );
            Setf = MakeSymbol( "setf" );
            Setq = MakeSymbol( "setq" );
            PrettyReader = Runtime.MakeSymbol( "pretty-reader", Runtime.SystemPackage );
            SpecialConstant = MakeSymbol( "special-constant" );
            SpecialForm = MakeSymbol( "special-form" );
            SpecialReadonlyVariable = MakeSymbol( "special-readonly-variable" );
            SpecialVariable = MakeSymbol( "special-variable" );
            StandoutBackgroundColor = MakeSymbol( "$standout-background-color" );
            StandoutColor = MakeSymbol( "$standout-color" );
            StdErr = MakeSymbol( "$stderr" );
            StdIn = MakeSymbol( "$stdin" );
            StdLog = MakeSymbol( "$stdlog" );
            StdOut = MakeSymbol( "$stdout" );
            Str = MakeSymbol( "string" );
            Stream = Runtime.MakeSymbol( "stream", Runtime.KeywordPackage );
            StructurallyEqual = MakeSymbol( "structurally-equal" );
            TagBody = MakeSymbol( "tagbody" );
            TailCall = MakeSymbol( "tailcall" );
            Target = MakeSymbol( "__target__" );
            Temp = MakeSymbol( @"__temp__" );
            Throw = MakeSymbol( "throw" );
            Tilde = MakeSymbol( "~" );
            Tracing = MakeSymbol( "$tracing" );
            True = MakeSymbol( "true" );
            Try = MakeSymbol( "try" );
            Undefined = MakeSymbol( "undefined" );
            Underscore = MakeSymbol( "_" );
            Unquote = Runtime.MakeSymbol( "unquote", Runtime.SystemPackage );
            UnquoteSplicing = Runtime.MakeSymbol( "unquote-splicing", Runtime.SystemPackage );
            UnquoteNSplicing = Runtime.MakeSymbol( "unquote-nsplicing", Runtime.SystemPackage );
            Values = MakeSymbol( "values" );
            Var = MakeSymbol( "var" );
            Variable = MakeSymbol( "variable" );
            Vector = MakeSymbol( "&vector" );
            Verbose = MakeSymbol( "$verbose" );
            Whole = MakeSymbol( "&whole" );
            Width = Runtime.MakeSymbol( "width", Runtime.KeywordPackage );
            WriteHook = MakeSymbol( "$write-hook" );
            kwForce = Runtime.MakeSymbol( "force", Runtime.KeywordPackage );

            // Add bq- prefix when writing an optimizer
            ForceAppend = MakeSymbol( "force-append" );
            bqClobberable = MakeSymbol( "clobberable" );

            NumberedVariables = new Symbol[]
		    {
			    MakeSymbol( @"\0" ),
			    MakeSymbol( @"\1" ),
			    MakeSymbol( @"\2" ),
			    MakeSymbol( @"\3" ),
			    MakeSymbol( @"\4" ),
			    MakeSymbol( @"\5" ),
			    MakeSymbol( @"\6" ),
			    MakeSymbol( @"\7" ),
			    MakeSymbol( @"\8" ),
			    MakeSymbol( @"\9" )
		    };

            DynamicVariables = new Symbol[]
		    {
			    MakeSymbol( "$0" ),
			    MakeSymbol( "$1" ),
			    MakeSymbol( "$2" ),
			    MakeSymbol( "$3" ),
			    MakeSymbol( "$4" ),
			    MakeSymbol( "$5" ),
			    MakeSymbol( "$6" ),
			    MakeSymbol( "$7" ),
			    MakeSymbol( "$8" ),
			    MakeSymbol( "$9" )
		    };

            ReservedVariables = new Symbol[]
            {
                MakeSymbol( "{0}" ),
                MakeSymbol( "{1}" ),
                MakeSymbol( "{2}" ),
                MakeSymbol( "{3}" ),
                MakeSymbol( "{4}" ),
                MakeSymbol( "{5}" ),
                MakeSymbol( "{6}" ),
                MakeSymbol( "{7}" ),
                MakeSymbol( "{8}" ),
                MakeSymbol( "{9}" )
            };

            ShortLambdaVariables = new Symbol[]
            {
			    MakeSymbol( "%" ),
			    MakeSymbol( "%1" ),
			    MakeSymbol( "%2" ),
			    MakeSymbol( "%3" ),
			    MakeSymbol( "%4" ),
			    MakeSymbol( "%5" ),
			    MakeSymbol( "%6" ),
			    MakeSymbol( "%7" ),
			    MakeSymbol( "%8" ),
			    MakeSymbol( "%9" )
            };
        }

        internal static Symbol MakeSymbol( string name )
        {
            return Runtime.MakeInitialSymbol( name );
        }
    }
}