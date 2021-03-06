﻿#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System.Collections;

    public partial class Runtime
    {
        #region Public Methods

        [Lisp("acons")]
        public static Cons Acons(object key, object value, Cons alist)
        {
            return MakeCons(MakeList(key, value), alist);
        }

        [Lisp("assoc")]
        public static Cons Assoc(object item, Cons alist, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "key", "test" });
            var key = GetClosure(kwargs[0]);
            var test = GetClosure(kwargs[1], EqualApply);
            return SeqBase.Assoc(item, alist, test, key);
        }

        [Lisp("assoc-if")]
        public static Cons AssocIf(IApply predicate, Cons alist, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "key" });
            var key = GetClosure(kwargs[0]);
            return SeqBase.AssocIf(predicate, alist, key);
        }

        [Lisp("pairlis")]
        public static Cons Pairlis(IEnumerable keys, IEnumerable values)
        {
            return Pairlis(keys, values, null);
        }

        [Lisp("pairlis")]
        public static Cons Pairlis(IEnumerable keys, IEnumerable values, Cons alist)
        {
            var iter1 = ToIter(keys).GetEnumerator();
            var iter2 = ToIter(values).GetEnumerator();
            while (iter1.MoveNext() && iter2.MoveNext())
            {
                alist = MakeCons(MakeList(iter1.Current, iter2.Current), alist);
            }
            return alist;
        }

        #endregion Public Methods
    }
}