#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.IO;
    using System.Text;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// Functions to handle comma or tab separated files.
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public partial class Runtime
    {
        #region Public Methods

        [Lisp("csv:read-string-to-grid")]
        public static Vector CsvReadStringToGrid(string str)
        {
            return CsvReadStringToGrid(str, null);
        }

        [Lisp("csv:read-string-to-grid")]
        public static Vector CsvReadStringToGrid(string str, Prototype options)
        {
            options = options ?? new Prototype(null);
            string fieldSeparator = (string)options.GetValue("field-separator") ?? ",";
            string quoteCharacter = (string)options.GetValue("quote-character") ?? "\"";
            bool trimSpaces = ToBool(options.GetValue("trim-spaces?") ?? true);
            char fieldChar = fieldSeparator[0];
            char quoteChar = quoteCharacter[0];

            var lines = new Vector();
            Vector fields = null;
            StringBuilder field = null;
            bool quoted = false;

            for (var i = 0; i < str.Length; ++i)
            {
                if (fields == null)
                {
                    fields = new Vector();
                }

                if (field == null)
                {
                    field = new StringBuilder();
                }

                char current = str[i];

                if (quoted)
                {
                    char next = (i + 1 < str.Length) ? str[i + 1] : '\0';

                    if (current == quoteChar && next == quoteChar)
                    {
                        field.Append(quoteChar);
                        ++i;
                    }
                    else if (current == quoteChar)
                    {
                        quoted = false;
                    }
                    else {
                        field.Append(current);
                    }
                }
                else {
                    if (current == '\n')
                    {
                        fields.Add(trimSpaces ? field.ToString().Trim() : field.ToString());
                        lines.Add(fields);
                        fields = null;
                        field = null;
                    }
                    else if (current == fieldChar)
                    {
                        fields.Add(trimSpaces ? field.ToString().Trim() : field.ToString());
                        field = new StringBuilder();
                    }
                    else if (field.Length != 0)
                    {
                        field.Append(current);
                    }
                    else if (current == quoteChar)
                    {
                        quoted = true;
                    }
                    else {
                        field.Append(current);
                    }
                }
            }

            if (field != null)
            {
                fields.Add(trimSpaces ? field.ToString().Trim() : field.ToString());
            }

            if (fields != null)
            {
                lines.Add(fields);
            }

            return lines;
        }

        [Lisp("csv:write-grid-to-string")]
        public static string CsvWriteGridToString(IEnumerable lines)
        {
            return CsvWriteGridToString(lines, null);
        }

        [Lisp("csv:write-grid-to-string")]
        public static string CsvWriteGridToString(IEnumerable lines, Prototype options)
        {
            options = options ?? new Prototype(null);
            string fieldSeparator = (string)options.GetValue("field-separator") ?? ",";
            string quoteCharacter = (string)options.GetValue("quote-character") ?? "\"";
            bool quoteAll = ToBool(options.GetValue("quote-all?") ?? false);
            char fieldChar = fieldSeparator[0];
            char quoteChar = quoteCharacter[0];
            var culture = (CultureInfo)options.GetValue("culture") ?? CultureInfo.InvariantCulture;
            var singleQuote = quoteCharacter;
            var doubleQuote = quoteCharacter + quoteCharacter;
            var forbidden = new char[] { fieldSeparator[0], quoteCharacter[0], '\n', '\r' };

            using (var stream = new StringWriter())
            {
                foreach (IEnumerable fields in lines)
                {
                    var separator = '\0';

                    foreach (object fieldvalue in fields)
                    {
                        var field = CsvWriteValueToString(fieldvalue, culture);
                        var quoted = quoteAll || field.IndexOfAny(forbidden) != -1;

                        if (separator != '\0')
                        {
                            stream.Write(separator);
                        }

                        if (quoted)
                        {
                            stream.Write(quoteCharacter);
                            stream.Write(field.Replace(singleQuote, doubleQuote));
                            stream.Write(quoteCharacter);
                        }
                        else {
                            stream.Write(field);
                        }

                        separator = fieldSeparator[0];
                    }

                    stream.Write("\n");
                }

                return stream.ToString();
            }
        }

        [Lisp("csv:write-value-to-string")]
        public static string CsvWriteValueToString(object value, CultureInfo culture)
        {
            if (value == null)
            {
                return "";
            }
            else if (value is string)
            {
                return (string)value;
            }
            else if (value is DateTime)
            {
                culture = culture ?? CultureInfo.InvariantCulture;
                var fmt = "{0:" + culture.DateTimeFormat.ShortDatePattern + "}";
                return string.Format(fmt, value);
            }
            else if (Numberp(value))
            {
                culture = culture ?? CultureInfo.InvariantCulture;
                return (string)InvokeMember(value, "ToString", culture);
            }
            else {
                return value.ToString();
            }
        }

        #endregion Public Methods
    }
}