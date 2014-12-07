// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System.Collections;

using KeyFunc = System.Func<object, object>;

namespace Kiezel
{
    public partial class Runtime
    {
        public static object QuasiQuoteExpand( Cons expr )
        {
            return QuasiQuoteExpandRest( Second( expr ) );
        }
            
        [Lisp("system:quasi-quote-expand")]
        public static object QuasiQuoteExpandRest( object expr )
        {
            Cons list = expr as Cons;

            if ( !( expr is Cons ) || list == null )
            {
                // not a list or empty
                if ( Symbolp( expr ) && !Keywordp( expr ) )
                {
                    return MakeList( Symbols.Quote, expr );
                }
                else
                {
                    return expr;
                }
            }

            if ( First( list ) == Symbols.Unquote )
            {
                return Second( list );
            }

            if ( First( list ) == Symbols.UnquoteSplicing )
            {
                throw new LispException( "`,@args is illegal syntax" );
            }

            if ( First( list ) == Symbols.UnquoteNSplicing )
            {
                throw new LispException( "`,.args is illegal syntax" );
            }

            return QuasiQuoteExpandList( list );
        }

        internal static object QuasiQuoteExpandList(Cons list)
        {
            Stack stack = new Stack();

            while ( list != null )
            {
                object item1 = First( list );
                list = Cdr( list );

                if ( item1 is Cons )
                {
                    var item2 = ( Cons ) item1;
                    if ( First( item2 ) == Symbols.Unquote )
                    {
                        stack.Push( new Cons( Symbols.List, Cdr( item2 ) ) );
                        continue;
                    }
                    else if ( First( item2 ) == Symbols.UnquoteSplicing )
                    {
                        stack.Push( Second( item2 ) );
                        continue;
                    }
                    else if ( First( item2 ) == Symbols.UnquoteNSplicing )
                    {
                        stack.Push( new Cons( Symbols.bqClobberable, Cdr( item2 ) ) );
                        continue;
                    }
                }

                object expansion = QuasiQuoteExpandRest( item1 );
                stack.Push( MakeList( Symbols.List, expansion ) );
            }

            Cons code = null;
            list = null;
            var listFunction = Symbols.List;

            while ( stack.Count > 0 )
            {
                var expr2 = stack.Pop();
                var temp = expr2 as Cons;

                if ( temp != null && First( temp ) == Symbols.List )
                {
                    list = new Cons( Second( temp ), list );
                }
                else if ( temp != null && First( temp ) == Symbols.bqClobberable )
                {
                    if ( list != null )
                    {
                        throw new LispException( ",.-expression must be at the end" );
                    }
                    listFunction = Symbols.ListStar;
                    list = new Cons( Second( temp ), list );
                }
                else
                {
                    if ( list != null )
                    {
                        code = new Cons( new Cons( Symbols.List, list ), code );
                        list = null;
                    }
                    code = new Cons( expr2, code );
                }
            }

            if ( list != null )
            {
                code = new Cons( new Cons( listFunction, list ), code );
                list = null;
            }

            if ( code != null && Cdr( code ) == null )
            {
                //return new Cons( Symbols.bqAppend, code );
                return First( code );
            }
            else
            {
                return new Cons( Symbols.ForceAppend, code );
            }
        }

        internal static object MapTree( KeyFunc fn, object x )
        {
            if ( Consp( x ) )
            {
                var c = ( Cons ) x;
                var a = fn( Car( c ) );
                var b = ( Cons ) MapTree( fn, Cdr( c ) );
                if ( a == Car( c ) && b == Cdr( c ) )
                {
                    return x;
                }
                else
                {
                    return MakeCons( a, b );
                }
            }
            else
            {
                return fn( x );
            }
        }

#if XXX
        [Lisp( "simplify" )]
        public static object Simplify( object x )
        {
            if ( Consp( x ) )
            {
                var y = First( x ) == Symbols.bqQuote ? x : MapTree( Simplify, x );
                if ( First( y ) != Symbols.bqAppend )
                {
                    return y;
                }
                else
                {
                    return SimplifyArgs( ( Cons ) y );
                }
            }
            else
            {
                return x;
            }
        }

