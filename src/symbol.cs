// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security.Permissions;
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

    public class Symbol: IPrintsValue
    {
        internal string Name;
        internal Cons PropList;
        internal Package Package;
        internal object /*Cons*/ Documentation;
        internal object /*Cons*/ FunctionSyntax;
        internal SymbolUsage Usage;
        internal bool IsTemp;

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

        internal bool IsDynamic
        {
            get
            {
                return Name[ 0 ] == '$';
            }
        }

        internal bool IsVariable
        {
            get
            {
                return Usage == SymbolUsage.Variable;
            }
        }

        internal bool IsReadonlyVariable
        {
            get
            {
                return Usage == SymbolUsage.ReadonlyVariable;
            }
        }

        internal bool IsConstant
        {
            get
            {
                return Usage == SymbolUsage.Constant;
            }
        }

        internal bool IsFunction
        {
            get
            {
                return Usage == SymbolUsage.Function;
            }
        }

        internal bool IsUndefined
        {
            get
            {
                return Usage == SymbolUsage.None;
            }
        }

        internal object _value;

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

        internal SpecialForm SpecialFormValue
        {
            get
            {
                return _value as SpecialForm;
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

        internal object ConstantValue
        {
            set
            {
                _value = value;
                Usage = SymbolUsage.Constant;
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

        internal object CheckedValue
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
                    throw new LispException( "Undefined variable: {0}", ContextualName );
                }

                if ( !IsVariable )
                {
                    throw new LispException( "Cannot assign to constant or function: {0}", ContextualName );
                }

                _value = value;
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

        public string ToString( bool escape )
        {
            var s = ContextualName;
            if ( escape && Package != null )
            {
                var buf = new StringBuilder();
                foreach ( var ch in s )
                {
                    if ( Scanner.IsTerminator(ch) || Scanner.IsWhiteSpace(ch) )
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

        internal string DiagnosticsName
        {
            get
            {
                return ContextualName;
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
                    return Package.Name + "." + Name;
                }
                else
                {
                    return Package.Name + "!" + Name;
                }
            }
        }
    }

    internal partial class Symbols
    {

        internal static Symbol MakeSymbol( string name )
        {
            return Runtime.MakeInitialSymbol( name );
        }

        internal static void Create()
        {
            Package = Runtime.MakeSymbol( "$package", Runtime.LispPackage );

            OptionalKeyword = Runtime.MakeSymbol( "optional", Runtime.KeywordPackage );
            MacroKeyword = Runtime.MakeSymbol( "macro", Runtime.KeywordPackage );
            FunctionKeyword = Runtime.MakeSymbol( "function", Runtime.KeywordPackage );
            MethodKeyword = Runtime.MakeSymbol( "method", Runtime.KeywordPackage );
            Escape = Runtime.MakeSymbol( "escape", Runtime.KeywordPackage );
            Pretty = Runtime.MakeSymbol( "pretty", Runtime.KeywordPackage );
            Stream = Runtime.MakeSymbol( "stream", Runtime.KeywordPackage );
            Left = Runtime.MakeSymbol( "left", Runtime.KeywordPackage );
            Right = Runtime.MakeSymbol( "right", Runtime.KeywordPackage );
            Width = Runtime.MakeSymbol( "width", Runtime.KeywordPackage );
            Padding = Runtime.MakeSymbol( "padding", Runtime.KeywordPackage );
            Base = Runtime.MakeSymbol( "base", Runtime.KeywordPackage );
            MaxElements = Runtime.MakeSymbol( "max-elements", Runtime.KeywordPackage );
            kwForce = Runtime.MakeSymbol( "force", Runtime.KeywordPackage );
            EofValue = Runtime.MakeSymbol( "eof-value", Runtime.KeywordPackage );
            Color = Runtime.MakeSymbol( "color", Runtime.KeywordPackage );
            InitialValue = Runtime.MakeSymbol( "initxxxial-value", Runtime.KeywordPackage );
            BackgroundColor = Runtime.MakeSymbol( "background-color", Runtime.KeywordPackage );

            Main = Runtime.MakeSymbol( "main", Runtime.UserPackage );

            I = Runtime.MakeSymbol( "I", Runtime.MathPackage );
            PI = Runtime.MakeSymbol( "PI", Runtime.MathPackage );
            E = Runtime.MakeSymbol( "E", Runtime.MathPackage );

            Declare = MakeSymbol( "declare" );
            Ignore = MakeSymbol( "ignore" );
            Strict = MakeSymbol( "$strict" );
            Underscore = MakeSymbol( "_" );
            Force = MakeSymbol( "force" );
            GetArgumentOrDefault = MakeSymbol( "get-argument-or-default" );
            TailCall = MakeSymbol( "tailcall" );
            Args = MakeSymbol( @"__args__" );
            LambdaList = MakeSymbol( @"__lambdas__" );
            Undefined = MakeSymbol( "undefined" );
            ReadEval = MakeSymbol( "$read-eval" );
            ReadSuppress = MakeSymbol( "$read-suppress" );
            Tilde = MakeSymbol( "~" );
            PrintCompact = MakeSymbol( "$print-compact" );
            PrintColor = MakeSymbol( "$print-color" );
            PrintBackgroundColor = MakeSymbol( "$print-background-color" );
            PrintForce = MakeSymbol( "$print-force" );
            PrintEscape = MakeSymbol( "$print-escape" );
            PrintBase = MakeSymbol( "$print-base" );
            PrintMaxElements = MakeSymbol( "$print-max-elements" );
            PrintShortSymbolNames = MakeSymbol( "$print-short-symbol-names" );
            It = MakeSymbol( "it" );
            Compiling = MakeSymbol( "compiling" );
            Def = MakeSymbol( "def" );
            DefConstant = MakeSymbol( "defconstant" );
            DefMacro = MakeSymbol( "defmacro" );
            DefMethod = MakeSymbol( "defmethod" );
            DefMulti = MakeSymbol( "defmulti" );
            Defun = MakeSymbol( "defun" );
            Do = MakeSymbol( "do" );
            MergeWithOuterDo = MakeSymbol( "merge-with-outer-do" );
            HiddenVar = MakeSymbol( "hidden-var" );
            Eval = MakeSymbol( "eval" );
            If = MakeSymbol( "if" );
            Return = MakeSymbol( "return" );
            Set = MakeSymbol( "set" );
            SetAttr = MakeSymbol( "set-attr" );
            SetAttrFunc = MakeSymbol( "%set-attr" );
            SetElt = MakeSymbol( "set-elt" );
            Throw = MakeSymbol( "throw" );
            Var = MakeSymbol( "var" );
            New = MakeSymbol( "new" );
            Optional = MakeSymbol( "&optional" );
            Returns = MakeSymbol( "&returns" );
            Key = MakeSymbol( "&key" );
            Rest = MakeSymbol( "&rest" );
            Body = MakeSymbol( "&body" );
            Vector = MakeSymbol( "&vector" );
            Params = MakeSymbol( "&params" );
            Whole = MakeSymbol( "&whole" );
            Tracing = MakeSymbol( "$tracing" );
            Verbose = MakeSymbol( "$verbose" );
            Features = MakeSymbol( "$features" );
            StdIn = MakeSymbol( "$stdin" );
            StdOut = MakeSymbol( "$stdout" );
            StdErr = MakeSymbol( "$stderr" );
            StdLog = MakeSymbol( "$stdlog" );
            Quote = MakeSymbol( "quote" );
            Comma = MakeSymbol( "," );
            CommaAt = MakeSymbol( ",@" );
            CommaDot = MakeSymbol( ",." );
            Lambda = MakeSymbol( "lambda" );
            GreekLambda = MakeSymbol( "\u03bb" );
            MakeLambdaParameterBinder = Runtime.MakeSymbol( "make-lambda-parameter-binder", Runtime.SystemPackage );
            Setq = MakeSymbol( "setq" );
            GetAttr = MakeSymbol( "attr" );
            GetAttrFunc = MakeSymbol( "%attr" );
            GetElt = MakeSymbol( "elt" );
            Block = MakeSymbol( "do" );
            TagBody = MakeSymbol( "tagbody" );
            Goto = MakeSymbol( "go" );
            Values = MakeSymbol( "values" );
            List = MakeSymbol( "list" );
            ListStar = MakeSymbol( "list*" );
            Append = MakeSymbol( "append" );
            Str = MakeSymbol( "string" );
            Bool = MakeSymbol( "bool" );
            Case = MakeSymbol( "case" );
            Default = MakeSymbol( "default" );
            Recur = MakeSymbol( "recur" );
            And = MakeSymbol( "and" );
            Not = MakeSymbol( "not" );
            Or = MakeSymbol( "or" );
            Exception = MakeSymbol( "$exception" );
            LoadPath = MakeSymbol( "$load-path" );
            ScriptDirectory = MakeSymbol( "$script-directory" );
            ScriptName = MakeSymbol( "$script-name" );
            CommandLineArguments = MakeSymbol( "$command-line-arguments" );
            Documentation = MakeSymbol( "documentation" );
            TryAndCatch = MakeSymbol( "try-and-catch" );
            TryFinally = MakeSymbol( "try-finally" );
            Accessor = MakeSymbol( "." );
            Math = MakeSymbol( "math" );
            PrettyPrintHook = MakeSymbol( "$pprint-hook" );
            WriteHook = MakeSymbol( "$write-hook" );
            Temp = MakeSymbol( @"__temp__" );
            AsLazyList = MakeSymbol( "as-lazy-list" );
            AsVector = MakeSymbol( "as-vector" );
            AsTuple = MakeSymbol( "as-tuple" );
            Nth = MakeSymbol( "nth" );
            Function = MakeSymbol( "function" );
            BuiltinFunction = MakeSymbol( "builtin-function" );
            ImportedFunction = MakeSymbol( "imported-function" );
            GenericFunction = MakeSymbol( "generic-function" );
            BuiltinConstructor = MakeSymbol( "builtin-constructor" );
            ImportedConstructor = MakeSymbol( "imported-constructor" );
            Macro = MakeSymbol( "macro" );
            Method = MakeSymbol( "method" );
            SpecialForm = MakeSymbol( "special-form" );
            DefSpecialForm = MakeSymbol( "define-special-form" );
            Variable = MakeSymbol( "variable" );
            ReadonlyVariable = MakeSymbol( "readonly-variable" );
            Constant = MakeSymbol( "constant" );
            SpecialVariable = MakeSymbol( "special-variable" );
            SpecialReadonlyVariable = MakeSymbol( "special-readonly-variable" );
            SpecialConstant = MakeSymbol( "special-constant" );
            EnableExternalDocumentation = MakeSymbol( "$enable-external-documentation" );
            HelpHook = MakeSymbol( "$help-hook" );
            ReplListenerPort = MakeSymbol( "$repl-listener-port" );
            Modules = MakeSymbol( "$modules" );
            LoadVerbose = MakeSymbol( "$load-verbose" );
            LoadPrint = MakeSymbol( "$load-print" );
            DebugMode = MakeSymbol( "$debug-mode" );
            BitAnd = MakeSymbol( "bit-and" );
            BitOr = MakeSymbol( "bit-or" );
            BitNot = MakeSymbol( "bit-not" );
            BitXor = MakeSymbol( "bit-xor" );
            BitShiftLeft = MakeSymbol( "bit-shift-left" );
            BitShiftRight = MakeSymbol( "bit-shift-right" );
            Pow = Runtime.MakeSymbol( "pow", Runtime.MathPackage, true  );
            Setf = MakeSymbol( "setf" );
            Equality = MakeSymbol( "=" );
            StructurallyEqual = MakeSymbol( "structurally-equal" );
            InteractiveMode = MakeSymbol( "$interactive-mode" );
            EnableWarnings = MakeSymbol( "$enable-warnings" );
            StandoutColor = MakeSymbol( "$standout-color" );
            StandoutBackgroundColor = MakeSymbol( "$standout-background-color" );
            QuickImport = MakeSymbol( "$quick-import" );

            // Add bq- prefix when writing an optimizer
            bqAppend = MakeSymbol( "force-append" );
            bqList = MakeSymbol( "list" );
            bqListStar = MakeSymbol( "list*" );
            bqClobberable = MakeSymbol( "clobberable" );
            bqQuote = MakeSymbol( "quote" );

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

        }

        internal static Symbol Declare;
        internal static Symbol Ignore;
        internal static Symbol I;
        internal static Symbol PI;
        internal static Symbol E;
        internal static Symbol Strict;
        internal static Symbol GetArgumentOrDefault;
        internal static Symbol TailCall;
        internal static Symbol Args;
        internal static Symbol LambdaList;
        internal static Symbol ReadEval;
        internal static Symbol ReadSuppress;
        internal static Symbol Underscore;
        internal static Symbol Tilde;
        internal static Symbol Package;
        internal static Symbol It;
        internal static Symbol EnableWarnings;
        internal static Symbol Color;
        internal static Symbol BackgroundColor;
        internal static Symbol PrintCompact;
        internal static Symbol PrintColor;
        internal static Symbol PrintBackgroundColor;
        internal static Symbol PrintForce;
        internal static Symbol PrintEscape;
        internal static Symbol PrintBase;
        internal static Symbol PrintMaxElements;
        internal static Symbol PrintShortSymbolNames;
        internal static Symbol Escape;
        internal static Symbol Stream;
        internal static Symbol EofValue;
        internal static Symbol Force;
        internal static Symbol kwForce;
        internal static Symbol Pretty;
        internal static Symbol Right;
        internal static Symbol Left;
        internal static Symbol Width;
        internal static Symbol Padding;
        internal static Symbol Base;
        internal static Symbol MaxElements;
        internal static Symbol Def;
        internal static Symbol DefConstant;
        internal static Symbol Defun;
        internal static Symbol DefMethod;
        internal static Symbol Return;
        internal static Symbol DefMacro;
        internal static Symbol DefMulti;
        internal static Symbol MakeLambdaParameterBinder;
        internal static Symbol If;
        internal static Symbol Do;
        internal static Symbol MergeWithOuterDo;
        internal static Symbol HiddenVar;
        internal static Symbol SetAttr;
        internal static Symbol SetAttrFunc;
        internal static Symbol SetElt;
        internal static Symbol Eval;
        internal static Symbol Set;
        internal static Symbol Var;
        internal static Symbol Throw;
        internal static Symbol Compiling;
        internal static Symbol Optional;
        internal static Symbol Returns;
        internal static Symbol Key;
        internal static Symbol Rest;
        internal static Symbol Body;
        internal static Symbol Params;
        internal static Symbol Whole;
        internal static Symbol Tracing;
        internal static Symbol Verbose;
        internal static Symbol Features;
        internal static Symbol StdIn;
        internal static Symbol StdOut;
        internal static Symbol StdErr;
        internal static Symbol StdLog;
        internal static Symbol Quote;
        internal static Symbol Comma;
        internal static Symbol CommaAt;
        internal static Symbol CommaDot;
        internal static Symbol Lambda;
        internal static Symbol GreekLambda;
        internal static Symbol Setq;
        internal static Symbol GetAttr;
        internal static Symbol GetAttrFunc;
        internal static Symbol GetElt;
        internal static Symbol TagBody;
        internal static Symbol Goto;
        internal static Symbol Block;
        internal static Symbol Values;
        internal static Symbol List;
        internal static Symbol ListStar;
        internal static Symbol Vector;
        internal static Symbol Append;
        internal static Symbol Str;
        internal static Symbol Bool;
        internal static Symbol Case;
        internal static Symbol Default;
        internal static Symbol Recur;
        internal static Symbol And;
        internal static Symbol Not;
        internal static Symbol Or;
        internal static Symbol Exception;
        internal static Symbol LoadPath;
        internal static Symbol ScriptDirectory;
        internal static Symbol ScriptName;
        internal static Symbol CommandLineArguments;
        internal static Symbol Documentation;
        internal static Symbol New;
        internal static Symbol TryAndCatch;
        internal static Symbol TryFinally;
        internal static Symbol[] DynamicVariables;
        internal static Symbol[] NumberedVariables;
        internal static Symbol[] ReservedVariables;
        internal static Symbol Accessor;
        internal static Symbol Math;
        internal static Symbol PrettyPrintHook;
        internal static Symbol WriteHook;
        internal static Symbol Temp;
        internal static Symbol AsLazyList;
        internal static Symbol AsVector;
        internal static Symbol AsTuple;
        internal static Symbol Nth;
        internal static Symbol MacroKeyword;
        internal static Symbol FunctionKeyword;
        internal static Symbol MethodKeyword;
        internal static Symbol OptionalKeyword;
        internal static Symbol Function;
        internal static Symbol BuiltinFunction;
        internal static Symbol ImportedFunction;
        internal static Symbol GenericFunction;
        internal static Symbol BuiltinConstructor;
        internal static Symbol ImportedConstructor;
        internal static Symbol Method;
        internal static Symbol Macro;
        internal static Symbol SpecialForm;
        internal static Symbol DefSpecialForm;
        internal static Symbol Variable;
        internal static Symbol ReadonlyVariable;
        internal static Symbol Constant;
        internal static Symbol SpecialVariable;
        internal static Symbol SpecialReadonlyVariable;
        internal static Symbol SpecialConstant;
        internal static Symbol EnableExternalDocumentation;
        internal static Symbol HelpHook;
        internal static Symbol ReplListenerPort;
        internal static Symbol Modules;
        internal static Symbol LoadPrint;
        internal static Symbol LoadVerbose;
        internal static Symbol DebugMode;
        internal static Symbol bqList;
        internal static Symbol bqListStar;
        internal static Symbol bqAppend;
        internal static Symbol bqClobberable;
        internal static Symbol bqQuote;
        internal static Symbol BitAnd;
        internal static Symbol BitOr;
        internal static Symbol BitXor;
        internal static Symbol BitNot;
        internal static Symbol BitShiftLeft;
        internal static Symbol BitShiftRight;
        internal static Symbol Pow;
        internal static Symbol Setf;
        internal static Symbol Equality;
        internal static Symbol StructurallyEqual;
        internal static Symbol InteractiveMode;
        internal static Symbol Main;
        internal static Symbol Undefined;
        internal static Symbol StandoutColor;
        internal static Symbol StandoutBackgroundColor;
        internal static Symbol QuickImport;
        internal static Symbol InitialValue;
    }

    public partial class Runtime
    {
        internal static Symbol DefineFunction(Symbol sym, object value, string doc )
        {
            sym.FunctionValue = value;
            sym.Documentation = String.IsNullOrWhiteSpace( doc ) ? null : MakeList( doc );
            return sym;
        }

        internal static Symbol DefineVariable( Symbol sym, object value, string doc )
        {
            sym.VariableValue = value;
            sym.Documentation = String.IsNullOrWhiteSpace( doc ) ? null : MakeList( doc );
            return sym;
        }

        internal static Symbol DefineConstant( Symbol sym, object value, string doc )
        {
            sym.ConstantValue = value;
            sym.Documentation = String.IsNullOrWhiteSpace( doc ) ? null : MakeList( doc );
            return sym;
        }

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

        [Lisp( "set-symbol-documentation" )]
        public static object SetSymbolDocumentation( object target, object value )
        {
            var sym = CheckSymbol( target );
            sym.Documentation = value;
            return value;
        }

        [Lisp( "symbol-function-syntax" )]
        public static Cons SymbolFunctionSyntax( object target )
        {
            var sym = CheckSymbol( target );
            return AsList( ( IEnumerable ) sym.FunctionSyntax );
        }

        [Lisp( "set-symbol-function-syntax" )]
        public static object SetSymbolFunctionSyntax( object target, object value )
        {
            var sym = CheckSymbol( target );
            sym.FunctionSyntax = value;
            return value;
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
    }


}
