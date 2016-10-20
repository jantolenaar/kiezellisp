// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Kiezel
{
    public class ReplHistory
    {
        private int cursor;
        private List<string> lines;
        private string histfile;

        public ReplHistory()
        {
            cursor = 0;
            lines = new List<string>();
            histfile = null;
        }

        public ReplHistory(string app) : this()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "kiezellisp");

            if (!Directory.Exists(dir))
            {
                try
                {
                    Directory.CreateDirectory(dir);
                }
                catch
                {
                    app = null;
                }
            }

            if (app != null)
            {
                histfile = PathExtensions.Combine(dir, app) + ".history";
            
                if (File.Exists(histfile))
                {
                    foreach (var ln in File.ReadAllLines(histfile))
                    {
                        lines.Add(ln.Replace("<CRLF>", "\n"));
                    }
                    cursor = lines.Count;
                }
            }
        }

        public int Count
        {
            get
            {
                return lines.Count;
            }
        }

        public void Append(string s)
        {
            s = s.TrimEnd();
            if (lines.Contains(s))
            {
                lines.Remove(s);
            }
            lines.Add(s);
            cursor = Count;
        }

        public void Clear()
        {
            lines.Clear();
            cursor = 0;
        }

        public void Close()
        {
            if (histfile == null)
            {
                return;
            }

            try
            {
                using (StreamWriter sw = File.CreateText(histfile))
                {
                    foreach (string s in lines)
                    {                       
                        sw.WriteLine(s.Replace("\n", "<CRLF>"));
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        public string Line(int index)
        {
            if (0 <= index && index < lines.Count)
            {
                return lines[index];
            }
            else
            {
                return "";
            }
        }

        public string Next()
        {
            if (cursor < Count)
            {
                ++cursor;
            }

            return Line(cursor);
        }

        public string Previous()
        {
            if (cursor >= 0)
            {
                --cursor;
            }

            return Line(cursor);
        }

    }

}


