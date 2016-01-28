using System;
using System.IO;

namespace Kiezel
{
    class WindowTextWriter: TextWriter
    {
        Window window;
        ColorType fgOrig;
        int attrOrig;
        bool savedOrig;

        public WindowTextWriter(Window window)
        {
            this.window = window;
            savedOrig = false;
        }

        public override void Write(string value)
        {
            var beg = value.IndexOf("!{");
            if (beg == -1)
            {
                WriteString(value);
                return;
            }

            var end = value.IndexOf("}", beg);
            if (end == -1)
            {
                end = value.Length;
            }

            if (!savedOrig)
            {
                fgOrig = window.ForeColor;
                attrOrig = window.Attr;
                savedOrig = true;
            }

            var head = value.Substring(0, beg);
            var middle = value.Substring(beg + 2, end - beg - 2);
            var tail = value.Substring(end + (end < value.Length ? 1 : 0));

            WriteString(head);

            var parts = StringExtensions.Split(middle);

            foreach (var part in parts)
            {
                if (part.StartsWith("+"))
                {
                    var word = part.Substring(1);
                    switch (word)
                    {
                        case "normal":
                            case "bold":
                            case "italic":
                            case "underline":
                            case "strikeout":
                            case "strike-out":
                            case "reverse":
                        {
                            window.AttrOn(word);
                            break;
                        }
                            default:
                        {
                            window.ForeColor = new ColorType(word);
                            break;
                        }
                    }
                }
                else if (part.StartsWith("-"))
                {
                    var word = part.Substring(1);
                    switch (word)
                    {
                        case "normal":
                            case "bold":
                            case "italic":
                            case "underline":
                            case "strikeout":
                            case "strike-out":
                            case "reverse":
                        {
                            window.AttrOff(word);
                            break;
                        }
                            default:
                        {
                            window.ForeColor = fgOrig;
                            break;
                        }
                    }
                }
                else
                {
                    var word = part;
                    switch (word)
                    {
                        case "normal":
                            case "bold":
                            case "italic":
                            case "underline":
                            case "strikeout":
                            case "strike-out":
                            case "reverse":
                        {
                            window.AttrSet(word);
                            break;
                        }
                            default:
                        {
                            window.ForeColor = new ColorType(word);
                            break;
                        }
                    }
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
            if (value == '\n' && savedOrig)
            {
                window.ForeColor = fgOrig;
                window.Attr = attrOrig;
                savedOrig = false;
            }
            window.Put(value, true);
            //Application.DoEvents();
        }

        public override System.Text.Encoding Encoding
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal object ForeColor(object color)
        {
            return Terminal.ForeColor(window, color);
        }

        internal object BackColor(object color)
        {
            return Terminal.BackColor(window, color);
        }
    }

}

