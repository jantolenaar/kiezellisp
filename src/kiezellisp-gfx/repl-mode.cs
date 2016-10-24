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
    public partial class RuntimeGfx: RuntimeConsoleBase
    {
        [Lisp("more")]
        public static void More(string text)
        {
            var win = Terminal.StdScr;
            var height = win.Height - 1;
            var count = 0;
            foreach (var ch in text)
            {
                var old = win.Col;
                win.Write(ch);
                if (win.Col <= old)
                {
                    ++count;
                    if (count == height)
                    {
                        win.Reverse = true;
                        win.Write("(Press 'q' or ESC to cancel, any other key to continue)");
                        win.Reverse = false;
                        var info = win.ReadKey();
                        win.Write("\r                                              \r");
                        height = win.Height - 1;
                        count = 0;
                        if (info.KeyData == TerminalKeys.Escape || info.KeyData == TerminalKeys.Q)
                        {
                            return;
                        }
                    }
                }
            }
        }

        public static void ReplResetDisplay()
        {
            Terminal.ResetColors();
            Terminal.CloseAllWindows();
            Terminal.StdScr.Clear();
        }

        public static string ReplReadLine()
        {
            return Terminal.ReadLine();
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
                Terminal.WriteLine(Runtime.GetVersion());
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