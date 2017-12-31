#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public partial class RuntimeConsole
    {
        #region Static Fields

        public static ReplHistory History = new ReplHistory();
        public static Exception LastException;
        public static string[] ReplCommands =
        {
            "clear", "continue", "globals", "quit",
            "abort", "backtrace", "variables", "$variables",
            "top", "exception", "Exception", "force",
            "describe", "reset", "time", "modify", "eval"
        };
        public static Stack<ThreadContextState> state;
        public static Stopwatch timer = Stopwatch.StartNew();

        #endregion Static Fields

        #region Public Methods

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

        public static string ExtractCommand(Cons code)
        {
            var head = Runtime.First(code);
            var sym = head as Symbol;
            var cons = head as Cons;

            if (sym != null)
            {
                if (Runtime.Keywordp(sym))
                {
                    return sym.Name;
                }
                else if (sym.Name == "?")
                {
                    return "?";
                }
            }

            if (cons != null)
            {
                var head2 = Runtime.First(cons);
                if (head2 == Symbols.Unquote)
                {
                    return Runtime.MakeString(Runtime.Second(cons));
                }
            }

            return null;
        }

        public static void EvalPrintCommand(string data, bool debugging)
        {
            var output = GetConsoleOut();
            bool leadingSpace = char.IsWhiteSpace(data, 0);
            Cons code = Runtime.ReadAllFromString(data);

            if (code == null)
            {
                return;
            }

            Runtime.RestoreStackAndFrame(state.Peek());

            var dotCommand = ExtractCommand(code);

            if (dotCommand != null)
            {
                var command = "";
                var commandsPrefix = ReplCommands.Where(x => x.StartsWith(dotCommand)).ToList();
                var commandsExact = ReplCommands.Where(x => x == dotCommand).ToList();

                if (commandsPrefix.Count == 0)
                {
                    Runtime.PrintLine(output, "Command not found");
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
                else {
                    Runtime.PrintLine(output, "Ambiguous command. Did you mean:");
                    for (var i = 0; i < commandsPrefix.Count; ++i)
                    {
                        var str = string.Format("{0} {1}", (i == 0 ? "" : i + 1 == commandsPrefix.Count ? " or" : ","), commandsPrefix[i]);
                        Runtime.PrintLine(output, str);
                    }
                    Runtime.PrintLine(output, "?");
                    return;
                }

                switch (command)
                {
                    case "continue":
                        {
                            if (debugging)
                            {
                                throw new ContinueFromBreakpointException();
                            }
                            break;
                        }
                    case "clear":
                        {
                            History.Clear();
                            Console.Clear();
                            state = new Stack<ThreadContextState>();
                            state.Push(Runtime.SaveStackAndFrame());
                            break;
                        }
                    case "abort":
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

                    case "top":
                        {
                            while (state.Count > 1)
                            {
                                state.Pop();
                            }
                            break;
                        }

                    case "quit":
                        {
                            Quit();
                            break;
                        }

                    case "globals":
                        {
                            var pattern = (string)Runtime.Second(code);
                            Runtime.DumpDictionary(output, Runtime.GetGlobalVariablesDictionary(pattern));
                            break;
                        }

                    case "eval":
                        {
                            var expr = Runtime.Second(code);
                            var pos = Runtime.Integerp(Runtime.Third(code)) ? (int)Runtime.Third(code) : 0;
                            var val = EvalCommand(expr, pos);
                            if (Runtime.ToBool(Runtime.GetDynamic(Symbols.ReplForceIt)))
                            {
                                val = Runtime.Force(val);
                            }
                            Runtime.SetSymbolValue(Symbols.It, val);
                            if (val != VOID.Value)
                            {
                                Runtime.PrintStream(output, "", "it: ");
                                Runtime.PrettyPrintLine(output, 4, null, val);
                            }
                            break;
                        }

                    case "modify":
                        {
                            var name = (Symbol)Runtime.Second(code);
                            var expr = Runtime.Third(code);
                            var pos = Runtime.Integerp(Runtime.Fourth(code)) ? (int)Runtime.Fourth(code) : 0;
                            ModifyCommand(name, expr, pos);
                            break;
                        }

                    case "variables":
                        {
                            var pos = Runtime.Integerp(Runtime.Second(code)) ? (int)Runtime.Second(code) : 0;
                            Runtime.DumpDictionary(output, Runtime.GetLexicalVariablesDictionary(pos));
                            break;
                        }

                    case "$variables":
                        {
                            var pos = Runtime.Integerp(Runtime.Second(code)) ? (int)Runtime.Second(code) : 0;
                            Runtime.DumpDictionary(output, Runtime.GetDynamicVariablesDictionary(pos));
                            break;
                        }

                    case "backtrace":
                        {
                            Runtime.PrintLine(output, Runtime.GetEvaluationStack());
                            break;
                        }

                    case "Exception":
                        {
                            Runtime.PrintLine(output, LastException.ToString());
                            break;
                        }

                    case "exception":
                        {
                            Runtime.PrintLine(output, RemoveDlrReferencesFromException(LastException));
                            break;
                        }

                    case "force":
                        {
                            var expr = Runtime.Second(code) ?? Symbols.It;
                            RunCommand(null, Runtime.MakeList(Runtime.MakeList(Symbols.Force, expr)));
                            break;
                        }

                    case "time":
                        {
                            var expr = Runtime.Second(code) ?? Symbols.It;
                            RunCommand(null, Runtime.MakeList(expr), showTime: true);
                            break;
                        }

                    case "describe":
                        {
                            RunCommand(x =>
                            {
                                Runtime.SetSymbolValue(Symbols.It, x);
                                Runtime.Describe(x);
                            }, Runtime.Cdr(code));
                            break;
                        }

                    case "reset":
                        {
                            var level = Runtime.Integerp(Runtime.Second(code)) ? (int)Runtime.Second(code) : -1;
                            while (state.Count > 1)
                            {
                                state.Pop();
                            }
                            var t1 = Runtime.GetCpuTime();
                            Reset(level);
                            var t2 = Runtime.GetCpuTime();
                            var t = t2 - t1;
                            var msg = String.Format("Reset {0:N3}s user {1:N3}s system", t.User, t.System);
                            Runtime.PrintTrace(msg);
                            break;
                        }

                }
            }
            else {
                RunCommand(null, code, smartParens: !leadingSpace);
            }
        }

        public static List<string> GetCompletions(string prefix)
        {
            var nameset = new HashSet<string>();

            foreach (string s in ReplCommands)
            {
                if (s.StartsWith(prefix))
                {
                    nameset.Add(s);
                }
            }

            Runtime.FindCompletions(prefix, nameset);

            var names = nameset.ToList();

            if (names.Count == 0)
            {
                //names.Add(prefix);
            }
            names.Sort();
            if (names.Count > 50)
            {
                names = names.GetRange(0, 50);
                names.Add("*** Too many completions ***");
            }
            return names;
        }

        public static object GetConsoleErr()
        {
            return Runtime.AssertStream(Symbols.StdErr.Value);
        }

        public static object GetConsoleLog()
        {
            return Runtime.AssertStream(Symbols.StdLog.Value);
        }

        public static object GetConsoleOut()
        {
            return Runtime.AssertStream(Symbols.StdOut.Value);
        }

        public static int[] GetIntegerArgs(string lispCode)
        {
            var results = new List<int>();
            foreach (var expr in Runtime.ReadAllFromString(lispCode))
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

        public static bool IsNotDlrCode(string line)
        {
            return line.IndexOf("CallSite") == -1
            && line.IndexOf("System.Dynamic") == -1
            && line.IndexOf("Microsoft.Scripting") == -1;
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

        public static void Quit()
        {
            History.Close();
            Environment.Exit(0);
        }

        public static string ReadCommand(bool debugging = false)
        {
            var output = GetConsoleOut();
            var data = "";

            while (string.IsNullOrWhiteSpace(data))
            {
                // This happens when only ENTER is pressed
                // Show prompt again

                string prompt;
                string debugText = debugging ? "> debug " : "";
                var package = Runtime.CurrentPackage();
                if (Console.IsInputRedirected)
                {
                    if (state.Count == 1)
                    {
                        Runtime.PrintLine(output);
                        prompt = string.Format("{0} {1}> ", package.Name, debugText);
                    }
                    else
                    {
                        Runtime.PrintLine(output);
                        prompt = string.Format("{0} {1}: {2} > ", package.Name, debugText, state.Count - 1);
                    }
                }
                else
                {
                    if (state.Count == 1)
                    {
                        Runtime.PrintLine(output);
                        prompt = string.Format("{0} {1}> ", package.Name, debugText);
                    }
                    else
                    {
                        Runtime.PrintLine(output);
                        prompt = string.Format("{0} {1}: {2} > ", package.Name, debugText, state.Count - 1);
                    }
                }
                Runtime.Print(output, prompt);

                while (true)
                {
                    var chunk = ReplRead(true, true, true);
                    if (chunk == null)
                    {
                        data = "";
                    }
                    else
                    {
                        data = data + (data.Length == 0 ? "" : " ") + chunk;
                        if (IsCompleteSourceCode(data))
                        {
                            break;
                        }
                    }
                }
            }

            History.Append(data);

            return data;
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
                        Reset(-1);
                    }

                    if (string.IsNullOrWhiteSpace(commandOptionArgument))
                    {
                        var command = ReadCommand(debugging);
                        EvalPrintCommand(command, debugging);
                    }
                    else {
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
                    Runtime.PrintStream(GetConsoleErr(), "error", "Interrupt.\n");
                }
                catch (Exception ex)
                {
                    //ClearKeyboardBuffer();
                    ex = Runtime.UnwindException(ex);
                    LastException = ex;
                    Runtime.PrintStream(GetConsoleErr(), "error", ex.Message + "\n");
                    state.Push(Runtime.SaveStackAndFrame());
                }
            }
        }

        public static string RemoveDlrReferencesFromException(Exception ex)
        {
            return string.Join("\n", ex.ToString().Split('\n').Where(IsNotDlrCode));
        }

        public static object WrapInFrame(object expr, int pos)
        {
            var dict = Runtime.GetLexicalVariablesDictionary(pos);
            if (dict.Count != 0)
            {
                var block = new Vector();
                block.Add(Symbols.Do);
                foreach (var item in dict)
                {
                    block.Add(Runtime.MakeList(Symbols.Let, item.Key, item.Value));
                    block.Add(Runtime.MakeList(Symbols.Declare, Runtime.MakeList(Symbols.Ignore, item.Key)));
                }
                block.Add(expr);
                return Runtime.AsList(block);
            }
            else
            {
                return expr;
            }
        }

        public static object EvalCommand(object expr, int pos)
        {
            var expr1 = WrapInFrame(expr, pos);
            var expr2 = Runtime.Compile(expr1);
            object val = Runtime.Execute(expr2);
            return val;
        }

        public static void ModifyCommand(Symbol name, object expr, int pos)
        {
            var val = Runtime.Force(EvalCommand(expr, pos));
            var frame = Runtime.GetFrameAt(pos);
            if (frame == null || !frame.Modify(name, val))
            {
                Runtime.PrintError("Lexical variable not found");
            }
        }

        public static void RunCommand(Action<object> func, Cons lispCode, bool showTime = false, bool smartParens = false)
        {
            var output = GetConsoleOut();

            if (lispCode != null)
            {
                var head = Runtime.First(lispCode) as Symbol;

                if (smartParens && head != null)
                {
                    if (Runtime.LooksLikeFunction(head))
                    {
                        lispCode = Runtime.MakeCons(lispCode, (Cons)null);
                    }
                }

                var t1 = Runtime.GetCpuTime();

                foreach (var expr in lispCode)
                {
                    var val = EvalCommand(expr, 0);
                    if (Runtime.ToBool(Runtime.GetDynamic(Symbols.ReplForceIt)))
                    {
                        val = Runtime.Force(val);
                    }
                    if (func == null)
                    {
                        Runtime.SetSymbolValue(Symbols.It, val);
                        if (val != VOID.Value)
                        {
                            Runtime.PrintStream(output, "", "it: ");
                            Runtime.PrettyPrintLine(output, 4, null, val);
                        }
                    }
                    else {
                        func(val);
                    }
                }


                if (showTime)
                {
                    var t2 = Runtime.GetCpuTime();
                    var t = t2 - t1;
                    var msg = String.Format("Time {0:N3}s user {1:N3}s system", t.User, t.System);
                    Runtime.PrintTrace(msg);
                }
            }
            else {
                func(Runtime.SymbolValue(Symbols.It));
            }
        }

        #endregion Public Methods

    }
}