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

        #endregion Fields

        #region Constructors

        public CommandLineOptions()
        {
            ScriptName = null;
            UserArguments = null;
            Repl = true;
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

            parser.Parse(args);

            var s = parser.GetArgument(0);

            if (s != null)
            {
                options.ScriptName = s;
                options.Repl = false;
                options.UserArguments = Runtime.AsList(parser.GetArgumentArray(1));
            }

            if (parser.GetOption("repl") != null)
            {
                options.Repl = true;
            }

            return options;
        }

        #endregion Public Methods
    }
}