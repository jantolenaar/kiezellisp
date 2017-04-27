#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;

    public class CharacterRepresentation
    {
        #region Fields

        public char Code;
        public string EscapeString;
        public string Name;

        #endregion Fields

        #region Constructors

        public CharacterRepresentation(char code, string escape, string name)
        {
            Code = code;
            EscapeString = escape;
            Name = name;
        }

        #endregion Constructors
    }

    public class LogTextWriter : TextWriter
    {
        #region Fields

        // .NET filestreams cannot really be shared by processes for logging, because
        // each process has its own idea of the end of the file when appending. So they
        // overwrite each others data.
        // This function opens/writes/closes the file for each writelog call.
        // It relies on a IOException in the case of file sharing problems.
        string Name;

        #endregion Fields

        #region Constructors

        public LogTextWriter(string name)
        {
            Name = name;
        }

        #endregion Constructors

        #region Public Properties

        public override System.Text.Encoding Encoding
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion Public Properties

        #region Private Methods

        TextWriter Open()
        {
            if (!string.IsNullOrWhiteSpace(Name))
            {
                for (var i = 0; i < 100; ++i)
                {
                    DateTime date = DateTime.Now.Date;
                    string file = Name + date.ToString("-yyyy-MM-dd") + ".log";
                    string dir = Path.GetDirectoryName(file);
                    Directory.CreateDirectory(dir);

                    try
                    {
                        var fs = new FileStream(file, FileMode.Append, FileAccess.Write, FileShare.Read);
                        try
                        {
                            var stream = new StreamWriter(fs);
                            return stream;
                        }
                        catch
                        {
                            fs.Close();
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.IndexOf("used by another") == -1)
                        {
                            throw;
                        }

                        // Give other process a chance to finish writing
                        System.Threading.Thread.Sleep(10);
                    }
                }
            }

            return Null;
        }

        #endregion Private Methods

        #region Public Methods

        public override void Write(string value)
        {
            using (var stream = Open())
            {
                stream.Write(value);
            }
        }

        public override void WriteLine(string value)
        {
            using (var stream = Open())
            {
                stream.WriteLine(value);
            }
        }

        #endregion Public Methods
    }

    public partial class Runtime
    {
        #region Static Fields

        public static CharacterRepresentation[] CharacterTable =
        {
            new CharacterRepresentation('\0', @"\0", "null"),
            new CharacterRepresentation('\a', @"\a", "alert"),
            new CharacterRepresentation('\b', @"\b", "backspace"),
            new CharacterRepresentation(' ', null, "space"),
            new CharacterRepresentation(':', null, "colon"),
            new CharacterRepresentation(';', null, "semicolon"),
            new CharacterRepresentation('"', null, "double-quote"),
            new CharacterRepresentation('(', null, "left-par"),
            new CharacterRepresentation(')', null, "right-par"),
            new CharacterRepresentation('/', null, "slash"),
            new CharacterRepresentation('\f', @"\f", "page"),
            new CharacterRepresentation('\n', @"\n", "newline"),
            new CharacterRepresentation('\r', @"\r", "return"),
            new CharacterRepresentation('\t', @"\t", "tab"),
            new CharacterRepresentation('\v', @"\v", "vtab"),
            new CharacterRepresentation('\"', @"\""", null),
            new CharacterRepresentation('\\', @"\\", "backslash")
        };
        public static ConditionalWeakTable<TextReader, LispReader> ReaderCache = new ConditionalWeakTable<TextReader, LispReader>();

        #endregion Static Fields

        #region Private Methods

        static bool EndsWithLf(object item)
        {
            return (item is string && ((string)item).EndsWith("\n")) || (item is char && ((char)item) == '\n');
        }

        static IEnumerable RewriteCompileTimeBranch(IEnumerable forms)
        {
            // Flatten top level compile branches created by #if #elif #else #endif.
            // Branches inside functions or macros are treated as merging-do forms.
            foreach (var expr in forms)
            {
                if (expr is Cons && First(expr) == Symbols.CompileTimeBranch)
                {
                    foreach (var form in RewriteCompileTimeBranch(ToIter(Cdr((Cons)expr))))
                    {
                        yield return form;
                    }
                }
                else {
                    yield return expr;
                }
            }
        }

        #endregion Private Methods

        #region Public Methods

        public static LispReader AcquireReader(TextReader stream)
        {
            // Allow single-threaded but nested use of textreader caused by custom reader macro handlers.
            var stream2 = stream ?? ((TextReader)GetDynamic(Symbols.StdIn));
            var reader = ReaderCache.GetOrCreateValue(stream2);
            reader.Stream = stream2;
            return reader;
        }

        public static object AssertStream(object stream)
        {
            if (stream == MissingValue)
            {
                stream = GetDynamic(Symbols.StdOut);
            }

            if (stream == null)
            {
                return TextWriter.Null;
            }
            else {
                return stream;
            }
        }

        public static TextWriter ConvertToTextWriter(object stream)
        {
            if (stream == MissingValue)
            {
                stream = GetDynamic(Symbols.StdOut);
            }

            return (TextWriter)stream;
        }

        public static char DecodeCharacterName(string token)
        {
            if (token.Length == 0)
            {
                return Convert.ToChar(0);
            }
            else if (token.Length == 1)
            {
                return token[0];
            }
            else {
                foreach (var rep in CharacterTable)
                {
                    if (rep.Name == token)
                    {
                        return rep.Code;
                    }
                }

                throw new LispException("Invalid character name: {0}", token);
            }
        }

        [Lisp("system:dispose")]
        public static void Dispose(object resource)
        {
            if (resource is IDisposable)
            {
                ((IDisposable)resource).Dispose();
            }
        }

        public static string EncodeCharacterName(char ch)
        {
            foreach (var rep in CharacterTable)
            {
                if (rep.Code == ch && rep.Name != null)
                {
                    return rep.Name;
                }
            }

            return new string(ch, 1);
        }

        public static string EscapeCharacterString(string str)
        {
            var buf = new StringWriter();
            foreach (char ch in str)
            {
                WriteEscapeCharacter(buf, ch);
            }
            return buf.ToString();
        }

        public static string FindOneOfSourceFiles(IEnumerable names)
        {
            var folders = (Cons)GetDynamic(Symbols.LoadPath);
            return FindOneOfSourceFiles(names, folders);
        }

        [Lisp("find-one-of-source-files")]
        public static string FindOneOfSourceFiles(IEnumerable names, IEnumerable folders)
        {
            var names2 = new List<string>();

            foreach (string file in ToIter(names))
            {
                if (Path.IsPathRooted(file))
                {
                    if (File.Exists(file))
                    {
                        return NormalizePath(file);
                    }
                }
                else {
                    names2.Add(file);
                }
            }

            if (true)
            {
                var dir = Environment.CurrentDirectory;
                if (!string.IsNullOrEmpty(dir))
                {
                    foreach (string file in ToIter(names2))
                    {
                        var path = NormalizePath(PathExtensions.Combine(dir, file));
                        if (File.Exists(path))
                        {
                            return path;
                        }
                    }
                }
            }

            foreach (string dir in ToIter(folders))
            {
                foreach (string file in ToIter(names2))
                {
                    var path = NormalizePath(PathExtensions.Combine(dir, file));
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }

            return null;
        }

        [Lisp("find-source-file")]
        public static string FindSourceFile(object filespec)
        {
            string file = NormalizePath(GetDesignatedString(filespec));
            string[] candidates;

            if (string.IsNullOrWhiteSpace(Path.GetExtension(file)))
            {
                var basename = Path.GetFileNameWithoutExtension(file);
                candidates = new string[]
                { file + ".k",
                    file + ".kiezel",
                    file + "/" + basename + ".k",
                    file + "/" + basename + ".kiezel",
                    file + "/main.k",
                    file + "/main.kiezel"
                };
            }
            else {
                candidates = new string[] { file };
            }

            string path = FindOneOfSourceFiles(candidates);

            return path;
        }

        [Lisp("get-clipboard")]
        public static string GetClipboardData()
        {
            string str = System.Windows.Forms.Clipboard.GetText();
            return str;
        }

        [Lisp("load")]
        public static void Load(object filespec, params object[] args)
        {
            var file = GetDesignatedString(filespec);

            if (!TryLoad(file, args))
            {
                throw new LispException("File not loaded: {0}", file);
            }
        }

        [Lisp("load-clipboard")]
        public static void LoadClipboardData()
        {
            var code = GetClipboardData();
            using (var stream = new StringReader(code))
            {
                TryLoadText(stream, null, null, false, false);
            }
        }

        public static string NormalizePath(string path)
        {
            return path == null ? "" : path.Replace("\\", "/");
        }

        [Lisp("open-log")]
        public static TextWriter OpenLog(string path)
        {
            return new LogTextWriter(path);
        }

        [Lisp("peek-char")]
        public static object PeekChar()
        {
            return PeekChar(null);
        }

        [Lisp("peek-char")]
        public static object PeekChar(object peekType)
        {
            return PeekChar(peekType, null);
        }

        [Lisp("peek-char")]
        public static object PeekChar(object peekType, TextReader stream)
        {
            return PeekChar(peekType, stream, true);
        }

        [Lisp("peek-char")]
        public static object PeekChar(object peekType, TextReader stream, object eofErrorp)
        {
            return PeekChar(peekType, stream, eofErrorp, null);
        }

        [Lisp("peek-char")]
        public static object PeekChar(object peekType, TextReader stream, object eofErrorp, object eofValue)
        {
            var parser = AcquireReader(stream);
            var eofError = ToBool(eofErrorp);
            var value = parser.PeekChar(peekType);
            if (parser.IsEof)
            {
                if (eofError)
                {
                    ThrowError("peek-char: unexpected EOF");
                }
                return eofValue;
            }
            else {
                return value;
            }
        }

        public static void PrettyPrint(object stream, object left, object right, object obj)
        {
            Write(obj, Symbols.Stream, stream, Symbols.Left, left, Symbols.Right, right, Symbols.Escape, true, Symbols.Pretty, true, Symbols.kwForce, false);
        }

        public static void PrettyPrintLine(object stream, object left, object right, object obj)
        {
            WriteLine(obj, Symbols.Stream, stream, Symbols.Left, left, Symbols.Right, right, Symbols.Escape, true, Symbols.Pretty, true, Symbols.kwForce, false);
        }

        [Lisp("print")]
        public static void Print(params object[] items)
        {
            PrintHelper(false, false, items);
        }

        public static void PrintHelper(bool crlf, bool insertSpace, object[] items)
        {
            var first = 0;
            object stream = ConvertToTextWriter(MissingValue);

            if (items.Length > 0 && items[0] is TextWriter)
            {
                stream = items[0];
                first = 1;
            }

            // Need single Write/WriteLine.
            using (var stream2 = new StringWriter())
            {
                PrintHelper(stream2, false, insertSpace, first, items);
                Write(stream2.ToString(), crlf, Symbols.Stream, stream, Symbols.Escape, false);
            }
        }

        public static void PrintHelper(object stream, bool crlf, bool insertSpace, int first, object[] items)
        {
            for (var i = first; i < items.Length; ++i)
            {
                var item = items[i];
                Write(item, Symbols.Stream, stream, Symbols.Escape, false);
                if (insertSpace && !EndsWithLf(item))
                {
                    Write(' ', Symbols.Stream, stream, Symbols.Escape, false);
                }
            }
            if (crlf)
            {
                Write('\n', Symbols.Stream, stream, Symbols.Escape, false);
            }
        }

        [Lisp("print-line", "println")]
        public static void PrintLine(params object[] items)
        {
            PrintHelper(true, false, items);
        }

        [Lisp("read")]
        public static object Read()
        {
            return Read(null);
        }

        [Lisp("read")]
        public static object Read(TextReader stream)
        {
            return Read(stream, true);
        }

        [Lisp("read")]
        public static object Read(TextReader stream, object eofErrorp)
        {
            return Read(stream, eofErrorp, null);
        }

        [Lisp("read")]
        public static object Read(TextReader stream, object eofErrorp, object eofValue)
        {
            var parser = AcquireReader(stream);
            var eofError = ToBool(eofErrorp);
            var value = parser.Read(EOF.Value);
            if (value == EOF.Value)
            {
                if (eofError)
                {
                    ThrowError("read: unexpected EOF");
                }
                return eofValue;
            }
            else {
                return value;
            }
        }

        [Lisp("read-all")]
        public static object ReadAll()
        {
            return ReadAll(null);
        }

        [Lisp("read-all")]
        public static Cons ReadAll(TextReader stream)
        {
            var parser = AcquireReader(stream);
            return parser.ReadAll();
        }

        [Lisp("read-all-from-string")]
        public static Cons ReadAllFromString(string text)
        {
            using (var stream = new StringReader(text))
            {
                return ReadAll(stream);
            }
        }

        [Lisp("read-char")]
        public static object ReadChar()
        {
            return ReadChar(null);
        }

        [Lisp("read-char")]
        public static object ReadChar(TextReader stream)
        {
            return ReadChar(stream, true);
        }

        [Lisp("read-char")]
        public static object ReadChar(TextReader stream, object eofErrorp)
        {
            return ReadChar(stream, eofErrorp, null);
        }

        [Lisp("read-char")]
        public static object ReadChar(TextReader stream, object eofErrorp, object eofValue)
        {
            var parser = AcquireReader(stream);
            var eofError = ToBool(eofErrorp);
            var value = parser.ReadChar();
            if (parser.IsEof)
            {
                if (eofError)
                {
                    ThrowError("read-char: unexpected EOF");
                }
                return eofValue;
            }
            else {
                return value;
            }
        }

        [Lisp("read-delimited-list")]
        public static object ReadDelimitedList(string terminator)
        {
            return ReadDelimitedList(terminator, null);
        }

        [Lisp("read-delimited-list")]
        public static object ReadDelimitedList(string terminator, TextReader stream)
        {
            var parser = AcquireReader(stream);
            var value = parser.ReadDelimitedList(terminator);
            return value;
        }

        [Lisp("read-from-string")]
        public static object ReadFromString(string text)
        {
            return ReadFromString(text, true);
        }

        [Lisp("read-from-string")]
        public static object ReadFromString(string text, object eofErrorp)
        {
            return ReadFromString(text, eofErrorp, null);
        }

        [Lisp("read-from-string")]
        public static object ReadFromString(string text, object eofErrorp, object eofValue)
        {
            using (var stream = new StringReader(text))
            {
                var eofError = ToBool(eofErrorp);
                var value = Read(stream, false, EOF.Value);
                if (value == EOF.Value)
                {
                    if (eofError)
                    {
                        ThrowError("read-from-string: unexpected EOF");
                    }
                    return eofValue;
                }
                else {
                    return value;
                }
            }
        }

        [Lisp("read-line")]
        public static object ReadLine()
        {
            return ReadLine(null, true);
        }

        [Lisp("read-line")]
        public static object ReadLine(TextReader stream)
        {
            return ReadLine(stream, true);
        }

        [Lisp("read-line")]
        public static object ReadLine(TextReader stream, object eofErrorp)
        {
            return ReadLine(stream, eofErrorp, null);
        }

        [Lisp("read-line")]
        public static object ReadLine(TextReader stream, object eofErrorp, object eofValue)
        {
            var parser = AcquireReader(stream);
            var eofError = ToBool(eofErrorp);
            var value = parser.ReadLine();
            if (value == null)
            {
                if (eofError)
                {
                    ThrowError("read: unexpected EOF");
                }
                return eofValue;
            }
            else {
                return value;
            }
        }

        [Lisp("require")]
        public static void Require(object filespec, params object[] args)
        {
            var file = GetDesignatedString(filespec);
            //if ( GetDynamic( Symbols.ScriptName ) == null )
            //{
            //    throw new LispException( "Require can only be called from another load/require." );
            //}
            var modules = (Cons)Symbols.Modules.Value;
            var found = IndexOf(file, modules);
            if (found == null)
            {
                Symbols.Modules.Value = MakeCons(file, modules);
                if (!TryLoad(file, args))
                {
                    Symbols.Modules.Value = Cdr(modules);
                    throw new LispException("File not loaded: {0}", file);
                }
            }
        }

        [Lisp("system:return-from-load")]
        public static void ReturnFromLoad()
        {
            throw new ReturnFromLoadException();
        }

        [Lisp("run")]
        public static void Run(object filespec, params object[] args)
        {
            Load(filespec, args);
            var main = Symbols.Main.Value as IApply;
            if (main != null)
            {
                Funcall(main);
            }
        }

        [Lisp("run-clipboard")]
        public static void RunClipboardData()
        {
            var code = GetClipboardData();
            using (var stream = new StringReader(code))
            {
                TryLoadText(stream, null, null, false, false);
                var main = Symbols.Main.Value as IApply;
                if (main != null)
                {
                    Funcall(main);
                }
            }
        }

        [Lisp("say")]
        public static void Say(params object[] items)
        {
            PrintHelper(true, true, items);
        }

        [Lisp("set-assembly-path")]
        public static Cons SetAssemblyPath(params string[] folders)
        {
            var paths = AsList(folders.Select(x => PathExtensions.GetFullPath(x)));
            Symbols.AssemblyPath.ReadonlyValue = paths;
            return paths;
        }

        [Lisp("set-clipboard")]
        public static void SetClipboardData(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                System.Windows.Forms.Clipboard.Clear();
            }
            else {
                System.Windows.Forms.Clipboard.SetText(str);
            }
        }

        [Lisp("set-load-path")]
        public static Cons SetLoadPath(params string[] folders)
        {
            var paths = AsList(folders.Select(x => PathExtensions.GetFullPath(x)));
            Symbols.LoadPath.ReadonlyValue = paths;
            return paths;
        }

        public static string ToPrintString(object obj, bool escape = true, int radix = -1)
        {
            if (obj == null)
            {
                if (!escape)
                {
                    return "";
                }
                else {
                    return "null";
                }
            }
            else if (obj is char)
            {
                if (escape)
                {
                    return @"#\" + EncodeCharacterName((char)obj);
                }
                else {
                    return obj.ToString();
                }
            }
            else if (obj is bool)
            {
                return obj.ToString().ToLower();
            }
            else if (obj is string)
            {
                if (escape)
                {
                    return "\"" + EscapeCharacterString(obj.ToString()) + "\"";
                }
                else {
                    return obj.ToString();
                }
            }
            else if (obj is Regex)
            {
                var rx = (Regex)obj;

                if (escape)
                {
                    var str = rx.ToString().Replace("/", "//");

                    return "#/" + str + "/"
                    + ((rx.Options & RegexOptions.IgnoreCase) != 0 ? "i" : "")
                    + ((rx.Options & RegexOptions.Multiline) != 0 ? "m" : "")
                    + ((rx.Options & RegexOptions.Singleline) != 0 ? "s" : "");
                }
                else {
                    return rx.ToString();
                }
            }
            else if (obj is DateTime)
            {
                var dt = (DateTime)obj;
                string s;
                if (dt.Hour == 0 && dt.Minute == 0 && dt.Second == 0)
                {
                    s = dt.ToString("yyyy-dd-MM");
                }
                else {
                    s = dt.ToString("yyyy-MM-dd HH:mm:ss");
                }

                if (escape)
                {
                    return "\"" + s + "\"";
                }
                else {
                    return s;
                }
            }
            else if (obj is Symbol)
            {
                return ((Symbol)obj).ToString(escape);
            }
            else if (obj is Exception)
            {
                var ex = (Exception)obj;
                return string.Format("#<{0} Message=\"{1}\">", ex.GetType().Name, ex.Message);
            }
            else if (obj is Complex)
            {
                var c = (Complex)obj;
                return string.Format("#c({0} {1})", c.Real, c.Imaginary);
            }
            else if (obj is Cons)
            {
                return ((Cons)obj).ToString(escape, radix);
            }
            else if (Integerp(obj) && radix != -1 && radix != 10)
            {
                return Number.ConvertToString(AsBigInteger(obj), escape, radix);
            }
            //else if ( obj is int || obj is long || obj is BigInteger )
            //{
            //    return String.Format( "{0:#,##0}", obj );
            //}
            //else if ( obj is double || obj is decimal )
            //{
            //    // figure out which precision is produced by default printing.
            //    var s = obj.ToString();
            //    var i = s.IndexOf( "." );
            //    var p = ( i == -1 ) ? 0 : s.Length - 1 - i;
            //    var f = "{0:N" + p.ToString() + "}";
            //    return String.Format( f, obj );
            //}
            //else if ( obj is BigRational )
            //{
            //    var r = ( BigRational ) obj;
            //    return String.Format( "{0:#,##0}/{1:#,##0}", r.Numerator, r.Denominator );
            //}
            else if (obj is ValueType || obj is IPrintsValue)
            {
                return obj.ToString();
            }
            else if (obj is Vector)
            {
                var brackets = ToBool(GetDynamic(Symbols.PrintVectorWithBrackets));
                var buf = new StringWriter();
                if (escape)
                {
                    buf.Write(brackets ? "[" : "#v(");
                }
                var space = "";
                foreach (object item in (IList)obj)
                {
                    buf.Write(space);
                    buf.Write(ToPrintString(item, escape, radix));
                    space = " ";
                }
                if (escape)
                {
                    buf.Write(brackets ? "]" : ")");
                }
                return buf.ToString();
            }
            else if (obj is IList)
            {
                var buf = new StringWriter();
                if (escape)
                {
                    buf.Write("[");
                }
                var space = "";
                foreach (object item in (IList)obj)
                {
                    buf.Write(space);
                    buf.Write(ToPrintString(item, escape, radix));
                    space = " ";
                }
                if (escape)
                {
                    buf.Write("]");
                }
                return buf.ToString();
            }
            else if (obj is Prototype)
            {
                var braces = ToBool(GetDynamic(Symbols.PrintPrototypeWithBraces));
                var proto = (Prototype)obj;
                var buf = new StringWriter();
                var space = "";
                buf.Write(braces ? "{ " : "#s(");
                var supers = proto.GetTypeSpecifier();
                if (supers != null)
                {
                    buf.Write(ToPrintString(supers));
                    space = " ";
                }
                foreach (string key in ToIter(Sort(proto.Keys)))
                {
                    buf.Write(space);
                    if (!key.StartsWith("["))
                    {
                        buf.Write(":");
                    }
                    buf.Write(key);
                    buf.Write(" ");
                    buf.Write(ToPrintString(proto.GetValue(key), escape, radix));
                    space = " ";
                }
                buf.Write(braces ? " }" : ")");
                return buf.ToString();
            }
            else if (obj is Type)
            {
                return "#<type " + obj + ">";
            }
            else {
                return "#<" + obj + ">";
            }
        }

        public static bool TryLoad(string file, object[] args)
        {
            object[] kwargs = ParseKwargs(args, new string[] { "verbose", "print" }, MissingValue, MissingValue);
            var verbose = ToBool(kwargs[0] == MissingValue ? GetDynamic(Symbols.LoadVerbose) : kwargs[0]);
            var print = ToBool(kwargs[1] == MissingValue ? GetDynamic(Symbols.LoadPrint) : kwargs[1]);
            return TryLoad(file, verbose, print);
        }

        public static bool TryLoad(string file, bool loadVerbose, bool loadPrint)
        {
            string path = FindSourceFile(file);

            if (path == null)
            {
                return false;
            }

            if (loadVerbose && file.IndexOf("kiezellisp") == -1)
            {
                PrintLine("Loading ", file, " from ", path);
            }

            var scriptDirectory = NormalizePath(Path.GetDirectoryName(path));
            var scriptName = Path.GetFileName(path);

            using (var stream = File.OpenText(path))
            {
                TryLoadText(stream, scriptDirectory, scriptName, loadVerbose, loadPrint);
            }

            return true;
        }

        public static void TryLoadText(TextReader stream, string newDir, string scriptName, bool loadVerbose, bool loadPrint)
        {
            var saved = SaveStackAndFrame();
            var env = MakeExtendedEnvironment();
            var scope = env.Scope;
            CurrentThreadContext.Frame = env.Frame;

            DefDynamic(Symbols.ScriptDirectory, newDir ?? GetDynamic(Symbols.ScriptDirectory));
            DefDynamic(Symbols.ScriptName, scriptName);
            DefDynamic(Symbols.Package, GetDynamic(Symbols.Package));
            DefDynamic(Symbols.PackageNamePrefix, null);
            DefDynamic(Symbols.LoadVerbose, loadVerbose);
            DefDynamic(Symbols.LoadPrint, loadPrint);

            var oldDir = Environment.CurrentDirectory;
            if (newDir != null)
            {
                Environment.CurrentDirectory = newDir;
            }

            try
            {
                var reader = AcquireReader(stream);
                var forms = RewriteCompileTimeBranch(reader.ReadAllEnum());

                foreach (object statement in forms)
                {
                    var code = Compile(statement, scope);
                    if (code == null)
                    {
                        // compile-time expression, e.g. (module xyz)
                    }
                    else {
                        var result = Execute(code);
                        if (loadPrint)
                        {
                            PrintLine(ToPrintString(result));
                        }
                    }
                }
            }
            catch (ReturnFromLoadException)
            {
            }
            finally
            {
                Environment.CurrentDirectory = oldDir;
            }

            RestoreStackAndFrame(saved);
        }

        public static char UnescapeCharacter(char ch)
        {
            foreach (var rep in CharacterTable)
            {
                if (rep.EscapeString != null && rep.EscapeString[1] == ch)
                {
                    return rep.Code;
                }
            }

            return ch;
        }

        [Lisp("unread-char")]
        public static void UnreadChar()
        {
            UnreadChar(null);
        }

        [Lisp("unread-char")]
        public static void UnreadChar(TextReader stream)
        {
            var parser = AcquireReader(stream);
            parser.UnreadChar();
        }

        [Lisp("write")]
        public static void Write(object item, params object[] kwargs)
        {
            Write(item, false, kwargs);
        }

        public static void Write(object item, bool crlf, params object[] kwargs)
        {
            var args = ParseKwargs(true, kwargs, new string[] { "escape", "width", "stream", "padding", "pretty", "left", "right", "base", "force", "format" },
                           GetDynamic(Symbols.PrintEscape), 0, MissingValue, " ", false, null, null, -1,
                           GetDynamic(Symbols.PrintForce), null);

            var outputstream = args[2];
            var stream = ConvertToTextWriter(outputstream);

            if (stream == null)
            {
                return;
            }

            var escape = ToBool(args[0]);
            var width = ToInt(args[1]);
            var padding = MakeString(args[3]);
            var pretty = ToBool(args[4]);
            var left = args[5];
            var right = args[6];
            var radix = ToInt(args[7]);
            var force = ToBool(args[8]);
            var format = (string)args[9];

            try
            {
                // Only the REPL result printer sets this variable to false.

                if (force)
                {
                    item = Force(item);
                }

                if (pretty && Symbols.PrettyPrintHook.Value != null)
                {
                    var saved = SaveStackAndFrame();

                    DefDynamic(Symbols.StdOut, stream);
                    DefDynamic(Symbols.PrintEscape, escape);
                    DefDynamic(Symbols.PrintBase, radix);
                    DefDynamic(Symbols.PrintForce, false);

                    var kwargs2 = new Vector();
                    kwargs2.Add(item);
                    kwargs2.Add(Symbols.Left);
                    kwargs2.Add(left);
                    kwargs2.Add(Symbols.Right);
                    kwargs2.Add(right);

                    ApplyStar((IApply)Symbols.PrettyPrintHook.Value, kwargs2);

                    if (crlf)
                    {
                        PrintLine();
                    }

                    RestoreStackAndFrame(saved);
                }
                else {
                    WriteImp(item, stream, escape, width, padding, radix, crlf, format);
                }
            }
            finally
            {
                if (outputstream is string)
                {
                    // Appending to log file
                    stream.Close();
                }
            }
        }

        public static void WriteEscapeCharacter(TextWriter stream, char ch)
        {
            foreach (var rep in CharacterTable)
            {
                if (rep.Code == ch && rep.EscapeString != null)
                {
                    stream.Write(rep.EscapeString);
                    return;
                }
            }

            if (ch < ' ')
            {
                stream.Write(@"\x{0:x2}", (int)ch);
            }
            else {
                stream.Write(ch);
            }
        }

        public static void WriteImp(object item, TextWriter stream, bool escape = true, int width = 0,
            string padding = " ", int radix = -1, bool crlf = false,
            string format = null)
        {
            string s;

            if (format == null)
            {
                s = ToPrintString(item, escape: escape, radix: radix);
            }
            else {
                s = string.Format("{0:" + format + "}", item);
            }

            if (width != 0)
            {
                var w = Math.Abs(width);

                if (s.Length > w)
                {
                    s = s.Substring(0, w);
                }
                else if (width < 0 || Numberp(item))
                {
                    s = s.PadLeft(w, string.IsNullOrEmpty(padding) ? ' ' : padding[0]);
                }
                else {
                    s = s.PadRight(w, string.IsNullOrEmpty(padding) ? ' ' : padding[0]);
                }
            }

            if (crlf)
            {
                stream.WriteLine(s.ConvertToExternalLineEndings());
            }
            else {
                stream.Write(s.ConvertToExternalLineEndings());
            }
        }

        [Lisp("write-line", "writeln")]
        public static void WriteLine(object item, params object[] kwargs)
        {
            Write(item, true, kwargs);
        }

        [Lisp("write-to-string")]
        public static string WriteToString(object item, params object[] kwargs)
        {
            using (var stream = new StringWriter())
            {
                var kwargs2 = new Vector(kwargs);
                kwargs2.Add(Symbols.Stream);
                kwargs2.Add(stream);
                Write(item, kwargs2.ToArray());
                return stream.ToString();
            }
        }

        #endregion Public Methods
    }
}