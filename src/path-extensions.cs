// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

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

    internal static class PathExtensions
    {
        [Extends(typeof(Path))]
        public static string GetUnixName( string name )
        {
            return name.Replace( @"\", @"/" );
        }

        [Extends( typeof( Path ) )]
        public static string GetWindowsName( string name )
        {
            return name.Replace( "/", "\\" );
        }

        [Extends( typeof( Path ) )]
        public static string GetDirectoryName( string path )
        {
            return GetUnixName( Path.GetDirectoryName( path ) );
        }

        [Extends( typeof( Path ) )]
        public static string Combine( string path1, string path2 )
        {
            return GetUnixName( Path.Combine( path1, path2 ) );
        }

    }
}
