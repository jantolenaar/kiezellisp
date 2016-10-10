// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Linq;
using System.Text;

namespace Kiezel
{
    [RestrictedImport]
    public partial class RuntimeConsole
    {
        public static string[] ReplCommands = new string[]
        {
            ":clear", ":continue", ":globals", ":quit",
            ":abort", ":backtrace", ":variables", ":$variables",
            ":top", ":exception", ":Exception", ":force",
            ":describe", ":reset", ":time"
        };

        public static Stack<ThreadContextState> state;

        public static Stopwatch timer = Stopwatch.StartNew();

        public static Exception LastException = null;

        [Lisp("get-version")]
        public static string GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
            var date = new DateTime(2000, 1, 1).AddDays(fileVersion.FileBuildPart);
            #if DEBUG
            return String.Format("{0} {1}.{2} (Debug Build {3} - {4:yyyy-MM-dd})", fileVersion.ProductName, fileVersion.FileMajorPart, fileVersion.FileMinorPart, fileVersion.FileBuildPart, date);
            #else
            return String.Format("{0} {1}.{2} (Release Build {3} - {4:yyyy-MM-dd})", fileVersion.ProductName, fileVersion.FileMajorPart, fileVersion.FileMinorPart, fileVersion.FileBuildPart, date);
            #endif
        }

        [Lisp("breakpoint")]
        public static void Breakpoint()
        {
            var oldState = state;
            state = new Stack<ThreadContextState>();
            state.Push(Runtime.SaveStackAndFrame());

            try
            {
                ReadEvalPrintLoop(initialized: true, debugging: true);
            }
            catch (ContinueFromBreakpointException)
            {
            }
            catch (AbortingDebuggerException)
            {
                throw new AbortedDebuggerException();
            }
            finally
            {
                state = oldState;
            }
        }

        public static void Quit()
        {
            Environment.Exit(0);
        }

        public static void EvalPrintCommand(string data, bool debugging)
        {
            bool leadingSpace = char.IsWhiteSpace(data, 0);
            Cons code = Runtime.ReadAllFromString(data);

            if (code == null)
            {
                return;
            }

            Runtime.RestoreStackAndFrame(state.Peek());

            var head = Runtime.First(code) as Symbol;

            if (head != null && (Runtime.Keywordp(head) || head.Name == "?"))
            {
                var dotCommand = Runtime.First(code).ToString();
                var command = "";
                var commandsPrefix = ReplCommands.Where(x => ((String)x).StartsWith(dotCommand)).ToList();
                var commandsExact = ReplCommands.Where(x => ((String)x) == dotCommand).ToList();

                if (commandsPrefix.Count == 0)
                {
                    Console.WriteLine("Command not found");
                    return;
                }
                else if (commandsExact.Count == 1)
                {
                    command = commandsExact[0];
                }
                else if (commandsPrefix.Count == 1)
                {
                    command = commandsPrefix[0];
                }
                else
                {
                    Console.Write("Ambiguous command. Did you mean:");
                    for (int i = 0; i < commandsPrefix.Count; ++i)
                    {
                        Console.Write("{0} {1}", (i == 0 ? "" : i + 1 == commandsPrefix.Count ? " or" : ","), commandsPrefix[i]);
                    }
                    Console.WriteLine("?");
                    return;
                }

                switch (command)
                {
                    case ":continue":
                    {
                        if (debugging)
                        {
                            throw new ContinueFromBreakpointException();
                        }
                        break;
                    }
                    case ":clear":
                    {
                        Console.Clear();
                        state = new Stack<ThreadContextState>();
                        state.Push(Runtime.SaveStackAndFrame());
                        break;
                    }
                    case ":abort":
                    {
                        if (state.Count > 1)
                        {
                            state.Pop();
                        }
                        else if (debugging)
                        {
                            throw new AbortingDebuggerException();
                        }
                        break;
                    }

                    case ":top":
                    {
                        while (state.Count > 1)
                        {
                            state.Pop();
                        }
                        break;
                    }

                    case ":quit":
                    {
                        Quit();
                        break;
                    }

                    case ":globals":
                    {
                        var pattern = (string)Runtime.Second(code);
                        Runtime.DumpDictionary(Console.Out, Runtime.GetGlobalVariablesDictionary(pattern));
                        break;
                    }

                    case ":variables":
                    {
                        var pos = Runtime.Integerp(Runtime.Second(code)) ? (int)Runtime.Second(code) : 0;
                        Runtime.DumpDictionary(Console.Out, Runtime.GetLexicalVariablesDictionary(pos));
                        break;
                    }

                    case ":$variables":
                    {
                        var pos = Runtime.Integerp(Runtime.Second(code)) ? (int)Runtime.Second(code) : 0;
                        Runtime.DumpDictionary(Console.Out, Runtime.GetDynamicVariablesDictionary(pos));
                        break;
                    }

                    case ":backtrace":
                    {
                        Console.Write(Runtime.GetEvaluationStack());
                        break;
                    }

                    case ":Exception":
                    {
                        Console.WriteLine(LastException.ToString());
                        break;
                    }

                    case ":exception":
                    {
                        Console.WriteLine(RemoveDlrReferencesFromException(LastException));
                        break;
                    }

                    case ":force":
                    {
                        var expr = (object)Runtime.Second(code) ?? Symbols.It;
                        RunCommand(null, Runtime.MakeList(Runtime.MakeList(Symbols.Force, expr)));
                        break;
                    }

                    case ":time":
                    {
                        var expr = (object)Runtime.Second(code) ?? Symbols.It;
                        RunCommand(null, Runtime.MakeList(expr), showTime: true);
                        break;
                    }

                    case ":describe":
                    {
                        RunCommand(x =>
                        {
                            Runtime.SetSymbolValue(Symbols.It, x);
                            Runtime.Describe(x);
                        }, Runtime.Cdr(code));
                        break;
                    }

                    case ":reset":
                    {
                        var level = Runtime.Integerp(Runtime.Second(code)) ? (int)Runtime.Second(code) : 0;
                        while (state.Count > 1)
                        {
                            state.Pop();
                        }
                        timer.Reset();
                        timer.Start();
                        Reset(level);
                        timer.Stop();
                        var time = timer.ElapsedMilliseconds;
                        Runtime.PrintLog("Startup time: ", time, "ms");
                        break;
                    }

                }
            }
            else
            {
                RunCommand(null, code, smartParens: !leadingSpace);
            }
        }

