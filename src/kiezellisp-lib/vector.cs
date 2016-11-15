#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    public partial class Runtime
    {
        #region Methods

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

        #endregion Methods
    }

    public class Vector : List<object>
    {
        #region Constructors

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

        #endregion Constructors

        #region Methods

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

        #endregion Methods
    }
}