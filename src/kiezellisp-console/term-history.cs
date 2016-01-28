// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Kiezel
{
    public class TerminalHistory
    {
        private int cursor;
        private string histfile;
        private Vector lines;

        public TerminalHistory(string app = "kiezellisp")
        {
            if (app != null)
            {
                string dir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

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
                }
            }

            lines = new Vector();
            cursor = 0;

            if (File.Exists(histfile))
            {
                using (StreamReader sr = File.OpenText(histfile))
                {
                    StringBuilder buf = new StringBuilder();
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line != "")
                        {
                            if (line[0] == '\x1F')
                            {
                                Append(buf.ToString());
                                buf.Length = 0;
                            }
                            else
                            {
                                if (buf.Length != 0)
                                {
                                    buf.Append('\n');
                                }
                                buf.Append(line);
                            }
                        }
                    }
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
                        sw.WriteLine(s);
                        sw.WriteLine('\x1F');
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        public void CursorToEnd()
        {
            cursor = lines.Count - 1;
        }

        public string Line(int index)
        {
            if (0 <= index && index < lines.Count)
            {
                return (string)lines[index];
            }
            else
            {
                return "";
            }
        }

        public string Next()
        {
            if (cursor + 1 < Count)
            {
                return (string)lines[++cursor];
            }
            else
            {
                return "";
            }
        }

        public string Previous()
        {
            if (cursor > 0)
            {
                return (string)lines[--cursor];
            }
            else
            {
                return "";
            }
        }

        public void RemoveLast()
        {
            if (lines.Count > 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }
        }

        //
        // Updates the current cursor location with the string,
        // to support editing of history items.   For the current
        // line to participate, an Append must be done before.
        //
        public void Update(string s)
        {
            lines[cursor] = s;
        }
    }

}