        public static bool IsNotDlrCode(string line)
        {
            return line.IndexOf("CallSite") == -1
            && line.IndexOf("System.Dynamic") == -1
            && line.IndexOf("Microsoft.Scripting") == -1;
        }

        public static string RemoveDlrReferencesFromException(Exception ex)
        {
            return String.Join("\n", ex.ToString().Split('\n').Where(IsNotDlrCode));
        }

        public static int[] GetIntegerArgs(string lispCode)
        {
            var results = new List<int>();
            foreach (var expr in Runtime.ReadAllFromString( lispCode ))
            {
                results.Add(Convert.ToInt32(Runtime.Eval(expr)));
            }
            return results.ToArray();
        }

        public static bool IsCompleteSourceCode(string data)
        {
            Cons code;

            return ParseCompleteSourceCode(data, out code);
        }

        public static bool ParseCompleteSourceCode(string data, out Cons code)
        {
            code = null;

            if (data.Trim() == "")
            {
                return true;
            }

            try
            {
                code = Runtime.ReadAllFromString(data);
                return true;
            }
            catch (LispException ex)
            {
                return !ex.Message.Contains("EOF:");
            }
        }

        public static string ReadCommand(bool debugging = false)
        {
            var data = "";

            while (String.IsNullOrWhiteSpace(data))
            {
                int counter = 1;
                string prompt;
                string debugText = debugging ? "> debug " : "";
                var package = Runtime.CurrentPackage();
                if (state.Count == 1)
                {
                    Console.WriteLine();
                    prompt = System.String.Format("{0} {2}> ", package.Name, counter, debugText);
                }
                else
                {
                    Console.WriteLine();
                    prompt = System.String.Format("{0} {2}: {3} > ", package.Name, counter, debugText, state.Count - 1);
                }

                Console.Write(prompt);

                data = BetterReadLine();
                if (String.IsNullOrWhiteSpace(data))
                {
                    // Show prompt again
                    continue;
                }

                while (!IsCompleteSourceCode(data))
                {
                    data += "\n" + BetterReadLine();
                }
            }

            return data;
        }

        static string BetterReadLine()
        {
            var top = Console.CursorTop;
            var left = Console.CursorLeft;
            var end = Console.BufferWidth - left - 1;
            var pos = 0;
            var len = 0;
            var buffer = new List<char>();

            while (true)
            {
                //
                // update display
                //

                Console.SetCursorPosition(left, top);
                for (var i = 0; i < len && i < end; ++i)
                {
                    Console.Write(buffer[i]);
                }
                for (var i = len; i < end; ++i)
                {
                    Console.Write(' ');
                }

                //
                // get next key
                //

                Console.SetCursorPosition(left + pos, top);
                var keyInfo = Console.ReadKey(true);
                var key = keyInfo.Key;
                var ch = keyInfo.KeyChar;

                switch (key)
                {
                    case ConsoleKey.Backspace:
                    {
                        if (pos > 0)
                        {
                            --pos;
                            --len;
                            buffer.RemoveAt(pos);
                        }
                        break;
                    }
                    case ConsoleKey.Delete:
                    {
                        if (pos < len)
                        {
                            --len;
                            buffer.RemoveAt(pos);
                        }
                        break;
                    }
                    case ConsoleKey.Tab:
                    {
                        break;
                    }
                    case ConsoleKey.Enter:
                    {
                        Console.WriteLine();
                        return new string(buffer.ToArray());
                    }
                    case ConsoleKey.Home:
                    {
                        pos = 0;
                        break;
                    }
                    case ConsoleKey.End:
                    {
                        pos = len;
                        break;
                    }
                    case ConsoleKey.LeftArrow:
                    {
                        if (pos > 0)
                        {
                            --pos;
                        }
                        break;
                    }
                    case ConsoleKey.RightArrow:
                    {
                        if (pos < end)
                        {
                            ++pos;
                        }
                        break;
                    }
                    default:
                    {
                        if (len == end)
                        {
                        }
                        else
                        {
                            if (ch != 0)
                            {
                                buffer.Insert(pos, ch);
                                ++pos;
                                ++len;
                            }
                        }
                        break;
                    }
                }
            }
        }

