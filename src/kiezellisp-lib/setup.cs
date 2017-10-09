#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Reflection;

    public partial class Runtime
    {
        #region Static Fields

        public static Dictionary<Type, List<Type>> AbstractTypes;
        public static Readtable DefaultReadtable;
        public static int DebugLevel;
        public static long GentempCounter;
        public static string HomeDirectory = Directory.GetCurrentDirectory();
        public static Package KeywordPackage;
        public static Package LispDocPackage;
        public static Package LispPackage;
        public static Missing MissingValue = Missing.Value;
        public static bool Mono;
        public static string ProgramFeature;
        public static bool ReadDecimalNumbers;
        public static bool Repl;
        public static int ForegroundColor;
        public static int BackgroundColor;
        public static string ScriptName;
        public static bool SetupMode;
        public static Package TempPackage;
        public static Cons UserArguments;
        public static Package UserPackage;
        [ThreadStatic]
        static ThreadContext _context;

        #endregion Static Fields

#if DEBUG

        public static bool AdaptiveCompilation = false;
        public static int CompilationThreshold = 40;

#else

        public static bool AdaptiveCompilation = true;
        public static int CompilationThreshold = 40;

#endif

        #region Public Properties

        public static ThreadContext CurrentThreadContext
        {
            get
            {
                if (_context == null)
                {
                    _context = new ThreadContext(null);
                }
                return _context;
            }
            set
            {
                _context = value;
            }
        }

        #endregion Public Properties

        #region Public Methods

        public static void AddFeature(string name)
        {
            var key = MakeSymbol(":", name);
            Symbols.Features.Value = MakeCons(key, (Cons)Symbols.Features.Value);
        }

        [Lisp("exit")]
        public static void Exit()
        {
            Environment.Exit(0);
        }

        [Lisp("exit")]
        public static void Exit(int code)
        {
            Environment.Exit(code);
        }

        public static string GetApplicationInitFile()
        {
            // application config file is same folder as kiezellisp-lib.dll
            var assembly = Assembly.GetExecutingAssembly();
            var root = assembly.Location;
            var dir = Path.GetDirectoryName(root);
            return PathExtensions.Combine(dir, "kiezellisp-init");
        }

        [Lisp("get-program")]
        public static string GetLibraryName()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
            var fileName = Path.GetFileNameWithoutExtension(fileVersion.FileName);
            return fileName;
        }

        [Lisp("get-mscorlib-version")]
        public static object GetMsCorLibVersion()
        {
            var v = FileVersionInfo.GetVersionInfo(typeof(int).Assembly.Location);
            return v;
        }

        [Lisp("get-program")]
        public static string GetProgramName()
        {
            var assembly = Assembly.GetEntryAssembly();
            var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
            var fileName = Path.GetFileNameWithoutExtension(fileVersion.FileName);
            return fileName;
        }

        [Lisp("get-version")]
        public static string GetVersion()
        {
            var assembly = Assembly.GetEntryAssembly();
            var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
            var date = new DateTime(2000, 1, 1).AddDays(fileVersion.FileBuildPart);
#if DEBUG
            return String.Format("{0} {1}.{2} (Debug Build {3} - {4:yyyy-MM-dd})", fileVersion.ProductName, fileVersion.FileMajorPart, fileVersion.FileMinorPart, fileVersion.FileBuildPart, date);
#else
            return string.Format("{0} {1}.{2} (Release Build {3} - {4:yyyy-MM-dd})", fileVersion.ProductName, fileVersion.FileMajorPart, fileVersion.FileMinorPart, fileVersion.FileBuildPart, date);
#endif
        }

        public static bool HasFeature(string feature)
        {
            var list = (Cons)Symbols.Features.Value;
            foreach (var item in ToIter(list))
            {
                if (SymbolName(item) == feature)
                {
                    return true;
                }
            }
            return false;
        }

        public static void InitAbstractTypes()
        {
            var types = new Type[]
            {
                typeof(Number), typeof(Complex), typeof(Integer), typeof(BigInteger), typeof(Numerics.BigRational),
                typeof(Rational), typeof(decimal), typeof(double), typeof(long), typeof(int), null,
                typeof(Rational), typeof(Numerics.BigRational), typeof(Integer), typeof(BigInteger), typeof(long), typeof(int), null,
                typeof(Integer), typeof(BigInteger), typeof(long), typeof(int), null,
                typeof(List), typeof(Cons), null,
                typeof(Sequence), typeof(List), typeof(Vector), null,
                typeof(Enumerable), typeof(IEnumerable), null,
                typeof(Symbol), typeof(KeywordClass), null,
                typeof(Atom), typeof(Symbol), typeof(KeywordClass), typeof(ValueType), typeof(string), typeof(Number),
                typeof(Complex), typeof(Integer), typeof(BigInteger), typeof(Numerics.BigRational), typeof(Rational), null,
                null
            };

            AbstractTypes = new Dictionary<Type, List<Type>>();

            Type key = null;
            List<Type> subtypes = null;

            foreach (var t in types)
            {
                if (key == null)
                {
                    key = t;
                    subtypes = new List<Type>();
                }
                else if (t == null)
                {
                    AbstractTypes[key] = subtypes;
                    key = null;
                }
                else {
                    subtypes.Add(t);
                }
            }
        }

        public static void Reset()
        {
            SetupMode = true;

            InitAbstractTypes();
            RestartVariables();
            RestartDependencies();
            RestartBinders();
            RestartSymbols();
            RestartCompiler();
            RestartSettings();
            RestartBuiltins(typeof(Runtime));
            RestartRuntimeSymbols();
        }

        public static void RestartBuiltins(Type type)
        {
            var flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public;
            var names = type.GetMethods(flags).Select(x => x.Name).Distinct();

            foreach (var name in names)
            {
                var members = type.GetMember(name, flags).OfType<MethodInfo>().Where(x => x.GetCustomAttributes(typeof(LispAttribute), false).Length != 0).ToArray();

                if (members.Length != 0)
                {
                    var pure = members[0].GetCustomAttributes(typeof(PureAttribute), false).Length != 0;
                    var lisp = members[0].GetCustomAttributes(typeof(LispAttribute), false);
                    var builtin = new ImportedFunction(name, type, members, pure);

                    foreach (string symbolName in ((LispAttribute)lisp[0]).Names)
                    {
                        var sym = FindSymbol(symbolName);
                        if (!sym.IsUndefined)
                        {
                            PrintWarning("Duplicate builtin name: ", sym.Name);
                        }
                        sym.FunctionValue = builtin;
                        sym.Package.Export(sym.Name);
                    }
                }
            }
        }

        public static object RestartDependencies()
        {
            // Make sure dlls are loaded when Kiezellisp starts.
            var temp = new object[]
            {
                new System.Data.Common.DataColumnMapping(),
                new System.Data.DataTable(),
                new System.Runtime.CompilerServices.ExtensionAttribute()
            };

            return temp;
        }

        public static void RestartLoadFiles(int level)
        {
            SetupMode = false;

            SetLoadPath(".");

            Symbols.Package.Value = UserPackage;

            string path = GetApplicationInitFile();

            if (path != null)
            {
                switch (level)
                {
                    case 1:
                        {
                            TryLoad(path, true, false);
                            break;
                        }
                    case 2:
                        {
                            TryLoad(path, true, true);
                            break;
                        }
                    case -1:
                    case 0:
                    default:
                        {
                            TryLoad(path, false, false);
                            break;
                        }
                }
            }
        }

        public static void RestartRuntimeSymbols()
        {
            Symbols.Cat.FunctionValue = Cat();
            Symbols.MacroexpandHook.VariableValue = Symbols.Funcall.Value;
        }

        [Lisp("shell:exec")]
        public static string Exec(string program, string input, string arguments)
        {
            using (var proc = new Process())
            {
                var info = proc.StartInfo;
                info.FileName = program;
                info.RedirectStandardError = true;
                info.RedirectStandardInput = true;
                info.RedirectStandardOutput = true;
                info.UseShellExecute = false;
                info.Arguments = arguments;
                proc.Start();
                using (var inp = proc.StandardInput)
                using (var outp = proc.StandardOutput)
                using (var err = proc.StandardError)
                {
                    if (input != null)
                    {
                        inp.Write(input);
                    }
                    inp.Close();
                    var result = outp.ReadToEnd();
                    outp.Close();
                    proc.WaitForExit();
                    var ok = proc.ExitCode == 0;
                    return ok ? result : null;
                }
            }
        }

        public static void RestartSettings()
        {
            if (Type.GetType("Mono.Runtime") != null)
            {
                Mono = true;
                AddFeature("mono");
            }
            else {
                Mono = false;
                AddFeature("microsoft");
            }

            if (Type.GetType("System.Windows.Forms.Form") != null)
            {
                AddFeature("winforms");
            }

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WINDOWID")))
                    {
                        AddFeature("wine");
                    }

                    AddFeature("windows");
                    break;
                case PlatformID.Unix:
                    var platform = Exec("uname", null, "-s");
                    if (platform != null)
                    {
                        if (platform.ToLower().IndexOf("linux") != -1)
                        {
                            AddFeature("linux");
                        }
                        if (platform.ToLower().IndexOf("bsd") != -1)
                        {
                            AddFeature("bsd");
                        }
                    }
                    if (Directory.Exists("/app"))
                    {
                        AddFeature("flatpak");
                    }
                    AddFeature("unix");
                    break;
                case PlatformID.MacOSX:
                    AddFeature("macosx");
                    break;
            }

            if (Environment.Is64BitProcess)
            {
                AddFeature("x64");
            }
            else {
                AddFeature("x32");
                AddFeature("x86");
            }

            AddFeature(ProgramFeature);

            AddFeature("kiezellisp");

            if (Repl)
            {
                AddFeature("repl");
            }

            Symbols.Features.VariableValue = AsList(Sort((Cons)Symbols.Features.Value));
        }

        public static void RestartSymbols()
        {
            // these packages do not use lisp package
            KeywordPackage = MakePackage3("keyword", reserved: true);
            TempPackage = MakePackage3("temp", reserved: true);
            LispPackage = MakePackage3("lisp", reserved: true);
            LispDocPackage = MakePackage3("example", reserved: true);

            // these packages do use lisp package
            UserPackage = MakePackage3("user", useLisp: true);

            Symbols.Create();

            // standard set of variables
            Symbols.AssemblyPath.ReadonlyValue = null;
            Symbols.CommandLineArguments.ReadonlyValue = UserArguments;
            Symbols.CommandLineScriptName.ReadonlyValue = ScriptName;
            Symbols.Debugging.ConstantValue = false;
            Symbols.E.ConstantValue = Math.E;
            Symbols.EnableWarnings.VariableValue = true;
            Symbols.Exception.ReadonlyValue = null;
            Symbols.Features.VariableValue = null;
            Symbols.HelpHook.VariableValue = null;
            Symbols.I.ConstantValue = Complex.ImaginaryOne;
            Symbols.It.VariableValue = null;
            Symbols.LazyImport.VariableValue = true;
            Symbols.LoadPath.ReadonlyValue = null;
            Symbols.LoadPrint.VariableValue = false;
            Symbols.LoadVerbose.VariableValue = false;
            Symbols.MissingValue.ConstantValue = MissingValue;
            Symbols.Modules.ReadonlyValue = null;
            Symbols.Package.VariableValue = LispPackage;
            Symbols.PackageNamePrefix.VariableValue = null;
            Symbols.PI.ConstantValue = Math.PI;
            Symbols.PlaybackDelay.VariableValue = 100;
            Symbols.PlaybackDelimiter.VariableValue = "<";
            Symbols.PrettyPrintHook.VariableValue = null;
            Symbols.PrintBase.VariableValue = 10;
            Symbols.PrintCompact.Value = true;
            Symbols.PrintEscape.VariableValue = true;
            Symbols.PrintForce.VariableValue = true;
            Symbols.PrintPrototypeWithBraces.VariableValue = false;
            Symbols.PrintShortSymbolNames.VariableValue = false;
            Symbols.PrintVectorWithBrackets.VariableValue = false;
            Symbols.ReadEval.VariableValue = null;
            Symbols.Readtable.VariableValue = GetStandardReadtable();
            Symbols.Self.ReadonlyValue = null;
            Symbols.ReplForceIt.VariableValue = false;
            Symbols.ReplListenerPort.VariableValue = 8080;
            Symbols.ScriptDirectory.ReadonlyValue = NormalizePath(HomeDirectory);
            Symbols.ScriptName.ReadonlyValue = null;
            Symbols.StdErr.VariableValue = null;
            Symbols.StdIn.VariableValue = null;
            Symbols.StdLog.VariableValue = null;
            Symbols.StdOut.VariableValue = null;
            Symbols.StdScr.VariableValue = null;
        }

        public static void RestartVariables()
        {
            ReadDecimalNumbers = true;
            GentempCounter = 0;
            LispPackage = null;
            LispDocPackage = null;
            KeywordPackage = null;
            UserPackage = null;
            Packages = new Dictionary<string, Package>();
            PackagesByType = new Dictionary<Type, Package>();
            Types = new Dictionary<Symbol, object>();

            InitRandom();

            DefaultReadtable = GetStandardReadtable();
        }

        [Lisp("set-read-decimal-numbers")]
        public static void SetReadDecimalNumbers(bool flag)
        {
            ReadDecimalNumbers = flag;
        }

        #endregion Public Methods
    }
}