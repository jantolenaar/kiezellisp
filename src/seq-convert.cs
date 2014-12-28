using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kiezel
{
    public partial class Runtime
    {
        [Lisp( "as-array" )]
        public static Array AsArray( IEnumerable seq, Symbol type )
        {
            var a = AsArray( seq );
            var t = ( Type ) GetType( type );
            Array b = Array.CreateInstance( t, a.Length );
            for ( int i = 0; i < a.Length; ++i )
            {
                b.SetValue( a[ i ], i );
            }
            return b;
        }

        [Lisp( "as-array" )]
        public static object[] AsArray( IEnumerable seq )
        {
            if ( seq == null )
            {
                return new object[ 0 ];
            }
            else if ( seq is object[] )
            {
                return ( object[] ) seq;
            }

            return new List<object>( ConvertToEnumerableObject( seq ) ).ToArray();
        }

        [Lisp( "as-lazy-list" )]
        public static Cons AsLazyList( IEnumerable seq )
        {
            if ( seq == null )
            {
                return null;
            }
            else if ( seq is Cons )
            {
                return ( Cons ) seq;
            }
            else
            {
                return MakeCons( seq.GetEnumerator() );
            }
        }

        [Lisp( "as-list" )]
        public static Cons AsList( IEnumerable seq )
        {
            if ( seq == null )
            {
                return null;
            }

            var v = seq as Cons;

            if ( v != null )
            {
                return ( Cons ) Force( v );
            }

            Cons head = null;
            Cons tail = null;

            foreach ( var item in seq )
            {
                if ( head == null )
                {
                    tail = head = new Cons( item, null );
                }
                else
                {
                    tail = tail.Cdr = new Cons( item, null );
                }
            }

            return head;
        }

        [Lisp( "as-multiple-elements" )]
        public static object[] AsMultipleElements( object seq, int size )
        {
            if ( seq is DictionaryEntry )
            {
                size = ( size < 0 ) ? 2 : size;
                var v = new object[ size ];
                var de = ( DictionaryEntry ) seq;
                if ( size > 0 )
                {
                    v[ 0 ] = de.Key;
                }
                if ( size > 1 )
                {
                    v[ 1 ] = de.Value;
                }
                return v;
            }
            else if ( seq is KeyValuePair<object, object> )
            {
                size = ( size < 0 ) ? 2 : size;
                var v = new object[ size ];
                var de = ( KeyValuePair<object, object> ) seq;
                if ( size > 0 )
                {
                    v[ 0 ] = de.Key;
                }
                if ( size > 1 )
                {
                    v[ 1 ] = de.Value;
                }
                return v;
            }
            else
            {
                if ( size < 0 )
                {
                    var v = AsArray( ToIter( seq ) );
                    return v;
                }
                else
                {
                    var v = new object[ size ];
                    var i = 0;
                    foreach ( var item in ToIter( seq ) )
                    {
                        if ( i == size )
                        {
                            break;
                        }
                        v[ i++ ] = item;
                    }
                    return v;
                }
            }
        }

        [Lisp( "as-multiple-elements" )]
        public static object[] AsMultipleElements( object seq )
        {
            return AsMultipleElements( seq, -1 );
        }

        [Lisp( "as-vector" )]
        public static Vector AsVector( IEnumerable seq )
        {
            if ( seq == null )
            {
                return new Vector();
            }

            var v = seq as Vector;

            if ( v != null )
            {
                return v;
            }

            v = new Vector();

            foreach ( var item in seq )
            {
                v.Add( item );
            }

            return v;
        }

        [Lisp( "as-vector" )]
        public static object AsVector( IEnumerable seq, Symbol type )
        {
            var t1 = ( Type ) GetType( type );
            var t2 = GenericListType.MakeGenericType( t1 );
            var c = t2.GetConstructors();
            var x = c[ 2 ].Invoke( new object[] { AsArray( seq, type ) } );
            return x;
        }

        [Lisp( "as-enumerable" )]
        public static IEnumerable<object> ConvertToEnumerableObject( IEnumerable seq )
        {
            return ToIter( seq ).Cast<object>();
        }

        [Lisp( "as-enumerable" )]
        public static object ConvertToEnumerableObject( IEnumerable seq, Symbol type )
        {
            var t = ( Type ) GetType( type );
            var m2 = CastMethod.MakeGenericMethod( t );
            var seq2 = m2.Invoke( null, new object[] { ToIter( seq ) } );
            return seq2;
        }

        [Lisp( "system:get-safe-enumerator" )]
        public static IEnumerator GetSafeEnumerator( object list )
        {
            // Some enumerables use a struct enumerator instead of class.
            // e.g. System.Data.InternalDataCollectionBase.
            var e = ToIter( ( IEnumerable ) list ).GetEnumerator();

            if ( e.GetType().IsValueType )
            {
                return new EnumeratorProxy( e );
                //return ConvertToVector( (IEnumerable) list ).GetEnumerator();
            }
            else
            {
                return e;
            }
        }

        [Lisp( "on-list-enumerator" )]
        public static IEnumerable OnListEnumerator( Cons seq, IApply step )
        {
            while ( seq != null )
            {
                yield return seq;
                seq = ( Cons ) Funcall( step, seq );
            }
        }

        [Lisp( "range-enumerator" )]
        public static IEnumerable RangeEnumerator( int start, int end, int step )
        {
            return SeqBase.Range( start, end, step );
        }

        [Lisp( "series-enumerator" )]
        public static IEnumerable SeriesEnumerator( int start, int end, int step )
        {
            return SeqBase.Range( start, end + step, step );
        }


        internal static IEnumerable ConvertToEnumerable( IEnumerable<object> seq )
        {
            if ( seq != null )
            {
                foreach ( object item in seq )
                {
                    yield return item;
                }
            }
        }

    }
}
