// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Reflection;
using System.IO;

namespace Kiezel
{
    public class CommandLineOptions
    {
        public string ScriptName;
        public bool Gui;
        public bool Repl;
        public bool Debug;
        public Cons UserArguments;
        public int Width;
        public int Height;
        public int BufferHeight;
        public string ForeColor;
        public string BackColor;
        public string InfoColor;
        public string ErrorColor;
        public string WarningColor;
        public string HighlightForeColor;
        public string HighlightBackColor;
        public string ShadowBackColor;
        public string FontName;
        public int FontSize;

        public Prototype Defaults;

        public CommandLineOptions(Prototype defaults)
        {
            Defaults = defaults;

            ScriptName = null;
            Gui = false;
            Repl = true;
            Debug = true;
            UserArguments = null;
            Width = Init("width", 110);
            Height = Init("height", 35);
            BufferHeight = Init("buffer-height", 10 * Height);
            ForeColor = Init("fore-color", "window-text");
            BackColor = Init("back-color", "window");
            InfoColor = Init("info-color", "gray");
            ErrorColor = Init("error-color", "red");
            WarningColor = Init("warning-color", "brown");
            HighlightForeColor = Init("highlight-fore-color", "highlight-text");
            HighlightBackColor = Init("highlight-back-color", "highlight");
            ShadowBackColor = Init("shadow-back-color", "control-light");
            var win = Environment.OSVersion.Platform == PlatformID.Win32NT;
            FontName = Init("font-name", win ? "consolas" : "monospace");
            FontSize = Init("font-size", 12);
        }

        T Init<T>(string key, T defaultValue)
        {
            if (Defaults.HasProperty(key))
            {
                return (T)Defaults.GetValue(key);
            }
            else
            {
                return defaultValue;
            }
        }

    }

    public partial class RuntimeGfx
    {
        public static string GetGfxConfigurationFile()
        {
            // application config file is same folder as kiezellisp-lib.dll
            var assembly = Assembly.GetExecutingAssembly();
            var root = assembly.Location;
            var dir = Path.GetDirectoryName(root);
            return PathExtensions.Combine(dir, "kiezellisp-gfx.conf");
        }

        public static Prototype ParseGfxConfigurationFile(string path)
        {
            var contents = File.ReadAllText(path);
            var dict = (Prototype)contents.JsonDecode();
            return dict;
        }


        public static CommandLineOptions ParseArgs(string[] args)
        {
            var defaults = ParseGfxConfigurationFile(GetGfxConfigurationFile());
            var options = new CommandLineOptions(defaults);
            var parser = new CommandLineParser();

            parser.AddOption("--gui");
            parser.AddOption("--debug");
            parser.AddOption("--release");
            parser.AddOption("--repl");
            parser.AddOption("--no-repl");
            parser.AddOption("--width number");
            parser.AddOption("--height number");
            parser.AddOption("--buffer-height number");
            parser.AddOption("--fore-color name");
            parser.AddOption("--back-color name");
            parser.AddOption("--info-color name");
            parser.AddOption("--error-color name");
            parser.AddOption("--warning-color name");
            parser.AddOption("--highlight-fore-color name");
            parser.AddOption("--highlight-back-color name");
            parser.AddOption("--shadow-back-color name");
            parser.AddOption("--font-name name");
            parser.AddOption("--font-size size");

            parser.Parse(args);

            var s = parser.GetArgument(0);

            if (s != null)
            {
                options.ScriptName = s;
                options.Debug = false;
                options.Repl = false;
                options.UserArguments = Runtime.AsList(parser.GetArgumentArray(1));
            }

            if (parser.GetOption("repl") != null)
            {
                options.Repl = true;
            }

            if (parser.GetOption("no-repl") != null)
            {
                options.Repl = false;
            }

            if (parser.GetOption("gui") != null)
            {
                options.Gui = true;
                options.Repl = false;
            }

            if (parser.GetOption("release") != null)
            {
                options.Debug = false;
            }

            if (parser.GetOption("debug") != null)
            {
                options.Debug = true;
            }

            if ((s = parser.GetOption("width")) != null)
            {
                options.Width = (int)s.ParseNumber();
            }

            if ((s = parser.GetOption("height")) != null)
            {
                options.Height = (int)s.ParseNumber();
                options.BufferHeight = 10 * options.Height;
            }

            if ((s = parser.GetOption("buffer-height")) != null)
            {
                options.BufferHeight = (int)s.ParseNumber();
            }

            if ((s = parser.GetOption("fore-color")) != null)
            {
                options.ForeColor = s;
            }

            if ((s = parser.GetOption("back-color")) != null)
            {
                options.BackColor = s;
            }

            if ((s = parser.GetOption("info-color")) != null)
            {
                options.InfoColor = s;
            }

            if ((s = parser.GetOption("error-color")) != null)
            {
                options.ErrorColor = s;
            }

            if ((s = parser.GetOption("warning-color")) != null)
            {
                options.WarningColor = s;
            }

            if ((s = parser.GetOption("highlight-fore-color")) != null)
            {
                options.HighlightForeColor = s;
            }

            if ((s = parser.GetOption("highlight-back-color")) != null)
            {
                options.HighlightBackColor = s;
            }

            if ((s = parser.GetOption("shadow-back-color")) != null)
            {
                options.ShadowBackColor = s;
            }

            if ((s = parser.GetOption("font-name")) != null)
            {
                options.FontName = s;
            }

            if ((s = parser.GetOption("font-size")) != null)
            {
                options.FontSize = (int)s.ParseNumber();
            }

            return options;
        }
    }
}

