// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Kiezel
{
    public enum CompareClassResult
    {
        NotComparable,
        Equal,
        Less,
        Greater
    }

    public class MultiMethod : IDynamicMetaObjectProvider, IApply, ISyntax
    {
        public List<LambdaClosure> Lambdas = new List<LambdaClosure>();
        public int RequiredArgsCount;
        public LambdaSignature Signature;

        public MultiMethod(LambdaSignature signature)
        {
            Signature = signature;
            RequiredArgsCount = signature.RequiredArgsCount;
        }

        public void Add(LambdaClosure method)
        {
            method.Owner = this;

            var comparer = new LambdaSignatureComparer();

            for (int i = 0; i < Lambdas.Count; ++i)
            {
                int result = comparer.Compare(method.Definition.Signature, Lambdas[i].Definition.Signature);

                if (result == -1)
                {
                    Lambdas.Insert(i, method);
                    return;
                }
                else if (result == 0)
                {
                    Lambdas[i] = method;
                    return;
                }
            }

            Lambdas.Add(method);
        }

        object IApply.Apply(object[] args)
        {
            var methods = Match(args);
            if (methods == null)
            {
                throw new LispException("No matching multi-method found");
            }
            var method = (LambdaClosure)methods.Car;
            var newargs = method.MakeArgumentFrame(args, null, null);
            return Runtime.CallNextMethod(methods, newargs);
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new MultiMethodApplyMetaObject(parameter, this);
        }

        Cons ISyntax.GetSyntax(Symbol context)
        {
            return Runtime.AsList(Lambdas.Select(Runtime.GetSyntax).Distinct());
        }

        public Cons Match(object[] args)
        {
            return Runtime.AsList(Lambdas.Where(x => x.Definition.Signature.ParametersMatchArguments(args)));
        }

        public object ApplyNext(LambdaClosure current, object[] args)
        {
            foreach (var lambda in Lambdas)
            {
                if (current != null)
                {
                    if (lambda == current)
                    {
                        current = null;
                    }
                }
                else if (lambda.Definition.Signature.ParametersMatchArguments(args))
                {
                    return ((IApply)lambda).Apply(args);
                }
            }

            return null;
        }

        //public override bool TryInvoke( InvokeBinder binder, object[] args, out object result )
        //{
        //    result = ( ( IApply ) this ).Apply( args );
        //    return true;
        //}
    }

    public partial class Runtime
    {
        [Lisp("system:call-next-method")]
        public static object CallNextMethod(Cons nextLambdas, object[] args)
        {
            if (nextLambdas == null)
            {
                return null;
            }
            var lambda = (LambdaClosure)nextLambdas.Car;
            return lambda.ApplyLambdaBind(nextLambdas.Cdr, args, true, null, null);
        }

        public static CompareClassResult CompareClass(object left, object right)
        {
            //
            // The most specialized class is the 'smallest'.
            //

            if (left == null)
            {
                if (right == null)
                {
                    // no specializer
                    return CompareClassResult.Equal;
                }
                else
                {
                    // rhs is more specialized
                    return CompareClassResult.Greater;
                }
            }
            else if (right == null)
            {
                // lhs is more specialized
                return CompareClassResult.Less;
            }
            else if (left is EqlSpecializer && right is EqlSpecializer)
            {
                if (Eql(((EqlSpecializer)left).Value, ((EqlSpecializer)right).Value))
                {
                    return CompareClassResult.Equal;
                }
                else
                {
                    return CompareClassResult.NotComparable;
                }
            }
            else if (left is EqlSpecializer)
            {
                // eql sorts before type
                return CompareClassResult.Less;
            }
            else if (right is EqlSpecializer)
            {
                // eql sorts before type
                return CompareClassResult.Greater;
            }
            else if (left is Type && right is Type)
            {
                var c1 = (Type)left;
                var c2 = (Type)right;

                if (c1 == c2)
                {
                    return CompareClassResult.Equal;
                }
                else if (c1.IsSubclassOf(c2))
                {
                    return CompareClassResult.Less;
                }
                else if (c2.IsSubclassOf(c2))
                {
                    return CompareClassResult.Greater;
                }
                else
                {
                    return CompareClassResult.NotComparable;
                }
            }
            else if (left is Prototype && right is Prototype)
            {
                var c1 = (Prototype)left;
                var c2 = (Prototype)right;

                if (c1 == c2)
                {
                    return CompareClassResult.Equal;
                }
                else if (c1.IsSubTypeOf(c2))
                {
                    return CompareClassResult.Less;
                }
                else if (c2.IsSubTypeOf(c2))
                {
                    return CompareClassResult.Greater;
                }
                else
                {
                    return CompareClassResult.NotComparable;
                }
            }
            else
            {
                return CompareClassResult.NotComparable;
            }
        }

        public static LambdaClosure DefineMethod(Symbol sym, LambdaClosure lambda)
        {
            var container = sym.Value as MultiMethod;

            if (container == null)
            {
                container = DefineMultiMethod(sym, lambda.Definition.Signature, null);
            }
            else if (container.RequiredArgsCount != 0 && lambda.Definition.Signature.RequiredArgsCount != container.RequiredArgsCount)
            {
                throw new LispException("Number of parameters of {0} and its multimethod are not the same", ToPrintString(lambda));
            }
            container.Add(lambda);
            return lambda;
        }

        public static MultiMethod DefineMultiMethod(Symbol sym, LambdaSignature signature, string doc)
        {
            var func = new MultiMethod(signature);
            sym.FunctionValue = func;
            sym.Documentation = doc;
            return func;
        }
    }

    public class EqlSpecializer
    {
        public object Value;

        public EqlSpecializer(object value)
        {
            Value = value;
        }
    }

    public class LambdaSignatureComparer : IComparer<LambdaSignature>
    {
        // if x more specific than y then return -1
        public int Compare(LambdaSignature x, LambdaSignature y)
        {
            if (x.RequiredArgsCount > y.RequiredArgsCount)
            {
                // more arguments is more specific
                return -1;
            }

            if (x.RequiredArgsCount < y.RequiredArgsCount)
            {
                // more arguments is more specific
                return 1;
            }

            for (int i = 0; i < x.RequiredArgsCount; ++i)
            {
                var type1 = x.Parameters[i].Specializer;
                var type2 = y.Parameters[i].Specializer;

                var result = Runtime.CompareClass(type1, type2);

                switch (result)
                {
                    case CompareClassResult.NotComparable:
                    {
                        // implies both are not null
                        // anything will do but the value must be reproducible.
                        return Runtime.Compare(type1.GetHashCode(), type2.GetHashCode());
                    }
                    case CompareClassResult.Less:
                    {
                        // more specific must be on top
                        return -1;
                    }
                    case CompareClassResult.Greater:
                    {
                        // less specific must be on bottom
                        return 1;
                    }
                    case CompareClassResult.Equal:
                    default:
                    {
                        // next slot
                        break;
                    }
                }
            }

            return 0;
        }
    }

    public class MultiMethodApplyMetaObject : DynamicMetaObject
    {
        public MultiMethod Generic;

        public MultiMethodApplyMetaObject(Expression objParam, MultiMethod generic)
            : base(objParam, BindingRestrictions.Empty, generic)
        {
            this.Generic = generic;
        }

        public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
        {
            var methods = Match(args);
            if (methods == null)
            {
                throw new LispException("No matching multi-method found");
            }
            var lambda = (LambdaClosure)methods.Car;
            if (lambda.GenericRestrictions == null)
            {
                lambda.GenericRestrictions = LambdaHelpers.GetGenericRestrictions(lambda, args);
            }
            var restrictions = lambda.GenericRestrictions;
            var callArgs = LambdaHelpers.FillDataFrame(lambda.Definition.Signature, args, ref restrictions);
            MethodInfo method = Runtime.RuntimeMethod("CallNextMethod");
            var expr = Expression.Call(method, Expression.Constant(methods), callArgs);
            restrictions = BindingRestrictions.GetInstanceRestriction(this.Expression, this.Value).Merge(restrictions);
            return new DynamicMetaObject(Runtime.EnsureObjectResult(expr), restrictions);
        }

        public Cons Match(DynamicMetaObject[] args)
        {
            return Runtime.AsList(Generic.Lambdas.Where(x => x.Definition.Signature.ParametersMatchArguments(args)));
        }
    }
}