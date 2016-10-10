// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;

namespace Kiezel
{
    public class CommandLineOptions
    {
        public string ScriptName;
        public Cons UserArguments;
        public bool Debug;
        public bool Repl;

        public CommandLineOptions()
        {
            ScriptName = null;
            UserArguments = null;
            Debug = true;
            Repl = true;
        }
    }

    public partial class RuntimeConsole
    {
        public static CommandLineOptions ParseArgs(string[] args)
        {
            var parser = new CommandLineParser();
            var options = new CommandLineOptions();

            parser.AddOption("--debug");
            parser.AddOption("--release");
            parser.AddOption("--repl");
            parser.AddOption("--no-repl");

            parser.Parse(args);

            var s = parser.GetArgument(0);

            if (s != null)
            {
                options.ScriptName = s;
                options.Debug = false;
                options.Repl = false;
                options.UserArguments = Runtime.AsList(parser.GetArgumentArray(1));
            }

            if (parser.GetOption("release") != null)
            {
                options.Debug = false;
            }

            if (parser.GetOption("debug") != null)
            {
                options.Debug = true;
            }

            if (parser.GetOption("repl") != null)
            {
                options.Repl = true;
            }

            if (parser.GetOption("no-repl") != null)
            {
                options.Repl = false;
            }

            return options;
        }
    }
}

