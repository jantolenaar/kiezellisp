#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.IO;

    public class TextWindowTextWriter : TextWriter, ILogWriter
    {
        #region Fields

        object oldColor;
        TextWindow window;

        #endregion Fields

        #region Constructors

        public TextWindowTextWriter(TextWindow window)
        {
            this.window = window;
        }

        #endregion Constructors

        #region Properties

        public override System.Text.Encoding Encoding
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion Properties

        #region Methods

        void ILogWriter.WriteLog(string style, string msg)
        {
            ((ILogWriter)window).WriteLog(style, msg);
        }

        public override void Write(string value)
        {
            if (string.IsNullOrEmpty(window.HtmlPrefix))
            {
                WriteString(value);
                return;
            }

            var beg = value.IndexOf(window.HtmlPrefix);
            if (beg == -1)
            {
                WriteString(value);
                return;
            }

            var end = value.IndexOf(window.HtmlSuffix, beg);
            if (end == -1)
            {
                WriteString(value);
                return;
            }

            var head = value.Substring(0, beg);
            var b = beg + window.HtmlPrefix.Length;
            var middle = value.Substring(b, end - b);
            var e = end + window.HtmlSuffix.Length;
            var tail = value.Substring(e);

            WriteString(head);

            switch (middle)
            {
                case "normal":
                {
                    window.Style = 0;
                    break;
                }
                case "b":
                case "bold":
                {
                    window.Bold = true;
                    break;
                }
                case "i":
                case "italic":
                {
                    window.Italic = true;
                    break;
                }
                case "u":
                case "underline":
                {
                    window.Underline = true;
                    break;
                }
                case "strike":
                case "strikeout":
                {
                    window.Strikeout = true;
                    break;
                }
                case "shadow":
                {
                    window.Shadow = true;
                    break;
                }
                case "highlight":
                {
                    window.Highlight = true;
                    break;
                }
                case "reverse":
                {
                    window.Reverse = true;
                    break;
                }
                case "/b":
                case "/bold":
                {
                    window.Bold = false;
                    break;
                }
                case "/i":
                case "/italic":
                {
                    window.Italic = false;
                    break;
                }
                case "/u":
                case "/underline":
                {
                    window.Underline = false;
                    break;
                }
                case "/strike":
                case "/strikeout":
                {
                    window.Strikeout = false;
                    break;
                }
                case "/shadow":
                {
                    window.Shadow = false;
                    break;
                }
                case "/highlight":
                {
                    window.Highlight = false;
                    break;
                }
                case "/reverse":
                {
                    window.Reverse = false;
                    break;
                }
                default:
                {
                    if (middle.StartsWith("/"))
                    {
                        window.ForeColor = oldColor;
                    }
                    else
                    {
                        var color = middle;
                        if (color.Length != 0 && char.IsLower(color, 0) && color.IndexOf(' ') == -1)
                        {
                            oldColor = window.ForeColor;
                            switch (color)
                            {
                                case "info":
                                {
                                    window.ForeColor = RuntimeRepl.DefaultInfoColor;
                                    break;
                                }
                                case "warning":
                                {
                                    window.ForeColor = RuntimeRepl.DefaultWarningColor;
                                    break;
                                }
                                case "error":
                                {
                                    window.ForeColor = RuntimeRepl.DefaultErrorColor;
                                    break;
                                }
                                default:
                                {
                                    window.ForeColor = color;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            WriteString(window.HtmlPrefix);
                            WriteString(middle);
                            WriteString(window.HtmlSuffix);
                        }
                    }
                    break;
                }
            }

            Write(tail);
        }

        public override void Write(char value)
        {
            window.Write(value);
        }

        public override void WriteLine(string value)
        {
            Write(value);
            Write('\n');
        }

        void WriteString(string value)
        {
            foreach (var ch in value)
            {
                Write(ch);
            }
        }

        #endregion Methods
    }
}