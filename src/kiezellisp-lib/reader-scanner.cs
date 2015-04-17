// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Kiezel
{
    [RestrictedImport]
    public class LispReader : IEnumerable, IEnumerator
    {
        public static char EofChar = Convert.ToChar( 0 );

        private string buffer;
        private int col;
        private object currentExpression;
        private Cons insertedForms = null;
        private int line;
        private int linePos;
        private bool prettyPrinting;
        private int pos;
        private int symbolSuppression = 0;
        private bool scanning = false;

        [Lisp]
        public LispReader( string sourceCode )
            : this( sourceCode, false )
        {
        }

        internal LispReader( string sourceCode, bool prettyPrinting )
        {
            // When parsing source code for subsequent pretty printing,
            // packages and symbols are handled in more relaxed way.
            buffer = sourceCode;
            linePos = pos = 0;
            line = 0;
            col = 0;
            this.prettyPrinting = prettyPrinting;
        }

        public bool Scanning
        {
            get
            {
                return scanning;
            }
        }

        
        object IEnumerator.Current
        {
            get
            {
                return currentExpression;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }

        bool IEnumerator.MoveNext()
        {
            currentExpression = Read( EOF.Value );
            return currentExpression != EOF.Value;
        }

        void IEnumerator.Reset()
        {
            throw new NotImplementedException();
        }

        [Lisp]
        public void InsertForm( object form )
        {
            insertedForms = Runtime.MakeCons( form, insertedForms );
        }

        [Lisp]
        public char PeekChar( int offset = 0 )
        {
            if ( pos + offset >= buffer.Length )
            {
                return EofChar;
            }
            else
            {
                return buffer[ pos + offset ];
            }
        }

        [Lisp]
        public object Read()
        {
            return Read( null );
        }

        [Lisp]
        public object Read( object eofValue = null )
        {
            object obj;
            while ( ( obj = MaybeRead( eofValue ) ) == VOID.Value )
            {
            }
            return obj;
        }

        [Lisp]
        public Vector ScanAll( object eofValue = null )
        {
            try
            {
                ++symbolSuppression;
                scanning = true;

                var v = new Vector();
                while ( true )
                {
                    var start = pos;
                    object obj = MaybeRead( eofValue );
                    if ( obj == eofValue )
                    {
                        break;
                    }
                    var end = pos;
                    v.Add( buffer.Substring( start, end - start ).Trim() );
                }
                return v;
            }
            finally
            {
                ++symbolSuppression;
                scanning = false;
            }
        }

        [Lisp]
        public Vector ReadAll()
        {
            return Runtime.AsVector( this );
        }

        [Lisp]
        public char ReadChar()
        {
            var chr = PeekChar();
            SkipChars( 1 );
            return chr;
        }

        [Lisp]
        public Cons ReadDelimitedList( string terminator )
        {
            while ( true )
            {
                ReadWhitespace();
                if ( PeekChar( 0 ) == EofChar )
                {
                    throw MakeScannerException( "Unexpected EOF: '{0}' expected", terminator );
                }
                if ( TryTakeChars( terminator ) )
                {
                    return null;
                }
                var first = MaybeRead();
                if ( first != VOID.Value )
                {
                    var rest = ( Cons ) ReadDelimitedList( terminator );
                    return Runtime.MakeCons( first, rest );
                }
            }
        }

        [Lisp]
        public string ReadLine()
        {
            var start = pos;
            char code;

            while ( ( code = ReadChar() ) != EofChar )
            {
                if ( code == '\n' )
                {
                    return buffer.Substring( start, pos - 1 - start );
                }
            }
            return buffer.Substring( start, pos - start );
        }

        [Lisp]
        public string ReadToken()
        {
            var buf = new StringBuilder();

            while ( true )
            {
                var code = PeekChar();
                var item = GetEntry( code );

                if ( item.Type == CharacterType.SingleEscape )
                {
                    if ( PeekChar( 1 ) == EofChar )
                    {
                        throw MakeScannerException( "EOF: expected character after '{0}'", item.Character );
                    }

                    buf.Append( PeekChar( 1 ) );
                    SkipChars( 2 );
                }
                else if ( item.Type == CharacterType.MultipleEscape )
                {
                    while ( true )
                    {
                        SkipChars( 1 );
                        var code2 = PeekChar();
                        if ( code2 == EofChar )
                        {
                            throw MakeScannerException( "EOF: expected character '{0}'", item.Character );
                        }
                        if ( code2 == code )
                        {
                            SkipChars( 1 );
                            break;
                        }
                        buf.Append( code2 );
                    }
                }
                else if ( item.Type == CharacterType.Constituent || item.Type == CharacterType.NonTerminatingMacro )
                {
                    buf.Append( code );
                    SkipChars( 1 );
                }
                else
                {
                    break;
                }
            }

            var token = buf.ToString();

            if ( token == "" )
            {
                throw MakeScannerException( "Empty token???" );
            }

            return token;
        }

        [Lisp]
        public string ReadWhitespace()
        {
            var start = pos;
            while ( true )
            {
                var code = PeekChar();
                var item = GetEntry( code );
                if ( item.Type != CharacterType.Whitespace )
                {
                    break;
                }
                SkipChars( 1 );
            }
            return buffer.Substring( start, pos - start );
        }

        [Lisp]
        public void SkipChars( int count )
        {
            while ( count-- > 0 && pos < buffer.Length )
            {
                ++pos;
                ++col;

                if ( buffer[ pos - 1 ] == '\n' )
                {
                    ++line;
                    linePos = pos;
                    col = 0;
                }
            }
        }

        [Lisp]
        public bool TryTakeChars( string str )
        {
            for ( int i = 0; i < str.Length; ++i )
            {
                if ( PeekChar( i ) != str[ i ] )
                {
                    return false;
                }
            }

            SkipChars( str.Length );
            return true;
        }

        internal int GrepShortLambdaParameters( Cons form )
        {
            var last = 0;

            if ( form != null )
            {
                for ( var head = form; head != null; head = head.Cdr )
                {
                    var sym = head.Car as Symbol;
                    var seq = head.Car as Cons;
                    var index = -1;

                    if ( sym != null )
                    {
                        var position = Runtime.Position( sym, Symbols.ShortLambdaVariables );
                        if ( position != null )
                        {
                            index = ( int ) position;
                            if ( index == 0 )
                            {
                                index = 1;
                                head.Car = Symbols.ShortLambdaVariables[ index ];
                            }
                        }
                    }
                    else if ( seq != null )
                    {
                        index = GrepShortLambdaParameters( seq );
                    }

                    if ( index != -1 )
                    {
                        last = ( last < index ) ? index : last;
                    }
                }
            }

            return last;
        }

        internal LispException MakeScannerException( string message )
        {
            int lineStart = linePos;
            int lineEnd = linePos;
            while ( lineEnd < buffer.Length && buffer[ lineEnd ] != '\n' )
            {
                ++lineEnd;
            }
            string lineData = buffer.Substring( lineStart, lineEnd - lineStart );
            return new LispException( "Line {0} column {1}: {2}\n{3}", line + 1, col, lineData, message );
        }

        internal LispException MakeScannerException( string fmt, params object[] args )
        {
            return MakeScannerException( string.Format( fmt, args ) );
        }

        internal object MaybeRead( object eofValue = null )
        {
            if ( insertedForms != null )
            {
                var obj = insertedForms.Car;
                insertedForms = insertedForms.Cdr;
                return obj;
            }

            ReadWhitespace();

            var code = PeekChar();
            var item = GetEntry( code );

            if ( item.Type == CharacterType.EOF )
            {
                return eofValue;
            }

            if ( item.Type == CharacterType.TerminatingMacro || item.Type == CharacterType.NonTerminatingMacro )
            {
                if ( item.DispatchReadtable == null )
                {
                    if ( item.Handler == null )
                    {
                        throw MakeScannerException( "Invalid character: '{0}'", code );
                    }
                    else
                    {
                        SkipChars( 1 );
                        return item.Handler( this, code );
                    }
                }
                else
                {
                    SkipChars( 1 );
                    var arg = ReadDecimalArg();
                    var subcode = PeekChar();
                    var subitem = item.DispatchReadtable.GetEntry( subcode );
                    if ( subitem.Handler2 != null )
                    {
                        SkipChars( 1 );
                        return subitem.Handler2( this, subcode, arg );
                    }
                    else
                    {
                        throw MakeScannerException( "Invalid character combination: '{0}{1}'", code, subcode );
                    }
                }
            }

            var token = ReadToken();

            object numberValue;
            object timespan;

            if ( scanning || symbolSuppression > 0 )
            {
                return null;
            }
            else if ( ( numberValue = token.TryParseNumber() ) != null )
            {
                return numberValue;
            }
            else if ( ( timespan = token.TryParseTime() ) != null )
            {
                return timespan;
            }
            else if ( token == "true" )
            {
                return true;
            }
            else if ( token == "false" )
            {
                return false;
            }
            else if ( token == "null" )
            {
                return null;
            }
            else if ( token.Length > 1 && token[ 0 ] == '.' )
            {
                // .a.b.c maps to ( . "a" "b" "c" )
                return Runtime.MakeList( Symbols.Dot, token.Substring( 1 ) );
            }
            else if ( token.Length > 1 && token[ 0 ] == '?' )
            {
                // ?a.b.c maps to ( ? "a" "b" "c" )
                return Runtime.MakeList( Symbols.NullableDot, token.Substring( 1 ) );
            }
            else
            {
                return Runtime.FindSymbol( token, prettyPrinting: prettyPrinting );
            }
        }

        internal string ParseDocString( string terminator )
        {
            // """..."""
            char ch;
            var buf = new StringWriter();

            while ( true )
            {
                ch = PeekChar( 0 );

                if ( ch == EofChar )
                {
                    throw MakeScannerException( "EOF: Unterminated string" );
                }

                bool haveSeparator = true;

                for ( int i = 0; i < terminator.Length; ++i )
                {
                    if ( PeekChar( i ) != terminator[ i ] )
                    {
                        haveSeparator = false;
                        break;
                    }
                }

                if ( haveSeparator )
                {
                    SkipChars( terminator.Length );
                    break;
                }
                else
                {
                    buf.Write( ch );
                    SkipChars( 1 );
                }
            }

            return buf.ToString();
        }

        internal object ParseInfixExpression()
        {
            var str = ReadInfixExpressionString();
            var code = Infix.CompileString( str );
            InsertForm( code );
            return VOID.Value;
        }

        internal object ParseLambdaCharacter()
        {
            var buf = new Vector();

            while ( Char.IsLetter( PeekChar() ) )
            {
                buf.Add( Runtime.FindSymbol( new string( PeekChar(), 1 ) ) );
                SkipChars( 1 );
            }

            var lastChar = PeekChar();
            var item = GetEntry( lastChar );

            if ( lastChar == '.' )
            {
                SkipChars( 1 );
                InsertForm( Runtime.AsList( buf ) );
            }
            else if ( item.Type == CharacterType.Constituent || item.Type == CharacterType.NonTerminatingMacro )
            {
                throw MakeScannerException( "Invalid single-letter variable name in {0}: {1}", Runtime.LambdaCharacter, lastChar );
            }
            else if ( buf.Count != 0 )
            {
                InsertForm( Runtime.AsList( buf ) );
            }

            return Symbols.GreekLambda;
        }

        internal string ParseMultiLineString()
        {
            // @"..."
            // supports double double quote escape
            // multi-line

            char ch;
            StringBuilder buf = new StringBuilder();

            while ( true )
            {
                ch = PeekChar( 0 );

                if ( ch == EofChar )
                {
                    throw MakeScannerException( "EOF: Unterminated string" );
                }

                if ( ch == '"' )
                {
                    if ( PeekChar( 1 ) == '"' )
                    {
                        buf.Append( ch );
                        SkipChars( 2 );
                    }
                    else
                    {
                        SkipChars( 1 );
                        break;
                    }
                }
                else
                {
                    buf.Append( ch );
                    SkipChars( 1 );
                }
            }

            return buf.ToString();
        }

        internal string ExtractMultiLineStringForm()
        {
            // @"..."
            // supports double double quote escape
            // multi-line

            char ch;
            StringBuilder buf = new StringBuilder();

            while ( true )
            {
                ch = PeekChar( 0 );

                if ( ch == EofChar )
                {
                    throw MakeScannerException( "EOF: Unterminated string" );
                }

                if ( ch == '"' )
                {
                    if ( PeekChar( 1 ) == '"' )
                    {
                        buf.Append( ch );
                        buf.Append( ch );
                        SkipChars( 2 );
                    }
                    else
                    {
                        SkipChars( 1 );
                        break;
                    }
                }
                else
                {
                    buf.Append( ch );
                    SkipChars( 1 );
                }
            }

            return "@\"" + buf.ToString() + "\"";
        }

        internal Regex ParseRegexString( char terminator )
        {
            // #"..."

            char ch;
            StringBuilder buf = new StringBuilder();

            while ( true )
            {
                ch = PeekChar( 0 );

                if ( ch == '\n' || ch == EofChar )
                {
                    throw MakeScannerException( "EOF: Unterminated string" );
                }

                if ( ch == terminator )
                {
                    if ( PeekChar( 1 ) == terminator )
                    {
                        buf.Append( ch );
                        SkipChars( 2 );
                    }
                    else
                    {
                        SkipChars( 1 );
                        break;
                    }
                }
                else
                {
                    buf.Append( ch );
                    SkipChars( 1 );
                }
            }

            var options = RegexOptions.None;

            while ( true )
            {
                ch = PeekChar( 0 );
                if ( Char.IsLetter( ch ) )
                {
                    switch ( ch )
                    {
                        case 'i':
                        {
                            options |= RegexOptions.IgnoreCase;
                            SkipChars( 1 );
                            break;
                        }
                        case 's':
                        {
                            options |= RegexOptions.Singleline;
                            SkipChars( 1 );
                            break;
                        }
                        case 'm':
                        {
                            options |= RegexOptions.Multiline;
                            SkipChars( 1 );
                            break;
                        }
                        default:
                        {
                            throw MakeScannerException( "invalid regular expresssion option" );
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            return new Regex( buf.ToString(), options );
        }

        internal object ParseShortLambdaExpression( string delimiter )
        {
            var body = ReadDelimitedList( delimiter );
            if ( !Runtime.Consp( Runtime.First( body ) ) )
            {
                body = Runtime.MakeList( body );
            }
            var lastIndex = GrepShortLambdaParameters( body );
            var args = Runtime.AsList( Symbols.ShortLambdaVariables.Skip( 1 ).Take( lastIndex ) );
            var code = Runtime.MakeListStar( Symbols.Lambda, args, body );
            return code;
        }

        internal string ParseSingleLineString()
        {
            // supports backsslash escapes
            // single line

            char ch;
            StringBuilder buf = new StringBuilder();

            while ( true )
            {
                ch = PeekChar( 0 );

                if ( ch == '\n' || ch == EofChar )
                {
                    throw MakeScannerException( "EOF: Unterminated string" );
                }

                if ( ch == '"' )
                {
                    SkipChars( 1 );
                    break;
                }

                if ( ch == '\\' )
                {
                    SkipChars( 1 );
                    ch = PeekChar( 0 );

                    if ( ch == EofChar )
                    {
                        throw MakeScannerException( "EOF: Unterminated string" );
                    }

                    switch ( ch )
                    {
                        case 'x':
                        {
                            char ch1 = PeekChar( 1 );
                            char ch2 = PeekChar( 2 );
                            SkipChars( 1 + 2 );
                            int n = ( int ) Number.ParseNumberBase( new string( new char[] { ch1, ch2 } ), 16 );
                            buf.Append( Convert.ToChar( n ) );
                            break;
                        }
                        case 'u':
                        {
                            char ch1 = PeekChar( 1 );
                            char ch2 = PeekChar( 2 );
                            char ch3 = PeekChar( 3 );
                            char ch4 = PeekChar( 4 );
                            SkipChars( 1 + 4 );
                            int n = ( int ) Number.ParseNumberBase( new string( new char[] { ch1, ch2, ch3, ch4 } ), 16 );
                            buf.Append( Convert.ToChar( n ) );
                            break;
                        }
                        default:
                        {
                            buf.Append( Runtime.UnescapeCharacter( ch ) );
                            SkipChars( 1 );
                            break;
                        }
                    }
                }
                else
                {
                    buf.Append( ch );
                    SkipChars( 1 );
                }
            }

            return buf.ToString();
        }

        internal string ExtractSingleLineStringForm()
        {
            // supports backsslash escapes
            // single line

            char ch;
            StringBuilder buf = new StringBuilder();

            while ( true )
            {
                ch = PeekChar( 0 );

                if ( ch == '\n' || ch == EofChar )
                {
                    throw MakeScannerException( "EOF: Unterminated string" );
                }

                if ( ch == '"' )
                {
                    SkipChars( 1 );
                    break;
                }

                if ( ch == '\\' )
                {
                    buf.Append( ch );
                    SkipChars( 1 );
                    ch = PeekChar( 0 );

                    if ( ch == EofChar )
                    {
                        throw MakeScannerException( "EOF: Unterminated string" );
                    }

                    switch ( ch )
                    {
                        case 'x':
                        {
                            char ch1 = PeekChar( 1 );
                            char ch2 = PeekChar( 2 );
                            SkipChars( 1 + 2 );
                            buf.Append( ch );
                            buf.Append( ch1 );
                            buf.Append( ch2 );
                            break;
                        }
                        case 'u':
                        {
                            char ch1 = PeekChar( 1 );
                            char ch2 = PeekChar( 2 );
                            char ch3 = PeekChar( 3 );
                            char ch4 = PeekChar( 4 );
                            SkipChars( 1 + 4 );
                            buf.Append( ch );
                            buf.Append( ch1 );
                            buf.Append( ch2 );
                            buf.Append( ch3 );
                            buf.Append( ch4 );
                            break;
                        }
                        default:
                        {
                            buf.Append( ch );
                            SkipChars( 1 );
                            break;
                        }
                    }
                }
                else
                {
                    buf.Append( ch );
                    SkipChars( 1 );
                }
            }

            return buf.ToString();
        }

        internal string ParseSpecialString()
        {
            var begin = PeekChar();
            var terminator = "";

            switch ( begin )
            {
                case '(':
                {
                    SkipChars( 1 );
                    terminator = ")";
                    break;
                }
                case '{':
                {
                    SkipChars( 1 );
                    terminator = "}";
                    break;
                }
                case '[':
                {
                    SkipChars( 1 );
                    terminator = "]";
                    break;
                }
                case '<':
                {
                    SkipChars( 1 );
                    terminator = ">";
                    break;
                }
                default:
                {
                    terminator = ReadLine().Trim();
                    if ( terminator == "" )
                    {
                        MakeScannerException( "No terminator after #q expression" );
                    }
                    break;
                }
            }

            return ParseDocString( terminator );
        }

        internal string ExtractSpecialStringForm()
        {
            var begin = ReadChar();
            var end = ' ';

            switch ( begin )
            {
                case '(':
                {
                    end = ')';
                    break;
                }
                case '{':
                {
                    end = '}';
                    break;
                }
                case '[':
                {
                    end = ']';
                    break;
                }
                case '<':
                {
                    end = '>';
                    break;
                }
                default:
                {
                    end = begin;
                    break;
                }
            }

            return begin + ParseDocString( new string( end, 1 ) ) + end;
        }

        internal string ParseString()
        {
            if ( TryTakeChars( "\"\"" ) )
            {
                return ParseDocString( "\"\"\"" );
            }
            else
            {
                return ParseSingleLineString();
            }
        }

        internal string ExtractStringForm()
        {
            if ( TryTakeChars( "\"\"" ) )
            {
                return "\"\"\"" + ParseDocString( "\"\"\"" ) + "\"\"\"";
            }
            else
            {
                return "\""+ ExtractSingleLineStringForm() + "\"";
            }
        }

        internal string ReadBlockComment( string startDelimiter, string endDelimiter )
        {
            var start = pos - startDelimiter.Length;
            var nesting = 1;

            while ( PeekChar() != EofChar )
            {
                if ( TryTakeChars( startDelimiter ) )
                {
                    ++nesting;
                }
                else if ( TryTakeChars( endDelimiter ) )
                {
                    if ( --nesting == 0 )
                    {
                        break;
                    }
                }
                else
                {
                    SkipChars( 1 );
                }
            }

            if ( nesting != 0 )
            {
                throw MakeScannerException( "EOF: Unterminated comment" );
            }

            return buffer.Substring( start, pos - start );
        }

        internal string ReadInfixExpressionString()
        {
            char ch;
            StringBuilder buf = new StringBuilder();
            int count = 0;

            //ch = ReadChar();
            //if ( ch != '(' )
            //{
            //   throw MakeScannerException( "EOF: infix expression must begin with a '('" );
            //}

            while ( true )
            {
                ch = PeekChar( 0 );

                if ( ch == EofChar )
                {
                    throw MakeScannerException( "EOF: Unterminated infix expression" );
                }

                SkipChars( 1 );

                buf.Append( ch );

                if ( ch == '(' )
                {
                    ++count;
                }
                else if ( ch == ')' )
                {
                    --count;
                    if ( count == 0 )
                    {
                        break;
                    }
                }
            }

            return buf.ToString();
        }

        internal void ReadSuppressed()
        {
            try
            {
                ++symbolSuppression;
                Read();
            }
            finally
            {
                --symbolSuppression;
            }
        }

        internal void SkipLineComment()
        {
            ReadLine();
        }

        internal void UnreadChar()
        {
            if ( pos > 0 )
            {
                --pos;
            }
        }

        private ReadtableEntry GetEntry( char ch )
        {
            var readTable = ( Readtable ) Runtime.GetDynamic( Symbols.Readtable );
            return readTable.GetEntry( ch );
        }

        private int ReadDecimalArg()
        {
            int arg = -1;
            while ( true )
            {
                var ch = PeekChar();
                if ( ch == EofChar || !char.IsDigit( ( char ) ch ) )
                {
                    break;
                }
                if ( arg == -1 )
                {
                    arg = 0;
                }
                arg = 10 * arg + ( ch - '0' );
                SkipChars( 1 );
            }
            return arg;
        }
    }
}