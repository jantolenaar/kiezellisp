// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.


using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Kiezel
{
    public class Parser : IEnumerable, IEnumerator
    {
        Stack<Token> parens = new Stack<Token>();
        Scanner scanner;
        Token token;
        object CurrentExpression;

        internal Parser( string buffer, bool loading )
            : this( null, buffer, loading )
        {
        }

        internal Parser( string filename, string buffer, bool loading )
        {
            scanner = new Scanner( this, filename, buffer, loading );
        }

        object IEnumerator.Current
        {
            get
            {
                return CurrentExpression;
            }
        }

        bool IEnumerator.MoveNext()
        {
            CurrentExpression = Read();
            return CurrentExpression != SyntacticToken.EOF;
        }

        void IEnumerator.Reset()
        {
            throw new NotImplementedException();
        }

        internal Vector ReadAll()
        {
            return Runtime.AsVector( this );
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
            //object x;
            //while ( TryRead( out x ) )
            //{
            //    yield return x;
            //}
        }

        internal bool EvalFeatureExpr( object expr )
        {
            if ( expr is Symbol )
            {
                return Runtime.HasFeature( ( ( Symbol ) expr ).Name );
            }
            else if ( expr is Cons )
            {
                var list = ( Cons ) expr;
                var oper = Runtime.First( list ) as Symbol;
                if ( oper != null )
                {
                    if ( oper.Name == "and" )
                    {
                        return Runtime.Every( EvalFeatureExpr, list.Cdr );
                    }
                    else if ( oper.Name == "or" )
                    {
                        return Runtime.Any( EvalFeatureExpr, list.Cdr );
                    }
                    else if ( oper.Name == "not" )
                    {
                        return !EvalFeatureExpr( Runtime.Second( ( Cons ) expr ) );
                    }
                }
            }
            ThrowParserException( "invalid feature expression" );
            return false;
        }

        void NextToken()
        {
            token = scanner.Next();
        }

        void PushOpeningToken()
        {
            parens.Push( token );
        }

        string GetClosingTokens()
        {
            var buf = new StringWriter();
            while ( parens.Count > 0 )
            {
                string opener = parens.Pop().Value.ToString();
                switch ( opener )
                {
                    case "[":
                    {
                        buf.Write( "]" );
                        break;
                    }
                    case "(":
                    {
                        buf.Write( ")" );
                        break;
                    }
                    case "{":
                    {
                        buf.Write( "}" );
                        break;
                    }
                    case "<%":
                    {
                        buf.Write( "%>" );
                        break;
                    }
                    case "(<)":
                    {
                        buf.Write( ">" );
                        break;
                    }

                }

            }

            return buf.ToString();
        }

        void PopClosingToken()
        {
            string closer = CurrentTokenValue().ToString();

            if ( parens.Count == 0 )
            {
                ThrowParserException( "unexpected {0}", closer );
            }

            string opener = parens.Peek().Value.ToString();

            switch ( opener )
            {
                case "[":
                {
                    if ( closer != "]" )
                    {
                        ThrowParserException( "expected '[' not '{0}'", closer );
                    }
                    break;
                }
                case "(":
                {
                    if ( closer != ")" )
                    {
                        ThrowParserException( "expected ')' not '{0}'", closer );
                    }
                    break;
                }
                case "{":
                {
                    if ( closer != "}" )
                    {
                        ThrowParserException( "expected '}' not '{0}'", closer );
                    }
                    break;
                }
                case "<%":
                {
                    if ( closer != "%>" )
                    {
                        ThrowParserException( "expected '%>' not '{0}'", closer );
                    }
                    break;
                }
                case "(<)":
                {
                    if ( closer != "(>)" )
                    {
                        ThrowParserException( "expected '>' not '{0}'", closer );
                    }
                    break;
                }

            }

            parens.Pop();
        }

        object CurrentTokenValue()
        {
            if ( token == null )
            {
                return null;
            }
            else
            {
                return token.Value;
            }
        }

        void ThrowParserException( string fmt, params object[] args )
        {
            string msg = System.String.Format( fmt, args );
            if ( token != null )
            {
                int lineStart = token.Offset;
                int lineEnd = token.Offset;
                while ( lineEnd < scanner.buffer.Length && scanner.buffer[ lineEnd ] != '\n' )
                {
                    ++lineEnd;
                }
                string lineData = scanner.buffer.Substring( lineStart, lineEnd - lineStart );
                if ( scanner.filename == null )
                {
                    throw new LispException( "line {1}: col {2}: {3}\n{4}", scanner.filename, token.Line, token.Col, lineData, msg );
                }
                else
                {
                    throw new LispException( "{0}: line {1}: col {2}: {3}\n{4}", scanner.filename, token.Line, token.Col, lineData, msg );
                }
            }
            else
            {
                if ( scanner.filename == null )
                {
                    throw new LispException( msg );
                }
                else
                {
                    throw new LispException( "{0}: {1}", scanner.filename, msg );
                }
            }
        }

        void NeedToken( object target )
        {
            if ( token.Value != target )
            {
                ThrowParserException( "expected <{0}> not <{1}>", target, token.Value );
            }
        }

        Cons ReadList( object term )
        {
            object p = ConditionalRead( inList: true );

            if ( p == term )
            {
                PopClosingToken();
                return null;
            }
            else if ( p == SyntacticToken.EOF )
            {
                Token opener = parens.Peek();
                ThrowParserException( "EOF: unterminated list started at line {0}, column {1}", opener.Line, opener.Col );
                return null;
            }
            else
            {
                Cons q = ReadList( term );
                return new Cons( p, q );
            }
        }

        public object Read()
        {
            var obj = ConditionalRead();

            if ( obj != SyntacticToken.EOF && obj is SyntacticToken )
            {
                ThrowParserException( "Unexpected token: {0}", obj );
            }

            return obj;
        }

        void SuppressedRead()
        {
            var saved = Runtime.SaveStackAndFrame();

            try
            {
                Runtime.DefDynamic( Symbols.ReadSuppress, true );
                ConditionalRead();
            }
            finally
            {
                Runtime.RestoreStackAndFrame( saved );
            }
        }

        object ConditionalRead( bool haveToken = false, bool inList = false )
        {
            if ( !haveToken )
            {
                NextToken();
            }

            object tokval = CurrentTokenValue();

            if ( tokval == SyntacticToken.SHARP_EVAL )
            {
                var val = Read();
                return Runtime.Eval( val );
            }
            else if ( tokval == SyntacticToken.SHARP_SKIP )
            {
                SuppressedRead();
                return ConditionalRead( inList: inList );
            }
            else if ( tokval == SyntacticToken.SHARP_PLUS )
            {
                object test = UnconditionalRead();
                bool haveFeatures = EvalFeatureExpr( test );
                if ( !haveFeatures )
                {
                    SuppressedRead();
                }
                return ConditionalRead( inList: inList );
            }
            else if ( tokval == SyntacticToken.SHARP_MINUS )
            {
                object test = UnconditionalRead();
                bool haveFeatures = EvalFeatureExpr( test );
                if ( haveFeatures )
                {
                    SuppressedRead();
                }
                return ConditionalRead( inList: inList );
            }
            else
            {
                return UnconditionalRead( haveToken: true, inList: inList );
            }
        }

        object UnconditionalRead( bool haveToken = false, bool inList = false )
        {
            if ( !haveToken  )
            {
                NextToken();
            }

            object token = CurrentTokenValue();

            if ( token == SyntacticToken.EOF )
            {
                return SyntacticToken.EOF;
            }

            if ( token is ValueType )
            {
                return token;
            }

            if ( token is string )
            {
                return InterpolateString( ( string ) token );
            }

            if ( token is StringToken )
            {
                var s = (StringToken)token;
                switch ( s.Type )
                {
                    case StringTokenType.AtDoubleQuote:
                    case StringTokenType.DoubleQuote:
                    case StringTokenType.TripleQuote:
                    {
                        return InterpolateString( s.Text );
                    }
                    case StringTokenType.Angle:
                    case StringTokenType.Brace:
                    case StringTokenType.Bracket:
                    case StringTokenType.Parenthesis:
                    default:
                    {
                        return s.Text;
                    }
                }
            }

            if ( token == SyntacticToken.LEFT_PAR )
            {
                PushOpeningToken();
                return ReadList( SyntacticToken.RIGHT_PAR );
            }

            if ( token == SyntacticToken.LEFT_BRACKET )
            {
                PushOpeningToken();
                var list = ReadList( SyntacticToken.RIGHT_BRACKET );
                return Runtime.AsVector( list );
            }

            if ( token == SyntacticToken.LEFT_BRACE )
            {
                PushOpeningToken();
                var list = ReadList( SyntacticToken.RIGHT_BRACE );
                var obj = new Prototype();
                obj.Create( true, Runtime.AsArray( list ) );
                return obj;
            }

            if ( token == SyntacticToken.QUOTE )
            {
                object exp = Read();
                if ( exp == SyntacticToken.EOF )
                {
                    ThrowParserException( "EOF: Incomplete '-expression" );
                }
                return Runtime.MakeList( Symbols.Quote, exp );
            }


            if ( token == SyntacticToken.QUASI_QUOTE )
            {
                object exp = Read();
                if ( exp == SyntacticToken.EOF )
                {
                    ThrowParserException( "EOF: Incomplete `-expression" );
                }
                //Console.Write( "in:  " );
                //Console.WriteLine( Runtime.ToPrintString( exp ) );
                var exp2 = Runtime.QuasiQuoteExpand( exp );
                //Console.Write( "out: " );
                //Console.WriteLine( Runtime.ToPrintString( exp2 ) );
                return exp2;
            }

            if ( token == SyntacticToken.UNQUOTE_SPLICING )
            {
                object exp = Read();
                if ( exp == SyntacticToken.EOF )
                {
                    ThrowParserException( "EOF: Incomplete ,@-expression" );
                }
                return Runtime.MakeList( Symbols.CommaAt, exp );
            }

            if ( token == SyntacticToken.UNQUOTE_DOT )
            {
                object exp = Read();
                if ( exp == SyntacticToken.EOF )
                {
                    ThrowParserException( "EOF: Incomplete ,.-expression" );
                }
                return Runtime.MakeList( Symbols.CommaDot, exp );
            }

            if ( token == SyntacticToken.UNQUOTE )
            {
                object exp = Read();
                if ( exp == SyntacticToken.EOF )
                {
                    ThrowParserException( "EOF: Incomplete ,-expression" );
                }
                return Runtime.MakeList( Symbols.Comma, exp );
            }

            if ( token == SyntacticToken.COMPLEX_LITERAL )
            {
                NextToken();
                if ( CurrentTokenValue() != SyntacticToken.LEFT_PAR )
                {
                    ThrowParserException( "Missing left parenthesis in complex literal" );
                }
                PushOpeningToken();
                Cons nums = ReadList( SyntacticToken.RIGHT_PAR );
                int count = Runtime.Length( nums );
                double real = count >= 1 ? Runtime.AsDouble( nums.Car ) : 0;
                double imag = count >= 2 ? Runtime.AsDouble( nums.Cdr.Car ) : 0;
                return new Complex( real, imag );
            }

            if ( token is SyntacticToken )
            {
                if ( !inList || ( token != SyntacticToken.RIGHT_PAR && token != SyntacticToken.RIGHT_BRACE && token != SyntacticToken.RIGHT_BRACKET ) )
                {
                    ThrowParserException( "Unexpected token: {0}", token );
                }
            }

            return token;

        }

        internal static Regex InterpolationPatterns = new Regex( @"``(.*?)``|\<\%(.*?)\%\>|\<\!(.*?)\!\>", RegexOptions.Singleline );

        internal static object InterpolateString( string s )
        {
            int pos = 0;
            Vector code = new Vector();

            for ( var match = InterpolationPatterns.Match( s, pos ); match.Success; match = match.NextMatch() )
            {
                var left = match.Index;
                var sideEffect = false;
                var script = "";

                code.Add( s.Substring( pos, left - pos ) );
                pos = match.Index + match.Length;

                if ( match.Groups[ 1 ].Success )
                {
                    // ``...``
                    script = match.Groups[ 1 ].Value;
                    sideEffect = false;
                }
                else if ( match.Groups[ 2 ].Success )
                {
                    // <%...%> <%=...%>
                    script = match.Groups[ 2 ].Value.Trim();
                    if ( script.StartsWith( "=" ) )
                    {
                        script = script.Substring( 1 );
                        sideEffect = false;
                    }
                    else
                    {
                        sideEffect = true;
                    }
                }
                else if ( match.Groups[ 3 ].Success )
                {
                    // <!...!> <!=...!>
                    script = match.Groups[ 3 ].Value.Trim();
                    if ( script.StartsWith( "=" ) )
                    {
                        script = script.Substring( 1 );
                        sideEffect = false;
                    }
                    else
                    {
                        sideEffect = true;
                    }
                }

                script = script.Trim();

                if ( script.Length > 0 )
                {
                    if ( sideEffect )
                    {
                        if ( script[ 0 ] != '(' )
                        {
                            // must have function call to have side effect
                            script = "(with-output-to-string ($stdout) (" + script + "))";
                        }
                        else
                        {
                            script = "(with-output-to-string ($stdout) " + script + ")";
                        }
                    }
                    var statements = new Parser( script, false ).ReadAll();

                    if ( statements.Count > 1 )
                    {
                        code.Add( new Cons( Symbols.Do, Runtime.AsList( statements ) ) );
                    }
                    else
                    {
                        code.AddRange( statements );
                    }
                }
            }

            if ( code.Count == 0 )
            {
                return s;
            }
            else
            {
                if ( pos < s.Length )
                {
                    code.Add( s.Substring( pos, s.Length - pos ) );
                }

                return new Cons( Symbols.Str, Runtime.AsList( code ) );
            }
        }


        internal static int CountNewLines( string source, int pos )
        {
            int count = 0;

            for ( int i = 0; i < pos && i < source.Length; ++i )
            {
                if ( source[ i ] == '\n' )
                {
                    ++count;
                }
            }

            return count;
        }


    }


}
