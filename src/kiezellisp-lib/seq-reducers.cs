// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/*
using KeyFunc = System.Func<object, object>;
using PredicateFunc = System.Func<object, bool>;
using TestFunc = System.Func<object, object, bool>;
using ActionFunc = System.Action<object>;
using ApplyFunc = System.Func<object[], object>;
using ThreadFunc = System.Func<object>;
using Transducer = System.Func<Kiezel.IApply, Kiezel.IApply>;
*/

using ApplyFunc = System.Func<object[], object>;

namespace Kiezel
{
    public partial class Runtime
    {
        public class Transducer
        {
            internal static IApply Cat()
            {
                ApplyFunc xform = rfs =>
                {
                    var rf = (IApply)rfs[0];

                    ApplyFunc nrf = args =>
                    {
                        switch (args.Length)
                        {
                            case 0:
                                return Funcall(rf);
                            case 1:
                                return Funcall(rf, args[0]);
                            default:
                                var result = args[0];
                                var input = (IEnumerable)args[1];
                                return Reduce(rf, result, input);
                        }
                    };
                    return new ApplyWrapper2(nrf);

                };

                return new ApplyWrapper2(xform);
            }

            internal static IApply Dedupe(IApply test)
            {
                ApplyFunc xform = rfs =>
                {
                    var rf = (IApply)rfs[0];
                    var seen = new object();

                    ApplyFunc nrf = args =>
                    {
                        switch (args.Length)
                        {
                            case 0:
                                return Funcall(rf);
                            case 1:
                                return Funcall(rf, args[0]);
                            default:
                                var result = args[0];
                                var input = args[1];
                                if (FuncallBool(test, seen, input))
                                {
                                    return result;
                                }
                                else
                                {
                                    seen = input;
                                    return Apply(rf, args);
                                }
                        }
                    };
                    return new ApplyWrapper2(nrf);
                };
                return new ApplyWrapper2(xform);
            }

            internal static IApply Distinct(IApply test)
            {
                ApplyFunc xform = rfs =>
                {
                    var rf = (IApply)rfs[0];
                    var bag = new Vector();

                    ApplyFunc nrf = args =>
                    {
                        switch (args.Length)
                        {
                            case 0:
                                return Funcall(rf);
                            case 1:
                                return Funcall(rf, args[0]);
                            default:
                                var result = args[0];
                                var input = args[1];
                                if (Runtime.IndexOf(input, test, bag) == null)
                                {
                                    bag.Add(input);
                                    return Apply(rf, args);
                                }
                                else
                                {
                                    return result;
                                }
                        }
                    };
                    return new ApplyWrapper2(nrf);
                };
                return new ApplyWrapper2(xform);
            }

            internal static IApply Drop(int count)
            {
                ApplyFunc xform = rfs =>
                {
                    var rf = (IApply)rfs[0];
                    var c = count;
                    ApplyFunc nrf = args =>
                    {
                        switch (args.Length)
                        {
                            case 0:
                                return Funcall(rf);
                            case 1:
                                return Funcall(rf, args[0]);
                            default:
                                var result = args[0];
                                return --c >= 0 ? result : Apply(rf, args);
                        }
                    };
                    return new ApplyWrapper2(nrf);
                };
                return new ApplyWrapper2(xform);
            }

            internal static IApply DropWhile(IApply f)
            {
                ApplyFunc xform = rfs =>
                {
                    var rf = (IApply)rfs[0];
                    var dropped = false;
                    ApplyFunc nrf = args =>
                    {
                        switch (args.Length)
                        {
                            case 0:
                                return Funcall(rf);
                            case 1:
                                return Funcall(rf, args[0]);
                            default:
                                var result = args[0];
                                var input = args[1];
                                if (!dropped)
                                {
                                    if (FuncallBool(f, input))
                                    {
                                        return result;
                                    }
                                    else
                                    {
                                        dropped = true;
                                        return Apply(rf, args);
                                    }
                                }
                                else
                                {
                                    return Apply(rf, args);
                                }
                        }
                    };
                    return new ApplyWrapper2(nrf);
                };
                return new ApplyWrapper2(xform);
            }

            internal static IApply Filter(IApply f)
            {
                ApplyFunc xform = rfs =>
                {
                    var rf = (IApply)rfs[0];

                    ApplyFunc nrf = args =>
                    {
                        switch (args.Length)
                        {
                            case 0:
                                return Funcall(rf);
                            case 1:
                                return Funcall(rf, args[0]);
                            default:
                                var result = args[0];
                                var input = args[1];
                                return FuncallBool(f, input) ? Apply(rf, args) : result;
                        }
                    };
                    return new ApplyWrapper2(nrf);
                };
                return new ApplyWrapper2(xform);
            }

