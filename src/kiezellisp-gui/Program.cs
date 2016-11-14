// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Globalization;
using System.Threading;

[assembly: CLSCompliant(false)]

namespace Kiezel
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            var options = RuntimeGui.ParseArgs(args);
            RuntimeGui.RunGuiMode(options);
        }
    }

}
