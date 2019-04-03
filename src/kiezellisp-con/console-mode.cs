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
    public partial class RuntimeConsole
    {
        #region Public Methods

        public static void WriteReverse(string text, bool reverse)
        {
            if (reverse)
            {
                if (Runtime.HasFeature("ansi-terminal"))
                {
                    Console.Write("\x1b[7m");
                    Console.Write(text);
                    Console.Write("\x1b[27m");
                }
                else
                {
                    Console.Write("[");
                    Console.Write(text);
                    Console.Write("]");
                }
            }
            else
            {
                Console.Write(text);
            }
        }

        [Lisp("more")]
        public static void More(string text)
        {
            if (Console.IsInputRedirected)
            {
                Console.Write(text);
                return;
            }

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
                        WriteReverse(prompt, true);
                        while (true)
                        {
                            var info = ReplReadKey();
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

        [Lisp("read-from-console")]
        public static string ReadFromConsole()
        {
            return ReplRead(false, false, false);
        }

        [Lisp("read-line-from-console")]
        public static string ReadLineFromConsole()
        {
            return ReplRead(false, false, true);
        }

        public static string ReplRead(bool lispCompletion, bool symbolCompletion, bool crlf)
        {
            try
            {
                if (Console.IsInputRedirected)
                {
                    var s = Console.ReadLine();
                    if (s == null)
                    {
                        Runtime.Exit();
                    }
                    return s;
                }
                else
                {
                    var s = ReplReadImp(lispCompletion, symbolCompletion, crlf);
                    return s;
                }
            }
            catch (Exception ex)
            {
                Console.Clear();
                Console.WriteLine(Runtime.GetDiagnostics(ex));
                Console.WriteLine("Temporarily lost control due to console display changes. Input aborted.");
                Console.Write("Press ENTER to continue.");
                Console.ReadLine();
                return "";
            }
        }

        public static ConsoleKeyInfo ReplReadKey()
        {
            var keyInfo = Console.ReadKey(true);
            return keyInfo;
        }

        public static void ReplReadTest()
        {
            while (true)
            {
                var i = ReplReadKey();
                Console.WriteLine("{0} {1} {2}", i.Key, i.Modifiers, (int)i.KeyChar);
            }
        }

        public static string ReplReadImp(bool lispCompletion, bool symbolCompletion, bool crlf)
        {
            //ReplReadTest ();

            var top = Console.CursorTop;
            var left = Console.CursorLeft;
            var pos = 0;
            var buffer = new StringBuilder();
            var col = 0;
            var row = 0;

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
                        --row;
                    }
                }
            };

            Action<string> writeStr = (string str) =>
            {
                foreach (var ch in str)
                {
                    writeChar(ch);
                }
            };

            Action cursorReset = () =>
            {
                Console.SetCursorPosition(left, top);
                row = top;
                col = left;
            };

            Action paint = () =>
            {
                //
                // update display
                //

                cursorReset();
                for (var i = 0; i < buffer.Length; ++i)
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

            Action erase = () =>
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
                paint();
                erase();

                //
                // get next key
                //

                ConsoleKeyInfo keyInfo;
                ConsoleKey key;
                ConsoleModifiers mod;
                char ch;

                Console.SetCursorPosition(col, row);

                keyInfo = ReplReadKey();

            codeCompletionRetry:

                key = keyInfo.Key;
                mod = keyInfo.Modifiers;
                ch = keyInfo.KeyChar;

                switch (key)
                {
                    case ConsoleKey.Backspace:
                        {
                            if (pos > 0)
                            {
                                --pos;
                                buffer.Remove(pos, 1);
                            }
                            break;
                        }
                    case ConsoleKey.Delete:
                        {
                            if (pos < buffer.Length)
                            {
                                buffer.Remove(pos, 1);
                            }
                            break;
                        }
                    case ConsoleKey.Enter:
                        {
                            var s = buffer.ToString();
                            if (!lispCompletion || (mod != ConsoleModifiers.Control && IsCompleteSourceCode(s)))
                            {
                                pos = buffer.Length;
                                paint();
                                if (crlf)
                                {
                                    writeChar('\n');
                                }
                                return s;
                            }
                            else
                            {
                                writeChar('\n');
                                buffer.Append('\n');
                                ++pos;
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
                            pos = buffer.Length;
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
                            if (pos < buffer.Length)
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
                                pos = buffer.Length;
                            }
                            break;
                        }
                    case ConsoleKey.DownArrow:
                        {
                            if (History != null)
                            {
                                var s = History.Next();
                                buffer = new StringBuilder(s);
                                pos = buffer.Length;
                            }
                            break;
                        }
                    case ConsoleKey.Escape:
                        {
                            buffer.Clear();
                            pos = buffer.Length;
                            paint();
                            erase();
                            Console.SetCursorPosition(col, row);
                            return null;
                        }
                    case ConsoleKey.Tab:
                        {
                            if (!symbolCompletion)
                            {
                                break;
                            }
                            var line = buffer.ToString();
                            var loc = Runtime.LocateWord(line, pos, false);
                            var prefix = line.Substring(0, loc.Begin);
                            var searchTerm = line.Substring(loc.Begin, loc.Span);
                            var suffix = line.Substring(loc.Begin + loc.Span);
                            var completions = GetCompletions(searchTerm);
                            var common = FindLongestCommonStringLength(completions);
                            if (common != 0)
                            {
                                searchTerm = completions[0].Substring(0, common);
                            }
                            var index = -1;
                            var done = false;
                            pos = buffer.Length;
                            while (!done)
                            {
                                var selectedTerm = index == -1 ? searchTerm : completions[index];

                                if (completions.Count < 2)
                                {
                                    buffer = new StringBuilder();
                                    buffer.Append(prefix);
                                    buffer.Append(selectedTerm);
                                    pos = buffer.Length;
                                    buffer.Append(suffix);
                                    break;
                                }

                                cursorReset();
                                erase();

                                cursorReset();

                                writeStr(prefix);
                                writeStr(selectedTerm);

                                var col2 = Console.CursorLeft;
                                var row2 = Console.CursorTop - row;

                                writeStr(suffix);
                                writeStr("\n");

                                writeStr("Completing symbol ");
                                writeStr(searchTerm);
                                writeStr("\n");

                                for (var i = 0; i < completions.Count; ++i)
                                {
                                    WriteReverse(completions[i], i == index);
                                    writeChar(' ');
                                }

                                Console.SetCursorPosition(col2, row + row2);

                                var keyInfo2 = ReplReadKey();
                                var key2 = keyInfo2.Key;
                                if (key2 == ConsoleKey.Enter)
                                {
                                    buffer = new StringBuilder(prefix + selectedTerm);
                                    pos = buffer.Length;
                                    buffer.Append(suffix);
                                    done = true;
                                }
                                else if (key2 == ConsoleKey.Tab && (keyInfo2.Modifiers & ConsoleModifiers.Shift) != 0)
                                {
                                    // Stay positive.
                                    index = (index + completions.Count - 1) % completions.Count;
                                }
                                else if (key2 == ConsoleKey.Tab)
                                {
                                    index = (index + 1) % completions.Count;
                                }
                                else if (key2 == ConsoleKey.Escape)
                                {
                                    buffer = new StringBuilder(prefix + searchTerm);
                                    pos = buffer.Length;
                                    buffer.Append(suffix);
                                    done = true;
                                }
                                else
                                {
                                    buffer = new StringBuilder(prefix + selectedTerm);
                                    pos = buffer.Length;
                                    buffer.Append(suffix);
                                    keyInfo = keyInfo2;
                                    paint();
                                    erase();
                                    Console.SetCursorPosition(col, row);
                                    goto codeCompletionRetry;

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
                                        }
                                        break;
                                    case ConsoleKey.U:
                                        var text3 = buffer.ToString(0, pos);
                                        buffer.Remove(0, pos);
                                        pos = 0;
                                        break;
                                    case ConsoleKey.K:
                                        var text4 = buffer.ToString(pos, buffer.Length - pos);
                                        buffer.Remove(pos, buffer.Length - pos);
                                        break;
                                    case ConsoleKey.W:
                                        var line = buffer.ToString();
                                        var loc = Runtime.LocateWord(line, pos, true);
                                        var text5 = buffer.ToString(loc.Begin, pos - loc.Begin);
                                        buffer.Remove(loc.Begin, pos - loc.Begin);
                                        pos = loc.Begin;
                                        break;
                                }
                            }
                            else if (ch == '\n')
                            {
                                var s = buffer.ToString();
                                if (mod != ConsoleModifiers.Control && IsCompleteSourceCode(s))
                                {
                                    pos = buffer.Length;
                                    paint();
                                    if (crlf)
                                    {
                                        writeChar('\n');
                                    }
                                    return s;
                                }
                                else
                                {
                                    writeChar('\n');
                                    buffer.Append('\n');
                                    ++pos;
                                }
                            }
                            else //if (ch >= ' ')
                            {
                                buffer.Insert(pos, ch);
                                ++pos;
                            }
                            break;
                        }
                }
            }
        }

        public static int FindLongestCommonStringLength(List<string> strings)
        {
            if (strings.Count == 0)
            {
                return 0;
            }

            if (strings.Count == 1)
            {
                return strings[0].Length;
            }

            var pos = 0;
            var str = strings[0];
            for (; pos < str.Length; ++pos)
            {
                for (var i = 1; i < strings.Count; ++i)
                {
                    var s = strings[i];
                    if (pos >= s.Length)
                    {
                        break;
                    }
                    if (str[pos] != s[pos])
                    {
                        return pos;
                    }
                }
            }

            return pos;
        }

        public static void RunConsoleMode(CommandLineOptions options)
        {
            Runtime.ProgramFeature = "kiezellisp-con";
            Runtime.Repl = options.Repl;
            Runtime.ScriptName = options.ScriptName;
            Runtime.UserArguments = options.UserArguments;

            if (options.ScriptName == null)
            {
                Console.Clear();
                Console.WriteLine(Runtime.GetVersionString());
                Console.WriteLine(Runtime.GetCopyrightString());
            }
            ReadEvalPrintLoop(commandOptionArgument: options.ScriptName, initialized: false);
        }

        #endregion Public Methods
    }
}