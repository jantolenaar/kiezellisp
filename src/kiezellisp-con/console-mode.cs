// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Linq;
using System.Text;
using System.IO;

namespace Kiezel
{
    [RestrictedImport]
    public partial class RuntimeConsole: RuntimeConsoleBase
    {
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

        public static void ReplResetDisplay()
        {
            Console.Clear();
        }

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
            var buffer = new List<char>();
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

            Action MoveBackOverSpaces = () =>
            {
                if (0 < pos && (pos == len || buffer[pos] == ' '))
                {
                    while (0 < pos && buffer[pos - 1] == ' ')
                    {
                        --pos;
                    }
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
                    case ConsoleKey.Enter:
                    {
                        var s = new string(buffer.ToArray());
                        if (mod != ConsoleModifiers.Control && IsCompleteSourceCode(s))
                        {
                            return s;
                        }
                        else
                        {
                            writeChar('\n');
                            buffer.Add('\n');
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
                            buffer = new List<char>(s);
                            pos = len = buffer.Count;
                        }
                        break;
                    }
                    case ConsoleKey.DownArrow:
                    {
                        if (History != null)
                        {
                            var s = History.Next();
                            buffer = new List<char>(s);
                            pos = len = buffer.Count;
                        }
                        break;
                    }
                    case ConsoleKey.Escape:
                    {
                        buffer = new List<char>();
                        pos = len = buffer.Count;
                        Paint();
                        Erase();
                        Console.SetCursorPosition(col, row);
                        return null;
                    }
                    case ConsoleKey.Tab:
                    {
                        MoveBackOverSpaces();
                        var text = new string(buffer.GetRange(0, pos).ToArray());                      
                        var searchTerm = Runtime.GetWordFromString(text, pos, Runtime.IsLispWordChar);
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
                            for (int i = 0; i < completions.Count; ++i)
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
                            if (key2 == ConsoleKey.Enter || (key2 == ConsoleKey.Tab && completions.Count == 1))
                            {
                                pos = posOrig - searchTerm.Length;
                                buffer.RemoveRange(pos, searchTerm.Length);
                                var inserted = completions[index] + " ";
                                buffer.InsertRange(pos, inserted);
                                len = buffer.Count;
                                pos += inserted.Length;
                                done = true;
                            }
                            else if (key2 == ConsoleKey.Tab)
                            {
                                index = (index + 1) % completions.Count;
                            }
                            else if (key2 == ConsoleKey.Escape)
                            {
                                pos = posOrig;
                                done = true;
                            }
                        }
                        break;
                    }
                    case ConsoleKey.V:
                    {
                        if (mod == ConsoleModifiers.Control)
                        {
                            var text = Runtime.GetClipboardData();
                            foreach (var ch2 in text)
                            {
                                var ch3 = (ch2 == '\n' || ch2 >= ' ') ? ch2 : ' ';
                                buffer.Insert(pos, ch3);
                                ++pos;
                                ++len;
                            }
                        }
                        else
                        {
                            buffer.Insert(pos, ch);
                            ++pos;
                            ++len;
                        }
                        break;
                    }
                    case ConsoleKey.C:
                    {
                        if (mod == ConsoleModifiers.Control)
                        {
                            var text = new string(buffer.ToArray());
                            Runtime.SetClipboardData(text);
                        }
                        else
                        {
                            buffer.Insert(pos, ch);
                            ++pos;
                            ++len;
                        }
                        break;
                    }
                    default:
                    {
                        if (ch != 0)
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


    }

}