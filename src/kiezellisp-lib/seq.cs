// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActionFunc = System.Action<object>;
using CompareFunc = System.Func<object, object, int>;
using KeyFunc = System.Func<object, object>;

using PredicateFunc = System.Func<object, bool>;
using TestFunc = System.Func<object, object, bool>;

namespace Kiezel
{
    public partial class Runtime
    {
        [Lisp("adjoin")]
        public static Cons Adjoin(object item, IEnumerable seq)
        {
            var seq2 = AsLazyList(seq);
            if (Position(item, seq2) == null)
            {
                return MakeCons(item, seq2);
            }
            else
            {
                return seq2;
            }
        }

        [Lisp("any?")]
        public static bool Any(IApply predicate, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "key" });
            var key = GetClosure(kwargs[0]);
            return SeqBase.Any(predicate, seq, key);
        }

        [Lisp("append", "concat")]
        public static Cons Append(params IEnumerable[] seqs)
        {
            return AsLazyList(SeqBase.Append(seqs));
        }

        [Lisp("average")]
        public static object Average(IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "key" });
            var key = GetClosure(kwargs[0]);
            return SeqBase.Average(seq, key);
        }

        [Lisp("conj")]
        public static object Conjoin(IEnumerable seq, params object[] items)
        {
            if (Listp(seq))
            {
                var lst = (Cons)seq;
                foreach (var item in ToIter(items))
                {
                    lst = MakeCons(item, lst);
                }
                return lst;
            }
            else if (Vectorp(seq))
            {
                if (items != null)
                {
                    ((Vector)seq).AddRange(items);
                }
                return seq;
            }
            else
            {
                throw new NotImplementedException();
            }
        }


        [Lisp("copy-seq")]
        public static Cons CopySeq(IEnumerable seq)
        {
            return Subseq(seq, 0);
        }

        [Lisp("count")]
        public static int Count(object item, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "test", "key" });
            var test = GetClosure(kwargs[0], EqualApply);
            var key = GetClosure(kwargs[1]);
            return SeqBase.Count(item, seq, test, key);
        }

        [Lisp("count-if")]
        public static int CountIf(IApply predicate, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "key" });
            var key = GetClosure(kwargs[0]);

            var count = 0;

            foreach (object x in ToIter( seq ))
            {
                if (FuncallBool(predicate, Funcall(key, x)))
                {
                    ++count;
                }
            }

            return count;
        }

        [Lisp("create-array")]
        public static Array CreateArray(Symbol type, int size)
        {
            var t = (Type)GetType(type);
            return Array.CreateInstance(t, size);
        }

        [Lisp("cycle")]
        public static Cons Cycle(IEnumerable seq)
        {
            return AsLazyList(SeqBase.Cycle(seq));
        }

        [Lisp("distinct")]
        public static Cons Distinct(IEnumerable seq1, params object[] args)
        {
            return Union(null, seq1, args);
        }

        [Lisp("drop")]
        public static Cons Drop(int count, IEnumerable seq)
        {
            return MakeCons(SeqBase.Drop(count, seq));
        }

        [Lisp("drop-while")]
        public static Cons DropWhile(IApply predicate, IEnumerable seq)
        {
            return MakeCons(SeqBase.DropWhile(predicate, seq).GetEnumerator());
        }

        [Lisp("each")]
        public static void Each(IApply action, IEnumerable seq)
        {
            foreach (object arg in ToIter( seq ))
            {
                Funcall(action, arg);
            }
        }

        [Lisp("every?")]
        public static bool Every(IApply predicate, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "key" });
            var key = GetClosure(kwargs[0]);
            return SeqBase.Every(predicate, seq, key);
        }

        [Lisp("except")]
        public static Cons Except(IEnumerable seq1, IEnumerable seq2, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "test", "key" });
            var test = GetClosure(kwargs[0], EqualApply);
            var key = GetClosure(kwargs[1]);
            return AsLazyList(SeqBase.Except(seq1, seq2, test, key));
        }

        [Lisp("filter")]
        public static Cons Filter(IApply predicate, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "key" });
            var key = GetClosure(kwargs[0]);
            return AsLazyList(SeqBase.Filter(predicate, seq, key));
        }

        [Lisp("find")]
        public static object Find(object item, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "test", "key", "default" });
            var test = GetClosure(kwargs[0], EqualApply);
            var key = GetClosure(kwargs[1]);
            var defaultValue = kwargs[2];
            var mv = SeqBase.FindItem(seq, item, test, key, defaultValue);
            return mv.Item1;
        }

        [Lisp("find-if")]
        public static object FindIf(IApply predicate, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "key", "default" });
            var key = GetClosure(kwargs[0]);
            var defaultValue = kwargs[1];
            var mv = SeqBase.FindItemIf(seq, predicate, key, defaultValue);
            return mv.Item1;
        }

        [Lisp("find-in-property-list")]
        public static object FindProperty(object item, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "test", "key", "default" });
            var test = GetClosure(kwargs[0], EqualApply);
            var key = GetClosure(kwargs[1]);
            var defaultValue = kwargs[2];
            return SeqBase.FindProperty(item, seq, test, key, defaultValue);
        }

        [Lisp("find-subsequence-position")]
        public static object FindSubsequencePosition(IEnumerable subseq, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "test", "key" });
            var test = GetClosure(kwargs[0], EqualApply);
            var key = GetClosure(kwargs[1]);
            return SeqBase.FindSubsequencePosition(subseq, seq, test, key);
        }

        [Lisp("flatten")]
        public static Cons Flatten(IEnumerable seq)
        {
            return AsLazyList(SeqBase.Flatten(seq, Int32.MaxValue));
        }

        [Lisp("flatten")]
        public static Cons Flatten(IEnumerable seq, int depth)
        {
            return AsLazyList(SeqBase.Flatten(seq, depth));
        }

        [Lisp("bq:append")]
        public static Cons ForceAppend(params IEnumerable[] seqs)
        {
            return AsList(SeqBase.Append(seqs));
        }

        [Lisp("group-by")]
        public static Cons GroupBy(IApply key, IEnumerable seq)
        {
            return AsLazyList(SeqBase.GroupBy(key, seq));
        }

        [Lisp("in-list-enumerator")]
        public static IEnumerable InListEnumerator(Cons seq, IApply step)
        {
            while (seq != null)
            {
                yield return First(seq);
                seq = (Cons)Funcall(step, seq);
            }
        }

        [Lisp("interleave")]
        public static Cons Interleave(params IEnumerable[] seqs)
        {
            return AsLazyList(SeqBase.Interleave(seqs));
        }

        [Lisp("interpose")]
        public static Cons Interpose(object separator, IEnumerable seq)
        {
            return AsLazyList(SeqBase.Interpose(separator, seq));
        }

        [Lisp("intersect")]
        public static Cons Intersect(IEnumerable seq1, IEnumerable seq2, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "test", "key" });
            var test = GetClosure(kwargs[0], EqualApply);
            var key = GetClosure(kwargs[1]);
            return AsLazyList(SeqBase.Intersect(seq1, seq2, test, key));
        }

        [Lisp("iterate")]
        public static Cons Iterate(IApply func, object value)
        {
            return AsLazyList(SeqBase.Iterate(-1, func, value));
        }

        [Lisp("keep")]
        public static Cons Keep(IApply func, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "key" });
            var key = GetClosure(kwargs[0]);
            return AsLazyList(SeqBase.Keep(func, seq, key));
        }

        [Lisp("keep-indexed")]
        public static Cons KeepIndexed(IApply func, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "key" });
            var key = GetClosure(kwargs[0]);
            return AsLazyList(SeqBase.KeepIndexed(func, seq, key));
        }

        [Lisp("length")]
        public static int Length(IEnumerable seq)
        {
            if (seq == null)
            {
                return 0;
            }
            else if (seq is string)
            {
                return ((string)seq).Length;
            }
            else if (seq is ICollection)
            {
                return ((ICollection)seq).Count;
            }
            else if (seq is IEnumerable)
            {
                int len = 0;
                foreach (object item in ( IEnumerable ) seq)
                {
                    ++len;
                }
                return len;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        [Lisp("map")]
        public static Cons Map(IApply func, params IEnumerable[] seqs)
        {
            return AsLazyList(SeqBase.Map(func, seqs));
        }

        [Lisp("max")]
        public static object Max(IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "key" });
            var key = GetClosure(kwargs[0]);
            Func<object, object, object> reducer = ( x, y) => NotLess(x, y) ? x : y;
            return ReduceSeq(reducer, seq, MissingValue, key);
        }

        [Lisp("merge")]
        public static Cons Merge(IEnumerable seq1, IEnumerable seq2, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "test", "key" });
            var test = GetClosure(kwargs[0], CompareApply);
            var key = GetClosure(kwargs[1]);
            return AsLazyList(SeqBase.Merge(seq1, seq2, test, key));
        }

        [Lisp("min")]
        public static object Min(IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "key" });
            var key = GetClosure(kwargs[0]);
            Func<object, object, object> reducer = ( x, y) => NotGreater(x, y) ? x : y;
            return ReduceSeq(reducer, seq, MissingValue, key);
        }

        [Lisp("mismatch")]
        public static object Mismatch(IEnumerable seq1, IEnumerable seq2, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "test", "key" });
            var test = GetClosure(kwargs[0], EqualApply);
            var key = GetClosure(kwargs[1]);
            return SeqBase.Mismatch(seq1, seq2, test, key);
        }

        [Lisp("not-any?")]
        public static bool NotAny(IApply predicate, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "key" });
            var key = GetClosure(kwargs[0]);
            foreach (var v in ToIter( seq ))
            {
                if (FuncallBool(predicate, Funcall(key, v)))
                {
                    return false;
                }
            }
            return true;
        }

        [Lisp("not-every?")]
        public static bool NotEvery(IApply predicate, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "key" });
            var key = GetClosure(kwargs[0]);
            foreach (var v in ToIter( seq ))
            {
                if (!FuncallBool(predicate, Funcall(key, v)))
                {
                    return true;
                }
            }
            return false;
        }


        [Lisp("parallel-each")]
        public static void ParallelEach(IApply action, IEnumerable seq)
        {
            var seq2 = ConvertToEnumerableObject(seq);
            var specials = GetCurrentThread().SpecialStack;
            ActionFunc wrapper = a =>
            {
                // We want an empty threadcontext because threads may be reused
                // and already have a broken threadcontext.
                CurrentThreadContext = new ThreadContext(specials);
                Funcall(action, a);
            };
            Parallel.ForEach<object>(seq2, wrapper);
        }

        [Lisp("parallel-map")]
        public static Cons ParallelMap(IApply action, IEnumerable seq)
        {
            return AsLazyList(SeqBase.ParallelMap(action, seq));
        }

        [Lisp("partition")]
        public static Cons Partition(int size, int step, IEnumerable pad, IEnumerable seq)
        {
            return AsLazyList(SeqBase.Partition(false, size, step, pad, seq));
        }

        [Lisp("partition")]
        public static Cons Partition(int size, int step, IEnumerable seq)
        {
            return AsLazyList(SeqBase.Partition(false, size, step, null, seq));
        }

        [Lisp("partition")]
        public static Cons Partition(int size, IEnumerable seq)
        {
            return AsLazyList(SeqBase.Partition(false, size, size, null, seq));
        }

        [Lisp("partition-all")]
        public static Cons PartitionAll(int size, int step, IEnumerable seq)
        {
            return AsLazyList(SeqBase.Partition(true, size, step, null, seq));
        }

        [Lisp("partition-all")]
        public static Cons PartitionAll(int size, IEnumerable seq)
        {
            return AsLazyList(SeqBase.Partition(true, size, size, null, seq));
        }

        [Lisp("partition-by")]
        public static Cons PartitionBy(IApply func, IEnumerable seq)
        {
            return AsLazyList(SeqBase.PartitionBy(func, 0, seq));
        }

        [Lisp("position")]
        public static object Position(object item, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "test", "key" });
            var test = GetClosure(kwargs[0], EqualApply);
            var key = GetClosure(kwargs[1]);
            var mv = SeqBase.FindItem(seq, item, test, key, null);
            return mv.Item2;
        }

        [Lisp("position-if")]
        public static object PositionIf(IApply predicate, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "key" });
            var key = GetClosure(kwargs[0]);
            var mv = SeqBase.FindItemIf(seq, predicate, key, null);
            return mv.Item2;
        }

        [Lisp("range")]
        public static Cons Range(int start, int end, int step)
        {
            return AsLazyList(SeqBase.Range(start, end, step));
        }

        [Lisp("range")]
        public static Cons Range(int start, int end)
        {
            return Range(start, end, 1);
        }

        [Lisp("range")]
        public static Cons Range(int end)
        {
            return Range(0, end, 1);
        }

        [Lisp("range")]
        public static Cons Range()
        {
            return Range(0, Int32.MaxValue, 1);
        }

  

        [Lisp("reduce")]
        public static object Reduce(IApply reducer, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "initial-value", "key" }, MissingValue, null);
            var seed = kwargs[0];
            var key = GetClosure(kwargs[1]);
            return SeqBase.ReduceSeq(reducer, seq, seed, key);
        }

        [Lisp("reductions")]
        public static Cons Reductions(IApply reducer, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "initial-value", "key" }, MissingValue, null);
            var seed = kwargs[0];
            var key = GetClosure(kwargs[1]);
            return AsLazyList(SeqBase.Reductions(reducer, seq, seed, key));
        }

        [Lisp("remove")]
        public static Cons Remove(IApply predicate, IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "key" }, null);
            var key = GetClosure(kwargs[0]);
            return AsLazyList(SeqBase.Remove(predicate, seq, key));
        }

        [Lisp("repeat")]
        public static Cons Repeat(int count, object value)
        {
            return AsLazyList(SeqBase.Repeat(count, value));
        }

        [Lisp("repeat")]
        public static Cons Repeat(object value)
        {
            return AsLazyList(SeqBase.Repeat(-1, value));
        }

        [Lisp("repeatedly")]
        public static Cons Repeatedly(IApply func)
        {
            return AsLazyList(SeqBase.Repeatedly(-1, func));
        }

        [Lisp("repeatedly")]
        public static Cons Repeatedly(int count, IApply func)
        {
            return AsLazyList(SeqBase.Repeatedly(count, func));
        }

        [Lisp("reverse")]
        public static Cons Reverse(IEnumerable seq)
        {
            return AsLazyList(SeqBase.Reverse(seq));
        }

        [Lisp("series")]
        public static Cons Series(int start, int end, int step)
        {
            return Range(start, end + step, step);
        }

        [Lisp("series")]
        public static Cons Series(int start, int end)
        {
            return Range(start, end + 1, 1);
        }

        [Lisp("series")]
        public static Cons Series(int end)
        {
            return Range(1, end + 1, 1);
        }

        [Lisp("series")]
        public static Cons Series()
        {
            return Range(1, Int32.MaxValue, 1);
        }

 
        [Lisp("shuffle")]
        public static Cons Shuffle(IEnumerable seq)
        {
            return AsLazyList(SeqBase.Shuffle(seq));
        }

        [Lisp("sort")]
        public static Cons Sort(IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "test", "key" });
            var test = GetClosure(kwargs[0], CompareApply);
            var key = GetClosure(kwargs[1]);
            return AsLazyList(SeqBase.Sort(seq, test, key));
        }

        [Lisp("split-at")]
        public static Cons SplitAt(int count, IEnumerable seq)
        {
            Cons left;
            Cons right;
            SeqBase.SplitAt(count, seq, out left, out right);
            return MakeList(left, right);
        }

        [Lisp("split-with")]
        public static Cons SplitWith(IApply pred, IEnumerable seq)
        {
            Cons left;
            Cons right;
            SeqBase.SplitWith(pred, seq, out left, out right);
            return MakeList(left, right);
        }

        [Lisp("subseq")]
        public static Cons Subseq(IEnumerable seq, int start, params object[] args)
        {
            return AsLazyList(SeqBase.Subseq(seq, start, args));
        }

        [Lisp("sum")]
        public static object Sum(IEnumerable seq, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "key" });
            var key = GetClosure(kwargs[0]);
            return ReduceSeq(Add2, seq, 0, key);
        }

        [Lisp("take")]
        public static Cons Take(int count, IEnumerable seq)
        {
            return AsLazyList(SeqBase.Take(count, seq));
        }

        [Lisp("take-nth")]
        public static Cons TakeNth(int step, IEnumerable seq)
        {
            return AsLazyList(SeqBase.TakeNth(step, seq));
        }

        [Lisp("take-while")]
        public static Cons TakeWhile(IApply predicate, IEnumerable seq)
        {
            return AsLazyList(SeqBase.TakeWhile(predicate, seq));
        }

        [Lisp("union")]
        public static Cons Union(IEnumerable seq1, IEnumerable seq2, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "test" });
            var test = GetClosure(kwargs[0], EqualApply);

            return AsLazyList(SeqBase.Union(seq1, seq2, test));
        }

        [Lisp("unzip")]
        public static Cons Unzip(IEnumerable seq)
        {
            int index = 0;
            var v1 = new Vector();
            var v2 = new Vector();
            foreach (object item in ToIter( seq ))
            {
                if (index == 0)
                {
                    v1.Add(item);
                }
                else
                {
                    v2.Add(item);
                }

                index = 1 - index;
            }

            return MakeList(AsLazyList(v1), AsLazyList(v2));
        }

        [Lisp("zip")]
        public static Cons Zip(params IEnumerable[] seqs)
        {
            return AsLazyList(SeqBase.Zip(seqs));
        }

 
        public static Cons Map(KeyFunc func, params IEnumerable[] seqs)
        {
            return AsLazyList(SeqBase.Map(func, seqs));
        }

        public static object ReduceSeq(Func<object, object, object> reducer, IEnumerable seq, object seed, IApply key)
        {
            var result = seed;
            foreach (object x in ToIter( seq ))
            {
                if (result == MissingValue)
                {
                    result = Funcall(key, x);
                }
                else
                {
                    result = reducer(result, Funcall(key, x));
                }
            }
            return result == MissingValue ? null : result;
        }

   
    }
}