// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.IO;

namespace Kiezel
{
 
    public class WindowTextWriter: TextWriter
    {
        Window window;
        object oldColor;

        public WindowTextWriter(Window window)
        {
            this.window = window;
        }

        public override void WriteLine(string value)
        {
            Write(value);
            Write('\n');
        }

        public override void Write(string value)
        {
            if (!window.HtmlEnabled)
            {
                WriteString(value);
                return;
            }

            var beg = value.IndexOf("<");
            if (beg == -1)
            {
                WriteString(value);
                return;
            }

            var end = value.IndexOf(">", beg);
            if (end == -1)
            {
                WriteString(value);
                return;
            }

            var head = value.Substring(0, beg);
            var middle = value.Substring(beg, end - beg + 1);
            var tail = value.Substring(end + 1);

            WriteString(head);

            switch (middle)
            {
                case "<normal>":
                {
                    window.Style = 0;
                    break;
                }
                case "<b>":
                case "<bold>":
                {
                    window.Bold = true;
                    break;
                }
                case "<i>":
                case "<italic>":
                {
                    window.Italic = true;
                    break;
                }
                case "<u>":
                case "<underline>":
                {
                    window.Underline = true;
                    break;
                }
                case "<strike>":
                case "<strikeout>":
                {
                    window.Strikeout = true;
                    break;
                }
                case "<shadow>":
                {
                    window.Shadow = true;
                    break;
                }
                case "<highlight>":
                {
                    window.Highlight = true;
                    break;
                }
                case "<reverse>":
                {
                    window.Reverse = true;
                    break;
                }
                case "</b>":
                case "</bold>":
                {
                    window.Bold = false;
                    break;
                }
                case "</i>":
                case "</italic>":
                {
                    window.Italic = false;
                    break;
                }
                case "</u>":
                case "</underline>":
                {
                    window.Underline = false;
                    break;
                }
                case "</strike>":
                case "</strikeout>":
                {
                    window.Strikeout = false;
                    break;
                }
                case "</shadow>":
                {
                    window.Shadow = false;
                    break;
                }
                case "</highlight>":
                {
                    window.Highlight = false;
                    break;
                }
                case "</reverse>":
                {
                    window.Reverse = false;
                    break;
                }
                default:
                {
                    if (middle.StartsWith("</") && middle.EndsWith(">"))
                    {
                        window.ForeColor = oldColor;
                    }
                    else if (middle.StartsWith("<") && middle.EndsWith(">"))
                    {
                        var color = middle.Substring(1, middle.Length - 2);
                        if (color.Length != 0 && char.IsLower(color, 0))
                        {
                            oldColor = window.ForeColor;
                            window.ForeColor = middle.Substring(1, middle.Length - 2);
                        }
                        else
                        {
                            WriteString(middle);
                        }
                    }
                    else
                    {
                        WriteString(middle);
                    }
                    break;
                }
            }

            Write(tail);

            //Application.DoEvents();
        }

        void WriteString(string value)
        {
            foreach (var ch in value)
            {
                Write(ch);
            }
        }

        public override void Write(char value)
        {  
            window.Write(value, window.AutoRefreshEnabled);
        }

        public override System.Text.Encoding Encoding
        {
            get
            {
                throw new NotImplementedException();
            }
        }

    }

}

