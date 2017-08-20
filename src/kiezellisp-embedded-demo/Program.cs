#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace kiezellisp_embedded_demo
{
    using System;

    class Program
    {
        #region Private Methods

        static void Main()
        {
            try
            {
                Kiezel.EmbeddedMode.Init();
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

        #endregion Private Methods
    }
}