            internal static IApply Interpose(object separator)
            {
                ApplyFunc xform = rfs =>
                {
                    var rf = (IApply)rfs[0];
                    var addSeparator = false;

                    ApplyFunc nrf = args =>
                    {
                        switch (args.Length)
                        {
                            case 0:
                                return Funcall(rf);
                            case 1:
                                return Funcall(rf, args[0]);
                            default:
                                var result = args[0];
                                var input = args[1];
                                if (addSeparator)
                                {
                                    result = Funcall(rf, result, separator);
                                    if (!Reducedp(result))
                                    {
                                        result = Funcall(rf, result, input);
                                    }
                                    return result;
                                }
                                else
                                {
                                    addSeparator = true;
                                    return Apply(rf, args);
                                }
                        }
                    };
                    return new ApplyWrapper2(nrf);
                };
                return new ApplyWrapper2(xform);
            }

            internal static IApply Keep(IApply f)
            {
                ApplyFunc xform = rfs =>
                {
                    var rf = (IApply)rfs[0];
                    ApplyFunc nrf = args =>
                    {
                        switch (args.Length)
                        {
                            case 0:
                                return Funcall(rf);
                            case 1:
                                return Funcall(rf, args[0]);
                            default:
                                var result = args[0];
                                var input = args[1];
                                return Funcall(f, input) != null ? Apply(rf, args) : result;
                        }
                    };
                    return new ApplyWrapper2(nrf);
                };
                return new ApplyWrapper2(xform);
            }

            internal static IApply KeepIndexed(IApply f)
            {
                ApplyFunc xform = rfs =>
                {
                    var rf = (IApply)rfs[0];
                    var index = -1;
                    ApplyFunc nrf = args =>
                    {
                        switch (args.Length)
                        {
                            case 0:
                                return Funcall(rf);
                            case 1:
                                return Funcall(rf, args[0]);
                            default:
                                var result = args[0];
                                var input = args[1];
                                ++index;
                                return Funcall(f, index, input) != null ? Apply(rf, args) : result;
                        }
                    };
                    return new ApplyWrapper2(nrf);
                };
                return new ApplyWrapper2(xform);
            }

            internal static IApply Map(IApply f)
            {
                ApplyFunc xform = rfs =>
                {
                    var rf = (IApply)rfs[0];

                    ApplyFunc nrf = args =>
                    {
                        switch (args.Length)
                        {
                            case 0:
                                return Funcall(rf);
                            case 1:
                                return Funcall(rf, args[0]);
                            case 2:
                                var result = args[0];
                                var input = args[1];
                                return Funcall(rf, result, Funcall(f, input));
                            default:
                                var result2 = args[0];
                                var input2 = Cdr(AsList(args));
                                return Funcall(rf, result2, ApplyStar(f, input2));
                        }
                    };
                    return new ApplyWrapper2(nrf);
                };
                return new ApplyWrapper2(xform);
            }

            internal static IApply Mapcat(IApply f)
            {
                return Runtime.Compose(Map(f), Cat());
            }

            internal static IApply MapIndexed(IApply f)
            {
                ApplyFunc xform = rfs =>
                {
                    var rf = (IApply)rfs[0];
                    var index = -1;
                    ApplyFunc nrf = args =>
                    {
                        switch (args.Length)
                        {
                            case 0:
                                return Funcall(rf);
                            case 1:
                                return Funcall(rf, args[0]);
                            default:
                                var result = args[0];
                                var input = args[1];
                                ++index;
                                return Funcall(rf, result, Funcall(f, index, input));
                        }
                    };
                    return new ApplyWrapper2(nrf);
                };
                return new ApplyWrapper2(xform);
            }

            internal static IApply PartitionAll(int size)
            {
                ApplyFunc xform = rfs =>
                {
                    var rf = (IApply)rfs[0];
                    var bag = new Vector();

                    ApplyFunc nrf = args =>
                    {
                        object result;
                        object input;

                        switch (args.Length)
                        {
                            case 0:
                                return Funcall(rf);
                            case 1:
                                result = args[0];
                                if (bag.Count != 0)
                                {
                                    var bag2 = bag;
                                    bag = new Vector();
                                    result = Unreduced(Funcall(rf, result, bag2));
                                }
                                return Funcall(rf, result);
                            default:
                                result = args[0];
                                input = args[1];
                                bag.Add(input);
                                if (bag.Count >= size)
                                {
                                    var bag2 = bag;
                                    bag = new Vector();
                                    result = Funcall(rf, result, bag2);
                                }
                                return result;
                        }
                    };
                    return new ApplyWrapper2(nrf);
                };
                return new ApplyWrapper2(xform);
            }


