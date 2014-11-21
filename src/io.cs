// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Kiezel
{
    public partial class Runtime
    {
        internal static CharacterRepresentation[] CharacterTable = new CharacterRepresentation[]
        {
            new CharacterRepresentation( '\0', @"\0", "null" ),
            new CharacterRepresentation( '\a', @"\a", "alert" ),
            new CharacterRepresentation( '\b', @"\b", "backspace" ),
            new CharacterRepresentation( ' ', null, "space" ),
            new CharacterRepresentation( ';', null, "semicolon" ),
            new CharacterRepresentation( '"', null, "double-quote" ),
            new CharacterRepresentation( '\f', @"\f", "page" ),
            new CharacterRepresentation( '\n', @"\n", "newline" ),
            new CharacterRepresentation( '\r', @"\r", "return" ),
            new CharacterRepresentation( '\t', @"\t", "tab" ),
            new CharacterRepresentation( '\v', @"\v", "vtab" ),
            new CharacterRepresentation( '\"', @"\""", null ),
            new CharacterRepresentation( '\\', @"\\", null )
        };

        internal static long prevReadKeyTime = -1;

        [Lisp( "break-on-ctrl-d" )]
        public static void BreakOnCtrlD()
        {
            if ( prevReadKeyTime == -1 || StopWatch.ElapsedMilliseconds >= prevReadKeyTime + 1000 )
            {
                prevReadKeyTime = StopWatch.ElapsedMilliseconds;

                if ( Console.KeyAvailable )
                {
                    var info = Console.ReadKey( true );
                    if ( info.Modifiers == ConsoleModifiers.Control && info.Key == ConsoleKey.D )
                    {
                        throw new InterruptException();
                    }
                }
            }
        }

        [Lisp( "system:dispose" )]
        public static void Dispose( object resource )
        {
            if ( resource is IDisposable )
            {
                ( ( IDisposable ) resource ).Dispose();
            }
        }

        [Lisp( "find-source-file" )]
        public static string FindSourceFile( object filespec )
        {
            string file = NormalizePath( GetDesignatedString( filespec ) );
            string[] candidates;

            if ( String.IsNullOrWhiteSpace( Path.GetExtension( file ) ) )
            {
                var basename = Path.GetFileNameWithoutExtension( file );
                candidates = new string[] { file + ".k",
                                            file + ".kiezel",
                                            file + "/" + basename + ".k",
                                            file + "/" + basename + ".kiezel",
                                            file + "/main.k",
                                            file + "/main.kiezel" };
            }
            else
            {
                candidates = new string[] { file };
            }

            string path = FindSourceFile( candidates );

            return path;
        }

        [Lisp( "load" )]
        public static void Load( object filespec, params object[] args )
        {
            var file = GetDesignatedString( filespec );

            if ( !TryLoad( file, args ) )
            {
                throw new LispException( "File not loaded: {0}", file );
            }
        }

        [Lisp( "print" )]
        public static void Print( params object[] items )
        {
            foreach ( object item in items )
            {
                Write( item, Symbols.Escape, false );
            }
        }

        [Lisp( "print-line" )]
        public static void PrintLine( params object[] items )
        {
            Print( items );
            Print( "\n" );
        }

        [Lisp( "read" )]
        public static object Read( params object[] kwargs )
        {
            var args = ParseKwargs( kwargs, new string[] { "stream", "eof-value", "eof-error?" }, GetDynamic( Symbols.StdIn ), null, true );
            var stdin = args[ 0 ];
            var eofValue = args[ 1 ];
            var eofError = ToBool( args[ 2 ] );

            if ( stdin as LispReader == null )
            {
                return null;
            }
            var parser = ( LispReader ) stdin;
            var value = parser.Read( eofValue );
            return value;
        }

        [Lisp( "read-all" )]
        public static object ReadAll( params object[] kwargs )
        {
            var args = ParseKwargs( kwargs, new string[] { "stream" }, GetDynamic( Symbols.StdIn ) );
            var stdin = args[ 0 ];
            if ( stdin as LispReader == null )
            {
                return null;
            }
            var parser = ( LispReader ) stdin;
            return AsList( parser.ReadAll() );
        }

        [Lisp( "read-all-from-string" )]
        public static Cons ReadAllFromString( string text, params object[] kwargs )
        {
            var parser = new LispReader( text );
            return AsList( parser.ReadAll() );
        }

        [Lisp( "read-delimited-list" )]
        public static object ReadDelimitedList( string terminator, params object[] kwargs )
        {
            var args = ParseKwargs( kwargs, new string[] { "stream", "eof-value", "eof-error?" }, GetDynamic( Symbols.StdIn ), null, true );
            var stdin = args[ 0 ];
            var eofValue = args[ 1 ];
            var eofError = ToBool( args[ 2 ] );

            if ( stdin as LispReader == null )
            {
                return null;
            }
            var parser = ( LispReader ) stdin;
            var value = parser.ReadDelimitedList( terminator );
            return value;
        }
        [Lisp( "read-from-string" )]
        public static object ReadFromString( string text, params object[] kwargs )
        {
            var args = ParseKwargs( kwargs, new string[] { "eof-value" }, null );
            var eofValue = args[ 0 ];
            var parser = new LispReader( text );
            return Read( Symbols.Stream, parser, Symbols.EofValue, eofValue );
        }
        [Lisp( "require" )]
        public static void Require( object filespec, params object[] args )
        {
            var file = GetDesignatedString( filespec );
            //if ( GetDynamic( Symbols.ScriptName ) == null )
            //{
            //    throw new LispException( "Require can only be called from another load/require." );
            //}
            var modules = ( Cons ) Symbols.Modules.Value;
            var found = Find( file, modules );
            if ( found == null )
            {
                Symbols.Modules.Value = MakeCons( file, modules );
                if ( !TryLoad( file, args ) )
                {
                    Symbols.Modules.Value = Cdr( modules );
                    throw new LispException( "File not loaded: {0}", file );
                }
            }
        }

        [Lisp( "return-from-load" )]
        public static void ReturnFromLoad()
        {
            throw new ReturnFromLoadException();
        }

        [Lisp( "run" )]
        public static void Run( object filespec, params object[] args )
        {
            Load( filespec, args );
            var main = Symbols.Main.Value as IApply;
            if ( main != null )
            {
                Funcall( main );
            }
        }

        [Lisp( "set-load-path" )]
        public static Cons SetLoadPath( params string[] folders )
        {
            var paths = AsList( folders.Select( x => PathExtensions.GetUnixName( Path.GetFullPath( x ) ) ) );
            Symbols.LoadPath.Value = paths;
            return paths;
        }

        [Lisp( "write" )]
        public static void Write( object item, params object[] kwargs )
        {
            Write( item, false, kwargs );
        }

        [Lisp( "write-line" )]
        public static void WriteLine( object item, params object[] kwargs )
        {
            Write( item, true, kwargs );
        }

        [Lisp( "write-to-string" )]
        public static string WriteToString( object item, params object[] kwargs )
        {
            using ( var stream = new StringWriter() )
            {
                var kwargs2 = new Vector( kwargs );
                kwargs2.Add( Symbols.Stream );
                kwargs2.Add( stream );
                Write( item, kwargs2.ToArray() );
                return stream.ToString();
            }
        }

        internal static TextWriter ConvertToTextWriter( object stream )
        {
            if ( stream == DefaultValue.Value )
            {
                stream = GetDynamic( Symbols.StdOut );
            }

            if ( stream == null )
            {
                return null;
            }
            else if ( stream is bool )
            {
                if ( ( bool ) stream )
                {
                    return Console.Out;
                }
                else
                {
                    return null;
                }
            }
            else if ( stream is string )
            {
                return OpenLogTextWriter( ( string ) stream );
            }
            else
            {
                return ( TextWriter ) stream;
            }
        }

        internal static char DecodeCharacterName( string token )
        {
            if ( token.Length == 0 )
            {
                return Convert.ToChar( 0 );
            }
            else if ( token.Length == 1 )
            {
                return token[ 0 ];
            }
            else
            {
                foreach ( var rep in CharacterTable )
                {
                    if ( rep.Name == token )
                    {
                        return rep.Code;
                    }
                }

                throw new LispException( "Invalid character name: {0}", token );
            }
        }

        internal static string EncodeCharacterName( char ch )
        {
            foreach ( var rep in CharacterTable )
            {
                if ( rep.Code == ch && rep.Name != null )
                {
                    return rep.Name;
                }
            }

            return new string( ch, 1 );
        }

        internal static string EscapeCharacterString( string str )
        {
            var buf = new StringWriter();
            foreach ( char ch in str )
            {
                WriteEscapeCharacter( buf, ch );
            }
            return buf.ToString();
        }

        internal static string FindSourceFile( string[] names )
        {
            if ( Path.IsPathRooted( names[ 0 ] ) )
            {
                foreach ( var file in names )
                {
                    if ( File.Exists( file ) )
                    {
                        return NormalizePath( file );
                    }
                }
            }
            else
            {
                if ( true )
                {
                    var dir = Environment.CurrentDirectory;
                    if ( !String.IsNullOrEmpty( dir ) )
                    {
                        foreach ( string file in names )
                        {
                            var path = NormalizePath( PathExtensions.Combine( dir, file ) );
                            if ( File.Exists( path ) )
                            {
                                return path;
                            }
                        }
                    }
                }

                foreach ( string dir in ToIter( ( Cons ) GetDynamic( Symbols.LoadPath ) ) )
                {
                    foreach ( string file in names )
                    {
                        var path = NormalizePath( PathExtensions.Combine( dir, file ) );
                        if ( File.Exists( path ) )
                        {
                            return path;
                        }
                    }
                }
            }

            return null;
        }

        internal static string NormalizePath( string path )
        {
            return path == null ? "" : path.Replace( "\\", "/" );
        }

        internal static TextWriter OpenLogTextWriter( string name )
        {
            // .NET filestreams cannot really be shared by processes for logging, because
            // each process has its own idea of the end of the file when appending. So they
            // overwrite each others data.
            // This function opens/writes/closes the file for each writelog call.
            // It relies on a IOException in the case of file sharing problems.

            if ( String.IsNullOrWhiteSpace( name ) )
            {
                return null;
            }

            for ( int i = 0; i < 100; ++i )
            {
                DateTime date = DateTime.Now.Date;
                string file = name + date.ToString( "-yyyy-MM-dd" ) + ".log";
                string dir = Path.GetDirectoryName( file );
                Directory.CreateDirectory( dir );

                try
                {
                    var fs = new FileStream( file, FileMode.Append, FileAccess.Write, FileShare.Read );
                    try
                    {
                        var stream = new StreamWriter( fs );
                        return stream;
                    }
                    catch
                    {
                        fs.Close();
                        throw;
                    }
                }
                catch ( Exception ex )
                {
                    if ( ex.Message.IndexOf( "used by another" ) == -1 )
                    {
                        throw;
                    }

                    // Give other process a chance to finish writing
                    System.Threading.Thread.Sleep( 10 );
                }
            }

            return null;
        }

        internal static void PrettyPrint( object stream, int currentOffset, object obj )
        {
            WriteLine( obj, Symbols.Stream, stream, Symbols.Left, currentOffset, Symbols.Escape, true, Symbols.Pretty, true, Symbols.kwForce, false );
        }

        internal static string ToPrintString( object obj, bool escape = true, int radix = -1 )
        {
            if ( obj == null )
            {
                if ( !escape )
                {
                    return "";
                }
                else
                {
                    return "null";
                }
            }
            else if ( obj is char )
            {
                if ( escape )
                {
                    return @"#\" + EncodeCharacterName( ( char ) obj );
                }
                else
                {
                    return obj.ToString();
                }
            }
            else if ( obj is bool )
            {
                return obj.ToString().ToLower();
            }
            else if ( obj is string )
            {
                if ( escape )
                {
                    return "\"" + EscapeCharacterString( obj.ToString() ) + "\"";
                }
                else
                {
                    return obj.ToString();
                }
            }
            else if ( obj is Regex )
            {
                var rx = ( Regex ) obj;

                if ( escape )
                {
                    return "#\"" + rx.ToString() + "\""
                                 + ( ( rx.Options & RegexOptions.IgnoreCase ) != 0 ? "i" : "" )
                                 + ( ( rx.Options & RegexOptions.Multiline ) != 0 ? "m" : "" )
                                 + ( ( rx.Options & RegexOptions.Singleline ) != 0 ? "s" : "" );
                }
                else
                {
                    return rx.ToString();
                }
            }
            else if ( obj is DateTime )
            {
                var dt = ( DateTime ) obj;
                string s;
                if ( dt.Hour == 0 && dt.Minute == 0 && dt.Second == 0 )
                {
                    s = dt.ToString( "yyyy-dd-MM" );
                }
                else
                {
                    s = dt.ToString( "yyyy-MM-dd HH:mm:ss" );
                }

                if ( escape )
                {
                    return "\"" + s + "\"";
                }
                else
                {
                    return s;
                }
            }
            else if ( obj is Symbol )
            {
                return ( ( Symbol ) obj ).ToString( escape );
            }
            else if ( obj is Exception )
            {
                var ex = ( Exception ) obj;
                return System.String.Format( "#<{0} Message=\"{1}\">", ex.GetType().Name, ex.Message );
            }
            else if ( obj is Complex )
            {
                var c = ( Complex ) obj;
                return System.String.Format( "#c({0} {1})", c.Real, c.Imaginary );
            }
            else if ( obj is Cons )
            {
                return ( ( Cons ) obj ).ToString( escape, radix );
            }
            else if ( Integerp( obj ) && radix != -1 && radix != 10 )
            {
                return Number.ConvertToString( AsBigInteger( obj ), escape, radix );
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
            else if ( obj is ValueType || obj is IPrintsValue )
            {
                return obj.ToString();
            }
            else if ( obj is IList )
            {
                var buf = new StringWriter();
                buf.Write( "[" );
                var space = "";
                foreach ( object item in ( IList ) obj )
                {
                    buf.Write( space );
                    buf.Write( ToPrintString( item, escape, radix ) );
                    space = " ";
                }
                buf.Write( "]" );
                return buf.ToString();
            }
            else if ( obj is Prototype )
            {
                var proto = ( Prototype ) obj;
                var buf = new StringWriter();
                buf.Write( "{ " );
                var space = "";
                foreach ( string key in ToIter( proto.Keys ) )
                {
                    buf.Write( space );
                    if ( !key.StartsWith( "[" ) )
                    {
                        buf.Write( ":" );
                    }
                    buf.Write( key );
                    buf.Write( " " );
                    buf.Write( ToPrintString( proto.GetValue( key ), escape, radix ) );
                    space = " ";
                }
                buf.Write( " }" );
                return buf.ToString();
            }
            else if ( obj is System.Type )
            {
                return "#<type " + obj.ToString() + ">";
            }
            else
            {
                return "#<" + obj.ToString() + ">";
            }
        }
        internal static bool TryLoad( string file, params object[] args )
        {
            object[] kwargs = ParseKwargs( args, new string[] { "verbose", "print" }, DefaultValue.Value, DefaultValue.Value );
            var verbose = ToBool( kwargs[ 0 ] == DefaultValue.Value ? GetDynamic( Symbols.LoadVerbose ) : kwargs[ 0 ] );
            var print = ToBool( kwargs[ 1 ] == DefaultValue.Value ? GetDynamic( Symbols.LoadPrint ) : kwargs[ 1 ] );
            return TryLoad( file, false, verbose, print );
        }

        internal static bool TryLoad( string file, bool loadDebug, bool loadVerbose, bool loadPrint )
        {
            string path = FindSourceFile( file );

            if ( path == null )
            {
                return false;
            }

            if ( loadVerbose )
            {
                PrintLog( ";;; Loading ", file, " from ", path );
            }

            var content = File.ReadAllText( path );
            var reader = new LispReader( content, true );
            var newDir = NormalizePath( Path.GetDirectoryName( path ) );
            var scriptName = Path.GetFileName( path );

            var saved = SaveStackAndFrame();

            var env = MakeExtendedEnvironment();
            var scope = env.Scope;
            CurrentThreadContext.Frame = env.Frame;

            DefDynamic( Symbols.ScriptDirectory, newDir );
            DefDynamic( Symbols.ScriptName, scriptName );
            DefDynamic( Symbols.CommandLineArguments, null );
            DefDynamic( Symbols.Package, GetDynamic( Symbols.Package ) );
            DefDynamic( Symbols.PackageNamePrefix, null );

            if ( loadDebug )
            {
                DefDynamic( Symbols.LoadPrint, true );
                DefDynamic( Symbols.LoadVerbose, true );
            }
            var oldDir = Environment.CurrentDirectory;
            Environment.CurrentDirectory = newDir;

            try
            {
                foreach ( object statement in reader )
                {
                    var code = Compile( statement, scope );
                    if ( code == null )
                    {
                        // compile-time expression, e.g. (module xyz)
                    }
                    else
                    {
                        var result = Execute( code );
                        if ( loadPrint )
                        {
                            PrintLog( ToPrintString( result ) );
                        }
                    }
                }
            }
            catch ( ReturnFromLoadException )
            {
            }
            finally
            {
                Environment.CurrentDirectory = oldDir;
            }

            RestoreStackAndFrame( saved );

            return true;
        }

        internal static char UnescapeCharacter( char ch )
        {
            foreach ( var rep in CharacterTable )
            {
                if ( rep.EscapeString != null && rep.EscapeString[ 1 ] == ch )
                {
                    return rep.Code;
                }
            }

            return ch;
        }

        internal static void Write( object item, bool crlf, params object[] kwargs )
        {
            var args = ParseKwargs( true, kwargs, new string[] { "escape", "width", "stream", "padding", "pretty", "left", "right", "base", "force", "color", "background-color", "format" },
                                            GetDynamic( Symbols.PrintEscape ), 0, DefaultValue.Value, " ", false, null, null, -1,
                                            GetDynamic( Symbols.PrintForce ), GetDynamic( Symbols.PrintColor ), GetDynamic( Symbols.PrintBackgroundColor ),
                                            null );

            var outputstream = args[ 2 ];
            var stream = ConvertToTextWriter( outputstream );

            if ( stream == null )
            {
                return;
            }

            var escape = ToBool( args[ 0 ] );
            var width = ToInt( args[ 1 ] );
            var padding = MakeString( args[ 3 ] );
            var pretty = ToBool( args[ 4 ] );
            var left = args[ 5 ];
            var right = args[ 6 ];
            var radix = ToInt( args[ 7 ] );
            var force = ToBool( args[ 8 ] );
            var color = args[ 9 ];
            var bkcolor = args[ 10 ];
            var format = ( string ) args[ 11 ];

            try
            {
                // Only the REPL result printer sets this variable to false.

                if ( force )
                {
                    item = Force( item );
                }

                if ( pretty && Symbols.PrettyPrintHook.Value != null )
                {
                    var saved = SaveStackAndFrame();

                    DefDynamic( Symbols.StdOut, stream );
                    DefDynamic( Symbols.PrintEscape, escape );
                    DefDynamic( Symbols.PrintBase, radix );
                    DefDynamic( Symbols.PrintForce, false );
                    DefDynamic( Symbols.PrintColor, color );
                    DefDynamic( Symbols.PrintBackgroundColor, bkcolor );

                    var kwargs2 = new Vector();
                    kwargs2.Add( item );
                    kwargs2.Add( Symbols.Left );
                    kwargs2.Add( left );
                    kwargs2.Add( Symbols.Right );
                    kwargs2.Add( right );

                    ApplyStar( ( IApply ) Symbols.PrettyPrintHook.Value, kwargs2 );

                    if ( crlf )
                    {
                        PrintLine();
                    }

                    RestoreStackAndFrame( saved );
                }
                else
                {
                    WriteImp( item, stream, escape, width, padding, radix, crlf, color, bkcolor, format );
                }
            }
            finally
            {
                if ( outputstream is string )
                {
                    // Appending to log file.
                    stream.Close();
                }
            }
        }

        internal static void WriteEscapeCharacter( TextWriter stream, char ch )
        {
            foreach ( var rep in CharacterTable )
            {
                if ( rep.Code == ch && rep.EscapeString != null )
                {
                    stream.Write( rep.EscapeString );
                    return;
                }
            }

            if ( ch < ' ' )
            {
                stream.Write( @"\x{0:x2}", ( int ) ch );
            }
            else
            {
                stream.Write( ch );
            }
        }
        internal static void WriteImp( object item, TextWriter stream, bool escape = true, int width = 0,
                        string padding = " ", int radix = -1, bool crlf = false,
                        object color = null, object bkcolor = null,
                        string format = null )
        {
            string s;

            if ( format == null )
            {
                s = ToPrintString( item, escape: escape, radix: radix );
            }
            else
            {
                s = String.Format( "{0:" + format + "}", item );
            }

            if ( width != 0 )
            {
                var w = Math.Abs( width );

                if ( s.Length > w )
                {
                    s = s.Substring( 0, w );
                }
                else if ( width < 0 || Numberp( item ) )
                {
                    s = s.PadLeft( w, String.IsNullOrEmpty( padding ) ? ' ' : padding[ 0 ] );
                }
                else
                {
                    s = s.PadRight( w, String.IsNullOrEmpty( padding ) ? ' ' : padding[ 0 ] );
                }
            }

#if !KIEZELLISPW
            if ( stream == Console.Out )
            {
                ConsoleSetColor( color, bkcolor );
            }
#endif

            try
            {
                if ( crlf )
                {
                    stream.WriteLine( s.ConvertToExternalLineEndings() );
                }
                else
                {
                    stream.Write( s.ConvertToExternalLineEndings() );
                }
            }
            finally
            {
#if !KIEZELLISPW
                if ( stream == Console.Out )
                {
                    Console.ResetColor();
                }
#endif
            }
        }
    }

    internal class CharacterRepresentation
    {
        internal char Code;
        internal string EscapeString;
        internal string Name;

        internal CharacterRepresentation( char code, string escape, string name )
        {
            Code = code;
            EscapeString = escape;
            Name = name;
        }
    };
}