// Copyright (C) Jan Tolenaar. See the file LICENSE for details.


using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

using System.Threading;
using System.Threading.Tasks;

using KeyFunc = System.Func<object, object>;
using PredicateFunc = System.Func<object, bool>;
using TestFunc = System.Func<object, object, bool>;
using ActionFunc = System.Action<object>;
using ReduceFunc = System.Func<object[], object>;
using ThreadFunc = System.Func<object>;
using ReduceTransformFunc = System.Func<System.Func<object[], object>, System.Func<object[], object>>;
using JustFunc = System.Func<object>;

namespace Kiezel
{
    public abstract class Sequence
    {

    }

    public partial class Runtime
    {
        [Lisp( "car" )]
        public static object Car( Cons list )
        {
            return list == null ? null : list.Car;
        }

        [Lisp( "cdr", "rest")]
        public static Cons Cdr( Cons list )
        {
            return list == null ? null : list.Cdr;
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

        [Lisp( "set-cdr")]
        public static object SetCdr( Cons list, Cons item )
        {
            if ( list == null )
            {
                throw new LispException( "Cannot set-cdr of null" );
            }
            list.Cdr = item;
            return item;
        }

        [Lisp( "caar" )]
        public static object Caar( Cons list )
        {
            return Car( ToCons( Car( list ) ) );
        }

        [Lisp( "cdar" )]
        public static Cons Cdar( Cons list )
        {
            return Cdr( ToCons( Car( list ) ) );
        }

        [Lisp( "cadr" )]
        public static object Cadr( Cons list )
        {
            return Car( Cdr( list ) );
        }

        [Lisp( "cddr" )]
        public static Cons Cddr( Cons list )
        {
            return Cdr( Cdr( list ) );
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

        internal static object First( object seq )
        {
            return Nth( 0, seq );
        }

        internal static object Second( object seq )
        {
            return Nth( 1, seq );
        }

        internal static object Third( object seq )
        {
            return Nth( 2, seq );
        }

        [Lisp( "first" )]
        public static object First( IEnumerable seq )
        {
            return Nth( 0, seq );
        }

        [Lisp( "second" )]
        public static object Second( IEnumerable seq )
        {
            return Nth( 1, seq );
        }

        [Lisp( "third" )]
        public static object Third( IEnumerable seq )
        {
            return Nth( 2, seq );
        }

        [Lisp( "fourth" )]
        public static object Fourth( IEnumerable seq )
        {
            return Nth( 3, seq );
        }

        [Lisp( "fifth" )]
        public static object Fifth( IEnumerable seq )
        {
            return Nth( 4, seq );
        }

        [Lisp( "last" )]
        public static Cons Last( Cons seq )
        {
            return Last( 1, seq );
        }

        [Lisp( "last" )]
        public static Cons Last(  int count, Cons seq )
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

        [Lisp( "set-first" )]
        public static object SetFirst( IEnumerable seq, object item )
        {
            return SetNth( seq, 0, item );
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

        [Lisp( "set-fourth" )]
        public static object SetFourth( IEnumerable seq, object item )
        {
            return SetNth( seq, 3, item );
        }

        [Lisp( "set-fifth" )]
        public static object SetFifth( IEnumerable seq, object item )
        {
            return SetNth( seq, 4, item );
        }

        [Lisp( "length" )]
        public static int Length( IEnumerable seq )
        {
            if ( seq == null )
            {
                return 0;
            }
            else if ( seq is string )
            {
                return ( ( string ) seq ).Length;
            }
            else if ( seq is ICollection )
            {
                return ( ( ICollection ) seq ).Count;
            }
            else if ( seq is IEnumerable )
            {
                int len = 0;
                foreach ( object item in ( IEnumerable ) seq )
                {
                    ++len;
                }
                return len;
            }
            else
            {
                throw new NotImplementedException();
            }
        }


        internal static IEnumerable<Vector> UnisonEnumerator( IEnumerable[] seqs )
        {
            return new UnisonIterator( seqs );
        }

        [Lisp("conj")]
        public static object Conjoin( IEnumerable seq, object item )
        {
            if ( Listp( seq ) )
            {
                return MakeCons( item, (Cons) seq );
            }
            else if ( Vectorp( seq ) )
            {
                ( ( Vector ) seq ).Add( item );
                return seq;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        [Lisp( "zip" )]
        public static Cons Zip( params IEnumerable[] seqs )
        {
            return AsLazyList( Enum.Zip( seqs ) );
        }

        [Lisp( "every?" )]
        public static bool Every( object pred, IEnumerable seq )
        {
            return Every( GetPredicateFunc( pred, null ), seq );
        }

        internal static bool Every( PredicateFunc pred, IEnumerable seq )
        {
            foreach ( var v in ToIter(seq) )
            {
                if ( !pred( v ) )
                {
                    return false;
                }
            }
            return true;
        }

        [Lisp( "any?" )]
        public static bool Any( object pred, IEnumerable seq )
        {
            return Any( GetPredicateFunc( pred, null ), seq );
        }

        public static bool Any( PredicateFunc pred, IEnumerable seq )
        {
            foreach ( var v in ToIter( seq ) )
            {
                if ( pred( v ) )
                {
                    return true;
                }
            }
            return false;
        }

        [Lisp( "not-any?" )]
        public static bool NotAny( object pred, IEnumerable seq )
        {
            var test = GetPredicateFunc( pred, null );
            foreach ( var v in ToIter( seq ) )
            {
                if ( test( v ) )
                {
                    return false;
                }
            }
            return true;
        }

        [Lisp( "not-every?" )]
        public static bool NotEvery( object pred, IEnumerable seq )
        {
            var test = GetPredicateFunc( pred, null );
            foreach ( var v in ToIter( seq ) )
            {
                if ( !test( v ) )
                {
                    return true;
                }
            }
            return false;
        }

        [Lisp( "system:get-safe-enumerator")]
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

        [Lisp("as-enumerable")]
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

        [Lisp( "as-tuple" )]
        public static object[] AsTuple( object seq, int size )
        {

            if ( seq is DictionaryEntry )
            {
                size = ( size < 0 ) ? 2 : size;
                var v = new object[ size ];
                var de = ( DictionaryEntry ) seq;
                if ( size > 0 )
                {
                    v[0] = de.Key;
                }
                if ( size > 1 )
                {
                    v[1] = de.Value;
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

        [Lisp( "as-tuple" )]
        public static object[] AsTuple( object seq )
        {
            return AsTuple( seq, -1 );
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
                return (Cons) Force( v );
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

        [Lisp( "create-array" )]
        public static Array CreateArray( Symbol type, int size )
        {
            var t = (Type) GetType( type );
            return Array.CreateInstance( t, size );
        }

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


        [Lisp( "average" )]
        public static object Average( IEnumerable seq, params object[] args )
        {
            var kwargs = ParseKwargs( args, new string[] { "key" } );
            var key = GetKeyFunc( kwargs[ 0 ] );
            object result = null;
            int count = 0;
            foreach ( object item in ToIter( seq ) )
            {
                var val = key( item );

                if ( val != null )
                {
                    ++count;

                    if ( result == null )
                    {
                        result = val;
                    }
                    else
                    {
                        result = Add2( result, val );
                    }
                }

            }

            return result == null ? null : Div( result, count );
        }

 
        [Lisp( "adjoin" )]
        public static Cons Adjoin( object item, IEnumerable seq )
        {
            var seq2 = AsLazyList( seq );
            if ( Position( item, seq2 ) == null )
            {
                return MakeCons( item, seq2 );
            }
            else
            {
                return seq2;
            }
        }

        [Lisp( "append" )]
        public static Cons Append( params IEnumerable[] seqs )
        {
            return AsLazyList( Enum.Append( seqs ) );
        }

        [Lisp( "force-append" )]
        public static Cons ForceAppend( params IEnumerable[] seqs )
        {
            return (Cons) Force( AsLazyList( Enum.Append( seqs ) ) );
        }

        [Lisp( "count" )]
        public static int Count( object item, IEnumerable seq, params object[] args )
        {
            var kwargs = ParseKwargs( args, new string[] { "test", "key" } );
            var test = GetTestFunc( kwargs[ 0 ] );
            var key = GetKeyFunc( kwargs[ 1 ] );
            var count = 0;

            foreach ( object x in ToIter( seq ) )
            {
                if ( test( item, key( x ) ) )
                {
                    ++count;
                }
            }

            return count;
        }

        [Lisp( "count-if" )]
        public static int CountIf( object predicate, IEnumerable seq, params object[] args )
        {
            var kwargs = ParseKwargs( args, new string[] { "key" } );
            var pred = GetPredicateFunc( predicate, null );
            var key = GetKeyFunc( kwargs[ 0 ] );
            var count = 0;

            foreach ( object x in ToIter( seq ) )
            {
                if ( pred( key( x ) ) )
                {
                    ++count;
                }
            }

            return count;
        }

        [Lisp( "find-in-property-list" )]
        public static object FindProperty( object item, IEnumerable seq, params object[] args )
        {
            var kwargs = ParseKwargs( args, new string[] { "test", "key", "default" } );
            var test = GetTestFunc( kwargs[ 0 ] );
            var key = GetKeyFunc( kwargs[ 1 ] );
            var defaultValue = kwargs[ 2 ];
            var iter = ToIter( seq ).GetEnumerator();

            while ( iter.MoveNext() )
            {
                var x = iter.Current;
                iter.MoveNext();

                if ( test( item, key( x ) ) )
                {
                    return iter.Current;
                }
            }

            return defaultValue;
        }

        [Lisp( "find" )]
        public static object Find( object item, IEnumerable seq, params object[] args )
        {
            var kwargs = ParseKwargs( args, new string[] { "test", "key", "default" } );
            var test = GetTestFunc( kwargs[ 0 ] );
            var key = GetKeyFunc( kwargs[ 1 ] );
            var defaultValue = kwargs[ 2 ];
            var mv = FindItem( seq, item, test, key, defaultValue );
            return mv.Item1;
        }

        [Lisp( "position" )]
        public static object Position( object item, IEnumerable seq, params object[] args )
        {
            var kwargs = ParseKwargs( args, new string[] { "test", "key" } );
            var test = GetTestFunc( kwargs[ 0 ] );
            var key = GetKeyFunc( kwargs[ 1 ] );
            var mv = FindItem( seq, item, test, key, null );
            return mv.Item2;
        }


        internal static Tuple<object,object> FindItem( IEnumerable seq, object item, TestFunc test, KeyFunc key, object defaultValue)
        {
            int i = -1;

            foreach ( object x in ToIter( seq ) )
            {
                ++i;

                if ( test( item, key( x ) ) )
                {
                    return Tuple.Create( x, (object)i );
                }
            }

            return Tuple.Create( defaultValue, (object) null );
        }

        [Lisp( "find-if" )]
        public static object FindIf( object predicate, IEnumerable seq, params object[] args )
        {
            var pred = GetPredicateFunc( predicate, null );
            var kwargs = ParseKwargs( args, new string[] { "key", "default" } );
            var key = GetKeyFunc( kwargs[ 0 ] );
            var defaultValue = kwargs[ 1 ];
            var mv = FindItemIf( seq, pred, key, defaultValue );
            return mv.Item1;
        }

        [Lisp( "position-if" )]
        public static object PositionIf( object predicate, IEnumerable seq, params object[] args )
        {
            var pred = GetPredicateFunc( predicate, null );
            var kwargs = ParseKwargs( args, new string[] { "key" } );
            var key = GetKeyFunc( kwargs[ 0 ] );
            var mv = FindItemIf( seq, pred, key, null );
            return mv.Item2;
        }

        internal static Tuple<object,object> FindItemIf( IEnumerable seq, PredicateFunc predicate, KeyFunc key, object defaultValue )
        {
            int i = -1;
            foreach ( object x in ToIter( seq ) )
            {
                ++i;

                if ( predicate( key( x ) ) )
                {
                    return Tuple.Create( x, (object)i );
                }
            }
            return Tuple.Create( defaultValue, ( object ) null );
        }


        [Lisp( "each" )]
        public static void Each( object action, IEnumerable seq )
        {
            var act = GetActionFunc( action );

            foreach ( object arg in ToIter(seq) )
            {
                act( arg );
            }
        }

        [Lisp( "parallel-each" )]
        public static void ParallelEach( object action, IEnumerable seq )
        {
            var act = GetActionFunc( action );
            var seq2 = ConvertToEnumerableObject( seq );
            var specials = GetCurrentThread().SpecialStack;
            ActionFunc wrapper = a =>
            {
                // We want an empty threadcontext because threads may be reused
                // and already have a broken threadcontext.
                CurrentThreadContext = new ThreadContext( specials );
                act( a );
            };
            Parallel.ForEach<object>( seq2, wrapper );
        }

        [Lisp( "parallel-map" )]
        public static Cons ParallelMap( object action, IEnumerable seq )
        {
            return AsLazyList( Enum.ParallelMap( action, seq ) );
        }

        [Lisp( "except" )]
        public static Cons Except( IEnumerable seq1, IEnumerable seq2, params object[] args )
        {
            return AsLazyList( Enum.Except( seq1, seq2, args ) );
        }

        [Lisp( "filter" )]
        public static Cons Filter( object predicate, IEnumerable seq, params object[] args )
        {
            return AsLazyList( Enum.Filter( predicate, seq, args ) );
        }


        [Lisp( "find-subsequence-position" )]
        public static object FindSubsequencePosition( IEnumerable subseq, IEnumerable seq, params object[] args )
        {
            var kwargs = ParseKwargs( args, new string[] { "test", "key" } );
            var test = GetTestFunc( kwargs[ 0 ] );
            var key = GetKeyFunc( kwargs[ 1 ] );

            var v1 = AsVector( seq );
            var v2 = AsVector( subseq );

            int start = 0;
            int end = v1.Count - v2.Count + 1;

            for ( int pos = start; pos < end; ++pos )
            {
                bool eq = true;
                for ( int i = 0; i < v2.Count; ++i )
                {
                    // todo: optimize
                    if ( !test( key( v2[ i ] ), key( v1[ pos + i ] ) ) )
                    {
                        eq = false;
                        break;
                    }
                }
                if ( eq )
                {
                    return pos;
                }
            }

            return null;
        }

        [Lisp( "flatten" )]
        public static Cons Flatten( IEnumerable seq )
        {
            return AsLazyList( Enum.Flatten( seq, Int32.MaxValue ) );
        }

        [Lisp( "flatten" )]
        public static Cons Flatten( IEnumerable seq, int depth )
        {
            return AsLazyList( Enum.Flatten( seq, depth ) );
        }

        [Lisp("group-by")]
        public static Cons GroupBy( object key, IEnumerable seq  )
        {
            return AsLazyList( Enum.GroupBy( key, seq ) );
        }

        [Lisp( "intersect" )]
        public static Cons Intersect( IEnumerable seq1, IEnumerable seq2, params object[] args )
        {
            return AsLazyList( Enum.Intersect( seq1, seq2, args ) );
        }

        [Lisp( "mapcat" )]
        public static Cons MapCat( object key, IEnumerable seq )
        {
            return AsLazyList( Enum.MapCat( GetClosure( key ), seq ) );
        }

        [Lisp( "map" )]
        public static Cons Map( object key, IEnumerable seq )
        {
            return AsLazyList( Enum.Map( GetClosure( key ), seq ) );
        }

        internal static Cons Map( KeyFunc key, IEnumerable seq )
        {
            return AsLazyList( Enum.Map( key, seq ) );
        }

        [Lisp( "merge" )]
        public static Cons Merge( IEnumerable seq1, IEnumerable seq2, params object[] args )
        {
            return AsLazyList( Enum.Merge( seq1, seq2, args ) );
        }

        [Lisp( "max" )]
        public static object Max( IEnumerable seq, params object[] args )
        {
            var kwargs = ParseKwargs( args, new string[] { "key" } );
            var key = GetKeyFunc( kwargs[ 0 ] );
            ReduceFunc adder = ( x ) => NotLess( x ) ? First( x ) : Second( x );
            return Reduce( adder, seq, DefaultValue.Value, key );
        }

        [Lisp( "min" )]
        public static object Min( IEnumerable seq, params object[] args )
        {
            var kwargs = ParseKwargs( args, new string[] { "key" } );
            var key = GetKeyFunc( kwargs[ 0 ] );
            ReduceFunc adder = ( x ) => NotGreater( x ) ? First( x ) : Second( x );
            return Reduce( adder, seq, DefaultValue.Value, key );
        }

        [Lisp( "mismatch" )]
        public static object Mismatch( IEnumerable seq1, IEnumerable seq2, params object[] args )
        {
            var kwargs = ParseKwargs( args, new string[] { "test", "key" } );
            var test = GetTestFunc( kwargs[ 0 ] );
            var key = GetKeyFunc( kwargs[ 1 ] );
            return Mismatch( seq1, seq2, test, key );
        }

        internal static object Mismatch( IEnumerable seq1, IEnumerable seq2, TestFunc test, KeyFunc key )
        {
            IEnumerator iter1 = ToIter( seq1 ).GetEnumerator();
            IEnumerator iter2 = ToIter( seq2 ).GetEnumerator();
            bool atEnd1 = !iter1.MoveNext();
            bool atEnd2 = !iter2.MoveNext();
            int position = 0;

            while ( !atEnd1 && !atEnd2 )
            {
                if ( !test( key( iter1.Current ), key( iter2.Current ) ) )
                {
                    break;
                }

                atEnd1 = !iter1.MoveNext();
                atEnd2 = !iter2.MoveNext();
                ++position;
            }

            if ( atEnd1 && atEnd2 )
            {
                return null;
            }
            else
            {
                return position;
            }

        }

        [Lisp( "reduce" )]
        public static object Reduce( object adder, IEnumerable seq, params object[] args )
        {
            var func = GetReduceFunc( adder );
            var kwargs = ParseKwargs( args, new string[] { "initial-value", "key" }, DefaultValue.Value, null );
            var seed = kwargs[ 0 ];
            var key = GetKeyFunc( kwargs[ 1 ] );
            return Reduce( func, seq, seed, key );
        }

        internal static object Reduce( ReduceFunc adder, IEnumerable seq, object seed, KeyFunc key )
        {
            if ( seq is Reducible )
            {
                var red = ( Reducible ) seq;
                return red.Reduce( adder, seed, key );

            }
            else
            {
                return ReduceSeq( adder, seq, seed, key );
            }
        }

        internal static object ReduceSeq( ReduceFunc adder, IEnumerable seq, object seed, KeyFunc key )
        {
            var result = seed;
            foreach ( object x in ToIter( seq ) )
            {
                if ( result == DefaultValue.Value )
                {
                    result = key( x );
                }
                else
                {
                    result = adder( new object[] { result, key( x ) } );
                }
                if ( result is ReduceBreakValue )
                {
                    result = ( ( ReduceBreakValue ) result ).Value;
                    break;
                }
            }
            return result == DefaultValue.Value ? null : result;
        }

        [Lisp( "repeatedly" )]
        public static Cons Repeatedly( object func )
        {
            return AsLazyList( Enum.Repeatedly( -1, func ) );
        }

        [Lisp( "repeatedly" )]
        public static Cons Repeatedly( int count, object func )
        {
            return AsLazyList( Enum.Repeatedly( count, func ) );
        }

        [Lisp( "iterate" )]
        public static Cons Iterate( object func, object value )
        {
            return AsLazyList( Enum.Iterate( -1, func, value ) );
        }

        [Lisp( "cycle" )]
        public static Cons Cycle( IEnumerable seq )
        {
            return AsLazyList( Enum.Cycle( seq ) );
        }

        [Lisp( "repeat" )]
        public static Cons Repeat( int count, object value )
        {
            return AsLazyList( Enum.Repeat( count, value ) );
        }

        [Lisp( "repeat" )]
        public static Cons Repeat( object value )
        {
            return AsLazyList( Enum.Repeat( -1, value ) );
        }

        [Lisp( "reverse" )]
        public static Cons Reverse( IEnumerable seq )
        {
            return AsLazyList( Enum.Reverse( seq ) );
        }

        [Lisp( "sort" )]
        public static Cons Sort( IEnumerable seq, params object[] args )
        {
            var kwargs = ParseKwargs( args, new string[] { "test", "key" } );
            var test = GetTestFunc( kwargs[ 0 ], Less );
            var key = GetKeyFunc( kwargs[ 1 ] );
            return Sort( seq, test, key );
        }

        internal static Cons Sort( IEnumerable seq, TestFunc less, KeyFunc key )
        {
            if ( seq == null )
            {
                return null;
            }
            else
            {
                var z = Enum.MergeSort( AsVector( seq ), less, key );
                return AsLazyList( z );
            }
        }

        [Lisp( "take-nth" )]
        public static Cons TakeNth( int step, IEnumerable seq )
        {
            return AsLazyList( Enum.TakeNth( step, seq ) );
        }

        [Lisp( "subseq" )]
        public static Cons Subseq( IEnumerable seq, int start, params object[] args )
        {
            return AsLazyList( Enum.Subseq( seq, start, args ) );
        }

        [Lisp( "copy-seq" )]
        public static Cons CopySeq( IEnumerable seq )
        {
            return Subseq( seq, 0 );
        }

        [Lisp( "sum" )]
        public static object Sum( IEnumerable seq, params object[] args )
        {
            var kwargs = ParseKwargs( args, new string[] { "key" } );
            var key = GetKeyFunc( kwargs[ 0 ] );
            return Reduce( Add, seq, 0, key );
        }

        [Lisp( "distinct" )]
        public static Cons Distinct( IEnumerable seq1, params object[] args )
        {
            return Union( null, seq1, args );
        }

        internal static Cons Distinct( IEnumerable seq1, TestFunc test )
        {
            return Union( null, seq1, test, Identity );
        }

        [Lisp( "union" )]
        public static Cons Union( IEnumerable seq1, IEnumerable seq2, params object[] args )
        {
            var kwargs = ParseKwargs( args, new string[] { "test", "key" } );
            var test = GetTestFunc( kwargs[ 0 ] );
            var key = GetKeyFunc( kwargs[ 1 ] );

            return Union( seq1, seq2, test, key );
        }

        internal static Cons Union( IEnumerable seq1, IEnumerable seq2, TestFunc test, KeyFunc key )
        {
            var z = new Vector();

            foreach ( object item in ToIter( seq1 ) )
            {
                var mv = FindItem( z, key( item ), test, key, null );

                if ( mv.Item2 == null )
                {
                    z.Add( item );
                }
            }

            foreach ( object item in ToIter( seq2 ) )
            {
                var mv = FindItem( z, key( item ), test, key, null );

                if ( mv.Item2 == null )
                {
                    z.Add( item );
                }
            }

            return AsLazyList( z );

        }


        [Lisp( "unzip" )]
        public static Cons Unzip( IEnumerable seq )
        {
            int index = 0;
            var v1 = new Vector();
            var v2 = new Vector();
            foreach ( object item in ToIter( seq ) )
            {
                if ( index == 0 )
                {
                    v1.Add( item );
                }
                else
                {
                    v2.Add( item );
                }

                index = 1 - index;
            }

            return MakeList( AsLazyList( v1 ), AsLazyList( v2 ) );
        }

        [Lisp( "interleave" )]
        public static Cons Interleave( params IEnumerable[] seqs )
        {
            return AsLazyList( Enum.Interleave( seqs ) );
        }

        [Lisp( "partition-by" )]
        public static Cons PartitionBy( object mapper, IEnumerable seq )
        {
            return AsLazyList( Enum.PartitionBy( mapper, 0, seq ) );
        }

        [Lisp( "take" )]
        public static Cons Take(  int count, IEnumerable seq )
        {
            return AsLazyList( Enum.Take( count, seq ) );
        }

        [Lisp( "take-while" )]
        public static Cons TakeWhile( object pred, IEnumerable seq )
        {
            return AsLazyList( Enum.TakeWhile( pred, seq ) );
        }

        [Lisp( "skip" )]
        public static Cons Skip( int count, IEnumerable seq )
        {
            return MakeCons( Enum.Skip( count, seq ) );
        }

        [Lisp( "skip-while" )]
        public static Cons SkipWhile( object pred, IEnumerable seq )
        {
            return MakeCons( Enum.SkipWhile( pred, seq ).GetEnumerator() );
        }

        [Lisp( "split-at" )]
        public static Cons SplitAt( int count, IEnumerable seq )
        {
            Cons left;
            Cons right;
            Enum.SplitAt( count, seq, out left, out right );
            return MakeList( left, right );
        }

        [Lisp( "split-with" )]
        public static Cons SplitWith( object pred, IEnumerable seq )
        {
            Cons left;
            Cons right;
            Enum.SplitWith( pred, seq, out left, out right );
            return MakeList( left, right );
        }

        [Lisp( "series" )]
        public static Cons Series( int start, int end, int step )
        {
            return Range( start, end + step, step );
        }

        [Lisp( "series" )]
        public static Cons Series( int start, int end )
        {
            return Range( start, end + 1, 1 );
        }

        [Lisp( "series" )]
        public static Cons Series( int end )
        {
            return Range( 1, end + 1, 1 );
        }

        [Lisp( "series" )]
        public static Cons Series()
        {
            return Range( 1, Int32.MaxValue, 1 );
        }

        [Lisp( "range" )]
        public static Cons Range( int start, int end, int step )
        {
            return AsLazyList( Enum.Range( start, end, step ) );
        }

        [Lisp( "range" )]
        public static Cons Range( int start, int end )
        {
            return Range( start, end, 1 );
        }

        [Lisp( "range" )]
        public static Cons Range( int end )
        {
            return Range( 0, end, 1 );
        }

        [Lisp( "range" )]
        public static Cons Range()
        {
            return Range( 0, Int32.MaxValue, 1 );
        }

        [Lisp( "range-enumerator" )]
        public static IEnumerable RangeEnumerator( int start, int end, int step )
        {
            return Enum.Range( start, end, step );
        }

        [Lisp( "series-enumerator" )]
        public static IEnumerable SeriesEnumerator( int start, int end, int step )
        {
            return Enum.Range( start, end + step, step );
        }

        [Lisp( "in-list-enumerator" )]
        public static IEnumerable InListEnumerator( Cons seq, object step )
        {
            var func = GetKeyFunc( step );
            while ( seq != null )
            {
                yield return First( seq );
                seq = ( Cons ) func( seq );
            }
        }

        [Lisp( "on-list-enumerator" )]
        public static IEnumerable OnListEnumerator( Cons seq, object step )
        {
            var func = GetKeyFunc( step );
            while ( seq != null )
            {
                yield return seq;
                seq = ( Cons ) func( seq );
            }
        }

        [Lisp( "shuffle" )]
        public static Cons Shuffle( IEnumerable seq )
        {
            return AsLazyList( Enum.Shuffle( seq ) );
        }

        [Lisp( "partition" )]
        public static Cons Partition( int size, int step, IEnumerable pad, IEnumerable seq )
        {
            return AsLazyList( Enum.Partition( false, size, step, pad, seq ) );
        }

        [Lisp( "partition" )]
        public static Cons Partition( int size, int step, IEnumerable seq )
        {
            return AsLazyList( Enum.Partition( false, size, step, null, seq ) );
        }

        [Lisp( "partition" )]
        public static Cons Partition( int size, IEnumerable seq )
        {
            return AsLazyList( Enum.Partition( false, size, size, null, seq ) );
        }

        [Lisp( "partition-all" )]
        public static Cons PartitionAll( int size, int step, IEnumerable seq )
        {
            return AsLazyList( Enum.Partition( true, size, step, null, seq ) );
        }

        [Lisp( "partition-all" )]
        public static Cons PartitionAll( int size, IEnumerable seq )
        {
            return AsLazyList( Enum.Partition( true, size, size, null, seq ) );
        }

        class Enum
        {
            public static IEnumerable Partition( bool all, int size, int step, IEnumerable pad, IEnumerable seq )
            {
                if ( size <= 0 )
                {
                    throw new LispException( "Invalid size: {0}", size );
                }

                if ( step <= 0 )
                {
                    throw new LispException( "Invalid step: {0}", step );
                }

                // We never need more than size-1 pad elements
                var source = Runtime.Append( seq, Take( size - 1, pad ) );
                var v = new Vector();

                while ( source != null )
                {
                    while ( source != null && v.Count < size )
                    {
                        v.Add( Car( source ) );
                        source = Cdr( source );
                    }

                    if ( all || v.Count == size )
                    {
                        yield return AsList( v );
                    }

                    if ( source != null )
                    {
                        if ( step < size )
                        {
                            v.RemoveRange( 0, step );
                        }
                        else if ( size < step )
                        {
                            source = Runtime.Skip( step - size, source );
                            v.Clear();
                        }
                        else
                        {
                            v.Clear();
                        }
                    }
                }

            }

            public static IEnumerable PartitionBy( object mapper, int maxParts, IEnumerable seq )
            {

                KeyFunc map = GetKeyFunc( mapper );
                object previous = null;
                Vector all = new Vector();
                Vector v = null;
                foreach ( var item in ToIter( seq ) )
                {
                    if ( v == null )
                    {
                        v = new Vector();
                        v.Add( item );
                        previous = map( item );
                    }
                    else
                    {
                        var current = map( item );
                        if ( all.Count + 1 == maxParts || Equal( current, previous ) )
                        {
                            v.Add( item );
                        }
                        else
                        {
                            all.Add( AsList( v ) );
                            v = new Vector();
                            previous = current;
                            v.Add( item );
                        }
                    }
                }

                if ( v != null )
                {
                    all.Add( AsList( v ) );
                }
                return all;
            }


            public static IEnumerable Shuffle( IEnumerable seq )
            {
                var v = AsVector( seq );
                var r = new Random();
                var v2 = new Vector();
                for ( int i = v.Count; i > 0; --i )
                {
                    var j = r.Next( i );
                    v2.Add( v[ j ] );
                    v.RemoveAt( j );
                }
                return AsList( v2 );
            }

            public static IEnumerable Range( int start, int end, int step )
            {
                if ( step > 0 )
                {
                    for ( int i = start; i < end; i += step )
                    {
                        yield return i;
                    }
                }
                else if ( step < 0 )
                {
                    for ( int i = start; i > end; i += step )
                    {
                        yield return i;
                    }
                }
            }

            public static IEnumerable TakeWhile( object pred, IEnumerable seq )
            {
                var f = GetPredicateFunc( pred );

                foreach ( var obj in ToIter( seq ) )
                {
                    if ( !f( obj ) )
                    {
                        break;
                    }
                    yield return obj;
                }
            }

            public static IEnumerable SkipWhile( object pred, IEnumerable seq )
            {
                if ( seq == null )
                {
                    return null;
                }

                var f = GetPredicateFunc( pred );
                var iter = seq.GetEnumerator();

                while ( iter.MoveNext() )
                {
                    if ( !f( iter.Current ) )
                    {
                        return MakeCons( iter.Current, iter );
                    }
                }

                return null;
            }

            public static void SplitWith( object pred, IEnumerable seq, out Cons left, out Cons right )
            {
                left = null;
                right = null;

                if ( seq != null )
                {
                    var iter = seq.GetEnumerator();
                    var f = GetPredicateFunc( pred, null );
                    var v = new Vector();

                    while ( iter.MoveNext() )
                    {
                        if ( f( iter.Current ) )
                        {
                            v.Add( iter.Current );
                        }
                        else
                        {
                            left = AsList( v );
                            right = MakeCons( iter.Current, iter );
                            return;
                        }
                    }

                    left = AsList( v );
                }
            }

            public static IEnumerable TakeUntil( object pred, IEnumerable seq )
            {
                var f = GetPredicateFunc( pred, null );

                foreach ( var obj in ToIter( seq ) )
                {
                    if ( f( obj ) )
                    {
                        break;
                    }
                    yield return obj;
                }
            }

            public static IEnumerable Take( int count, IEnumerable seq )
            {
                if ( count > 0 )
                {
                    foreach ( var obj in ToIter( seq ) )
                    {
                        if ( --count < 0 )
                        {
                            break;
                        }
                        yield return obj;
                    }
                }
            }

            public static IEnumerator Skip( int count, IEnumerable seq )
            {
                if ( seq == null )
                {
                    return null;
                }

                var iter = seq.GetEnumerator();

                if ( count > 0 )
                {
                    while ( --count >= 0 )
                    {
                        if ( !iter.MoveNext() )
                        {
                            return null;
                        }
                    }
                }

                return iter;
            }

            public static void SplitAt( int count, IEnumerable seq, out Cons left, out Cons right )
            {
                if ( seq == null )
                {
                    left = null;
                    right = null;
                    return;
                }

                var vleft = new Vector();
                var iter = seq.GetEnumerator();

                if ( count > 0 )
                {
                    while ( --count >= 0 )
                    {
                        if ( !iter.MoveNext() )
                        {
                            break;
                        }

                        vleft.Add( iter.Current );
                    }
                }

                left = AsList( vleft );
                right = MakeCons( iter );
            }

            public static IEnumerable Zip( IEnumerable[] seqs )
            {
                if ( seqs == null || seqs.Length == 0 )
                {
                    return null;
                }

                return ConvertToEnumerable( UnisonEnumerator( seqs ) );
            }

            public static IEnumerable Append( IEnumerable[] seqs )
            {
                foreach ( IEnumerable seq in seqs )
                {
                    foreach ( object item in ToIter( seq ) )
                    {
                        yield return item;
                    }
                }
            }

            public static IEnumerable ParallelMap( object action, IEnumerable seq )
            {
                if ( seq == null )
                {
                    return null;
                }
                var act = GetKeyFunc( action, null );
                var seq2 = ConvertToEnumerableObject( seq );
                var seq3 = ParallelEnumerable.AsParallel( seq2 ).AsOrdered();
                var specials = GetCurrentThread().SpecialStack;

                Func<object, object> wrapper = a =>
                {
                    // We want an empty threadcontext because threads may be reused
                    // and already have a broken threadcontext.
                    CurrentThreadContext = new ThreadContext( specials );
                    return act( a );
                };

                return seq3.Select( wrapper );
            }

            public static IEnumerable Except( IEnumerable seq1, IEnumerable seq2, object[] args )
            {
                var kwargs = ParseKwargs( args, new string[] { "test", "key" } );
                var test = GetTestFunc( kwargs[ 0 ] );
                var key = GetKeyFunc( kwargs[ 1 ] );

                return Except( seq1, seq2, test, key );
            }

            internal static IEnumerable Except( IEnumerable seq1, IEnumerable seq2, TestFunc test, KeyFunc key )
            {
                var v2 = AsVector( seq2 );
                foreach ( object item in ToIter( seq1 ) )
                {
                    var mv = FindItem( v2, key( item ), test, key, null );

                    if ( mv.Item2 == null )
                    {
                        yield return item;
                    }
                }
            }

            public static IEnumerable Filter( object predicate, IEnumerable seq, object[] args )
            {
                var pred = GetPredicateFunc( predicate, null );
                var kwargs = ParseKwargs( args, new string[] { "key" } );
                var key = GetKeyFunc( kwargs[ 0 ] );

                foreach ( object item in ToIter( seq ) )
                {
                    if ( pred( key( item ) ) )
                    {
                        yield return item;
                    }
                }
            }

            public static IEnumerable Flatten( IEnumerable seq, int depth )
            {
                foreach ( var item in ToIter( seq ) )
                {
                    if ( depth <= 0 )
                    {
                        yield return item;
                    }
                    else if ( Sequencep( item ) && !Stringp( item ) )
                    {
                        foreach ( var item2 in Flatten( ( IEnumerable ) item, depth - 1 ) )
                        {
                            yield return item2;
                        }
                    }
                    else
                    {
                        yield return item;
                    }
                }
            }

            public static IEnumerable GroupBy( object key, IEnumerable seq )
            {
                var keyf = GetKeyFunc( key );
                var result = ConvertToEnumerableObject( seq ).GroupBy( keyf );
                foreach ( var grp in result )
                {
                    var obj = MakeList( grp.Key, AsLazyList( grp ) );
                    yield return obj;
                }
            }

            public static IEnumerable Intersect( IEnumerable seq1, IEnumerable seq2, object[] args )
            {
                var kwargs = ParseKwargs( args, new string[] { "test", "key" } );
                var test = GetTestFunc( kwargs[ 0 ] );
                var key = GetKeyFunc( kwargs[ 1 ] );

                var v2 = AsVector( seq2 );

                foreach ( object item in ToIter( seq1 ) )
                {
                    var mv = FindItem( v2, key( item ), test, key, null );
                    if ( mv.Item2 != null )
                    {
                        yield return item;
                    }
                }
            }

            public static IEnumerable Map( KeyFunc key, IEnumerable seq )
            {
                foreach ( var item in ToIter( seq ) )
                {
                    yield return key( item );
                }
            }

            public static IEnumerable Map( IApply key, IEnumerable seq )
            {
                var args = new object[ 1 ];

                foreach ( var item in ToIter( seq ) )
                {
                    args[ 0 ] = item;
                    yield return key.Apply( args );
                }
            }

            public static IEnumerable MapCat( IApply key, IEnumerable seq )
            {
                var args = new object[ 1 ];

                foreach ( var item in ToIter( seq ) )
                {
                    args[ 0 ] = item;
                    var seq2 = (IEnumerable) key.Apply( args );
                    foreach ( var item2 in ToIter( seq2 ) )
                    {
                        yield return item2;
                    }
                }
            }

            public static IEnumerable Merge( IEnumerable seq1, IEnumerable seq2, object[] args )
            {
                var kwargs = ParseKwargs( args, new string[] { "test", "key" } );
                var test = GetTestFunc( kwargs[ 0 ], Less );
                var key = GetKeyFunc( kwargs[ 1 ] );
                return Merge( seq1, seq2, test, key );
            }

            internal static IEnumerable Merge( IEnumerable seq1, IEnumerable seq2, TestFunc less_fun, KeyFunc key )
            {
                less_fun = less_fun ?? Less;
                IEnumerator iter1 = seq1.GetEnumerator();
                IEnumerator iter2 = seq2.GetEnumerator();
                bool atEnd1 = !iter1.MoveNext();
                bool atEnd2 = !iter2.MoveNext();

                while ( !atEnd1 && !atEnd2 )
                {
                    //
                    // put in the first element when it is less than or equal to the second element: stable merge
                    //

                    if ( !less_fun( key( iter2.Current ), key( iter1.Current ) ) )
                    {
                        yield return iter1.Current;
                        atEnd1 = !iter1.MoveNext();
                    }
                    else
                    {
                        yield return iter2.Current;
                        atEnd2 = !iter2.MoveNext();
                    }
                }

                while ( !atEnd1 )
                {
                    yield return iter1.Current;
                    atEnd1 = !iter1.MoveNext();
                }

                while ( !atEnd2 )
                {
                    yield return iter2.Current;
                    atEnd2 = !iter2.MoveNext();
                }

            }

            public static IEnumerable Cycle( IEnumerable seq )
            {
                while ( seq != null )
                {
                    foreach ( var item in seq )
                    {
                        yield return item;
                    }
                }
            }

            public static IEnumerable Repeat( int count, object value )
            {
                if ( count < 0 )
                {
                    while ( true )
                    {
                        yield return value;
                    }
                }
                else
                {
                    while ( count-- > 0 )
                    {
                        yield return value;
                    }
                }
            }

            public static IEnumerable Repeatedly( int count, object func )
            {
                JustFunc f = GetJustFunc( func );
                if ( count < 0 )
                {
                    while ( true )
                    {
                        yield return f();
                    }
                }
                else
                {
                    while ( count-- > 0 )
                    {
                        yield return f();
                    }
                }
            }

            public static IEnumerable Iterate( int count, object func, object val )
            {
                KeyFunc f = GetKeyFunc( func );
                if ( count < 0 )
                {
                    while ( true )
                    {
                        yield return val;
                        val = f( val );
                    }
                }
                else
                {
                    while ( count-- > 0 )
                    {
                        yield return val;
                        val = f( val );
                    }
                }
            }

            public static IEnumerable Reverse( IEnumerable seq )
            {
                var z = AsVector( seq );
                z.Reverse();
                return z;
            }

            internal static IEnumerable MergeSort( Vector seq, TestFunc less, KeyFunc key )
            {
                if ( seq == null || seq.Count <= 1 )
                {
                    return seq;
                }

                int middle = seq.Count / 2;
                Vector left = seq.GetRange( 0, middle );
                Vector right = seq.GetRange( middle, seq.Count - middle );
                var left2 = MergeSort( left, less, key );
                var right2 = MergeSort( right, less, key );
                return Merge( left2, right2, less, key );
            }

            public static IEnumerable TakeNth( int step, IEnumerable seq )
            {
                int countdown = 1;

                foreach ( object item in ToIter( seq ) )
                {
                    if ( --countdown <= 0 )
                    {
                        yield return item;
                        countdown = step;
                    }
                }

            }

            public static IEnumerable Subseq( IEnumerable seq, int start, object[] args )
            {
                object[] kwargs = ParseKwargs( args, new string[] { "end", "count", "default" }, null, null, DefaultValue.Value );
                int end = ToInt( kwargs[ 0 ] ?? Int32.MaxValue );
                int count = ToInt( kwargs[ 1 ] ?? -1 );
                var defaultValue = kwargs[ 2 ];
                int i = -1;
                int yielded = 0;

                foreach ( var item in ToIter( seq ) )
                {
                    ++i;

                    if ( i < start )
                    {
                        continue;
                    }
                    else if ( end <= i || ( count >= 0 && count <= yielded ) )
                    {
                        break;
                    }

                    ++yielded;
                    yield return item;
                }

                if ( defaultValue != DefaultValue.Value )
                {
                    while ( yielded < count )
                    {
                        ++yielded;
                        yield return defaultValue;
                    }
                }
            }

            public static IEnumerable Interleave( IEnumerable[] seqs )
            {
                foreach ( var tuple in UnisonEnumerator( seqs ) )
                {
                    foreach ( var obj in tuple )
                    {
                        yield return obj;
                    }
                }
            }

        }


    }

   


    internal class UnisonIterator : IEnumerable<Vector>
    {
        IEnumerable[] sequences;

        public UnisonIterator( IEnumerable[] sequences )
        {
            this.sequences = sequences ?? new IEnumerable[ 0 ];
        }

        IEnumerator<Vector> IEnumerable<Vector>.GetEnumerator()
        {
            List<IEnumerator> iterators = new List<IEnumerator>();

            foreach ( object seq in sequences )
            {
                if ( seq is IEnumerable )
                {
                    iterators.Add( ( ( IEnumerable ) seq ).GetEnumerator() );
                }
                else
                {
                    iterators.Add( null );
                }
            }

            while ( true )
            {
                var data = new Vector();
                int count = 0;

                for ( int i = 0; i < iterators.Count; ++i )
                {
                    if ( iterators[ i ] == null )
                    {
                        break;
                    }
                    else if ( iterators[ i ].MoveNext() )
                    {
                        ++count;
                        data.Add( iterators[ i ].Current );
                    }
                    else
                    {
                        break;
                    }
                }

                if ( count != 0 && count == iterators.Count )
                {
                    // full set
                    yield return data;
                }
                else
                {
                    break;
                }

            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    internal class EnumeratorProxy : IEnumerator
    {
        internal IEnumerator Iter;

        internal EnumeratorProxy( IEnumerator iter )
        {
            Iter = iter;
        }

        public object Current
        {
            get
            {
                return Iter.Current;
            }
        }

        public bool MoveNext()
        {
            return Iter.MoveNext();
        }

        public void Reset()
        {
            Iter.Reset();
        }
    }

  
}
