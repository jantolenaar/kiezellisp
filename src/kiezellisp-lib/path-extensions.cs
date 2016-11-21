#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System.IO;

    public static class PathExtensions
    {
        #region Public Methods

        [Extends(typeof(Path))]
        public static string Combine(string path1, string path2)
        {
            return GetUnixName(Path.Combine(path1, path2));
        }

        [Extends(typeof(Path))]
        public static string GetDirectoryName(string path)
        {
            return GetUnixName(Path.GetDirectoryName(path));
        }

        [Extends(typeof(Path))]
        public static string GetFullPath(string path)
        {
            return GetUnixName(Path.GetFullPath(path));
        }

        [Extends(typeof(Path))]
        public static string GetUnixName(string name)
        {
            return name.Replace(@"\", @"/");
        }

        [Extends(typeof(Path))]
        public static string GetWindowsName(string name)
        {
            return name.Replace("/", "\\");
        }

        #endregion Public Methods
    }
}