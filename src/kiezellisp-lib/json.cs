// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Text;

namespace Kiezel
{
    public class JsonDecoder
    {
        private char ch;
        private char eofChar = Convert.ToChar(0);
        private int index;
        private string text;
        private string token;
        private bool tokenIsNumber;
        private bool tokenIsString;

        public object Decode(string encodedText)
        {
            text = encodedText;
            index = 0;

            return Read();
        }

        private void GetToken()
        {
            tokenIsString = tokenIsNumber = false;
            token = null;

            while ((ch = ReadChar()) != eofChar)
            {
                if (!Char.IsWhiteSpace(ch) && !Char.IsControl(ch))
                {
                    break;
                }
            }

            if (ch == eofChar)
            {
                return;
            }

            if (ch == '[' || ch == ']' || ch == '{' || ch == '}' || ch == ':' || ch == ',')
            {
                token = new string(ch, 1);
            }
            else if (ch == '"')
            {
                tokenIsString = true;
                StringBuilder buf = new StringBuilder();

                while (true)
                {
                    if ((ch = ReadChar()) == eofChar)
                    {
                        throw new LispException("json: unterminated string");
                    }

                    if (ch == '\n')
                    {
                        throw new LispException("json: unterminated string");
                    }

                    if (ch == '"')
                    {
                        break;
                    }

                    if (ch == '\\')
                    {
                        if ((ch = ReadChar()) == eofChar)
                        {
                            throw new LispException("json: Unterminated string");
                        }

                        switch (ch)
                        {
                            case 'b':
                            {
                                ch = '\b';
                                break;
                            }
                            case 'f':
                            {
                                ch = '\f';
                                break;
                            }
                            case 'n':
                            {
                                ch = '\n';
                                break;
                            }
                            case 't':
                            {
                                ch = '\t';
                                break;
                            }
                            case 'r':
                            {
                                ch = '\r';
                                break;
                            }
                        }
                    }

                    buf.Append(ch);
                }

                token = buf.ToString();
            }
            else
            {
                tokenIsNumber = true;
                StringBuilder buf = new StringBuilder();

                while (true)
                {
                    buf.Append(ch);

                    if ((ch = PeekChar()) == eofChar || Char.IsWhiteSpace(ch) || Char.IsControl(ch))
                    {
                        break;
                    }

                    if (ch == '{' || ch == '}' || ch == '[' || ch == ']' || ch == ',' || ch == ':' || ch == '\"')
                    {
                        break;
                    }

                    ReadChar();
                }

                token = buf.ToString();
            }
        }

        private bool Match(string target)
        {
            if (tokenIsString || tokenIsNumber)
            {
                return false;
            }
            else if (token == null)
            {
                return null == target;
            }
            else
            {
                return String.Compare(token, target, true) == 0;
            }
        }

        private void NeedToken(string target)
        {
            if (!Match(target))
            {
                throw new LispException("json: expected token <{0}> instead of <{1}>", target, token);
            }
        }

        private char PeekChar(int offset = 0)
        {
            if (index + offset >= text.Length)
            {
                return eofChar;
            }
            else
            {
                return text[index + offset];
            }
        }

        private object Read()
        {
            return ReadExp(false);
        }

        private char ReadChar()
        {
            if (index >= text.Length)
            {
                return eofChar;
            }
            else
            {
                return text[index++];
            }
        }

        private object ReadExp(bool haveToken)
        {
            if (!haveToken)
            {
                GetToken();
            }

            if (Match(null))
            {
                return null;
            }

            if (tokenIsString)
            {
                // string
                return token;
            }

            if (tokenIsNumber)
            {
                if (token == "true")
                {
                    return true;
                }

                if (token == "false")
                {
                    return false;
                }

                if (token == "null")
                {
                    return null;
                }

                double val;

                if (!double.TryParse(token, System.Globalization.NumberStyles.Any, null, out val))
                {
                    throw new LispException("json: invalid number: {0}", token);
                }
                return val;
            }

            if (Match("["))
            {
                return ReadVector();
            }

            if (Match("{"))
            {
                return ReadObject();
            }

            throw new LispException("json: unexpected token: {0}", token);
        }

        private Prototype ReadObject()
        {
            var obj = new Prototype();
            GetToken();

            if (!Match("}"))
            {
                while (true)
                {
                    if (!tokenIsString)
                    {
                        throw new LispException("json: expected string: {0}", token);
                    }

                    var key = token;

                    GetToken();
                    NeedToken(":");

                    object val = Read();

                    obj.SetValue(key, val);

                    GetToken();
                    if (Match("}"))
                    {
                        break;
                    }

                    NeedToken(",");
                    GetToken();
                }
            }

            return obj;
        }

        private Vector ReadVector()
        {
            var vector = new Vector();

            GetToken();

            if (!Match("]"))
            {
                while (true)
                {
                    object p = ReadExp(true);

                    if (p == null)
                    {
                        throw new LispException("json: incomplete array");
                    }

                    vector.Add(p);

                    GetToken();
                    if (Match("]"))
                    {
                        break;
                    }

                    NeedToken(",");
                    GetToken();
                }
            }

            return vector;
        }
    }

    public class JsonEncoder
    {
        private StringBuilder buf = new StringBuilder();

        public string Encode(object value)
        {
            Add(value);
            return buf.ToString();
        }

        private void Add(object value)
        {
            if (value == null)
            {
                buf.Append("null");
            }
            else if (value is bool)
            {
                buf.Append((bool)value ? "true" : "false");
            }
            else if (value is Prototype)
            {
                Add(((Prototype)value).AsDictionary());
            }
            else if (value is IDictionary)
            {
                string comma = " ";
                buf.Append("{");
                foreach (DictionaryEntry de in ( ( IDictionary ) value ))
                {
                    buf.Append(comma);
                    comma = ", ";
                    Add(de.Key);
                    buf.Append(": ");
                    Add(de.Value);
                }
                buf.Append(" }");
            }
            else if (value is string)
            {
                buf.AppendFormat("\"{0}\"", StringEscape((string)value));
            }
            else if (value is IEnumerable)
            {
                string comma = " ";
                buf.Append("[");
                foreach (object val in Runtime.ToIter( value ))
                {
                    buf.Append(comma);
                    comma = ", ";
                    Add(val);
                }
                buf.Append(" ]");
            }
            else if (value is Symbol)
            {
                buf.AppendFormat("\"{0}\"", StringEscape(((Symbol)value).Name));
            }
            else if (value is ValueType)
            {
                buf.Append(value);
            }
            else
            {
                throw new LispException("json: value not supported: {0}", value);
            }
        }

        private string StringEscape(string str)
        {
            str = str.Replace("\"", "\\\"");
            str = str.Replace(@"\", @"\\");
            str = str.Replace("/", @"\/");
            str = str.Replace("\b", @"\b");
            str = str.Replace("\f", @"\f");
            str = str.Replace("\n", @"\n");
            str = str.Replace("\r", @"\r");
            str = str.Replace("\t", @"\t");
            return str;
        }
    }
}