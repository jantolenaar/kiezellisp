// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kiezel
{
    public partial class Runtime
    {

        internal static object Identity( object a )
        {
            return a;
        }

        [Lisp( "identity" )]
        public static object Identity( params object[] a )
        {
            if ( a.Length == 1 )
            {
                return a[ 0 ];
            }
            else
            {
                return AsList( a );
            }
        }

        [Lisp( "apply" )]
        public static IApply Apply( object func )
        {
            var func2 = GetClosure( func );
            return new ApplyWrapper( func2 );
            //return new Func<IEnumerable, object>( new DelegateWrapper( func2 ).Obj_Apply_Enumerable );
        }

        [Lisp( "apply" )]
        public static object Apply( object func, params object[] args )
        {
            var func2 = GetClosure( func );

            return func2.Apply( MakeArrayStar( args ) );
        }

        [Lisp( "funcall" )]
        public static object Funcall( object func, params object[] args )
        {
            var func2 = GetClosure( func );
            return func2.Apply( args );
        }

        internal static object Funcall( IApply func, params object[] args )
        {
            return func.Apply( args );
        }

    }
}