            internal static IApply PartitionBy(IApply f)
            {
                ApplyFunc xform = rfs =>
                {
                    var rf = (IApply)rfs[0];
                    var bag = new Vector();
                    object key = null;

                    ApplyFunc nrf = args =>
                    {
                        object result;
                        object input;
                        object key2;

                        switch (args.Length)
                        {
                            case 0:
                                return Funcall(rf);
                            case 1:
                                result = args[0];
                                if (bag.Count != 0)
                                {
                                    var bag2 = bag;
                                    bag = new Vector();
                                    result = Unreduced(Funcall(rf, result, bag2));
                                }
                                return Funcall(rf, result);
                            default:
                                result = args[0];
                                input = args[1];
                                key2 = Funcall(f, input);
                                if (bag.Count == 0 || Equals(key2, key))
                                {
                                    bag.Add(input);
                                    key = key2;
                                    return result;
                                }
                                else
                                {
                                    var bag2 = bag;
                                    bag = new Vector();
                                    result = Funcall(rf, result, bag2);
                                    if (!Reducedp(result))
                                    {
                                        key = key2;
                                        bag.Add(input);
                                    }
                                    return result;
                                }
                        }
                    };
                    return new ApplyWrapper2(nrf);
                };
                return new ApplyWrapper2(xform);
            }

            internal static IApply Remove(IApply f)
            {
                ApplyFunc xform = rfs =>
                {
                    var rf = (IApply)rfs[0];

                    ApplyFunc nrf = args =>
                    {
                        switch (args.Length)
                        {
                            case 0:
                                return Funcall(rf);
                            case 1:
                                return Funcall(rf, args[0]);
                            default:
                                var result = args[0];
                                var input = args[1];
                                return FuncallBool(f, input) ? result : Apply(rf, args);
                        }
                    };
                    return new ApplyWrapper2(nrf);
                };
                return new ApplyWrapper2(xform);
            }

            internal static IApply Take(int count)
            {
                ApplyFunc xform = rfs =>
                {
                    var rf = (IApply)rfs[0];
                    var c = count;

                    ApplyFunc nrf = args =>
                    {
                        switch (args.Length)
                        {
                            case 0:
                                return Funcall(rf);
                            case 1:
                                return Funcall(rf, args[0]);
                            default:
                                var result = args[0];
                                return --c >= 0 ? Apply(rf, args) : Reduced(result);
                        }
                    };
                    return new ApplyWrapper2(nrf);
                };
                return new ApplyWrapper2(xform);
            }

            internal static IApply TakeNth(int step)
            {
                ApplyFunc xform = rfs =>
                {
                    var rf = (IApply)rfs[0];
                    var s = 1;

                    ApplyFunc nrf = args =>
                    {
                        switch (args.Length)
                        {
                            case 0:
                                return Funcall(rf);
                            case 1:
                                return Funcall(rf, args[0]);
                            default:
                                var result = args[0];
                                if (--s <= 0)
                                {
                                    s = step;
                                    return Apply(rf, args);
                                }
                                else
                                {
                                    return result;
                                }
                        }
                    };
                    return new ApplyWrapper2(nrf);
                };
                return new ApplyWrapper2(xform);
            }

            internal static IApply TakeWhile(IApply f)
            {
                ApplyFunc xform = rfs =>
                {
                    var rf = (IApply)rfs[0];

                    ApplyFunc nrf = args =>
                    {
                        switch (args.Length)
                        {
                            case 0:
                                return Funcall(rf);
                            case 1:
                                return Funcall(rf, args[0]);
                            default:
                                var result = args[0];
                                var input = args[1];
                                if (FuncallBool(f, input))
                                {
                                    return Apply(rf, args);
                                }
                                else
                                {
                                    return Reduced(result);
                                }
                        }
                    };
                    return new ApplyWrapper2(nrf);
                };
                return new ApplyWrapper2(xform);
            }

        }

        [Lisp("eduction")]
        public static IEnumerable Eduction(params object[] args)
        {
            var xforms = new List<IApply>();
            for (var i = 0; i + 1 < args.Length; ++i)
            {
                xforms.Add((IApply)args[i]);
            }
            var seq = (IEnumerable)args[args.Length - 1];
            var xform = Compose(xforms.ToArray());
            return new Reducible(xform, seq, false);
        }

