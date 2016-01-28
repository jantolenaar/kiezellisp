// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Kiezel
{
    public partial class Runtime
    {
        [Lisp("vector")]
        public static Vector MakeVector(params object[] items)
        {
            return new Vector(items);
        }

        [Lisp("vector*")]
        public static Vector MakeVectorStar(params object[] items)
        {
            // Last item must be a seq, works like list*
            var v = new Vector();
            var n = items.Length;
            if (n > 0)
            {
                for (var i = 0; i < n - 1; ++i)
                {
                    v.Add(items[i]);
                }
            }
            v.AddRange((IEnumerable)items[n - 1]);
            return v;
        }
    }

    public class Vector : List<object>
    {
        public Vector()
            : base(20)
        {
        }

        public Vector(int size)
            : base(size)
        {
        }

        public Vector(params object[] items)
        {
            if (items != null)
            {
                var j = items.Length;
                for (var i = 0; i < j; ++i)
                {
                    Add(items[i]);
                }
            }
        }

        public void AddRange(IEnumerable items)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    Add(item);
                }
            }
        }

        public new Vector GetRange(int index, int count)
        {
            Vector z = new Vector();
            for (int i = 0; i < count; ++i)
            {
                z.Add(this[i + index]);
            }
            return z;
        }
    }
}