using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kiezel
{
    public partial class Runtime
    {

        [Lisp( "caar" )]
        public static object Caar( Cons list )
        {
            return Car( ToCons( Car( list ) ) );
        }

        [Lisp( "cadr" )]
        public static object Cadr( Cons list )
        {
            return Car( Cdr( list ) );
        }

        [Lisp( "car" )]
        public static object Car( Cons list )
        {
            return list == null ? null : list.Car;
        }

        [Lisp( "cdar" )]
        public static Cons Cdar( Cons list )
        {
            return Cdr( ToCons( Car( list ) ) );
        }

        [Lisp( "cddr" )]
        public static Cons Cddr( Cons list )
        {
            return Cdr( Cdr( list ) );
        }

        [Lisp( "cdr", "rest" )]
        public static Cons Cdr( Cons list )
        {
            return list == null ? null : list.Cdr;
        }

        [Lisp( "fifth" )]
        public static object Fifth( IEnumerable seq )
        {
            return Nth( 4, seq );
        }

        [Lisp( "first" )]
        public static object First( IEnumerable seq )
        {
            return Nth( 0, seq );
        }

        [Lisp( "fourth" )]
        public static object Fourth( IEnumerable seq )
        {
            return Nth( 3, seq );
        }

        [Lisp( "last" )]
        public static Cons Last( Cons seq )
        {
            return Last( 1, seq );
        }

        [Lisp( "last" )]
        public static Cons Last( int count, Cons seq )
        {
            if ( seq == null || count <= 0 )
            {
                return null;
            }

            var list = seq;
            var size = Length( list );

            for ( var i = count; i < size; ++i )
            {
                list = list.Cdr;
            }
            return list;
        }

        [Lisp( "nth" )]
        public static object Nth( int pos, object seq )
        {
            if ( pos < 0 || seq == null )
            {
                return null;
            }
            else if ( seq is string )
            {
                var s = ( string ) seq;
                if ( pos < s.Length )
                {
                    return s[ pos ];
                }
                else
                {
                    return null;
                }
            }
            else if ( seq is IList )
            {
                var l = ( IList ) seq;
                if ( pos < l.Count )
                {
                    return ( ( IList ) seq )[ pos ];
                }
                else
                {
                    return null;
                }
            }
            else if ( seq is IEnumerable )
            {
                foreach ( object item in ( IEnumerable ) seq )
                {
                    if ( --pos < 0 )
                    {
                        return item;
                    }
                }

                return null;
            }
            else
            {
                throw new LispException( "Cannot cast as IEnumerable: {0}", ToPrintString( seq ) );
            }
        }

        [Lisp( "second" )]
        public static object Second( IEnumerable seq )
        {
            return Nth( 1, seq );
        }

        [Lisp( "set-car" )]
        public static object SetCar( Cons list, object item )
        {
            if ( list == null )
            {
                throw new LispException( "Cannot set-car of null" );
            }
            list.Car = item;
            return item;
        }

        [Lisp( "set-cdr" )]
        public static object SetCdr( Cons list, Cons item )
        {
            if ( list == null )
            {
                throw new LispException( "Cannot set-cdr of null" );
            }
            list.Cdr = item;
            return item;
        }

        [Lisp( "set-fifth" )]
        public static object SetFifth( IEnumerable seq, object item )
        {
            return SetNth( seq, 4, item );
        }

        [Lisp( "set-first" )]
        public static object SetFirst( IEnumerable seq, object item )
        {
            return SetNth( seq, 0, item );
        }

        [Lisp( "set-fourth" )]
        public static object SetFourth( IEnumerable seq, object item )
        {
            return SetNth( seq, 3, item );
        }

        [Lisp( "set-second" )]
        public static object SetSecond( IEnumerable seq, object item )
        {
            return SetNth( seq, 1, item );
        }

        [Lisp( "set-third" )]
        public static object SetThird( IEnumerable seq, object item )
        {
            return SetNth( seq, 2, item );
        }

        [Lisp( "third" )]
        public static object Third( IEnumerable seq )
        {
            return Nth( 2, seq );
        }

        internal static object First( object seq )
        {
            return Nth( 0, seq );
        }

        internal static object Second( object seq )
        {
            return Nth( 1, seq );
        }

        internal static object SetNth( object seq, int pos, object item )
        {
            if ( seq is Cons )
            {
                var list = ( Cons ) seq;
                list[ pos ] = item;
                return item;
            }
            else if ( seq is IList )
            {
                var list = ( IList ) seq;
                list[ pos ] = item;
                return item;
            }

            throw new LispException( "Cannot setf on: {0}", ToPrintString( seq ) );
        }

        internal static object Third( object seq )
        {
            return Nth( 2, seq );
        }



    }

}
