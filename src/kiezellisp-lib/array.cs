// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Kiezel
{
    public partial class Runtime
    {
        [Lisp("elt")]
        public static object Elt(object target, params object[] indexes)
        {
            if (target == null)
            {
                return null;
            }
            else if (target is Prototype)
            {
                var proto = (Prototype)target;
                return proto[indexes[0]];
            }
            else
            {
                var args = new List<Expression>();
                args.Add(Expression.Constant(target));
                args.AddRange(indexes.Select(x => Expression.Constant(x)));
                var binder = GetGetIndexBinder(args.Count - 1);
                var code = CompileDynamicExpression(binder, typeof(object), args);
                var result = Execute(code);
                return result;
            }
        }

        [Lisp("array")]
        public static object[] MakeArray(params object[] items)
        {
            return items;
        }

        [Lisp("array*")]
        public static object[] MakeArrayStar(params object[] items)
        {
            // Last item must be a seq, works like list*
            int s1 = items.Length - 1;

            if (s1 == -1)
            {
                return new object[ 0 ];
            }

            var tail = AsArray((IEnumerable)items[s1]);
            var s2 = tail.Length;

            var result = new object[ s1 + s2 ];

            Array.Copy(items, result, s1);
            Array.Copy(tail, 0, result, s1, s2);

            return result;
        }

        [Lisp("set-elt")]
        public static object SetElt(object target, params object[] indexesAndValue)
        {
            if (target is Prototype)
            {
                var proto = (Prototype)target;
                var indexes = indexesAndValue.Take(indexesAndValue.Length - 1).ToArray();
                var value = indexesAndValue[indexesAndValue.Length - 1];
                proto[indexes[0]] = value;
                return value;
            }
            else
            {
                var args = new List<Expression>();
                args.Add(Expression.Constant(target));
                args.AddRange(indexesAndValue.Select(x => Expression.Constant(x)));
                var binder = GetSetIndexBinder(args.Count - 1);
                var code = CompileDynamicExpression(binder, typeof(object), args);
                var result = Execute(code);
                return result;
            }
        }
    }
}