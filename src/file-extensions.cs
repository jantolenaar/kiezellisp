// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.IO;
using System.Globalization;
using System.Linq;

namespace Kiezel
{
    internal static class FileExtensions
    {
        [Extends(typeof(File))]
        public static string ReadAllText( string path )
        {
            var contents = File.ReadAllText( path );
            return contents.ConvertToInternalLineEndings();
        }

        [Extends( typeof( File ) )]
        public static string ReadAllText( string path, Encoding encoding )
        {
            var contents = File.ReadAllText( path, encoding );
            return contents.ConvertToInternalLineEndings();
        }

        [Extends( typeof( File ) )]
        public static void WriteAllText( string path, string contents )
        {
            contents = contents.ConvertToExternalLineEndings();
            File.WriteAllText( path, contents );
        }

        [Extends( typeof( File ) )]
        public static void WriteAllText( string path, string contents, Encoding encoding )
        {
            contents = contents.ConvertToExternalLineEndings();
            File.WriteAllText( path, contents, encoding );
        }

    }
}
