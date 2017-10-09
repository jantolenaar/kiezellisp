#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Dynamic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public class CaseInsensitiveEqualityComparer : IEqualityComparer<object>
    {
        #region Fields

        private CaseInsensitiveComparer comparer = CaseInsensitiveComparer.DefaultInvariant;

        #endregion Fields

        #region Private Methods

        bool IEqualityComparer<object>.Equals(object x, object y)
        {
            return comparer.Compare(x, y) == 0;
        }

        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            if (obj is string)
            {
                return obj.ToString().ToLowerInvariant().GetHashCode();
            }
            else
            {
                return obj.GetHashCode();
            }
        }

        #endregion Private Methods
    }

    [RestrictedImport]
    public class Prototype : IDynamicMetaObjectProvider, IEnumerable, IApply
    {
        #region Fields

        public PrototypeDictionary Dict = new PrototypeDictionary(false);
        public List<Prototype> Parents = new List<Prototype>();

        #endregion Fields

        #region Constructors

        [Lisp]
        public Prototype(params object[] args)
        {
            Create(args);
        }

        #endregion Constructors

        #region Public Properties

        public Symbol ClassName { get; set; }

        [Lisp]
        public IEnumerable Keys
        {
            get
            {
                return Runtime.AsList(AsDictionary().Keys);
            }
        }

        public object this[object index]
        {
            get
            {
                return GetValue(index);
            }
            set
            {
                SetValue(index, value);
            }
        }

        #endregion Public Properties

        #region Private Methods

        object IApply.Apply(object[] args)
        {
            var arg = args[0];
            if (arg is LambdaClosure)
            {
                return Runtime.Funcall((IApply)arg, this);
            }
            else
            {
                return GetValue(arg);
            }
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new PrototypeMetaObject(parameter, this);
        }

        #endregion Private Methods

        #region Public Methods

        public void AddParent(Prototype parent)
        {
            if (parent != null)
            {
                Parents.Add(parent);
            }
        }

        [Lisp]
        public PrototypeDictionary AsDictionary()
        {
            var dict = new PrototypeDictionary();
            MergeInto(dict);
            return dict;
        }

        public void Create(object[] args)
        {
            if (args == null)
            {
                return;
            }

            int offset = 0;

            if ((args.Length & 1) != 0)
            {
                offset = 1;

                // Odd length: first item is type specification
                var a = args[0];

                if (Runtime.Listp(a))
                {
                    foreach (var b in Runtime.ToIter(a))
                    {
                        ProcessTypeArg(b);
                    }
                }
                else
                {
                    ProcessTypeArg(a);
                }
            }

            for (var i = offset; i + 1 < args.Length; i += 2)
            {
                var key = GetKey(args[i]);
                if (key != null)
                {
                    var value = args[i + 1];
                    Dict[key] = value;
                }
            }
        }

        [Lisp]
        public static Prototype FromDictionary(PrototypeDictionary dict)
        {
            var proto = new Prototype();
            if (dict != null)
            {
                proto.Dict = dict;
            }
            return proto;
        }

        public IEnumerator GetEnumerator()
        {
            var h = new Hashtable();
            foreach (var pair in AsDictionary())
            {
                h[pair.Key] = pair.Value;
            }
            return h.GetEnumerator();
        }

        public object GetKey(object key)
        {
            if (key is Symbol)
            {
                return ((Symbol)key).Name;
            }
            else
            {
                return key;
            }
        }

        [Lisp]
        public Cons GetParents(params object[] args)
        {
            object[] kwargs = Runtime.ParseKwargs(args, new string[] { "inherited" }, false);
            var inherited = Runtime.ToBool(kwargs[0]);
            var v = new Vector();
            GetParents(v, inherited);
            return Runtime.AsList(v);
        }

        public void GetParents(Vector result, bool inherited)
        {
            foreach (var parent in Parents)
            {
                result.Add(parent);
                if (inherited)
                {
                    parent.GetParents(result, inherited);
                }
            }
        }

        public void GetTypeNames(Vector result)
        {
            if (ClassName != null)
            {
                result.Add(ClassName);
            }
            else
            {
                foreach (var parent in Parents)
                {
                    parent.GetTypeNames(result);
                }
            }
        }

        [Lisp]
        public object GetTypeSpecifier()
        {
            var result = new Vector();
            GetTypeNames(result);
            switch (result.Count)
            {
                case 0:
                    {
                        return null;
                    }
                case 1:
                    {
                        return result[0];
                    }
                default:
                    {
                        return Runtime.AsList(result);
                    }
            }
        }

        public object GetValue(object ident)
        {
            return GetValueFor(this, ident);
        }

        public object GetValueFor(Prototype target, object ident)
        {
            var name = GetKey(ident);
            object result = null;

            if (Dict.TryGetValue(name, out result))
            {
                return result;
            }

            foreach (var parent in Parents)
            {
                result = parent.GetValueFor(target, ident);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        public bool HasInheritedProperty(object ident)
        {
            var name = GetKey(ident);

            if (Dict.ContainsKey(name))
            {
                return true;
            }

            return Parents.Any(x => x.Dict.ContainsKey(name));
        }

        [Lisp]
        public bool HasProperty(object ident, params object[] args)
        {
            var name = GetKey(ident);

            object[] kwargs = Runtime.ParseKwargs(args, new string[] { "inherited" }, true);
            var inherited = Runtime.ToBool(kwargs[0]);

            if (inherited)
            {
                return HasInheritedProperty(name);
            }
            else if (Dict.ContainsKey(name))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsSubTypeOf(Prototype proto)
        {
            if (proto == this)
            {
                return true;
            }

            foreach (var parent in Parents)
            {
                if (parent.IsSubTypeOf(proto))
                {
                    return true;
                }
            }

            return false;
        }

        [Lisp("merge")]
        public static Prototype Merge(params Prototype[] prototypes)
        {
            var dict = new PrototypeDictionary();
            foreach (var p in prototypes)
            {
                if (p != null)
                {
                    p.MergeInto(dict);
                }
            }
            return FromDictionary(dict);
        }

        public void MergeInto(PrototypeDictionary dict)
        {
            foreach (var item in Dict)
            {
                if (!dict.ContainsKey(item.Key))
                {
                    dict[item.Key] = item.Value;
                }
            }

            foreach (var parent in Parents)
            {
                parent.MergeInto(dict);
            }
        }

        public void ProcessTypeArg(object a)
        {
            if (a is Prototype)
            {
                AddParent((Prototype)a);
            }
            else if (a is Symbol)
            {
                var type = Runtime.GetType((Symbol)a) as Prototype;
                if (type == null)
                {
                    throw new LispException("Type not found or not a prototype/structure: {0}", a);
                }
                AddParent(type);
            }
            else
            {
                throw new LispException("Invalid type specifier in prototype constructor: {0}", a);
            }
        }

        [Lisp]
        public void SetParents(IEnumerable parents)
        {
            var newlist = new List<Prototype>();
            foreach (Prototype p in Runtime.ToIter(parents))
            {
                if (p != null)
                {
                    newlist.Add(p);
                }
            }
            Parents = newlist;
        }

        public object SetValue(object name, object value)
        {
            Dict[GetKey(name)] = value;
            return value;
        }

        #endregion Public Methods
    }

    public class PrototypeDictionary : Dictionary<object, object>
    {
        #region Constructors

        public PrototypeDictionary()
        {
        }

        public PrototypeDictionary(bool caseInsensitive)
            : base(caseInsensitive ? new CaseInsensitiveEqualityComparer() : null)
        {
        }

        #endregion Constructors
    }

    public class PrototypeMetaObject : GenericApplyMetaObject<Prototype>
    {
        #region Fields

        public Prototype Proto;

        #endregion Fields

        #region Constructors

        public PrototypeMetaObject(Expression parameter, Prototype proto)
            : base(parameter, proto)
        {
            this.Proto = proto;
        }

        #endregion Constructors

        #region Public Methods

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            // Handles (attr obj 'member)
            MethodInfo method = typeof(Prototype).GetMethod("GetValue");
            var index = Expression.Constant(binder.Name);
            var expr = Expression.Call(Expression.Convert(this.Expression, typeof(Prototype)), method, index);
            var restrictions = BindingRestrictions.GetTypeRestriction(this.Expression, typeof(Prototype));
            return new DynamicMetaObject(Runtime.EnsureObjectResult(expr), restrictions);
        }

        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            // Handles (.member obj)
            MethodInfo method = typeof(Prototype).GetMethod("GetValue");
            var index = Expression.Constant(binder.Name);
            var expr = Expression.Call(Expression.Convert(this.Expression, typeof(Prototype)), method, index);
            var restrictions = BindingRestrictions.GetTypeRestriction(this.Expression, typeof(Prototype));
            return new DynamicMetaObject(Runtime.EnsureObjectResult(expr), restrictions);
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            MethodInfo method = typeof(Prototype).GetMethod("SetValue");
            var index = Expression.Constant(binder.Name);
            var expr = Expression.Call(Expression.Convert(this.Expression, typeof(Prototype)), method, index, value.Expression);
            var restrictions = BindingRestrictions.GetTypeRestriction(this.Expression, typeof(Prototype));
            return new DynamicMetaObject(Runtime.EnsureObjectResult(expr), restrictions);
        }

        #endregion Public Methods
    }
}