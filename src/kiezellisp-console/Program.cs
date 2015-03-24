using System;
using System.Threading;
using System.Globalization;

namespace Kiezel
{
    class Program
    {
        [STAThread]
        static void Main( string[] args )
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            ConsoleMode.Run( args );
        }
    }
}
