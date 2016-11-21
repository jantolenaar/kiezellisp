#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections;
    using System.IO;

    public class Cons : IEnumerable, IPrintsValue
    {
        #region Fields

        internal object car;
        internal object cdr;

        #endregion Fields

        #region Constructors

        public Cons()
        {
            car = null;
            cdr = this;
        }

        public Cons(object first, object second)
        {
            car = first;
            cdr = second;
        }

        #endregion Constructors

        #region Public Properties

        public object Car
        {
            get
            {
                return car;
            }
            set
            {
                if (this == null)
                {
                    throw new LispException("Cannot set car of empty list");
                }
                else {
                    car = value;
                }
            }
        }

        public Cons Cdr
        {
            get
            {
                if (this == null)
                {
                    return null;
                }

                if (cdr is IEnumerator)
                {
                    cdr = Runtime.MakeCons((IEnumerator)cdr);
                }
                else if (cdr is DelayedExpression)
                {
                    cdr = Runtime.Force(cdr);
                }

                return (Cons)cdr;
            }
            set
            {
                if (this == null)
                {
                    throw new LispException("Cannot set cdr of empty list");
                }
                else {
                    cdr = value;
                }
            }
        }

        public int Count
        {
            get
            {
                int count = 0;
                Cons list = this;
                while (list != null)
                {
                    ++count;
                    list = list.Cdr;
                }
                return count;
            }
        }

        public bool Forced
        {
            get
            {
                return cdr == null || cdr is Cons;
            }
        }

        public object this[int index]
        {
            get
            {
                int n = index;
                Cons list = this;

                if (n < 0)
                {
                    throw new IndexOutOfRangeException();
                }

                while (n-- > 0)
                {
                    if (list == null)
                    {
                        return null;
                    }

                    list = list.Cdr;
                }

                if (list == null)
                {
                    return null;
                }
                else {
                    return list.Car;
                }
            }

            set
            {
                int n = index;
                Cons list = this;

                if (n < 0)
                {
                    throw new IndexOutOfRangeException();
                }

                while (n-- > 0)
                {
                    if (list == null)
                    {
                        throw new IndexOutOfRangeException();
                    }

                    list = list.Cdr;
                }

                if (list == null)
                {
                    throw new IndexOutOfRangeException();
                }
                else {
                    list.Car = value;
                }
            }
        }

        #endregion Public Properties

        #region Private Methods

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ListEnumerator(this);
        }

        #endregion Private Methods

        #region Public Methods

        public bool Contains(object value)
        {
            foreach (object item in this)
            {
                if (Runtime.Equal(item, value))
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return ToString(true);
        }

        //public static bool printCompact = true;
        public string ToString(bool escape, int radix = -1)
        {
            if (!(cdr is IEnumerator || cdr is DelayedExpression))
            {
                bool printCompact = Runtime.ToBool(Runtime.GetDynamic(Symbols.PrintCompact));

                if (escape && printCompact)
                {
                    var first = Runtime.First(this);
                    var second = Runtime.Second(this);
                    var third = Runtime.Third(this);

                    if (first == Symbols.Dot && second is string && third == null)
                    {
                        return string.Format(".{0}", second);
                    }
                    else if (first == Symbols.NullableDot && second is string && third == null)
                    {
                        return string.Format("?{0}", second);
                    }
                    else if (first == Symbols.Quote && third == null)
                    {
                        return string.Format("'{0}", second);
                    }
                }
            }

            var buf = new StringWriter();

            if (escape)
            {
                buf.Write("(");
            }

            Cons list = this;
            bool needcomma = false;

            while (list != null)
            {
                if (needcomma)
                {
                    buf.Write(" ");
                }

                buf.Write(Runtime.ToPrintString(list.Car, escape, radix));

                if (escape)
                {
                    if (list.cdr is IEnumerator || list.cdr is DelayedExpression)
                    {
                        buf.Write(" ...");
                        break;
                    }
                }

                needcomma = true;

                list = list.Cdr;
            }

            if (escape)
            {
                buf.Write(")");
            }

            return buf.ToString();
        }

        #endregion Public Methods

        #region Other

        private class ListEnumerator : IEnumerator
        {
            #region Fields

            // This enumerator does NOT keep a reference to the start of the list.
            public bool initialized;
            public Cons list;

            #endregion Fields

            #region Constructors

            public ListEnumerator(Cons list)
            {
                this.list = list;
            }

            #endregion Constructors

            #region Public Properties

            public object Current
            {
                get
                {
                    return list == null ? null : list.Car;
                }
            }

            #endregion Public Properties

            #region Public Methods

            public bool MoveNext()
            {
                if (initialized)
                {
                    if (list == null)
                    {
                        return false;
                    }
                    else {
                        list = list.Cdr;
                        return list != null;
                    }
                }
                else {
                    initialized = true;
                    return list != null;
                }
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            #endregion Public Methods
        }

        #endregion Other
    }

    public partial class Runtime
    {
        #region Public Methods

        [Lisp("copy-tree")]
        public static object CopyTree(object a)
        {
            if (Consp(a))
            {
                var tree = (Cons)a;
                return MakeCons(CopyTree(tree.Car), (Cons)CopyTree(tree.Cdr));
            }
            else {
                return a;
            }
        }

        [Lisp("cons")]
        public static Cons MakeCons(object item, Cons list)
        {
            return new Cons(item, list);
        }

        [Lisp("cons")]
        public static Cons MakeCons(object item, DelayedExpression delayedExpression)
        {
            return new Cons(item, delayedExpression);
        }

        public static Cons MakeCons(object a, IEnumerator seq)
        {
            return new Cons(a, seq);
        }

        public static Cons MakeCons(IEnumerator seq)
        {
            if (seq != null && seq.MoveNext())
            {
                return new Cons(seq.Current, seq);
            }
            else {
                return null;
            }
        }

        [Lisp("list", "bq:list")]
        public static Cons MakeList(params object[] items)
        {
            return AsList(items);
        }

        [Lisp("list*", "bq:list*")]
        public static Cons MakeListStar(params object[] items)
        {
            if (items.Length == 0)
            {
                return null;
            }

            var list = AsLazyList((IEnumerable)items[items.Length - 1]);

            for (var i = items.Length - 2; i >= 0; --i)
            {
                list = new Cons(items[i], list);
            }

            return list;
        }

        #endregion Public Methods
    }
}