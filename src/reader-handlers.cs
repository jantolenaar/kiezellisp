// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System.Numerics;
using System.Text.RegularExpressions;

namespace Kiezel
{
    public partial class Runtime
    {
        internal static Regex InterpolationPatterns = new Regex( @"``(.*?)``|\<\%(.*?)\%\>|\<\!(.*?)\!\>", RegexOptions.Singleline );

        internal static bool EvalFeatureExpr( object expr )
        {
            if ( expr == null )
            {
                return false;
            }
            else if ( expr is Symbol )
            {
                return HasFeature( ( ( Symbol ) expr ).Name );
            }
            else if ( expr is Cons )
            {
                var list = ( Cons ) expr;
                var oper = First( list ) as Symbol;
                if ( oper != null )
                {
                    if ( oper.Name == "and" )
                    {
                        return SeqBase.Every( EvalFeatureExpr, list.Cdr );
                    }
                    else if ( oper.Name == "or" )
                    {
                        return SeqBase.Any( EvalFeatureExpr, list.Cdr );
                    }
                    else if ( oper.Name == "not" )
                    {
                        return !EvalFeatureExpr( Second( ( Cons ) expr ) );
                    }
                }
            }

            throw new LispException( "Invalid feature expression" );
        }

        internal static object ParseInterpolateString( string s )
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
                    var statements = new LispReader( script ).ReadAll();

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

        internal static object ReadBlockCommentHandler( LispReader stream, char ch, int arg )
        {
            // Nested comments are allowed.
            stream.ReadBlockComment( "#|", "|#" );
            return VOID.Value;
        }

        internal static object ReadCharacterHandler( LispReader stream, char ch, int arg )
        {
            stream.UnreadChar();
            var name = Runtime.MakeString( stream.Read() );
            var chr = Runtime.DecodeCharacterName( name );
            return chr;
        }

        internal static object ReadCommaHandler( LispReader stream, char ch )
        {
            var code = stream.PeekChar();

            if ( code == '@' )
            {
                stream.ReadChar();
                return MakeList( Symbols.CommaAt, stream.Read() );
            }
            else if ( code == '.' )
            {
                stream.ReadChar();
                return MakeList( Symbols.CommaDot, stream.Read() );
            }
            else
            {
                return MakeList( Symbols.Comma, stream.Read() );
            }
        }

        internal static object ReadComplexNumberHandler( LispReader stream, char ch, int arg )
        {
            if ( stream.ReadChar() != '(' )
            {
                throw stream.MakeScannerException( "Invalid #c expression" );
            }
            var nums = stream.ReadDelimitedList( ")" );
            int count = Runtime.Length( nums );
            double real = count >= 1 ? Runtime.AsDouble( nums.Car ) : 0;
            double imag = count >= 2 ? Runtime.AsDouble( nums.Cdr.Car ) : 0;
            return new Complex( real, imag );
        }

        internal static object ReadExecuteHandler( LispReader stream, char ch, int arg )
        {
            var readEval = Runtime.GetDynamic( Symbols.ReadEval );
            if ( readEval == null )
            {
                readEval = false; //stream.loading;
            }
            if ( !Runtime.ToBool( readEval ) )
            {
                throw stream.MakeScannerException( "Invalid use of '#.' (prohibited by $read-eval variable)" );
            }
            var expr = stream.Read();
            var value = Eval( expr );
            return value;
        }

        internal static object ReadExprCommentHandler( LispReader stream, char ch, int arg )
        {
            stream.ReadSuppressed();
            return VOID.Value;
        }

        internal static object ReadInfixHandler( LispReader stream, char ch, int arg )
        {
            return stream.ParseInfixExpression();
        }

        internal static object ReadLambdaCharacterHandler( LispReader stream, char ch )
        {
            return stream.ParseLambdaCharacter();
        }

        internal static object ReadLineCommentHandler( LispReader stream, char ch )
        {
            stream.ReadLine();
            return VOID.Value;
        }

        internal static object ReadLineCommentHandler2( LispReader stream, char ch, int arg )
        {
            stream.ReadLine();
            return VOID.Value;
        }

        internal static object ReadListHandler( LispReader stream, char ch )
        {
            return stream.ReadDelimitedList( ")" );
        }

        internal static object ReadMinusExprHandler( LispReader stream, char ch, int arg )
        {
            object test = stream.Read();
            bool haveFeatures = EvalFeatureExpr( test );
            if ( haveFeatures )
            {
                stream.ReadSuppressed();
                return VOID.Value;
            }
            else
            {
                return stream.Read();
            }
        }

        internal static object ReadNumberHandler( LispReader stream, char ch, int arg )
        {
            var token = stream.ReadToken();

            switch ( ch )
            {
                case 'r':
                {
                    return Number.ParseNumberBase( token, arg );
                }
                case 'o':
                {
                    return Number.ParseNumberBase( token, 8 );
                }
                case 'b':
                {
                    return Number.ParseNumberBase( token, 2 );
                }
                case 'x':
                {
                    return Number.ParseNumberBase( token, 16 );
                }
                default:
                {
                    // not reached
                    return null;
                }
            }
        }

        internal static object ReadPlusExprHandler( LispReader stream, char ch, int arg )
        {
            object test = stream.Read();
            bool haveFeatures = EvalFeatureExpr( test );
            if ( !haveFeatures )
            {
                stream.ReadSuppressed();
                return VOID.Value;
            }
            else
            {
                return stream.Read();
            }
        }

        internal static object ReadPrototypeHandler( LispReader stream, char ch )
        {
            var list = stream.ReadDelimitedList( "}" );
            var obj = new Prototype();
            obj.Create( true, Runtime.AsArray( list ) );
            return obj;
        }

        internal static object ReadQuasiQuoteHandler( LispReader stream, char ch )
        {
            var exp1 = stream.Read();
            var exp2 = Runtime.QuasiQuoteExpand( exp1 );
            return exp2;
        }

        internal static object ReadQuoteHandler( LispReader stream, char ch )
        {
            return MakeList( Symbols.Quote, stream.Read() );
        }

        internal static object ReadRegexHandler( LispReader stream, char ch, int arg )
        {
            var rx = stream.ParseRegexString( ch );
            return rx;
        }

        internal static object ReadShortLambdaExpressionHandler( LispReader stream, char ch, int arg )
        {
            return stream.ParseShortLambdaExpression( ")" );
        }

        internal static object ReadSpecialStringHandler( LispReader stream, char ch, int arg )
        {
            var str = stream.ParseSpecialString();
            return str;
        }

        internal static object ReadStringHandler( LispReader stream, char ch )
        {
            // C# string "...."
            return ParseInterpolateString( stream.ParseString() );
        }

        internal static object ReadStringHandler2( LispReader stream, char ch, int arg )
        {
            // C# string @"..."
            return ParseInterpolateString( stream.ParseMultiLineString() );
        }

        internal static object ReadUninternedSymbolHandler( LispReader stream, char ch, int arg )
        {
            throw stream.MakeScannerException( "Uninterned symbols are not supported." );
        }

        internal static object ReadVectorHandler( LispReader stream, char ch )
        {
            var list = stream.ReadDelimitedList( "]" );
            var obj = new Vector();
            obj.AddRange( list );
            return obj;
        }
    }
}