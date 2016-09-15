// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Kiezel
{
    [RestrictedImport]
    public class LispReader
    {
        private TextReader stream;
        private bool autoClose;

        private int col = 0;
        private int line = 0;
        private int symbolSuppression = 0;
        private int lastChar;
        private bool haveUnreadChar = false;
        private StringBuilder lineBuffer = new StringBuilder();
        private bool haveCompleteLine = false;

        public bool IsEof
        {
            get
            {
                return lastChar == -1;
            }
        }

        public LispReader(TextReader stream)
        {
            this.stream = stream;
            this.autoClose = true;
            this.lastChar = (stream == null) ? -1 : 0;
        }

        public IEnumerable ReadAllEnum()
        {
            while (true)
            {
                var expr = Read(EOF.Value);
                if (expr == EOF.Value)
                {
                    break;
                }
                yield return expr;
            }
        }

        public void UnreadChar()
        {
            if (haveUnreadChar)
            {
                throw MakeScannerException("Too many calls to unread-char.");
            }
            if (!IsEof)
            {
                haveUnreadChar = true;
            }
        }

        public char PeekChar()
        {
            return PeekChar(null);
        }

        public char PeekChar(object type)
        {
            if (type is bool || type == null)
            {
                var flag = Runtime.ToBool(type);
                if (flag)
                {
                    var ch = SkipWhitespace();
                    return ch;
                }
                else
                {
                    var ch = ReadChar();
                    UnreadChar();
                    return ch;
                }
            }
            else if (type is Char)
            {
                var target = (char)type;
                while (true)
                {
                    var ch = ReadChar();
                    if (IsEof || ch == target)
                    {
                        UnreadChar();
                        return ch;
                    }
                }
            }
            else
            {
                throw MakeScannerException("peek-char: invalid type '{0}'", type);
            }
        }

        public char ReadChar()
        {
            if (lastChar == -1 || stream == null)
            {
                return (char)0;
            }
            else if (haveUnreadChar)
            {
                haveUnreadChar = false;
                return (char)lastChar;
            }
            else
            {
                int ch = stream.Read();
                if (ch == -1)
                {
                    if (autoClose)
                    {
                        stream.Close();
                    }
                    stream = null;
                    haveCompleteLine = true;
                    lastChar = ch;
                    return (char)0;
                }
                else if (ch == '\n')
                {
                    haveCompleteLine = true;
                    ++line;
                    col = 0;
                    lastChar = ch;
                    return '\n';
                }
                else
                {
                    if (haveCompleteLine)
                    {
                        haveCompleteLine = false;
                        lineBuffer.Clear();
                    }
                    ++col;
                    lastChar = ch;
                    lineBuffer.Append((char)lastChar);
                    return (char)lastChar; 
                }
            }
        }

        public object Read()
        {
            var obj = Read(EOF.Value);
            if (obj == EOF.Value)
            {
                throw MakeScannerException("EOF: unexpected end");
            }
            return obj;
        }

        public object Read(object eofValue = null)
        {
            object obj;
            while ((obj = MaybeRead(eofValue)) == VOID.Value)
            {
            }
            return obj;
        }

        public Cons ReadAll()
        {
            return Runtime.AsList(ReadAllEnum());
        }

        public Cons ReadDelimitedList(string terminator)
        {
            if (terminator.Length != 1)
            {
                throw MakeScannerException("Terminator string must contain exactly one character: {0}", terminator);
            }

            var term = terminator[0];

            while (true)
            {
                SkipWhitespace();
                var ch = ReadChar();
                if (IsEof)
                {
                    throw MakeScannerException("EOF: missing terminator: '{0}'", terminator);
                }
                if (ch == term)
                {
                    return null;
                }              
                UnreadChar();
                var first = MaybeRead();
                if (first != VOID.Value)
                {
                    var rest = (Cons)ReadDelimitedList(terminator);
                    return Runtime.MakeCons(first, rest);
                }
            }
        }

        public string ReadLine()
        {
            var buf = new StringBuilder();
            while (true)
            {
                var ch = ReadChar();
                if (IsEof || ch == '\n')
                {
                    break;
                }
                buf.Append(ch);
            }
            return buf.ToString();
        }

        public string ReadToken()
        {
            var buf = new StringBuilder();

            if (IsEof)
            {
                throw MakeScannerException("EOF: expected token");
            }

            while (true)
            {
                var code = ReadChar();

                if (IsEof)
                {
                    break;
                }

                var item = GetEntry(code);

                if (item.Type == CharacterType.SingleEscape)
                {
                    var code2 = ReadChar();
                    if (IsEof)
                    {
                        throw MakeScannerException("EOF: expected character after '{0}'", item.Character);
                    }
                    buf.Append(code2);
                }
                else if (item.Type == CharacterType.MultipleEscape)
                {
                    while (true)
                    {
                        var code2 = ReadChar();
                        if (IsEof)
                        {
                            throw MakeScannerException("EOF: expected character '{0}'", item.Character);
                        }
                        if (code2 == code)
                        {
                            break;
                        }
                        buf.Append(code2);
                    }
                }
                else if (item.Type == CharacterType.Constituent || item.Type == CharacterType.NonTerminatingMacro)
                {
                    buf.Append(code);
                }
                else
                {
                    UnreadChar();
                    break;
                }
            }

            var token = buf.ToString();

            if (token == "")
            {
                throw MakeScannerException("Empty token???");
            }

            return token;
        }

        public char SkipWhitespace()
        {
            while (true)
            {
                var ch = ReadChar();
                if (IsEof)
                {
                    return ch;
                }
                var item = GetEntry(ch);
                if (item.Type != CharacterType.Whitespace)
                {
                    UnreadChar();
                    return ch;
                }
            }
        }

        public int GrepShortLambdaParameters(Cons form)
        {
            var last = 0;

            if (form != null)
            {
                for (var head = form; head != null; head = head.Cdr)
                {
                    var sym = head.Car as Symbol;
                    var seq = head.Car as Cons;
                    var index = -1;

                    if (sym != null)
                    {
                        var position = Runtime.Position(sym, Symbols.ShortLambdaVariables);
                        if (position != null)
                        {
                            index = (int)position;
                            if (index == 0)
                            {
                                index = 1;
                                head.Car = Symbols.ShortLambdaVariables[index];
                            }
                        }
                    }
                    else if (seq != null)
                    {
                        index = GrepShortLambdaParameters(seq);
                    }

                    if (index != -1)
                    {
                        last = (last < index) ? index : last;
                    }
                }
            }

            return last;
        }

        public LispException MakeScannerException(string message)
        {
            var l = line + 1;
            var c = col;
            if (!haveCompleteLine)
            {
                ReadLine();
            }
            var s = lineBuffer.ToString();
            return new LispException("Line {0} column {1}: {2}\n{3}", l, c, s, message);
//            return new LispException("Line {0} column {1}: {2}\n{3}", line + 1, col, "", message);
        }

        public LispException MakeScannerException(string fmt, params object[] args)
        {
            return MakeScannerException(string.Format(fmt, args));
        }

        public object MaybeRead(object eofValue = null)
        {
            SkipWhitespace();

            var code = ReadChar();

            if (IsEof)
            {
                return eofValue;
            }

            var item = GetEntry(code);

            if (item.Type == CharacterType.TerminatingMacro || item.Type == CharacterType.NonTerminatingMacro)
            {
                if (item.DispatchReadtable == null)
                {
                    if (item.Handler == null)
                    {
                        throw MakeScannerException("Invalid character: '{0}'", code);
                    }
                    else
                    {
                        return item.Handler(this, code);
                    }
                }
                else
                {
                    var arg = ReadDecimalArg();
                    var ch = ReadChar();
                    var target = ch.ToString();
                    var handlers = item.DispatchReadtable.Where(x => x.Key[0] == ch).ToList();

                    if (handlers.Count > 1 || (handlers.Count > 0 && handlers[0].Key.Length > 1))
                    {
                        // form target key from letters
                        var buf = new StringBuilder();
                        buf.Append(ch);
                        while (true)
                        {
                            ch = ReadChar();
                            if (IsEof)
                            {
                                break;
                            }
                            if (!char.IsLetter(ch))
                            {
                                UnreadChar();
                                break;
                            }
                            buf.Append(ch);
                        }
                        target = buf.ToString();
                        handlers = handlers.Where(x => x.Key == target).ToList();
                    }

                    if (handlers.Count != 0)
                    {
                        var handler = handlers[0];
                        var form = handler.Value(this, handler.Key, arg);
                        return form;
                    }
                    else
                    {
                        throw MakeScannerException("Invalid character combination: '{0}{1}'", code, target);
                    }
                }
            }

            UnreadChar();
            var token = ReadToken();

            object numberValue;
            object timespan;

            if (symbolSuppression > 0)
            {
                return null;
            }
            else if ((numberValue = token.TryParseNumber()) != null)
            {
                return numberValue;
            }
            else if ((timespan = token.TryParseTime()) != null)
            {
                return timespan;
            }
            else if (token == "true")
            {
                return true;
            }
            else if (token == "false")
            {
                return false;
            }
            else if (token == "null")
            {
                return null;
            }
            else if (token.Length > 1 && token[0] == '.')
            {
                // .a.b.c maps to ( . "a" "b" "c" )
                return Runtime.MakeList(Symbols.Dot, token.Substring(1));
            }
            else if (token.Length > 1 && token[0] == '?')
            {
                // ?a.b.c maps to ( ? "a" "b" "c" )
                return Runtime.MakeList(Symbols.NullableDot, token.Substring(1));
            }
            else
            {
                return Runtime.FindSymbol(token);
            }
        }

        bool EndsWith(List<char> chars, string str)
        {
            var m = chars.Count;
            var n = str.Length;
            if (m >= n)
            {               
                for (int i = m - n, j = 0; j < n; ++i, ++j)
                {
                    if (chars[i] != str[j])
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        string StringWithoutTerminator(List<char> chars, string terminator)
        {
            var m = chars.Count;
            var n = terminator.Length;
            if (m >= n)
            {
                return new string(chars.GetRange(0, m - n).ToArray());
            }
            else
            {
                return "";
            }
        }

        public string ParseDocString(string terminator)
        {
            // """..."""
            var buf = new List<char>();

            while (true)
            {
                var ch = ReadChar();

                if (IsEof)
                {
                    throw MakeScannerException("EOF: Unterminated doc string");
                }

                buf.Add(ch);

                if (EndsWith(buf, terminator))
                {
                    break;
                }
            }

            return StringWithoutTerminator(buf, terminator);
        }

        public object ParseInfixExpression()
        {
            var str = ReadInfixExpressionString();
            var code = Infix.CompileString(str);
            return code;
        }

        public string ParseMultiLineString()
        {
            // @"..."
            // supports double double quote escape
            // multi-line
            var buf = new StringBuilder();

            while (true)
            {
                var ch = ReadChar();

                if (IsEof)
                {
                    throw MakeScannerException("EOF: Unterminated string");
                }

                if (ch == '"')
                {
                    var ch2 = ReadChar();
                    if (ch2 == '"')
                    {
                        buf.Append('"');
                    }
                    else
                    {
                        UnreadChar();
                        break;
                    }
                }
                else
                {
                    buf.Append(ch);
                }
            }

            return buf.ToString();
        }

        public Regex ParseRegexString(char terminator)
        {
            // #/.../

            char ch;
            var buf = new StringBuilder();

            while (true)
            {
                ch = ReadChar();

                if (IsEof || ch == '\n')
                {
                    throw MakeScannerException("EOF: Unterminated string");
                }

                if (ch == terminator)
                {
                    var ch2 = ReadChar();
                    if (ch2 == terminator)
                    {
                        buf.Append(ch);
                    }
                    else
                    {
                        UnreadChar();
                        break;
                    }
                }
                else
                {
                    buf.Append(ch);
                }
            }

            var options = RegexOptions.None;
            var wildcard = false;

            while (true)
            {
                ch = ReadChar();
                if (!Char.IsLetter(ch))
                {
                    UnreadChar();
                    break;
                }
                switch (ch)
                {
                    case 'i':
                    {
                        options |= RegexOptions.IgnoreCase;
                        break;
                    }
                    case 's':
                    {
                        options |= RegexOptions.Singleline;
                        break;
                    }
                    case 'm':
                    {
                        options |= RegexOptions.Multiline;
                        break;
                    }
                    case 'w':
                    {
                        wildcard = true;
                        break;
                    }
                    default:
                    {
                        throw MakeScannerException("invalid regular expresssion option");
                    }
                }
            }
            var pattern = buf.ToString();
            if (wildcard)
            {
                pattern = StringExtensions.MakeWildcardRegexString(pattern);
            }
            return new RegexPlus(pattern, options);
        }

        public object ParseShortLambdaExpression(bool quasiQuoted, string delimiter)
        {
            SkipWhitespace();
            var ch = ReadChar();
            if (ch != '(')
            {
                throw MakeScannerException("expected ')' character");
            }

            // The list is treated as a single form! 
            // So the entire thing is (like) one function call.
            var form = ReadDelimitedList(delimiter);
            var lastIndex = GrepShortLambdaParameters(form);
            var args = Runtime.AsList(Symbols.ShortLambdaVariables.Skip(1).Take(lastIndex));
            if (quasiQuoted)
            {
                var form2 = Runtime.QuasiQuoteExpandRest(form);
                var code = Runtime.MakeList(Symbols.Lambda, args, form2);
                return code;
            }
            else
            {
                var code = Runtime.MakeList(Symbols.Lambda, args, form);
                return code;
            }
        }

        public string ParseSingleLineString()
        {
            // supports backslash escapes
            // single line

            StringBuilder buf = new StringBuilder();

            while (true)
            {
                var ch = ReadChar();

                if (IsEof || ch == '\n')
                {
                    throw MakeScannerException("Unterminated string");
                }

                if (ch == '"')
                {
                    break;
                }

                if (ch == '\\')
                {
                    ch = ReadChar();

                    switch (ch)
                    {
                        case 'x':
                        {
                            var ch1 = ReadChar();
                            var ch2 = ReadChar();
                            if (IsEof)
                            {
                                throw MakeScannerException("Unterminated string");
                            }
                            var n = (int)Number.ParseNumberBase(new string(new char[] { ch1, ch2 }), 16);
                            buf.Append(Convert.ToChar(n));
                            break;
                        }
                        case 'u':
                        {
                            var ch1 = ReadChar();
                            var ch2 = ReadChar();
                            var ch3 = ReadChar();
                            var ch4 = ReadChar();
                            if (IsEof)
                            {
                                throw MakeScannerException("Unterminated string");
                            }
                            var n = (int)Number.ParseNumberBase(new string(new char[] { ch1, ch2, ch3, ch4 }), 16);
                            buf.Append(Convert.ToChar(n));
                            break;
                        }
                        default:
                        {
                            if (IsEof)
                            {
                                throw MakeScannerException("Unterminated string");
                            }
                            buf.Append(Runtime.UnescapeCharacter(ch));
                            break;
                        }
                    }
                }
                else
                {
                    buf.Append(ch);
                }
            }

            return buf.ToString();
        }

        public string ParseSpecialString()
        {
            var begin = ReadChar();
            var terminator = "";

            switch (begin)
            {
                case '(':
                {
                    terminator = ")";
                    break;
                }
                case '{':
                {
                    terminator = "}";
                    break;
                }
                case '[':
                {
                    terminator = "]";
                    break;
                }
                case '<':
                {
                    terminator = ">";
                    break;
                }
                default:
                {
                    UnreadChar();
                    terminator = ReadLine().Trim();
                    if (terminator == "")
                    {
                        MakeScannerException("No terminator after #q expression");
                    }
                    break;
                }
            }

            return ParseDocString(terminator);
        }

        public string ParseString()
        {
            var ch = ReadChar();
            if (IsEof)
            {
                throw MakeScannerException("EOF: Unterminated string");
            }
            else if (ch == '"')
            {
                var ch2 = ReadChar();
                if (ch2 == '"')
                {
                    return ParseDocString("\"\"\"");
                }
                else
                {
                    UnreadChar();
                    return "";
                }
            }
            else
            {
                UnreadChar();
                return ParseSingleLineString();
            }
        }

        public string ReadBlockComment(string startDelimiter, string endDelimiter)
        {
            var buffer = new List<char>();
            var nesting = 1;
            while (true)
            {
                var ch = ReadChar();
                if (IsEof)
                {
                    break;
                }
                buffer.Add(ch);
                if (EndsWith(buffer, startDelimiter))
                {
                    ++nesting;
                }
                else if (EndsWith(buffer, endDelimiter))
                {
                    if (--nesting == 0)
                    {
                        break;
                    }
                }
            }
            if (nesting != 0)
            {
                throw MakeScannerException("EOF: Unterminated comment");
            }

            return StringWithoutTerminator(buffer, endDelimiter);
        }

        public string ReadInfixExpressionString()
        {
            var buf = new StringBuilder();
            var count = 0;

            while (true)
            {
                var ch = ReadChar();

                if (IsEof)
                {
                    throw MakeScannerException("EOF: Unterminated infix expression");
                }

                buf.Append(ch);

                if (ch == '(')
                {
                    ++count;
                }
                else if (ch == ')')
                {
                    --count;
                    if (count == 0)
                    {
                        break;
                    }
                }
            }

            return buf.ToString();
        }

        public object ReadSuppressed()
        {
            try
            {
                ++symbolSuppression;
                return Read();
            }
            finally
            {
                --symbolSuppression;
            }
        }

        private ReadtableEntry GetEntry(char ch)
        {
            var readTable = (Readtable)Runtime.GetDynamic(Symbols.Readtable);
            return readTable.GetEntry(ch);
        }

        private int ReadDecimalArg()
        {
            int arg = -1;
            while (true)
            {
                var ch = ReadChar();
                if (IsEof)
                {
                    break;
                }
                if (!char.IsDigit(ch))
                {
                    UnreadChar();
                    break;
                }
                if (arg == -1)
                {
                    arg = 0;
                }
                arg = 10 * arg + (ch - '0');
            }
            return arg;
        }
    }
}