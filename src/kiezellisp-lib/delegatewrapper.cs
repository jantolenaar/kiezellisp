// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Kiezel
{
    public class DelegateWrapper : IComparer, IEqualityComparer, IEqualityComparer<object>
    {
        private IApply function;

        public DelegateWrapper(IApply function)
        {
            this.function = function;
        }

        public int Compare(object item1, object item2)
        {
            return Runtime.ToInt(Runtime.Funcall(function, item1, item2));
        }

        public new bool Equals(object x, object y)
        {
            return Runtime.ToBool(Runtime.Funcall(function, x, y));
        }

        public int GetHashCode(object obj)
        {
            return obj.GetHashCode();
        }

        public object Obj()
        {
            return Runtime.Funcall(function);
        }

        public object Obj_Apply_Enumerable(IEnumerable item)
        {
            return Runtime.ApplyStar(function, item);
        }

        public bool Obj_Bool(object item)
        {
            return Runtime.ToBool(Runtime.Funcall(function, item));
        }

        public IEnumerable<object> Obj_Enum(object item1)
        {
            return (IEnumerable<object>)Runtime.Funcall(function, item1);
        }

        public void Obj_Evt_Void(object target, EventArgs evt)
        {
            Runtime.Funcall(function, target, evt);
        }

        public bool Obj_Int_Bool(object item1, int item2)
        {
            return Runtime.ToBool(Runtime.Funcall(function, item1, item2));
        }

        public IEnumerable<object> Obj_Int_Enum(object item1, int item2)
        {
            return (IEnumerable<object>)Runtime.Funcall(function, item1, item2);
        }

        public object Obj_Int_Obj(object item1, int item2)
        {
            return Runtime.Funcall(function, item1, item2);
        }

        public object Obj_Obj(object item)
        {
            return Runtime.Funcall(function, item);
        }

        public bool Obj_Obj_Bool(object item1, object item2)
        {
            return Runtime.ToBool(Runtime.Funcall(function, item1, item2));
        }

        public int Obj_Obj_Int(object item1, object item2)
        {
            return (int)Runtime.Funcall(function, item1, item2);
        }

        public object Obj_Obj_Obj(object item1, object item2)
        {
            return Runtime.Funcall(function, item1, item2);
        }

        public void Obj_Void(object item)
        {
            Runtime.Funcall(function, item);
        }

        public object ObjA_Obj(object[] items)
        {
            return Runtime.ApplyStar(function, new object[] { items });
        }

        public void Void()
        {
            Runtime.Funcall(function);
        }
    }
}