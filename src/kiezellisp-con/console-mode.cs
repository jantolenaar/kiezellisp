#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;

    [RestrictedImport]
    public partial class RuntimeConsole : RuntimeConsoleBase
    {
        #region Public Methods

        [Lisp("more")]
        public static void More(string text)
        {
            var height = Console.WindowHeight - 1;
            var count = 0;
            foreach (var ch in text)
            {
                var old = Console.CursorLeft;
                Console.Write(ch);
                if (Console.CursorLeft <= old)
                {
                    ++count;
                    if (count == height)
                    {
                        var prompt = "(Press ' ' or ENTER to continue, 'q' or ESC to quit)";
                        Console.Write(prompt);
                        while (true)
                        {
                            var info = Console.ReadKey(true);
                            if (info.Modifiers == 0)
                            {
                                if (info.Key == ConsoleKey.Spacebar)
                                {
                                    Console.Write("\r");
                                    Console.Write(new String(' ', prompt.Length));
                                    Console.Write("\r");
                                    height = Console.WindowHeight - 1;
                                    count = 0;
                                    break;
                                }
                                else if (info.Key == ConsoleKey.Enter)
                                {
                                    Console.Write("\r");
                                    Console.Write(new String(' ', prompt.Length));
                                    Console.Write("\r");
                                    height = 1;
                                    count = 0;
                                    break;
                                }
                                else if (info.Key == ConsoleKey.Escape || info.Key == ConsoleKey.Q)
                                {
                                    Console.Write("\r");
                                    Console.Write(new String(' ', prompt.Length));
                                    Console.Write("\r");
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        [Lisp("read-console")]
        public static string ReplRead()
        {
            try
            {
                var s = ReplReadImp();
                return s;
            }
            catch (Exception)
            {
                Console.Clear();
                Console.WriteLine("Temporarily lost control due to console display changes. Input aborted.");
                Console.Write("Press ENTER to continue.");
                Console.ReadLine();
                return "";
            }
        }

        public static string ReplReadImp()
        {
            var top = Console.CursorTop;
            var left = Console.CursorLeft;
            var len = 0;
            var pos = 0;
            var buffer = new StringBuilder();
            var col = 0;
            var row = 0;
            var topChoice = 0;

            Action<char> writeChar = (char ch) =>
            {
                var r1 = Console.CursorTop;
                var c1 = Console.CursorLeft;
                Console.Write(ch);
                var r2 = Console.CursorTop;
                var c2 = Console.CursorLeft;
                if (r1 == Console.BufferHeight - 1)
                {
                    // on last row
                    if (c1 > c2 || ch == '\n')
                    {
                        // scrolled
                        --top;
                        --topChoice;
                        --row;
                    }
                }
            };

            Action Paint = () =>
            {
                //
                // update display
                //

                Console.SetCursorPosition(left, top);
                row = top;
                col = left;

                for (var i = 0; i < len; ++i)
                {
                    writeChar(buffer[i]);

                    if (i + 1 == pos)
                    {
                        row = Console.CursorTop;
                        col = Console.CursorLeft;
                    }

                }

                writeChar(' ');
            };

            Action Erase = () =>
            {
                var start = Console.CursorLeft + Console.CursorTop * Console.BufferWidth;
                var end = (Console.WindowTop + Console.WindowHeight) * Console.BufferWidth - 1;
                if (start < end)
                {
                    var blanks = new String(' ', end - start);
                    Console.Write(blanks);
                }
            };

            while (true)
            {
                Paint();
                Erase();

                //
                // get next key
                //

                Console.SetCursorPosition(col, row);
                var keyInfo = Console.ReadKey(true);
                var key = keyInfo.Key;
                var mod = keyInfo.Modifiers;
                var ch = keyInfo.KeyChar;

                switch (key)
                {
                    case ConsoleKey.Backspace:
                        {
                            if (pos > 0)
                            {
                                --pos;
                                --len;
                                buffer.Remove(pos, 1);
                            }
                            break;
                        }
                    case ConsoleKey.Delete:
                        {
                            if (pos < len)
                            {
                                --len;
                                buffer.Remove(pos, 1);
                            }
                            break;
                        }
                    case ConsoleKey.Enter:
                        {
                            var s = buffer.ToString();
                            if (mod != ConsoleModifiers.Control && IsCompleteSourceCode(s))
                            {
                                return s;
                            }
                            else {
                                writeChar('\n');
                                buffer.Append('\n');
                                ++pos;
                                ++len;
                            }
                            break;
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
                            if (pos < len)
                            {
                                ++pos;
                            }
                            break;
                        }
                    case ConsoleKey.UpArrow:
                        {
                            if (History != null)
                            {
                                var s = History.Previous();
                                buffer = new StringBuilder(s);
                                pos = len = buffer.Length;
                            }
                            break;
                        }
                    case ConsoleKey.DownArrow:
                        {
                            if (History != null)
                            {
                                var s = History.Next();
                                buffer = new StringBuilder(s);
                                pos = len = buffer.Length;
                            }
                            break;
                        }
                    case ConsoleKey.Escape:
                        {
                            buffer.Clear();
                            pos = len = buffer.Length;
                            Paint();
                            Erase();
                            Console.SetCursorPosition(col, row);
                            return null;
                        }
                    case ConsoleKey.Tab:
                        {
                            var line = buffer.ToString();
                            var loc = Runtime.LocateLeftWord(line, pos);
                            var searchTerm = line.Substring(loc.Begin, loc.Span);
                            var completions = RuntimeConsoleBase.GetCompletions(searchTerm);
                            var posOrig = pos;
                            var index = 0;
                            var leftChoice = 0;
                            var done = false;
                            pos = len;
                            while (!done)
                            {
                                Paint();
                                writeChar('\n');
                                for (var i = 0; i < completions.Count; ++i)
                                {
                                    foreach (var ch3 in completions[i])
                                    {
                                        writeChar(ch3);
                                    }
                                    if (i == index)
                                    {
                                        topChoice = Console.CursorTop;
                                        leftChoice = Console.CursorLeft;
                                    }
                                    writeChar(' ');
                                    writeChar(' ');
                                }
                                Erase();
                                Console.SetCursorPosition(leftChoice, topChoice);
                                var keyInfo2 = Console.ReadKey(true);
                                var key2 = keyInfo2.Key;
                                if (key2 == ConsoleKey.DownArrow || key2 == ConsoleKey.Enter || (key2 == ConsoleKey.Tab && completions.Count == 1))
                                {
                                    line = line.Remove(loc.Begin, loc.Span).Insert(loc.Begin, completions[index]);
                                    pos = loc.Begin + completions[index].Length;
                                    if (pos == line.Length || !char.IsWhiteSpace(line, pos))
                                    {
                                        line = line.Insert(pos, " ");
                                    }
                                    ++pos;
                                    buffer = new StringBuilder(line);
                                    len = buffer.Length;
                                    done = true;
                                }
                                else if (key2 == ConsoleKey.LeftArrow || (key2 == ConsoleKey.Tab && (keyInfo2.Modifiers & ConsoleModifiers.Shift) != 0))
                                {
                                    // Stay positive.
                                    index = (index + completions.Count - 1) % completions.Count;
                                }
                                else if (key2 == ConsoleKey.RightArrow || key2 == ConsoleKey.Tab)
                                {
                                    index = (index + 1) % completions.Count;
                                }
                                else if (key2 == ConsoleKey.UpArrow || key2 == ConsoleKey.Escape)
                                {
                                    pos = posOrig;
                                    done = true;
                                }
                            }
                            break;
                        }
                    default:
                        {
                            if (mod == ConsoleModifiers.Control)
                            {
                                switch (key)
                                {
                                    case ConsoleKey.V:
                                        var text = Runtime.GetClipboardData();
                                        foreach (var ch2 in text)
                                        {
                                            var ch3 = (ch2 == '\n' || ch2 >= ' ') ? ch2 : ' ';
                                            buffer.Insert(pos, ch3);
                                            ++pos;
                                            ++len;
                                        }
                                        break;
                                    case ConsoleKey.C:
                                        var text2 = buffer.ToString();
                                        Runtime.SetClipboardData(text2);
                                        break;
                                    case ConsoleKey.U:
                                        buffer.Remove(0, pos);
                                        pos = 0;
                                        len = buffer.Length;
                                        break;
                                    case ConsoleKey.K:
                                        buffer.Remove(pos, buffer.Length - pos);
                                        len = buffer.Length;
                                        break;
                                    case ConsoleKey.W:
                                        var line = buffer.ToString();
                                        var loc = Runtime.LocateLeftWord(line, pos);
                                        buffer.Remove(loc.Begin, pos - loc.Begin);
                                        pos = loc.Begin;
                                        len = buffer.Length;
                                        break;
                                }
                            }
                            else if (ch >= ' ')
                            {
                                buffer.Insert(pos, ch);
                                ++pos;
                                ++len;
                            }
                            break;
                        }
                }
            }
        }

        public static void ReplResetDisplay()
        {
            Console.Clear();
        }

        public static void RunConsoleMode(CommandLineOptions options)
        {
            Runtime.ProgramFeature = "kiezellisp-con";
            Runtime.DebugMode = options.Debug;
            Runtime.Repl = options.Repl;
            Runtime.OptimizerEnabled = !Runtime.DebugMode;
            Runtime.ScriptName = options.ScriptName;
            Runtime.UserArguments = options.UserArguments;

            if (options.ScriptName == null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
                Console.WriteLine(Runtime.GetVersion());
                Console.WriteLine(fileVersion.LegalCopyright);
                Console.WriteLine("Type `help` for help on top-level commands");
            }
            ReadEvalPrintLoop(commandOptionArgument: options.ScriptName, initialized: false);
        }

        #endregion Public Methods
    }
}