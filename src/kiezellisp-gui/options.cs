// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Reflection;
using System.IO;

namespace Kiezel
{
    public class CommandLineOptions
    {
        public string ScriptName;
        public bool Debug;
        public Cons UserArguments;

        public CommandLineOptions()
        {
            ScriptName = null;
            Debug = true;
            UserArguments = null;
        }

    }

    public partial class RuntimeGui
    {

        public static CommandLineOptions ParseArgs(string[] args)
        {
            var options = new CommandLineOptions();
            var parser = new CommandLineParser();

            parser.AddOption("--debug");
            parser.AddOption("--release");

            parser.Parse(args);

            var s = parser.GetArgument(0);

            if (s != null)
            {
                options.ScriptName = s;
                options.Debug = false;
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

            return options;
        }
    }
}

