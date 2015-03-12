// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Linq;
using CompareFunc = System.Func<object, object, int>;
using JustFunc = System.Func<object>;
using KeyFunc = System.Func<object, object>;
using PredicateFunc = System.Func<object, bool>;
using TestFunc = System.Func<object, object, bool>;

namespace Kiezel
{
    public partial class Runtime
    {
        internal class SeqBase
        {
            internal static bool Any( IApply predicate, IEnumerable seq, IApply key )
            {
                foreach ( var v in ToIter( seq ) )
                {
                    if ( FuncallBool( predicate, Funcall( key, v ) ) )
                    {
                        return true;
                    }
                }
                return false;
            }

            internal static bool Any( PredicateFunc predicate, IEnumerable seq )
            {
                foreach ( var v in ToIter( seq ) )
                {
                    if ( predicate( v ) )
                    {
                        return true;
                    }
                }
                return false;
            }

            internal static IEnumerable Append( IEnumerable[] seqs )
            {
                foreach ( IEnumerable seq in seqs )
                {
                    foreach ( object item in ToIter( seq ) )
                    {
                        yield return item;
                    }
                }
            }

            internal static object Average( IEnumerable seq, IApply key )
            {
                object result = null;
                int count = 0;
                foreach ( object item in ToIter( seq ) )
                {
                    var val = Funcall( key, item );

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

            internal static int Count( object item, IEnumerable seq, IApply test, IApply key )
            {
                var count = 0;

                foreach ( object x in ToIter( seq ) )
                {
                    if ( FuncallBool( test, item, Funcall( key, x ) ) )
                    {
                        ++count;
                    }
                }

                return count;
            }

            internal static IEnumerable Cycle( IEnumerable seq )
            {
                while ( seq != null )
                {
                    foreach ( var item in seq )
                    {
                        yield return item;
                    }
                }
            }

            internal static IEnumerable Distinct( IEnumerable seq1, IApply test )
            {
                return Union( null, seq1, test );
            }

            internal static IEnumerator Drop( int count, IEnumerable seq )
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

            internal static IEnumerable DropWhile( IApply pred, IEnumerable seq )
            {
                if ( seq == null )
                {
                    return null;
                }

                var iter = seq.GetEnumerator();

                while ( iter.MoveNext() )
                {
                    if ( !FuncallBool( pred, iter.Current ) )
                    {
                        return MakeCons( iter.Current, iter );
                    }
                }

                return null;
            }

            internal static bool Every( IApply predicate, IEnumerable seq, IApply key )
            {
                foreach ( var v in ToIter( seq ) )
                {
                    if ( !FuncallBool( predicate, Funcall( key, v ) ) )
                    {
                        return false;
                    }
                }
                return true;
            }

            internal static bool Every( PredicateFunc predicate, IEnumerable seq )
            {
                foreach ( var v in ToIter( seq ) )
                {
                    if ( !predicate( v ) )
                    {
                        return false;
                    }
                }
                return true;
            }

            internal static IEnumerable Except( IEnumerable seq1, IEnumerable seq2, IApply test, IApply key )
            {
                var v2 = AsVector( seq2 );
                foreach ( object item in ToIter( seq1 ) )
                {
                    var mv = FindItem( v2, Funcall( key, item ), test, key, null );

                    if ( mv.Item2 == null )
                    {
                        yield return item;
                    }
                }
            }

            internal static IEnumerable Filter( IApply pred, IEnumerable seq, IApply key )
            {
                foreach ( object item in ToIter( seq ) )
                {
                    if ( FuncallBool( pred, Funcall( key, item ) ) )
                    {
                        yield return item;
                    }
                }
            }

            internal static Tuple<object, object> FindItem( IEnumerable seq, object item, IApply test, IApply key, object defaultValue )
            {
                int i = -1;

                foreach ( object x in ToIter( seq ) )
                {
                    ++i;

                    if ( FuncallBool( test, item, Funcall( key, x ) ) )
                    {
                        return Tuple.Create( x, ( object ) i );
                    }
                }

                return Tuple.Create( defaultValue, ( object ) null );
            }

            internal static Tuple<object, object> FindItem( IEnumerable seq, object item, TestFunc test, KeyFunc key, object defaultValue )
            {
                int i = -1;

                foreach ( object x in ToIter( seq ) )
                {
                    ++i;

                    if ( test( item, key( x ) ) )
                    {
                        return Tuple.Create( x, ( object ) i );
                    }
                }

                return Tuple.Create( defaultValue, ( object ) null );
            }

            internal static Tuple<object, object> FindItemIf( IEnumerable seq, IApply predicate, IApply key, object defaultValue )
            {
                int i = -1;
                foreach ( object x in ToIter( seq ) )
                {
                    ++i;

                    if ( FuncallBool( predicate, Funcall( key, x ) ) )
                    {
                        return Tuple.Create( x, ( object ) i );
                    }
                }
                return Tuple.Create( defaultValue, ( object ) null );
            }

            internal static object FindProperty( object item, IEnumerable seq, IApply test, IApply key, object defaultValue )
            {
                var iter = ToIter( seq ).GetEnumerator();

                while ( iter.MoveNext() )
                {
                    var x = iter.Current;
                    iter.MoveNext();

                    if ( FuncallBool( test, item, Funcall( key, x ) ) )
                    {
                        return iter.Current;
                    }
                }

                return defaultValue;
            }

            internal static object FindSubsequencePosition( IEnumerable subseq, IEnumerable seq, IApply test, IApply key )
            {
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
                        var k1 = Funcall( key, v1[ pos + i ] );
                        var k2 = Funcall( key, v2[ i ] );
                        if ( !FuncallBool( test, k2, k1 ) )
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
            internal static IEnumerable Flatten( IEnumerable seq, int depth )
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

            internal static IEnumerable GroupBy( IApply key, IEnumerable seq )
            {
                Func<object, object> keyf = x => Funcall( key, x );
                var result = ConvertToEnumerableObject( seq ).GroupBy( keyf );
                foreach ( var grp in result )
                {
                    var obj = MakeList( grp.Key, AsLazyList( grp ) );
                    yield return obj;
                }
            }

            internal static IEnumerable Interleave( IEnumerable[] seqs )
            {
                foreach ( var tuple in new UnisonEnumerator( seqs ) )
                {
                    foreach ( var obj in ToIter( tuple ) )
                    {
                        yield return obj;
                    }
                }
            }

            internal static IEnumerable Intersect( IEnumerable seq1, IEnumerable seq2, IApply test, IApply key )
            {
                var v2 = AsVector( seq2 );

                foreach ( object item in ToIter( seq1 ) )
                {
                    var mv = FindItem( v2, Funcall(key, item ), test, key, null );
                    if ( mv.Item2 != null )
                    {
                        yield return item;
                    }
                }
            }

            internal static IEnumerable Iterate( int count, IApply func, object val )
            {
                if ( count < 0 )
                {
                    while ( true )
                    {
                        yield return val;
                        val = Funcall( func, val );
                    }
                }
                else
                {
                    while ( count-- > 0 )
                    {
                        yield return val;
                        val = Funcall( func, val );
                    }
                }
            }

            internal static IEnumerable Keep( IApply func, IEnumerable seq, IApply key )
            {
                foreach ( object item in ToIter( seq ) )
                {
                    var value = Funcall( func, Funcall( key, item ) );
                    if ( value != null )
                    {
                        yield return item;
                    }
                }
            }

            internal static IEnumerable KeepIndexed( IApply func, IEnumerable seq, IApply key )
            {
                var index = -1;

                foreach ( object item in ToIter( seq ) )
                {
                    ++index;
                    var value = Funcall( func, index, Funcall( key, item ) );
                    if ( value != null )
                    {
                        yield return item;
                    }
                }
            }

            internal static IEnumerable Map( KeyFunc func, IEnumerable seq )
            {
                foreach ( var item in ToIter( seq ) )
                {
                    yield return func( item );
                }
            }

            internal static IEnumerable Map( IApply func, IEnumerable[] seqs )
            {
                switch ( seqs.Length )
                {
                    case 1:
                    {
                        var args = new object[ 1 ];
                        foreach ( var item in ToIter( seqs[ 0 ] ) )
                        {
                            args[ 0 ] = item;
                            yield return func.Apply( args );
                        }
                        break;
                    }
                    case 2:
                    {
                        var args = new object[ 2 ];
                        var iter1 = ToIter( seqs[ 0 ] ).GetEnumerator();
                        var iter2 = ToIter( seqs[ 1 ] ).GetEnumerator();
                        while ( iter1.MoveNext() && iter2.MoveNext() )
                        {
                            args[ 0 ] = iter1.Current;
                            args[ 1 ] = iter2.Current;
                            yield return func.Apply( args );
                        }
                        break;
                    }
                    case 3:
                    {
                        var args = new object[ 3 ];
                        var iter1 = ToIter( seqs[ 0 ] ).GetEnumerator();
                        var iter2 = ToIter( seqs[ 1 ] ).GetEnumerator();
                        var iter3 = ToIter( seqs[ 2 ] ).GetEnumerator();
                        while ( iter1.MoveNext() && iter2.MoveNext() && iter3.MoveNext() )
                        {
                            args[ 0 ] = iter1.Current;
                            args[ 1 ] = iter2.Current;
                            args[ 2 ] = iter3.Current;
                            yield return func.Apply( args );
                        }
                        break;
                    }
                    default:
                    {
                        var iter = new UnisonEnumerator( seqs );
                        foreach ( Vector item in iter )
                        {
                            yield return func.Apply( AsArray( item ) );
                        }
                        break;
                    }
                }
            }

            internal static IEnumerable Merge( IEnumerable seq1, IEnumerable seq2, IApply test, IApply key )
            {
                IEnumerator iter1 = seq1.GetEnumerator();
                IEnumerator iter2 = seq2.GetEnumerator();
                bool atEnd1 = !iter1.MoveNext();
                bool atEnd2 = !iter2.MoveNext();

                while ( !atEnd1 && !atEnd2 )
                {
                    //
                    // put in the first element when it is less than or equal to the second element: stable merge
                    //

                    if ( FuncallInt( test, Funcall( key, iter1.Current ), Funcall( key, iter2.Current ) ) <= 0 )
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

            internal static object Mismatch( IEnumerable seq1, IEnumerable seq2, IApply test, IApply key )
            {
                IEnumerator iter1 = ToIter( seq1 ).GetEnumerator();
                IEnumerator iter2 = ToIter( seq2 ).GetEnumerator();
                bool atEnd1 = !iter1.MoveNext();
                bool atEnd2 = !iter2.MoveNext();
                int position = 0;

                while ( !atEnd1 && !atEnd2 )
                {
                    if ( !FuncallBool( test, Funcall( key, iter1.Current ), Funcall( key, iter2.Current ) ) )
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


            internal static IEnumerable Sort( IEnumerable seq, IApply test, IApply key )
            {
                return MergeSort( AsVector( seq ), test, key );
            }

            internal static IEnumerable MergeSort( Vector seq, IApply test, IApply key )
            {
                if ( seq == null || seq.Count <= 1 )
                {
                    return seq;
                }

                int middle = seq.Count / 2;
                Vector left = seq.GetRange( 0, middle );
                Vector right = seq.GetRange( middle, seq.Count - middle );
                var left2 = MergeSort( left, test, key );
                var right2 = MergeSort( right, test, key );
                return Merge( left2, right2, test, key );
            }

            internal static IEnumerable ParallelMap( IApply action, IEnumerable seq )
            {
                if ( seq == null )
                {
                    return null;
                }
                var seq2 = ConvertToEnumerableObject( seq );
                var seq3 = ParallelEnumerable.AsParallel( seq2 ).AsOrdered();
                var specials = GetCurrentThread().SpecialStack;

                Func<object, object> wrapper = a =>
                {
                    // We want an empty threadcontext because threads may be reused
                    // and already have a broken threadcontext.
                    CurrentThreadContext = new ThreadContext( specials );
                    return Funcall( action, a );
                };

                return seq3.Select( wrapper );
            }

            internal static IEnumerable Partition( bool all, int size, int step, IEnumerable pad, IEnumerable seq )
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
                            source = Runtime.Drop( step - size, source );
                            v.Clear();
                        }
                        else
                        {
                            v.Clear();
                        }
                    }
                }
            }

            internal static IEnumerable PartitionBy( IApply func, int maxParts, IEnumerable seq )
            {
                object previous = null;
                Vector all = new Vector();
                Vector v = null;
                foreach ( var item in ToIter( seq ) )
                {
                    if ( v == null )
                    {
                        v = new Vector();
                        v.Add( item );
                        previous = Funcall( func, item );
                    }
                    else
                    {
                        var current = Funcall( func, item );
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

            internal static IEnumerable Range( int start, int end, int step )
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

            internal static object ReduceSeq( IApply reducer, IEnumerable seq, object seed, IApply key )
            {
                var result = seed;
                foreach ( object x in ToIter( seq ) )
                {
                    if ( result == DefaultValue.Value )
                    {
                        result = Funcall( key, x );
                    }
                    else
                    {
                        result = Runtime.Funcall( reducer, result, Funcall( key, x ) );
                    }
                }
                return result == DefaultValue.Value ? null : result;
            }

            internal static IEnumerable Reductions( IApply reducer, IEnumerable seq, object seed, IApply key )
            {
                var result = seed;
                foreach ( object x in ToIter( seq ) )
                {
                    if ( result == DefaultValue.Value )
                    {
                        result = Funcall( key, x );
                    }
                    else
                    {
                        result = Funcall( reducer, result, Funcall( key, x ) );
                    }

                    yield return result;
                }
            }

            internal static IEnumerable Remove( IApply predicate, IEnumerable seq )
            {
                foreach ( object item in ToIter( seq ) )
                {
                    if ( !FuncallBool( predicate, item ) )
                    {
                        yield return item;
                    }
                }
            }

            internal static IEnumerable Repeat( int count, object value )
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

            internal static IEnumerable Repeatedly( int count, IApply func )
            {
                if ( count < 0 )
                {
                    while ( true )
                    {
                        yield return Funcall( func );
                    }
                }
                else
                {
                    while ( count-- > 0 )
                    {
                        yield return Funcall( func );
                    }
                }
            }

            internal static IEnumerable Reverse( IEnumerable seq )
            {
                var z = AsVector( seq );
                z.Reverse();
                return z;
            }

            internal static IEnumerable Shuffle( IEnumerable seq )
            {
                var v = AsVector( seq );
                var v2 = new Vector();
                for ( int i = v.Count; i > 0; --i )
                {
                    var j = Random( i );
                    v2.Add( v[ j ] );
                    v.RemoveAt( j );
                }
                return AsList( v2 );
            }

            internal static void SplitAt( int count, IEnumerable seq, out Cons left, out Cons right )
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

            internal static void SplitWith( IApply predicate, IEnumerable seq, out Cons left, out Cons right )
            {
                left = null;
                right = null;

                if ( seq != null )
                {
                    var iter = seq.GetEnumerator();
                    var v = new Vector();

                    while ( iter.MoveNext() )
                    {
                        if ( FuncallBool( predicate, iter.Current ) )
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

            internal static IEnumerable Subseq( IEnumerable seq, int start, object[] args )
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

            internal static IEnumerable Take( int count, IEnumerable seq )
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

            internal static IEnumerable TakeNth( int step, IEnumerable seq )
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

            internal static IEnumerable TakeUntil( IApply predicate, IEnumerable seq )
            {
                foreach ( var obj in ToIter( seq ) )
                {
                    if ( FuncallBool( predicate, obj ) )
                    {
                        break;
                    }
                    yield return obj;
                }
            }

            internal static IEnumerable TakeWhile( IApply predicate, IEnumerable seq )
            {
                foreach ( var obj in ToIter( seq ) )
                {
                    if ( !FuncallBool( predicate, obj ) )
                    {
                        break;
                    }
                    yield return obj;
                }
            }

            internal static IEnumerable Union( IEnumerable seq1, IEnumerable seq2, IApply test )
            {
                var z = new Vector();

                foreach ( object item in ToIter( seq1 ) )
                {
                    var mv = FindItem( z, item, test, null, null );

                    if ( mv.Item2 == null )
                    {
                        z.Add( item );
                    }
                }

                foreach ( object item in ToIter( seq2 ) )
                {
                    var mv = FindItem( z, item, test, null, null );

                    if ( mv.Item2 == null )
                    {
                        z.Add( item );
                    }
                }

                return z;
            }

            internal static IEnumerable Zip( IEnumerable[] seqs )
            {
                if ( seqs == null || seqs.Length == 0 )
                {
                    return null;
                }

                return ConvertToEnumerable( new UnisonEnumerator( seqs ) );
            }
        }
    }
}