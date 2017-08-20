#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    public class CommandLineOptions
    {
        #region Fields

        public bool Repl;
        public string ScriptName;
        public Cons UserArguments;
        public int ForegroundColor;
        public int BackgroundColor;
        public bool Mono5;

        #endregion Fields

        #region Constructors

        public CommandLineOptions()
        {
            ScriptName = null;
            UserArguments = null;
            Repl = true;
            ForegroundColor = -1;
            BackgroundColor = -1;
            Mono5 = false;
        }

        #endregion Constructors
    }

    public partial class RuntimeConsole
    {
        #region Public Methods

        public static CommandLineOptions ParseArgs(string[] args)
        {
            var parser = new CommandLineParser();
            var options = new CommandLineOptions();

            parser.AddOption("--repl");
            parser.AddOption("--fg number");
            parser.AddOption("--bg number");
            parser.AddOption("--mono5");

            parser.Parse(args);

            var s = parser.GetArgument(0);

            if (s != null)
            {
                options.ScriptName = s;
                options.Repl = false;
                options.UserArguments = Runtime.AsList(parser.GetArgumentArray(1));
            }

            if (parser.GetOption("fg") != null)
            {
                options.ForegroundColor = -1 + Number.ParseNumberBase(parser.GetOption("fg"), 10);
            }

            if (parser.GetOption("bg") != null)
            {
                options.BackgroundColor = -1 + Number.ParseNumberBase(parser.GetOption("bg"), 10);
            }

            if (parser.GetOption("repl") != null)
            {
                options.Repl = true;
            }

            if (parser.GetOption("mono5") != null)
            {
                options.Mono5 = true;
            }

            return options;
        }

        #endregion Public Methods
    }
}