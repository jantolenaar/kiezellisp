#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
	public class CommandLineOptions
	{
		#region Fields

		public int DebugLevel;
		public string ScriptName;
		public Cons UserArguments;

		#endregion Fields

		#region Constructors

		public CommandLineOptions()
		{
			ScriptName = null;
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

			parser.Parse(args);

			var s = parser.GetArgument(0);

			if (s != null)
			{
				options.ScriptName = s;
				options.DebugLevel = 0;
				options.UserArguments = Runtime.AsList(parser.GetArgumentArray(1));
			}

			return options;
		}

		#endregion Public Methods
	}
}