        public static void ReadEvalPrintLoop(string commandOptionArgument = null, bool initialized = false, bool debugging = false)
        {
            if (!initialized)
            {
                state = new Stack<ThreadContextState>();
                state.Push(Runtime.SaveStackAndFrame());
            }

            while (true)
            {
                try
                {
                    if (!initialized)
                    {
                        initialized = true;
                        Reset(0);
                    }

                    if (String.IsNullOrWhiteSpace(commandOptionArgument))
                    {
                        var command = ReadCommand(debugging);
                        EvalPrintCommand(command, debugging);
                    }
                    else
                    {
                        var scriptFile = commandOptionArgument;
                        commandOptionArgument = "";
                        Runtime.Run(scriptFile, Symbols.LoadPrintKeyword, false, Symbols.LoadVerboseKeyword, false);
                        if (!Runtime.Repl)
                        {
                            Runtime.Exit();
                        }
                    }
                }
                catch (ContinueFromBreakpointException)
                {
                    if (debugging)
                    {
                        throw;
                    }
                }
                catch (AbortingDebuggerException)
                {
                    if (debugging)
                    {
                        throw;
                    }
                }
                catch (AbortedDebuggerException)
                {
                }
                catch (InterruptException)
                {
                    // Effect of Ctrl+D
                    Console.WriteLine("Interrupt.");
                }
                catch (Exception ex)
                {
                    //ClearKeyboardBuffer();
                    ex = Runtime.UnwindException(ex);
                    LastException = ex;
                    Console.WriteLine(ex.Message);
                    state.Push(Runtime.SaveStackAndFrame());
                } 
            }
        }

        public static void RunCommand(Action<object> func, Cons lispCode, bool showTime = false, bool smartParens = false)
        {
            if (lispCode != null)
            {
                var head = Runtime.First(lispCode) as Symbol;
                var scope = Runtime.ReplGetCurrentAnalysisScope();

                if (smartParens && head != null)
                {
                    if (Runtime.LooksLikeFunction(head))
                    {
                        lispCode = Runtime.MakeCons(lispCode, (Cons)null);
                    }
                }

                timer.Reset();

                foreach (var expr in lispCode)
                {
                    var expr2 = Runtime.Compile(expr, scope);
                    timer.Start();
                    object val = Runtime.Execute(expr2);
                    if (Runtime.ToBool(Runtime.GetDynamic(Symbols.ReplForceIt)))
                    {
                        val = Runtime.Force(val);
                    }
                    timer.Stop();
                    if (func == null)
                    {
                        Runtime.SetSymbolValue(Symbols.It, val);
                        if (val != VOID.Value)
                        {
                            Console.Write("it: ");
                            Runtime.PrettyPrintLine(Console.Out, 4, null, val);
                        }
                    }
                    else
                    {
                        func(val);
                    }
                }

                var time = timer.ElapsedMilliseconds;
                if (showTime)
                {
                    Console.WriteLine("Elapsed time: {0} ms", time);
                }
            }
            else
            {
                func(Runtime.SymbolValue(Symbols.It));
            }
        }

        public static void RunConsoleMode(CommandLineOptions options)
        {
            Runtime.ConsoleMode = true;
            Runtime.GraphicalMode = false;
            Runtime.EmbeddedMode = false;
            Runtime.DebugMode = options.Debug;
            Runtime.Repl = options.Repl;
            Runtime.OptimizerEnabled = !Runtime.DebugMode;
            Runtime.ScriptName = options.ScriptName;
            Runtime.UserArguments = options.UserArguments;

            if (options.ScriptName == null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
                Console.WriteLine(GetVersion());
                Console.WriteLine(fileVersion.LegalCopyright);
                Console.WriteLine("Type `help` for help on top-level commands");
            }
            ReadEvalPrintLoop(commandOptionArgument: options.ScriptName, initialized: false);
        }


    }

}