// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Kiezel
{
    public class DelegateWrapper : IComparer, IEqualityComparer, IEqualityComparer<object>
    {
        private IApply function;

        internal DelegateWrapper( IApply function )
        {
            this.function = function;
        }

        public int Compare( object item1, object item2 )
        {
            return Runtime.ToInt( Runtime.Funcall( function, item1, item2 ) );
        }

        public new bool Equals( object x, object y )
        {
            return Runtime.ToBool( Runtime.Funcall( function, x, y ) );
        }

        public int GetHashCode( object obj )
        {
            return obj.GetHashCode();
        }

        internal object Obj()
        {
            return Runtime.Funcall( function );
        }

        internal object Obj_Apply_Enumerable( IEnumerable item )
        {
            return Runtime.ApplyStar( function, item );
        }

        internal bool Obj_Bool( object item )
        {
            return Runtime.ToBool( Runtime.Funcall( function, item ) );
        }

        internal IEnumerable<object> Obj_Enum( object item1 )
        {
            return ( IEnumerable<object> ) Runtime.Funcall( function, item1 );
        }

        internal void Obj_Evt_Void( object target, EventArgs evt )
        {
            Runtime.Funcall( function, target, evt );
        }

        internal bool Obj_Int_Bool( object item1, int item2 )
        {
            return Runtime.ToBool( Runtime.Funcall( function, item1, item2 ) );
        }

        internal IEnumerable<object> Obj_Int_Enum( object item1, int item2 )
        {
            return ( IEnumerable<object> ) Runtime.Funcall( function, item1, item2 );
        }

        internal object Obj_Int_Obj( object item1, int item2 )
        {
            return Runtime.Funcall( function, item1, item2 );
        }

        internal object Obj_Obj( object item )
        {
            return Runtime.Funcall( function, item );
        }

        internal bool Obj_Obj_Bool( object item1, object item2 )
        {
            return Runtime.ToBool( Runtime.Funcall( function, item1, item2 ) );
        }

        internal int Obj_Obj_Int( object item1, object item2 )
        {
            return ( int ) Runtime.Funcall( function, item1, item2 );
        }

        internal object Obj_Obj_Obj( object item1, object item2 )
        {
            return Runtime.Funcall( function, item1, item2 );
        }

        internal void Obj_Void( object item )
        {
            Runtime.Funcall( function, item );
        }

        internal object ObjA_Obj( object[] items )
        {
            return Runtime.ApplyStar( function, new object[] { items } );
        }

        internal void Void()
        {
            Runtime.Funcall( function );
        }
    }
}