        [Lisp("completing")]
        public static IApply Completing(IApply f)
        {
            return Completing(f, IdentityApply);
        }

        [Lisp("completing")]
        public static IApply Completing(IApply f, IApply cf)
        {
            ApplyFunc nrf = args =>
            {
                switch (args.Length)
                {
                    case 0:
                        return Funcall(f);
                    case 1:
                        return Funcall(cf, args[0]);
                    default:
                        return Apply(f, args);
                }
            };
            return new ApplyWrapper2(nrf);

        }

        [Lisp("reduced?")]
        public static bool Reducedp(object obj)
        {
            return obj is ReduceBreakValue;
        }

        [Lisp("reduced")]
        public static object Reduced(object obj)
        {
            return new ReduceBreakValue(obj);
        }

        [Lisp("unreduced")]
        public static object Unreduced(object obj)
        {
            var rbv = obj as ReduceBreakValue;
            return rbv == null ? obj : rbv.Value;
        }
    }

    class ReduceBreakValue
    {
        internal object Value;
        internal ReduceBreakValue(object value)
        {
            Value = value;
        }
    }

    public class Reducible : IEnumerable
    {
        internal IEnumerable Seq;
        internal IApply Transform;
        internal bool UseApply;

        internal Reducible(IApply transform, IEnumerable seq, bool useApply)
        {
            UseApply = useApply;

            if (seq is Reducible)
            {
                var reducer = (Reducible)seq;
                Seq = reducer.Seq;
                Transform = Runtime.Compose(transform, reducer.Transform);
            }
            else
            {
                Seq = seq;
                Transform = transform;
            }
        }

        class QueueReducer : IApply
        {
            object IApply.Apply(object[] args)
            {
                switch (args.Length)
                {
                    case 0:
                        return new Queue();
                    case 1:
                        return args[0];
                    case 2:
                    default:
                        ((Queue)args[0]).Enqueue(args[1]);
                        return args[0];
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var reducer = (IApply)Runtime.Funcall(Transform, new QueueReducer());
            var queue = (Queue)Runtime.Funcall(reducer);
            foreach (var item in Runtime.ToIter(Seq))
            {
                var result = UseApply ? Runtime.ApplyStar(reducer, queue, item) : Runtime.Funcall(reducer, queue, item);
                if (Runtime.Reducedp(result))
                {
                    break;
                }
                else
                {
                    while (queue.Count > 0)
                    {
                        yield return queue.Dequeue();
                    }
                }
            }
            Runtime.Funcall(reducer, queue);
            while (queue.Count > 0)
            {
                yield return queue.Dequeue();
            }
        }

        public Cons AsLazyList()
        {
            return Runtime.AsLazyList(this);
        }

        class VectorReducer : IApply
        {
            object IApply.Apply(object[] args)
            {
                switch (args.Length)
                {
                    case 0:
                        return new Vector();
                    case 1:
                        return args[0];
                    case 2:
                    default:
                        ((Vector)args[0]).Add(args[1]);
                        return args[0];
                }
            }
        }

        public Vector AsVector()
        {
            var reducer = (IApply)Runtime.Funcall(Transform, new VectorReducer());
            var result = Runtime.Funcall(reducer);
            foreach (var item in Runtime.ToIter(Seq))
            {
                result = (UseApply ? Runtime.ApplyStar(reducer, result, item) : Runtime.Funcall(reducer, result, item));
                if (Runtime.Reducedp(result))
                {
                    break;
                }
            }
            return (Vector)Runtime.Unreduced(result);
        }

        class ListReducer : IApply
        {
            Cons Last = new Cons();

            object IApply.Apply(object[] args)
            {
                switch (args.Length)
                {
                    case 0:
                        return Last;
                    case 1:
                        return ((Cons)args[0]).Cdr;
                    case 2:
                    default:
                        Last.Cdr = new Cons(args[1], null);
                        Last = Last.Cdr;
                        return args[0];
                }
            }
        }

        public Cons AsList()
        {
            var reducer = (IApply)Runtime.Funcall(Transform, new ListReducer());
            var result = Runtime.Funcall(reducer);
            foreach (var item in Runtime.ToIter(Seq))
            {
                result = (UseApply ? Runtime.ApplyStar(reducer, result, item) : Runtime.Funcall(reducer, result, item));
                if (Runtime.Reducedp(result))
                {
                    break;
                }
            }
            return ((Cons)Runtime.Unreduced(result)).Cdr;
        }

    }

}