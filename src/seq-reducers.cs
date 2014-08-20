// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KeyFunc = System.Func<object, object>;
using PredicateFunc = System.Func<object, bool>;
using TestFunc = System.Func<object, object, bool>;
using ActionFunc = System.Action<object>;
using ReduceFunc = System.Func<object[], object>;
using ThreadFunc = System.Func<object>;
using ReduceTransformFunc = System.Func<System.Func<object[], object>, System.Func<object[], object>>;

namespace Kiezel
{
    public partial class Runtime
    {
        internal static ReduceTransformFunc Mapping( object f )
        {
            var func = GetKeyFunc( f );
            ReduceTransformFunc transform = xf =>
            {
                ReduceFunc rf = args => xf( new object[] { args[ 0 ], func( args[ 1 ] ) } );
                return rf;
            };
            return transform;
        }

        internal static ReduceTransformFunc MapCatting( object f )
        {
            var func = GetKeyFunc( f );
            ReduceTransformFunc transform = xf =>
            {
                ReduceFunc rf = args => Reduce( xf, (IEnumerable) func( args[ 1 ] ), args[ 0 ], Identity );
                return rf;
            };
            return transform;
        }

        internal static ReduceTransformFunc Filtering( object f )
        {
            var func = GetPredicateFunc( f );
            ReduceTransformFunc transform = xf =>
            {
                ReduceFunc rf = args => func( args[ 1 ] ) ? xf( args ) : args[ 0 ];
                return rf;
            };
            return transform;
        }

        internal static ReduceTransformFunc Taking( int count )
        {
            ReduceTransformFunc transform = xf =>
            {
                ReduceFunc rf = args => --count >= 0 ? xf( args ) : new ReduceBreakValue( args[ 0 ] );
                return rf;
            };
            return transform;
        }

        internal static ReduceTransformFunc TakingWhile( object f )
        {
            var pred = GetPredicateFunc( f );
            ReduceTransformFunc transform = xf =>
            {
                ReduceFunc rf = args => pred( args[ 1 ] ) ? xf( args ) : new ReduceBreakValue( args[ 0 ] );
                return rf;
            };
            return transform;
        }


        [Lisp( "reducer" )]
        public static object Reducer( ReduceTransformFunc func, IEnumerable seq )
        {
            return new Reducible( func, seq );
        }

        [Lisp( "r/map" )]
        public static object RMap( object func, IEnumerable seq )
        {
            return new Reducible( Mapping( func ), seq );
        }

        [Lisp( "r/mapcat" )]
        public static object RMapCat( object func, IEnumerable seq )
        {
            return new Reducible( MapCatting( func ), seq );
        }

        [Lisp( "r/filter" )]
        public static object RFilter( object pred, IEnumerable seq )
        {
            return new Reducible( Filtering( pred ), seq );
        }

        [Lisp( "r/take" )]
        public static object RTake( int count, IEnumerable seq )
        {
            return new Reducible( Taking( count ), seq );
        }

        [Lisp( "r/take-while" )]
        public static object RTakeWhile( object pred, IEnumerable seq )
        {
            return new Reducible( TakingWhile( pred ), seq );
        }

    }

    class ReduceBreakValue
    {
        internal object Value;
        internal ReduceBreakValue( object value )
        {
            Value = value;
        }
    }

    public class Reducible : IEnumerable
    {
        internal IEnumerable Seq;
        internal ReduceTransformFunc Transform;

        internal Reducible( ReduceTransformFunc transform, IEnumerable seq )
        {
            if ( seq is Reducible )
            {
                var reducer = ( Reducible ) seq;
                Seq = reducer.Seq;
                Transform = f => reducer.Transform( transform( f ) );
            }
            else
            {
                Seq = seq;
                Transform = transform;
            }
        }

        internal object Reduce( ReduceFunc adder, object seed, KeyFunc key )
        {
            var newAdder = Transform( adder );
            if ( seed == DefaultValue.Value )
            {
                seed = adder( new object[ 0 ] );
            }
            var result = seed;
            foreach ( object x in Runtime.ToIter( Seq ) )
            {
                result = newAdder( new object[] { result, key( x ) } );
                if ( result is ReduceBreakValue )
                {
                    result = ( ( ReduceBreakValue ) result ).Value;
                    break;
                }
            }
            return result;
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            var v = new Vector();
            ReduceFunc adder = x =>
            {
                v.Add( x[ 1 ] );
                return v;
            };
            Runtime.Reduce( adder, this, v, Runtime.Identity );
            return v.GetEnumerator();

            //ReduceFunc adder = Transform( x => x[ 1 ] );
            //foreach ( object x in Runtime.ToIter( Seq ) )
            //{
            //    var result = adder( new object[] { DefaultValue.Value, x } );

            //    //
            //    // Tests for break, e.g. by r/take
            //    //
            //    if ( result is ReduceBreakValue )
            //    {
            //        Console.WriteLine( "break value: {0}", ( ( ReduceBreakValue ) result ).Value );
            //        break;
            //    }
            //    else
            //    {
            //        Console.WriteLine( "value: {0}", result );
            //    }

            //    //
            //    // Tests for r/filter
            //    //
            //    if ( result != DefaultValue.Value )
            //    {
            //        yield return result;
            //    }
            //}
        }
    }

}