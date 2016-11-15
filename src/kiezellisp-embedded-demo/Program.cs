#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace kiezellisp_embedded_demo
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class Program
    {
        #region Methods

        static void Main(string[] args)
        {
            try
            {
                Kiezel.EmbeddedMode.Init(debugMode: false);
                var a = Kiezel.EmbeddedMode.Funcall("+", 2, 3);
                Console.WriteLine("a={0}", a);
                Kiezel.EmbeddedMode.Funcall("++", 3, 4);
            }
            catch (Exception ex)
            {
                var s = Kiezel.EmbeddedMode.GetDiagnostics(ex);
                Console.WriteLine(s);
            }
        }

        #endregion Methods
    }
}