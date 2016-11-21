#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
	using System;

	public partial class RuntimeGui
	{
		#region Public Methods

		public static void RunGuiMode(CommandLineOptions options)
		{
			Runtime.ProgramFeature = "kiezellisp-gui";
			Runtime.DebugMode = options.Debug;
			Runtime.Repl = false;
			Runtime.OptimizerEnabled = !Runtime.DebugMode;
			Runtime.ScriptName = options.ScriptName;
			Runtime.UserArguments = options.UserArguments;

			try
			{
				Runtime.Reset();
				Runtime.RestartLoadFiles(0);
				Runtime.Run(options.ScriptName, Symbols.LoadPrintKeyword, false, Symbols.LoadVerboseKeyword, false);
				Runtime.Exit();
			}
			catch (Exception ex)
			{
				Runtime.PrintTrace(Runtime.GetDiagnostics(ex));
			}
		}

		#endregion Public Methods
	}
}