// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.IO;

namespace Kiezel
{
    public class Cons : IEnumerable, IPrintsValue
    {
        internal static Cons EMPTY = new Cons();
        internal object car;
        internal object cdr;
        internal Cons()
        {
            car = null;
            cdr = this;
        }

        internal Cons( object first, object second )
        {
            car = first;
            cdr = second;
        }

        internal object Car
        {
            get
            {
                if ( this == Cons.EMPTY )
                {
                    return null;
                }
                else
                {
                    return car;
                }
            }
            set
            {
                if ( this == Cons.EMPTY )
                {
                    throw new LispException( "Cannot set car of empty list" );
                }
                else
                {
                    car = value;
                }
            }
        }

        internal Cons Cdr
        {
            get
            {
                if ( this == Cons.EMPTY )
                {
                    return this;
                }

                if ( cdr is IEnumerator )
                {
                    cdr = Runtime.MakeCons( ( IEnumerator ) cdr );
                }
                else if ( cdr is DelayedExpression )
                {
                    cdr = Runtime.Force( cdr );
                }

                return ( Cons ) cdr;
            }
            set
            {
                if ( this == Cons.EMPTY )
                {
                    throw new LispException( "Cannot set cdr of empty list" );
                }
                else if ( value == Cons.EMPTY )
                {
                    cdr = null;
                }
                else
                {
                    cdr = value;
                }
            }
        }

        internal int Count
        {
            get
            {
                int count = 0;
                Cons list = this;
                while ( list != null )
                {
                    ++count;
                    list = list.Cdr;
                }
                return count;
            }
        }

        internal bool Forced
        {
            get
            {
                return cdr == null || cdr is Cons;
            }
        }

        public object this[ int index ]
        {
            get
            {
                int n = index;
                Cons list = this;

                if ( n < 0 )
                {
                    throw new IndexOutOfRangeException();
                }

                while ( n-- > 0 )
                {
                    if ( list == null )
                    {
                        return null;
                    }

                    list = list.Cdr;
                }

                if ( list == null )
                {
                    return null;
                }
                else
                {
                    return list.Car;
                }
            }

            set
            {
                int n = index;
                Cons list = this;

                if ( n < 0 )
                {
                    throw new IndexOutOfRangeException();
                }

                while ( n-- > 0 )
                {
                    if ( list == null )
                    {
                        throw new IndexOutOfRangeException();
                    }

                    list = list.Cdr;
                }

                if ( list == null )
                {
                    throw new IndexOutOfRangeException();
                }
                else
                {
                    list.Car = value;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ListEnumerator( this );
        }

        public override string ToString()
        {
            return ToString( true );
        }

        //internal static bool printCompact = true;
        public string ToString( bool escape, int radix = -1 )
        {
            if ( !( cdr is IEnumerator || cdr is DelayedExpression ) )
            {
                bool printCompact = Runtime.ToBool( Runtime.GetDynamic( Symbols.PrintCompact ) );

                if ( printCompact )
                {
                    var first = Runtime.First( this );
                    var second = Runtime.Second( this );
                    var third = Runtime.Third( this );

                    if ( first == Symbols.Dot && second is string && third == null )
                    {
                        return System.String.Format( ".{0}", second );
                    }
                    else if ( first == Symbols.NullableDot && second is string && third == null )
                    {
                        return System.String.Format( "?{0}", second );
                    }
                    else if ( first == Symbols.Quote && third == null )
                    {
                        return System.String.Format( "'{0}", second );
                    }
                }
            }

            var buf = new StringWriter();

            buf.Write( "(" );
            Cons list = this;
            bool needcomma = false;

            while ( list != null )
            {
                if ( needcomma )
                {
                    buf.Write( " " );
                }

                buf.Write( Runtime.ToPrintString( list.Car, escape, radix ) );

                if ( list.cdr is IEnumerator || list.cdr is DelayedExpression )
                {
                    buf.Write( " ..." );
                    break;
                }

                needcomma = true;

                list = list.Cdr;
            }

            buf.Write( ")" );

            return buf.ToString();
        }

        internal bool Contains( object value )
        {
            foreach ( object item in this )
            {
                if ( Runtime.Equal( item, value ) )
                {
                    return true;
                }
            }
            return false;
        }

        private class ListEnumerator : IEnumerator
        {
            // This enumerator does NOT keep a reference to the start of the list.

            public bool initialized = false;
            public Cons list;
            public ListEnumerator( Cons list )
            {
                this.list = list;
            }

            public object Current
            {
                get
                {
                    return list == null ? null : list.Car;
                }
            }

            public bool MoveNext()
            {
                if ( initialized )
                {
                    if ( list == null )
                    {
                        return false;
                    }
                    else
                    {
                        list = list.Cdr;
                        return list != null;
                    }
                }
                else
                {
                    initialized = true;
                    return list != null;
                }
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }

    public partial class Runtime
    {
 
        [Lisp( "copy-tree" )]
        public static object CopyTree( object a )
        {
            if ( Consp( a ) )
            {
                var tree = ( Cons ) a;
                return Runtime.MakeCons( CopyTree( tree.Car ), ( Cons ) CopyTree( tree.Cdr ) );
            }
            else
            {
                return a;
            }
        }

        [Lisp( "cons" )]
        public static Cons MakeCons( object item, Cons list )
        {
            return new Cons( item, list );
        }

        [Lisp( "cons" )]
        public static Cons MakeCons( object item, DelayedExpression delayedExpression )
        {
            return new Cons( item, delayedExpression );
        }
        [Lisp( "list" )]
        public static Cons MakeList( params object[] items )
        {
            return AsList( items );
        }

        [Lisp( "list*" )]
        public static Cons MakeListStar( params object[] items )
        {
            if ( items.Length == 0 )
            {
                return null;
            }

            var list = AsLazyList( ( IEnumerable ) items[ items.Length - 1 ] );

            for ( int i = items.Length - 2; i >= 0; --i )
            {
                list = new Cons( items[ i ], list );
            }

            return list;
        }

        internal static Cons MakeCons( object a, IEnumerator seq )
        {
            return new Cons( a, seq );
        }

        internal static Cons MakeCons( IEnumerator seq )
        {
            if ( seq != null && seq.MoveNext() )
            {
                return new Cons( seq.Current, seq );
            }
            else
            {
                return null;
            }
        }
    }
}