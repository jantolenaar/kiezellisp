// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System.IO;

namespace Kiezel
{
    internal static class PathExtensions
    {
        [Extends( typeof( Path ) )]
        public static string Combine( string path1, string path2 )
        {
            return GetUnixName( Path.Combine( path1, path2 ) );
        }

        [Extends( typeof( Path ) )]
        public static string GetDirectoryName( string path )
        {
            return GetUnixName( Path.GetDirectoryName( path ) );
        }

        [Extends( typeof( Path ) )]
        public static string GetUnixName( string name )
        {
            return name.Replace( @"\", @"/" );
        }

        [Extends( typeof( Path ) )]
        public static string GetWindowsName( string name )
        {
            return name.Replace( "/", "\\" );
        }
    }
}