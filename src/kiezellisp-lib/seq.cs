#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using System.Threading.Tasks;

    using ActionFunc = System.Action<object>;
    using KeyFunc = System.Func<object, object>;
    using Transducer = System.Func<Kiezel.IApply, Kiezel.IApply>;

    public partial class Runtime
    {
        #region Public Methods

        [Lisp("adjoin")]
        public static Cons Adjoin(object item, IEnumerable seq)
        {
            var seq2 = AsLazyList(seq);
            if (IndexOf(item, seq2) == null)
            {
                return MakeCons(item, seq2);
            }
            else
            {
                return seq2;
            }
        }

        [Lisp("any?")]
        public static bool Any(IApply predicate, IEnumerable seq)
        {
            return SeqBase.Any(predicate, seq);
        }

        [Lisp("append", "concat")]
        public static Cons Append(params IEnumerable[] seqs)
        {
            // Advise compiler that seqs is one (but not two or more) argument for the 
            // sequence function in this particular case.
            return Sequence(Cat(), (IEnumerable)seqs);
        }

        [Lisp("average")]
        public static object Average(IEnumerable seq)
        {
            return SeqBase.Average(seq);
        }

        internal static IApply Cat()
        {
            // The value of the symbol Symbols.Cat
            return Transducer.Cat();
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
        public static int Count(IApply predicate, IEnumerable seq)
        {
            var count = 0;

            foreach (object x in ToIter(seq))
            {
                if (FuncallBool(predicate, x))
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

        [Lisp("dedupe")]
        public static IApply Dedupe()
        {
            return Transducer.Dedupe(EqualApply);
        }

        [Lisp("dedupe")]
        public static IApply Dedupe(IApply test)
        {
            return Transducer.Dedupe(EqualApply);
        }

        [Lisp("dedupe")]
        public static Cons Dedupe(IEnumerable seq)
        {
            return Sequence(Dedupe(), seq);
        }

        [Lisp("dedupe")]
        public static Cons Dedupe(IApply test, IEnumerable seq)
        {
            return Sequence(Dedupe(test), seq);
        }

        [Lisp("distinct")]
        public static IApply Distinct()
        {
            return Transducer.Distinct(EqualApply);
        }

        [Lisp("distinct")]
        public static IApply Distinct(IApply test)
        {
            return Transducer.Distinct(test);
        }

        [Lisp("distinct")]
        public static Cons Distinct(IEnumerable seq)
        {
            return Sequence(Distinct(), seq);
        }

        [Lisp("distinct")]
        public static Cons Distinct(IApply test, IEnumerable seq)
        {
            return Sequence(Distinct(test), seq);
        }

        [Lisp("drop")]
        public static IApply Drop(int count)
        {
            return Transducer.Drop(count);
        }

        [Lisp("drop")]
        public static Cons Drop(int count, IEnumerable seq)
        {
            return Sequence(Drop(count), seq);
        }

        [Lisp("drop-while")]
        public static IApply DropWhile(IApply predicate)
        {
            return Transducer.DropWhile(predicate);
        }

        [Lisp("drop-while")]
        public static Cons DropWhile(IApply predicate, IEnumerable seq)
        {
            return Sequence(DropWhile(predicate), seq);
        }

        [Lisp("each")]
        public static void Each(IApply action, IEnumerable seq)
        {
            foreach (object arg in ToIter(seq))
            {
                Funcall(action, arg);
            }
        }

        [Lisp("every?")]
        public static bool Every(IApply predicate, IEnumerable seq)
        {
            return SeqBase.Every(predicate, seq);
        }

        [Lisp("except")]
        public static Cons Except(IEnumerable seq1, IEnumerable seq2)
        {
            return Except(EqualApply, seq1, seq2);
        }

        [Lisp("except")]
        public static Cons Except(IApply test, IEnumerable seq1, IEnumerable seq2)
        {
            return AsLazyList(SeqBase.Except(test, seq1, seq2));
        }

        [Lisp("filter")]
        public static IApply Filter(IApply predicate)
        {
            return Transducer.Filter(predicate);
        }

        [Lisp("filter")]
        public static Cons Filter(IApply predicate, IEnumerable seq)
        {
            return Sequence(Filter(predicate), seq);
        }

        [Lisp("find-subsequence-position")]
        public static object FindSubsequencePosition(IEnumerable subseq, IEnumerable seq)
        {
            return SeqBase.FindSubsequencePosition(subseq, seq);
        }

        [Lisp("first")]
        public static object First(IApply predicate, IEnumerable seq)
        {
            foreach (object x in ToIter(seq))
            {
                if (FuncallBool(predicate, x))
                {
                    return x;
                }
            }
            return null;
        }

        [Lisp("find")]
        public static object Find(IApply predicate, IEnumerable seq)
        {
            var i = -1;
            foreach (object x in ToIter(seq))
            {
                ++i;

                if (FuncallBool(predicate, x))
                {
                    return MakeList(x, i);
                }
            }
            return null;
        }

        [Lisp("flatten")]
        public static Cons Flatten(IEnumerable seq)
        {
            return AsLazyList(SeqBase.Flatten(seq, int.MaxValue));
        }

        [Lisp("flatten")]
        public static Cons Flatten(IEnumerable seq, int depth)
        {
            return AsLazyList(SeqBase.Flatten(seq, depth));
        }

        [Lisp("bq:append")]
        public static Cons ForceAppend(params IEnumerable[] seqs)
        {
            return AsList(Eduction(Cat(), seqs));
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

        [Lisp("index-of")]
        public static object IndexOf(object item, IEnumerable seq)
        {
            return IndexOf(item, EqualApply, seq);
        }

        [Lisp("index-of")]
        public static object IndexOf(object item, IApply test, IEnumerable seq)
        {
            var mv = SeqBase.IndexOf(item, test, seq);
            return mv.Index >= 0 ? (object)mv.Index : null;
        }

        [Lisp("interleave")]
        public static Cons Interleave(params IEnumerable[] seqs)
        {
            return AsLazyList(SeqBase.Interleave(seqs));
        }

        [Lisp("interpose")]
        public static IApply Interpose(object separator)
        {
            return Transducer.Interpose(separator);
        }

        [Lisp("interpose")]
        public static Cons Interpose(object separator, IEnumerable seq)
        {
            return Sequence(Interpose(separator), seq);
        }

        [Lisp("intersect")]
        public static Cons Intersect(IEnumerable seq1, IEnumerable seq2)
        {
            return Intersect(EqualApply, seq1, seq2);
        }

        [Lisp("intersect")]
        public static Cons Intersect(IApply test, IEnumerable seq1, IEnumerable seq2)
        {
            return AsLazyList(SeqBase.Intersect(test, seq1, seq2));
        }

        [Lisp("iterate")]
        public static Cons Iterate(IApply func, object value)
        {
            return AsLazyList(SeqBase.Iterate(-1, func, value));
        }

        [Lisp("keep")]
        public static IApply Keep(IApply func)
        {
            return Transducer.Keep(func);
        }

        [Lisp("keep")]
        public static Cons Keep(IApply func, IEnumerable seq)
        {
            return Sequence(Keep(func), seq);
        }

        [Lisp("keep-indexed")]
        public static IApply KeepIndexed(IApply func)
        {
            return Transducer.KeepIndexed(func);
        }

        [Lisp("keep-indexed")]
        public static Cons KeepIndexed(IApply func, IEnumerable seq)
        {
            return Sequence(KeepIndexed(func), seq);
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
                foreach (object item in seq)
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
        public static IApply Map(IApply func)
        {
            return Transducer.Map(func);
        }

        [Lisp("map")]
        public static Cons Map(IApply func, params IEnumerable[] seqs)
        {
            return Sequence(Map(func), seqs);
        }

        [Lisp("mapcat")]
        public static IApply Mapcat(IApply func)
        {
            return Transducer.Mapcat(func);
        }

        [Lisp("mapcat")]
        public static Cons Mapcat(IApply func, params IEnumerable[] seqs)
        {
            return Sequence(Mapcat(func), seqs);
        }

        [Lisp("map-indexed")]
        public static IApply MapIndexed(IApply func)
        {
            return Transducer.MapIndexed(func);
        }

        [Lisp("map-indexed")]
        public static Cons MapIndexed(IApply func, params IEnumerable[] seqs)
        {
            return Sequence(MapIndexed(func), seqs);
        }

        public static Cons Map(KeyFunc func, params IEnumerable[] seqs)
        {
            return Map(new ApplyWrapper1(func), seqs);
        }

        [Lisp("maximize")]
        public static object Maximize(IEnumerable seq)
        {
            var reducer = new ApplyWrapper2(Max);
            return Reduce(reducer, seq);
        }

        [Lisp("merge")]
        public static Cons Merge(IEnumerable seq1, IEnumerable seq2)
        {
            return AsLazyList(SeqBase.Merge(seq1, seq2, CompareApply, IdentityApply));
        }

        [Lisp("merge")]
        public static Cons Merge(IApply comparer, IEnumerable seq1, IEnumerable seq2)
        {
            return AsLazyList(SeqBase.Merge(seq1, seq2, comparer, IdentityApply));
        }

        [Lisp("merge-by")]
        public static Cons MergeBy(IApply key, IEnumerable seq1, IEnumerable seq2)
        {
            return AsLazyList(SeqBase.Merge(seq1, seq2, CompareApply, key));
        }

        [Lisp("merge-by")]
        public static Cons MergeBy(IApply key, IApply comparer, IEnumerable seq1, IEnumerable seq2)
        {
            return AsLazyList(SeqBase.Merge(seq1, seq2, comparer, key));
        }

        [Lisp("minimize")]
        public static object Minimize(IEnumerable seq)
        {
            var reducer = new ApplyWrapper2(Min);
            return Reduce(reducer, seq);
        }

        [Lisp("mismatch")]
        public static object Mismatch(IEnumerable seq1, IEnumerable seq2)
        {
            return Mismatch(EqualApply, seq1, seq2);
        }

        [Lisp("mismatch")]
        public static object Mismatch(IApply test, IEnumerable seq1, IEnumerable seq2)
        {
            return SeqBase.Mismatch(test, seq1, seq2);
        }

        [Lisp("not-any?")]
        public static bool NotAny(IApply predicate, IEnumerable seq)
        {
            foreach (var v in ToIter(seq))
            {
                if (FuncallBool(predicate, v))
                {
                    return false;
                }
            }
            return true;
        }

        [Lisp("not-every?")]
        public static bool NotEvery(IApply predicate, IEnumerable seq)
        {
            foreach (var v in ToIter(seq))
            {
                if (!FuncallBool(predicate, v))
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
            Parallel.ForEach(seq2, wrapper);
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
        public static IApply PartitionAll(int size)
        {
            return Transducer.PartitionAll(size);
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
        public static IApply PartitionBy(IApply func)
        {
            return Transducer.PartitionBy(func);
        }

        [Lisp("partition-by")]
        public static Cons PartitionBy(IApply func, IEnumerable seq)
        {
            return Sequence(PartitionBy(func), seq);
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
            return Range(0, int.MaxValue, 1);
        }

        [Lisp("reduce", "fold")]
        public static object Reduce(IApply reducer, IEnumerable seq)
        {
            return Reduce(reducer, MissingValue, seq);
        }

        [Lisp("reduce", "fold")]
        public static object Reduce(IApply reducer, object seed, IEnumerable seq)
        {
            return SeqBase.Reduce(reducer, seed, seq);
        }

        [Lisp("reductions")]
        public static Cons Reductions(IApply reducer, IEnumerable seq)
        {
            return Reductions(reducer, MissingValue, seq);
        }

        [Lisp("reductions")]
        public static Cons Reductions(IApply reducer, object seed, IEnumerable seq)
        {
            return AsLazyList(SeqBase.Reductions(reducer, seed, seq));
        }

        [Lisp("remove")]
        public static IApply Remove(IApply predicate)
        {
            return Transducer.Remove(predicate);
        }

        [Lisp("remove")]
        public static Cons Remove(IApply predicate, IEnumerable seq)
        {
            return Sequence(Remove(predicate), seq);
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

        [Lisp("sequence")]
        public static Cons Sequence(IEnumerable seq)
        {
            return AsLazyList(seq);
        }

        [Lisp("sequence")]
        public static Cons Sequence(IApply xform, params IEnumerable[] seqs)
        {
            if (seqs == null || seqs.Length == 0)
            {
                return null;
            }
            else if (seqs.Length == 1)
            {
                var seq = seqs[0];
                var eduction = new Reducible(xform, seq, false);
                return eduction.AsLazyList();
            }
            else
            {
                var seq = new UnisonEnumerator(seqs);
                var eduction = new Reducible(xform, seq, true);
                return eduction.AsLazyList();
            }
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
            return Range(1, int.MaxValue, 1);
        }

        [Lisp("shuffle")]
        public static Cons Shuffle(IEnumerable seq)
        {
            return AsLazyList(SeqBase.Shuffle(seq));
        }

        [Lisp("sort")]
        public static Cons Sort(IEnumerable seq)
        {
            return AsLazyList(SeqBase.Sort(seq, CompareApply, IdentityApply));
        }

        [Lisp("sort")]
        public static Cons Sort(IApply comparer, IEnumerable seq)
        {
            return AsLazyList(SeqBase.Sort(seq, comparer, IdentityApply));
        }

        [Lisp("sort-by")]
        public static Cons SortBy(IApply key, IEnumerable seq)
        {
            return AsLazyList(SeqBase.Sort(seq, CompareApply, key));
        }

        [Lisp("sort-by")]
        public static Cons SortBy(IApply key, IApply comparer, IEnumerable seq)
        {
            return AsLazyList(SeqBase.Sort(seq, comparer, key));
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
        public static object Sum(IEnumerable seq)
        {
            var reducer = new ApplyWrapper2(Add);
            return Reduce(reducer, seq);
        }

        [Lisp("take")]
        public static IApply Take(int count)
        {
            return Transducer.Take(count);
        }

        [Lisp("take")]
        public static Cons Take(int count, IEnumerable seq)
        {
            return Sequence(Take(count), seq);
        }

        [Lisp("take-nth")]
        public static IApply TakeNth(int step)
        {
            return Transducer.TakeNth(step);
        }

        [Lisp("take-nth")]
        public static Cons TakeNth(int step, IEnumerable seq)
        {
            return Sequence(TakeNth(step), seq);
        }

        [Lisp("take-while")]
        public static IApply TakeWhile(IApply predicate)
        {
            return Transducer.TakeWhile(predicate);
        }

        [Lisp("take-while")]
        public static Cons TakeWhile(IApply predicate, IEnumerable seq)
        {
            return Sequence(TakeWhile(predicate), seq);
        }

        [Lisp("transduce")]
        public static object Transduce(IApply xform, IApply func, IEnumerable seq)
        {
            return Transduce(xform, MissingValue, func, seq);
        }

        [Lisp("transduce")]
        public static object Transduce(IApply xform, object seed, IApply func, IEnumerable seq)
        {
            var reducer = (IApply)Funcall(xform, func);
            var result = Reduce(reducer, seed, seq);
            return result;
        }

        [Lisp("union")]
        public static Cons Union(IEnumerable seq1, IEnumerable seq2)
        {
            return Union(EqualApply, seq1, seq2);
        }

        [Lisp("union")]
        public static Cons Union(IApply test, IEnumerable seq1, IEnumerable seq2)
        {
            return AsLazyList(SeqBase.Union(test, seq1, seq2));
        }

        [Lisp("unzip")]
        public static Cons Unzip(IEnumerable seq)
        {
            int index = 0;
            var v1 = new Vector();
            var v2 = new Vector();
            foreach (object item in ToIter(seq))
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

        #endregion Public Methods
    }
}