#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System.IO;
    using System.Text;

    public static class FileExtensions
    {
        #region Public Methods

        [Extends(typeof(File))]
        public static string ReadAllText(string path)
        {
            var contents = File.ReadAllText(path);
            return contents.ConvertToInternalLineEndings();
        }

        [Extends(typeof(File))]
        public static string ReadAllText(string path, Encoding encoding)
        {
            var contents = File.ReadAllText(path, encoding);
            return contents.ConvertToInternalLineEndings();
        }

        [Extends(typeof(File))]
        public static string[] ReadSharedAllLines(string path)
        {
            var contents = ReadSharedAllText(path);
            return contents.Split('\n');
        }

        [Extends(typeof(File))]
        public static string ReadSharedAllText(string path)
        {
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var file = new StreamReader(stream))
                {
                    var contents = file.ReadToEnd();
                    return contents.ConvertToInternalLineEndings();
                }
            }
        }

        [Extends(typeof(File))]
        public static void WriteAllText(string path, string contents)
        {
            contents = contents.ConvertToExternalLineEndings();
            File.WriteAllText(path, contents);
        }

        [Extends(typeof(File))]
        public static void WriteAllText(string path, string contents, Encoding encoding)
        {
            contents = contents.ConvertToExternalLineEndings();
            File.WriteAllText(path, contents, encoding);
        }

        #endregion Public Methods
    }
}