        internal static Cons SimplifyArgs( Cons x )
        {
            Cons result = null;

            foreach ( var arg in Reverse( Cdr( x ) ) )
            {
                var list = arg as Cons;

                if ( list == null )
                {
                    result = AttachAppend( Symbols.bqAppend, arg, result );
                }
                else if ( First( list ) == Symbols.bqList && true )
                {
                    result = AttachConses( Cdr( list ), result );
                }
                else if ( First( list ) == Symbols.bqListStar && true )
                {
                    result = AttachConses( Reverse( Cdr( Reverse( Cdr( list ) ) ) ), AttachAppend( Symbols.bqAppend, Car( Last( list ) ), result ) );
                }
                else if ( First( list ) == Symbols.bqQuote && Consp( Second( list ) ) && true && Cddr( list ) == null )
                {
                    result = AttachConses( MakeList( MakeList( Symbols.bqQuote, First( Second( list ) ) ) ), result );
                }
                else
                {
                    result = AttachAppend( Symbols.bqAppend, list, result );
                }
            }

            return result;
        }

        internal static bool NullOrQuoted( object x )
        {
            if ( x == null )
            {
                return true;
            }

            var y = ( Cons ) x;

            if ( y != null && Car( y ) == Symbols.bqQuote )
            {
                return true;
            }

            return false;
        }

        internal static Cons bqQuoteNil = MakeList( Symbols.bqQuote, null );

        // When BQ-ATTACH-APPEND is called, the OP should be #:BQ-APPEND
        // or #:BQ-NCONC.  This produces a form (op item result) but
        // some simplifications are done on the fly:
        //
        //  (op '(a b c) '(d e f g)) => '(a b c d e f g)
        //  (op item 'nil) => item, provided item is not a splicable frob
        //  (op item 'nil) => (op item), if item is a splicable frob
        //  (op item (op a b c)) => (op item a b c)

        internal static Cons AttachAppend( Symbol op, object item, Cons result )
        {
            if ( NullOrQuoted( item ) && NullOrQuoted( result ) )
            {
                return MakeList( Symbols.bqQuote, Append( (Cons) Second( item ), (Cons) Second( result ) ) );
            }
            else if ( result == null || StructurallyEqual( result, bqQuoteNil ) )
            {
                return (Cons)item;
            }
            else if ( Consp( result ) && First( result ) == op )
            {
                return MakeListStar( op, item, Cdr( result ) );
            }
            else
            {
                return MakeList( op, item, result );
            }
        }

        // The effect of BQ-ATTACH-CONSES is to produce a form as if by
        // `(LIST* ,@items ,result) but some simplifications are done
        // on the fly.
        //
        //  (LIST* 'a 'b 'c 'd) => '(a b c . d)
        //  (LIST* a b c 'nil) => (LIST a b c)
        //  (LIST* a b c (LIST* d e f g)) => (LIST* a b c d e f g)
        //  (LIST* a b c (LIST d e f g)) => (LIST a b c d e f g)

        internal static Cons AttachConses (Cons items, Cons result)
        {
            if ( Every( NullOrQuoted, items ) && NullOrQuoted( result ) )
            {
                return MakeList( Symbols.bqQuote, Append( Map( Second, items ), (Cons) Second( result ) ) );
            }
            else if ( result == null || StructurallyEqual( result, bqQuoteNil ) )
            {
                return new Cons( Symbols.bqList, items );
            }
            else if ( Consp( result ) && ( Car( result ) == Symbols.bqList || Car( result ) == Symbols.bqListStar ) )
            {
                return new Cons( Car( result ), Append( items, Cdr( result ) ) );
            }
            else
            {
                return new Cons( Symbols.bqList, Append( items, MakeList( result ) ) );
            }
        }

#endif
    }
}