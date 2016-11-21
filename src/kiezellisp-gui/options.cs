#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
	public class CommandLineOptions
	{
		#region Fields

		public bool Debug;
		public string ScriptName;
		public Cons UserArguments;

		#endregion Fields

		#region Constructors

		public CommandLineOptions()
		{
			ScriptName = null;
			Debug = true;
			UserArguments = null;
		}

		#endregion Constructors
	}

	public partial class RuntimeGui
	{
		#region Public Methods

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

		#endregion Public Methods
	}
}