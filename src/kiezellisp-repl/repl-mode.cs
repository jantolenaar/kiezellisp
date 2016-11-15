#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Windows.Forms;

    public partial class RuntimeRepl
    {
        #region Methods

        [Lisp("more")]
        public static void More(string text)
        {
            var win = StdScr;
            var height = win.WindowHeight - 1;
            var count = 0;
            foreach (var ch in text)
            {
                var old = win.CursorLeft;
                win.Write(ch);
                if (win.CursorLeft <= old)
                {
                    ++count;
                    if (count == height)
                    {
                        var prompt = "(Press ' ' or ENTER to continue, 'q' or ESC to quit)";
                        win.Reverse = true;
                        win.Write(prompt);
                        win.Reverse = false;
                        var key = win.SelectKey(Keys.Space, Keys.Enter, Keys.Q, Keys.Escape);
                        win.Write("\r");
                        win.Write(new String(' ', prompt.Length));
                        win.Write("\r");
                        if (key >= 2)
                        {
                            return;
                        }
                        if (key == 0)
                        {
                            // accept next screen
                            height = win.WindowHeight - 1;
                            count = 0;
                        }
                        else
                        {
                            // accept next line
                            height = 1;
                            count = 0;
                        }
                    }
                }
            }
        }

        [Lisp("playback")]
        public static void Playback(string script, params object[] args)
        {
            var kwargs = Runtime.ParseKwargs(args, new string[] { "window", "delay", "delimiter" });
            var window = (TextWindow)(kwargs[0] ?? StdScr);
            var delay = (int)(kwargs[1] ?? Runtime.GetDynamic(Symbols.PlaybackDelay));
            var prefix = (string)(kwargs[2] ?? Runtime.GetDynamic(Symbols.PlaybackDelimiter));
            var suffix = GetSuffixFromPrefix(prefix);
            var list = GetPlaybackList(script, prefix, suffix);
            foreach (var key in list)
            {
                window.SendKey(key);
                if (delay > 0)
                {
                    Runtime.Sleep(delay);
                }
            }
        }

        public static string ReplRead()
        {
            return StdScr.Read();
        }

        public static void ReplResetDisplay()
        {
            ResetColors();
            StdScr.Clear();
        }

        public static object RunGuiReplMode()
        {
            while (!TerminalWindow.Visible)
            {
                Runtime.Sleep(5);
            }
            var options = Options;
            Runtime.ProgramFeature = "kiezellisp-repl";
            Runtime.DebugMode = options.Debug;
            Runtime.Repl = options.Repl;
            Runtime.OptimizerEnabled = !Runtime.DebugMode;
            Runtime.ScriptName = options.ScriptName;
            Runtime.UserArguments = options.UserArguments;

            if (options.ScriptName == null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
                Out.WriteLine(Runtime.GetVersion());
                Out.WriteLine(fileVersion.LegalCopyright);
                Out.WriteLine("Type `help` for help on top-level commands");
            }

            ReadEvalPrintLoop(commandOptionArgument: options.ScriptName, initialized: false);

            return null;
        }

        internal static Keys GetKeyCode(string name)
        {
            switch (name)
            {
                default:
                {
                    return (Keys)Runtime.TryConvertToEnumType(typeof(Keys), name);
                }
            }
        }

        internal static List<KeyInfo> GetPlaybackList(string text, string prefix, string suffix)
        {
            // remove spaces at end-of-line
            var lines = text.Trim().Split(new char[] { '\n' }).Select(x => x.TrimEnd());
            var v = new List<KeyInfo>();
            foreach (var line in lines)
            {
                if (!line.StartsWith(";"))
                {
                    ParseKeyStrokes(line, prefix, suffix, v);
                }
            }
            return v;
        }

        internal static string GetSuffixFromPrefix(string prefix)
        {
            var s = new String(prefix.Reverse().ToArray());
            return s.Replace("[", "]").Replace("{", "}").Replace("(", ")").Replace("<", ">");
        }

        internal static void ParseKeyStrokes(string text, string prefix, string suffix, List<KeyInfo> keys)
        {
            var pos = 0;

            while (pos < text.Length)
            {
                var beg = text.IndexOf(prefix, pos);
                if (beg == -1)
                {
                    break;
                }
                foreach (var ch in text.Substring(pos,beg-pos))
                {
                    keys.Add(new KeyInfo(ch));
                }
                beg += prefix.Length;
                var end = text.IndexOf(suffix, beg);
                if (end == -1)
                {
                    end = text.Length;
                }
                var data = text.Substring(beg, end - beg);
                var parts = data.Split(new char[] { '+' });
                var code = (Keys)0;
                foreach (var part in parts)
                {
                    code |= GetKeyCode(part);
                }
                keys.Add(new KeyInfo(code));
                pos = end + 1;
            }

            foreach (var ch in text.Substring(pos))
            {
                keys.Add(new KeyInfo(ch));
            }
        }

        #endregion Methods
    }
}