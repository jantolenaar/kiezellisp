// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;

namespace Kiezel
{
    public class CommandLineOptions
    {
        public string ScriptName;
        public bool HasGui;
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

        public CommandLineOptions()
        {
            ScriptName = null;
            HasGui = false;
            Debug = true;
            UserArguments = null;
            Width = 110;
            Height = 35;
            BufferHeight = 10 * Height;
            ForeColor = "window-text";
            BackColor = "window";
            InfoColor = "gray";
            ErrorColor = "red";
            WarningColor = "brown";
            HighlightForeColor = "highlight-text";
            HighlightBackColor = "highlight";
            ShadowBackColor = "control-light";
            var win = Environment.OSVersion.Platform == PlatformID.Win32NT;
            FontName = win ? "consolas" : "monospace";
            FontSize = 12;
        }
    }

    public partial class RuntimeConsole
    {
        public static CommandLineOptions ParseArgs(string[] args)
        {
            var parser = new CommandLineParser();
            var options = new CommandLineOptions();

            parser.AddOption("--gui");
            parser.AddOption("--debug");
            parser.AddOption("--release");
            parser.AddOption("--width number");
            parser.AddOption("--height number");
            parser.AddOption("--buffer-height number");
            parser.AddOption("--status-height number");
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
                options.UserArguments = Runtime.AsList(parser.GetArgumentArray(1));
            }

            options.HasGui = (parser.GetOption("gui") != null);

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

