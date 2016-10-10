// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Linq;


namespace Kiezel
{
    [RestrictedImport]
    public partial class RuntimeGfx
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
            names.Add(prefix);
            names.Sort();
            return names;

        }

        public static void Quit()
        {
            Terminal.History.Close();
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
                    Terminal.WriteLine("Command not found");
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
                    Terminal.Write("Ambiguous command. Did you mean:");
                    for (int i = 0; i < commandsPrefix.Count; ++i)
                    {
                        Terminal.Write("{0} {1}", (i == 0 ? "" : i + 1 == commandsPrefix.Count ? " or" : ","), commandsPrefix[i]);
                    }
                    Terminal.WriteLine("?");
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
                        Terminal.StdScr.Clear();
                        Terminal.History.Clear();
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
                        Runtime.DumpDictionary(Terminal.Out, Runtime.GetGlobalVariablesDictionary(pattern));
                        break;
                    }

                    case ":variables":
                    {
                        var pos = Runtime.Integerp(Runtime.Second(code)) ? (int)Runtime.Second(code) : 0;
                        Runtime.DumpDictionary(Terminal.Out, Runtime.GetLexicalVariablesDictionary(pos));
                        break;
                    }

                    case ":$variables":
                    {
                        var pos = Runtime.Integerp(Runtime.Second(code)) ? (int)Runtime.Second(code) : 0;
                        Runtime.DumpDictionary(Terminal.Out, Runtime.GetDynamicVariablesDictionary(pos));
                        break;
                    }

                    case ":backtrace":
                    {
                        Terminal.Write(Runtime.GetEvaluationStack());
                        break;
                    }

                    case ":Exception":
                    {
                        Terminal.WriteLine(LastException.ToString());
                        break;
                    }

                    case ":exception":
                    {
                        Terminal.WriteLine(RemoveDlrReferencesFromException(LastException));
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
                        Terminal.ResetColors();
                        Terminal.CloseAllWindows();
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
            //bool isExternalInput = false;

            while (String.IsNullOrWhiteSpace(data))
            {
                int counter = Terminal.History.Count + 1;
                string prompt;
                string debugText = debugging ? "> debug " : "";
                var package = Runtime.CurrentPackage();
                if (state.Count == 1)
                {
                    Terminal.WriteLine();
                    prompt = System.String.Format("{0} {1} {2}> ", package.Name, counter, debugText);
                }
                else
                {
                    Terminal.WriteLine();
                    prompt = System.String.Format("{0} {1} {2}: {3} > ", package.Name, counter, debugText, state.Count - 1);
                }

                Terminal.Write(prompt);
                data = Terminal.ReadLine();

            }

            Terminal.History.Append(data);

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
                    Runtime.PrintLogColor(Terminal.StdScr, "error", "Interrupt.");
                }
                catch (Exception ex)
                {
                    //ClearKeyboardBuffer();
                    ex = Runtime.UnwindException(ex);
                    LastException = ex;
                    Runtime.PrintLogColor(Terminal.StdScr, "error", ex.Message);
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
                            Terminal.Write("it: ");
                            Runtime.PrettyPrintLine(Terminal.Out, 4, null, val);
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
                    Terminal.WriteLine("Elapsed time: {0} ms", time);
                }
            }
            else
            {
                func(Runtime.SymbolValue(Symbols.It));
            }
        }

        public static void RunGuiReplMode(CommandLineOptions options)
        {
            Runtime.ConsoleMode = false;
            Runtime.GraphicalMode = true;
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
                Terminal.WriteLine(GetVersion());
                Terminal.WriteLine(fileVersion.LegalCopyright);
                Terminal.WriteLine("Type `help` for help on top-level commands");
            }

            ReadEvalPrintLoop(commandOptionArgument: options.ScriptName, initialized: false);
        }

        public static void ClearKeyboardBuffer()
        {
            Terminal.TerminalWindow.ClearKeyboardBuffer();
        }

        [Lisp("playback")]
        public static void Playback(object file)
        {
            Playback(file, 1, 0);
        }

        [Lisp("playback")]
        public static void Playback(object file, int delayAfterKey, int delayAfterMessage)
        {
            var path = Runtime.FindSourceFile(file);
            var script = FileExtensions.ReadAllText(path);
            PlaybackScript(script, delayAfterKey, delayAfterMessage);
        }

        [Lisp("playback-script")]
        public static void PlaybackScript(string script)
        {
            PlaybackScript(script, 1, 0);
        }

        [Lisp("playback-script")]
        public static void PlaybackScript(string script, int delayAfterKey, int delayAfterMessage)
        {
            var list = GetPlaybackList(script, delayAfterKey, delayAfterMessage);
            Terminal.TerminalWindow.LoadKeyboardBuffer(list);
        }

        [Lisp("get-playback-list")]
        public static List<KeyInfo> GetPlaybackList(string text, int delayAfterKey, int delayAfterMessage)
        {
            // remove spaces at end-of-line
            text = text + "\n; THE END";
            var lines = text.Trim().Split(new char[] { '\n' }).Select(x => x.TrimEnd());
            var v = new List<KeyInfo>();
            foreach (var line in lines)
            {
                if (line.StartsWith(";"))
                {
                    ParseInstructions(line, v, delayAfterMessage);
                }
                else
                {
                    ParseKeyStrokes(line, v, delayAfterKey);
                }
            }
            return v;
        }

        static void ParseKeyStrokes(string text, List<KeyInfo> keys, int delay)
        {
            var i = 0;
            while (i < text.Length)
            {
                var ch = text[i];
                if (ch == '<' && i + 1 < text.Length && char.IsLetter(text, i + 1))
                {
                    var a = i + 1;
                    var b = text.IndexOf(">", a) - 1;
                    if (a <= b)
                    {
                        var data = text.Substring(a, b - a + 1);
                        var parts = data.Split(new char[] { '+' });
                        var code = (TerminalKeys)0;
                        foreach (var part in parts)
                        {
                            code |= GetKeyCode(part);
                        }
                        keys.Add(new KeyInfo(code, 0, 0, 0, 0));
                        keys.Add(new PlaybackInfo { Time = delay });
                    }
                    i = b + 2;
                    continue;
                }

                if (ch >= ' ')
                {
                    keys.Add(new KeyInfo(ch));
                    keys.Add(new PlaybackInfo { Time = delay });
                }

                i += 1;
            }
        }

        static void ParseInstructions(string text, List<KeyInfo> results, int delay)
        {
            text = text.Trim(" ;");

            PlaybackInfo playback = results.LastOrDefault() as PlaybackInfo;
            if (playback == null || playback.Lines.Count == 0)
            {
                playback = new PlaybackInfo();
                results.Add(playback);
            }

            if (text == "")
            {
                playback.Time = delay;
            }
            else if (char.IsDigit(text, 0))
            {
                int count = Runtime.AsInt(1000 * Runtime.AsDecimal(text.Trim().ParseNumber()));
                playback.Time = count;
            }
            else
            {
                playback.Lines.Add(text);
                playback.Time = delay;
            }
        }

        public static TerminalKeys GetKeyCode(string name)
        {
            return (TerminalKeys)Runtime.TryConvertToEnumType(typeof(TerminalKeys), name);
        }
    }

}