// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.


using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Kiezel
{

	class Token
	{
		internal object Value;
		internal int Offset;
		internal int Line;
		internal int Col;

		internal Token( object value, int offset, int line, int col )
		{
			Value = value;
			Offset = offset;
			Line = line;
			Col = col;
		}
	}

    class SyntacticToken
    {
        internal string Text;

        internal SyntacticToken( string text )
        {
            Text = text;
        }

        public override string ToString()
        {
            return Text;
        }

        internal static object EOF = new SyntacticToken("EOF");
        internal static object SHARP_EVAL = new SyntacticToken( "#." );
        internal static object SHARP_PLUS = new SyntacticToken( "#+" );
        internal static object SHARP_MINUS = new SyntacticToken( "#-" );
        internal static object SHARP_SKIP = new SyntacticToken( "#;" );
        internal static object COMPLEX_LITERAL = new SyntacticToken("#c");
        internal static object QUOTE = new SyntacticToken("'");
        internal static object QUASI_QUOTE = new SyntacticToken("`");
        internal static object UNQUOTE = new SyntacticToken(",");
        internal static object UNQUOTE_SPLICING = new SyntacticToken(",@");
        internal static object UNQUOTE_DOT = new SyntacticToken( ",." );
        internal static object LEFT_PAR = new SyntacticToken( "(" );
        internal static object RIGHT_PAR = new SyntacticToken(")");
        internal static object LEFT_BRACE = new SyntacticToken( "{" );
        internal static object RIGHT_BRACE= new SyntacticToken( "}" );
        internal static object LEFT_BRACKET = new SyntacticToken( "[" );
        internal static object RIGHT_BRACKET = new SyntacticToken( "]" );

    }

    enum StringTokenType
    {
        DoubleQuote,
        AtDoubleQuote,
        TripleQuote,
        Parenthesis,
        Bracket,
        Brace,
        Angle
    }

    class StringToken
    {
        internal string Text;
        internal StringTokenType Type;

        public StringToken( string text, StringTokenType type )
        {
            Text = text;
            Type = type;
        }
    }

	class Scanner
	{
        Parser parser;
		internal string filename;
		internal string buffer;
		int index;
		int line;
		int offset;
		int col;
		int startcol;
		char eofChar = Convert.ToChar( 0 );
        internal Cons preparedTokens = null;
        bool loading;

		internal Scanner( Parser parser, string filename, string buffer, bool loading )
		{
            this.parser = parser;
			this.filename = filename;
			this.buffer = buffer;
            this.loading = loading;

			offset = index = 0;
			line = 1;
			col = 0;
		}

        internal Token Next()
        {
            object token;

            if ( preparedTokens != null )
            {
                token = preparedTokens.Car;
                preparedTokens = preparedTokens.Cdr;
            }
            else
            {
                token = ScanToken();
            }

            //Console.Write( token );
            //Console.Write( " " );

            return new Token( token, offset, line, startcol );
        }

		public static bool IsTerminator( char ch )
		{
            return ch == '\'' || ch == '"' || ch == '(' || ch == ')' || ch == '{' || ch == '}' || ch == '[' || ch == ']' || ch == ',' || ch == ';' || ch == '#' || ch == '`';
		}

		bool GoesBeforeLeadingDot( char ch )
		{
			return IsWhiteSpace(ch) || ch == '(' || ch == '[' || ch == '{' || ch == '\'' || ch == '`';
		}

		public static bool IsWhiteSpace( char ch )
		{
			return Char.IsWhiteSpace( ch ) || Char.IsControl( ch );
		}

		void EatChars( int count )
		{
			while ( count-- > 0 && index < buffer.Length )
			{
				++index;
				++col;

				if ( buffer[index - 1] == '\n' )
				{
					++line;
					offset = index;
					col = 1;
				}
			}
		}

		char PeekChar( int offset )
		{
			if ( index + offset < 0 )
			{
				return ' ';
			}
			else if ( index + offset >= buffer.Length )
			{
				return eofChar;
			}
			else
			{
				return buffer[index + offset];
			}
		}

        bool EatWord( string str )
        {
            for ( int i = 0; i < str.Length; ++i )
            {
                if ( PeekChar( i ) != str[ i ] )
                {
                    return false;
                }
            }

            var ch = PeekChar( str.Length);
            
            if ( ch == eofChar || IsTerminator( ch ) || IsWhiteSpace( ch ) )
            {
                EatChars( str.Length );
                return true;
            }

            return false;
        }

        bool Eat( string str )
        {
            for ( int i = 0; i < str.Length; ++i )
            {
                if ( PeekChar( i ) != str[ i ] )
                {
                    return false;
                }
            }

            EatChars( str.Length );
            return true;
        }

        bool Eat( char ch )
        {
            if ( PeekChar( 0 ) == ch )
            {
                EatChars( 1 );
                return true;
            }
            else
            {
                return false;
            }
        }

        void EatTokens( int count )
		{
			index += count;
		}


		void ThrowScannerException( string fmt, params object[] args )
		{
			string msg = System.String.Format( fmt, args );
			int lineStart = offset;
			int lineEnd = offset;
			while ( lineEnd < buffer.Length && buffer[lineEnd] != '\n' )
			{
				++lineEnd;
			}
			string lineData = buffer.Substring( lineStart, lineEnd - lineStart );
			if ( filename == null )
			{
				throw new LispException( "Line {1}: col {2}: {3}\n{4}", filename, line, col, lineData, msg );
			}
			else
			{
				throw new LispException( "{0}: line {1}: col {2}: {3}\n{4}", filename, line, col, lineData, msg );
			}
		}

		char SkipWhiteSpace()
		{
			char ch;

			while ( ( ch = PeekChar( 0 ) ) != eofChar )
			{
				if ( !IsWhiteSpace( ch ) )
				{
					break;
				}
				EatChars( 1 );
			}

			return ch;
		}

		string SkipComment()
		{
            var buf = new StringWriter();
			char ch;

			while ( ( ch = PeekChar( 0 ) ) != eofChar )
			{
				if ( ch == '\n' )
				{
					EatChars( 1 );
					break;
				}
                if ( ch != '\r' )
                {
                    buf.Write( ch );
                }
				EatChars( 1 );
			}

            return buf.ToString();
		}

		void SkipBalancedComment()
		{
			int count = 1;
			char ch;

			while ( ( ch = PeekChar( 0 ) ) != eofChar )
			{
				if ( Eat( "#|" ) )
				{
					++count;
				}
				else if ( Eat( "|#" ) )
				{
					if ( --count == 0 )
					{
						break;
					}
				}
				else
				{
					EatChars( 1 );
				}
			}

			if ( count != 0 )
			{
				ThrowScannerException( "EOF: unclosed comment" );
			}
		}

		string ParseSingleLineString()
		{
            // supports backsslash escapes
            // single line

			char ch;
			StringBuilder buf = new StringBuilder();

			while ( true )
			{
				ch = PeekChar( 0 );

				if ( ch == '\n' || ch == eofChar )
				{
					ThrowScannerException( "EOF: Unterminated string" );
				}

                if ( ch == '"' )
                {
                    EatChars( 1 );
                    break;
                }

				if ( ch == '\\' )
				{
                    EatChars( 1 );
                    ch = PeekChar( 0 );

					if ( ch == eofChar )
					{
						ThrowScannerException( "EOF: Unterminated string" );
					}

					switch ( ch )
					{
						case 'x':
						{
							char ch1 = PeekChar( 1 );
							char ch2 = PeekChar( 2 );
							EatChars( 1 + 2 );
                            int n = (int) Number.ParseNumberBase( new string( new char[] { ch1, ch2 } ), 16 );
							buf.Append( Convert.ToChar( n ) );
							break;
						}
						case 'u':
						{
							char ch1 = PeekChar( 1 );
							char ch2 = PeekChar( 2 );
							char ch3 = PeekChar( 3 );
							char ch4 = PeekChar( 4 );
							EatChars( 1 + 4 );
                            int n = (int) Number.ParseNumberBase( new string( new char[] { ch1, ch2, ch3, ch4 } ), 16 );
							buf.Append( Convert.ToChar( n ) );
							break;
						}
                        default:
						{
                            buf.Append( Runtime.UnescapeCharacter( ch ) );
                            EatChars( 1 );
							break;
						}
					}
				}
				else
				{
					buf.Append( ch );
					EatChars( 1 );
				}
			}

			return buf.ToString();

		}

        string ParseInfixExpressionString()
        {
            char ch;
            StringBuilder buf = new StringBuilder();
            int count = 0;

            while ( true )
            {
                ch = PeekChar( 0 );

                if ( ch == eofChar )
                {
                    ThrowScannerException( "EOF: Unterminated infix expression" );
                }

                EatChars( 1 );

                buf.Append(ch);

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

        string ParseMultiLineString()
        {
            // @"..."
            // supports double double quote escape
            // multi-line

            char ch;
            StringBuilder buf = new StringBuilder();

            while ( true )
            {
                ch = PeekChar( 0 );

                if ( ch == eofChar )
                {
                    ThrowScannerException( "EOF: Unterminated string" );
                }

                if ( ch == '"' )
                {
                    if ( PeekChar( 1 ) == '"' )
                    {
                        buf.Append( ch );
                        EatChars( 2 );
                    }
                    else
                    {
                        EatChars( 1 );
                        break;
                    }
                }
                else
                {
                    buf.Append( ch );
                    EatChars( 1 );
                }
            }

            return buf.ToString();
        }

        Regex ParseRegexString()
        {
            // #"..."

            char ch;
            StringBuilder buf = new StringBuilder();

            while ( true )
            {
                ch = PeekChar( 0 );

                if ( ch == '\n' || ch == eofChar )
                {
                    ThrowScannerException( "EOF: Unterminated string" );
                }

                if ( ch == '"' )
                {
                    if ( PeekChar( 1 ) == '"' )
                    {
                        buf.Append( ch );
                        EatChars( 2 );
                    }
                    else
                    {
                        EatChars( 1 );
                        break;
                    }
                }
                else
                {
                    buf.Append( ch );
                    EatChars( 1 );
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
                            EatChars( 1 );
                            break;
                        }
                        case 's':
                        {
                            options |= RegexOptions.Singleline;
                            EatChars( 1 );
                            break;
                        }
                        case 'm':
                        {
                            options |= RegexOptions.Multiline;
                            EatChars( 1 );
                            break;
                        }
                        default:
                        {
                            ThrowScannerException( "invalid regular expresssion option" );
                            break;
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

        string ParseDocString( string terminator )
        {
            // """..."""
            char ch;
            var buf = new StringWriter();

            while ( true )
            {
                ch = PeekChar( 0 );

                if ( ch == eofChar )
                {
                    ThrowScannerException( "EOF: Unterminated string" );
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
                    if ( ch == '"' && terminator == "\"\"\"" && PeekChar( 3 ) == '"' )
                    {
                        // Append quote if there four or more in succession.
                        haveSeparator = false;
                    }
                }

                if ( haveSeparator )
                {
                    EatChars( terminator.Length );
                    break;
                }
                else
                {
                    buf.Write( ch );
                    EatChars( 1 );
                }
            }

            return buf.ToString();

        }


        object ScanToken()
        {
            char ch;
            string token;

        retry:

            ch = SkipWhiteSpace();

            if ( ch == ';' )
            {
                EatChars( 1 );
                SkipComment();
                goto retry;
            }

            if ( ch == '#' )
            {
                EatChars( 1 );

                // Eat the decimal number in front of the letter
                var digits = 0;
                while ( char.IsDigit( PeekChar( 0 ) ) )
                {
                    digits = 10 * digits + PeekChar( 0 ) - '0';
                    EatChars( 1 );
                }

                if ( Eat( 'r' ) )
                {
                    token = ScanSimpleToken( false );
                    return Number.ParseNumberBase( token, digits == 0 ? 10 : digits );
                }

                if ( Eat( 'b' ) )
                {
                    token = ScanSimpleToken( false );
                    return Number.ParseNumberBase( token, 2 );
                }

                if ( Eat( 'o' ) )
                {
                    token = ScanSimpleToken( false );
                    return Number.ParseNumberBase( token, 8 );
                }

                if ( Eat( 'x' ) )
                {
                    token = ScanSimpleToken( false );
                    return Number.ParseNumberBase( token, 16 );
                }

                if ( Eat( "q(" ) )
                {
                    return new StringToken( ParseDocString( ")" ), StringTokenType.Parenthesis );
                }

                if ( Eat( "q[" ) )
                {
                    return new StringToken( ParseDocString( "]" ), StringTokenType.Bracket );
                }

                if ( Eat( "q{" ) )
                {
                    return new StringToken( ParseDocString( "}" ), StringTokenType.Brace );
                }

                if ( Eat( "q<" ) )
                {
                    return new StringToken( ParseDocString( ">" ), StringTokenType.Angle );
                }

                if ( Eat( '"' ) )
                {
                    return ParseRegexString();
                }

                if ( Eat( '.' ) )
                {
                    var readEval = Runtime.GetDynamic( Symbols.ReadEval );
                    if ( readEval == null )
                    {
                        readEval = loading;
                    }
                    if ( !Runtime.ToBool( readEval ) )
                    {
                        ThrowScannerException( "Invalid use of '#.' (prohibited by $read-eval variable)" );
                    }

                    return SyntacticToken.SHARP_EVAL;
                }

                if ( Eat( '!' ) )
                {
                    SkipComment();
                    goto retry;
                }

                if ( Eat( '|' ) )
                {
                    // balanced comment
                    SkipBalancedComment();
                    goto retry;
                }

                if ( Eat( ';' ) )
                {
                    return SyntacticToken.SHARP_SKIP;
                }

                if ( Eat( ':' ) )
                {
                    var sym = ( Symbol ) ScanToken();
                    return Runtime.MakeSymbol( sym.Name, ( Package ) null );
                }

                if ( Eat( '+' ) )
                {
                    return SyntacticToken.SHARP_PLUS;
                }

                if ( Eat( '-' ) )
                {
                    return SyntacticToken.SHARP_MINUS;
                }

                if ( PeekChar( 0 ) == '\\' )
                {
                    //EatChars( 1 );
                    var sym = ( Symbol ) ScanToken();
                    return Runtime.DecodeCharacterName( sym.Name );
                }

                if ( Eat( 'c' ) || Eat( 'C' ) )
                {
                    return SyntacticToken.COMPLEX_LITERAL;
                }

                if ( ( PeekChar( 0 ) == 'i' || PeekChar( 0 ) == 'I' ) && PeekChar( 1 ) == '(' )
                {
                    EatChars( 1 );
                    var str = ParseInfixExpressionString();
                    var code1 = Runtime.ParseInfixExpression( str );
                    var code2 = RewriteCodeForParser( code1 );
                    preparedTokens = code2.Cdr;
                    return code2.Car;
                }

                ThrowScannerException( "Invalid use of '#'" );
            }

            if ( ch == eofChar )
            {
                return SyntacticToken.EOF;
            }

            startcol = col;

            if ( Eat( '\'' ) )
            {
                return SyntacticToken.QUOTE;
            }

            if ( Eat( '`' ) )
            {
                return SyntacticToken.QUASI_QUOTE;
            }

            if ( Eat( '(' ) )
            {
                return SyntacticToken.LEFT_PAR;
            }

            if ( Eat( ')' ) )
            {
                return SyntacticToken.RIGHT_PAR;
            }

            if ( Eat( '[' ) )
            {
                return SyntacticToken.LEFT_BRACKET;
            }

            if ( Eat( ']' ) )
            {
                return SyntacticToken.RIGHT_BRACKET;
            }

            if ( Eat( '{' ) )
            {
                return SyntacticToken.LEFT_BRACE;
            }

            if ( Eat( '}' ) )
            {
                return SyntacticToken.RIGHT_BRACE;
            }

            if ( Eat( ',' ) )
            {
                if ( Eat( '@' ) )
                {
                    return SyntacticToken.UNQUOTE_SPLICING;
                }
                else if ( Eat( '.' ) )
                {
                    return SyntacticToken.UNQUOTE_DOT;
                }
                else
                {
                    return SyntacticToken.UNQUOTE;
                }
            }

            if ( Eat( "\"\"\"" ) )
            {
                return new StringToken( ParseDocString( "\"\"\"" ), StringTokenType.TripleQuote );
            }

            if ( Eat( "@\"" ) )
            {
                return new StringToken( ParseMultiLineString(), StringTokenType.AtDoubleQuote );
            }

            if ( Eat( "\"" ) )
            {
                return new StringToken( ParseSingleLineString(), StringTokenType.DoubleQuote );
            }

            if ( Eat( Runtime.LambdaCharacter ) )
            {
                var buf = new Vector();
                buf.Add( SyntacticToken.LEFT_PAR );

                while ( Char.IsLetter( PeekChar( 0 ) ) )
                {
                    buf.Add( Runtime.FindSymbol( new string( PeekChar( 0 ), 1 ), false ) );
                    EatChars( 1 );
                }

                buf.Add( SyntacticToken.RIGHT_PAR );

                var lastChar = PeekChar( 0 );

                if ( lastChar == '.' )
                {
                    EatChars( 1 );
                    preparedTokens = Runtime.AsList( buf );
                }
                else if ( !IsWhiteSpace( lastChar ) && !IsTerminator( lastChar ) )
                {
                    throw new LispException( "Invalid single-letter variable name in {0}: {1}", Runtime.LambdaCharacter, lastChar );
                }
                else if ( buf.Count > 2 )
                {
                    preparedTokens = Runtime.AsList( buf );
                }
                else
                {
                    preparedTokens = null;
                }

                return Symbols.GreekLambda;
            }

            token = ScanSimpleToken( true );

            if ( token == "" )
            {
                throw new LispException( "Empty token???" );
            }

            object numberValue;
            object timespan;

            if ( ( numberValue = token.TryParseNumber() ) != null )
            {
                return numberValue;
            }
            else if ( ( timespan = token.TryParseTime() ) != null )
            {
                return timespan;
            }
            else
            {

                if ( token == "true" )
                {
                    return true;
                }

                if ( token == "false" )
                {
                    return false;
                }

                if ( token == "null" )
                {
                    return null;
                }

                if ( token == "." )
                {
                    return Symbols.Accessor;
                }
                else if ( token.StartsWith( "." ) )
                {
                    // .name maps to ( . "name" )
                    preparedTokens = Runtime.MakeList( Symbols.Accessor, token.Substring( 1 ), SyntacticToken.RIGHT_PAR );
                    return SyntacticToken.LEFT_PAR;
                }
                else
                {
                    return Runtime.FindSymbol( token, false );
                }
            }
        }

        Cons RewriteCodeForParser( object code )
        {
            var v = new Vector();
            RewriteCodeForParser( v, code );
            return Runtime.AsList( v );
        }

        void RewriteCodeForParser( Vector output, object code )
        {
            if ( code is Cons )
            {
                output.Add( SyntacticToken.LEFT_PAR );
                foreach ( var item in Runtime.ToIter( code ) )
                {
                    RewriteCodeForParser( output, item );
                }
                output.Add( SyntacticToken.RIGHT_PAR );
            }
            else
            {
                output.Add( code );
            }
        }

        string ScanSimpleToken( bool backslashEscapes )
        {
            bool isEscaped;
            return ScanSimpleToken( backslashEscapes, out isEscaped );
        }

        string ScanSimpleToken( bool backslashEscapes, out bool isEscaped )
        {
            isEscaped = false;
            var buf = new StringWriter();
            var empty = true;

            while ( true )
            {
                var ch = PeekChar( 0 );

                if ( ch == eofChar )
                {
                    break;
                }

                if ( backslashEscapes && ch == '\\' )
                {
                    EatChars( 1 );
                    ch = PeekChar( 0 );
                    if ( ch == eofChar )
                    {
                        ThrowScannerException( "EOF: unexpected end" );
                    }
                    isEscaped = true;
                }
                else if ( IsTerminator( ch ) || IsWhiteSpace( ch ) )
                {
                    if ( !empty && ch == ',' )
                    {
                        // allow embedded comma in numbers
                    }
                    else
                    {
                        break;
                    }
                }

                buf.Write( ch );
                empty = false;

                EatChars( 1 );
            }

            var token = buf.ToString();

            if ( token == "" )
            {
                throw new LispException( "Empty token???" );
            }

            return token;
        }

	}


}
