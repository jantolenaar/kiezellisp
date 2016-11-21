#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
	using System;
	using System.Globalization;
	using System.Threading;

	class Program
	{
		#region Private Methods

		[STAThread]
		static void Main(string[] args)
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			var options = RuntimeGui.ParseArgs(args);
			RuntimeGui.RunGuiMode(options);
		}

		#endregion Private Methods
	}
}