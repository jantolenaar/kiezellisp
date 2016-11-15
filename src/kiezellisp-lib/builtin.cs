#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    public class AccessorLambda : IDynamicMetaObjectProvider, IApply
    {
        #region Fields

        public string Members;
        public bool Nullable;
        public Func<object> Proc0;
        public Func<object, object> Proc1;
        public Func<object, object, object> Proc2;
        public Func<object, object, object, object> Proc3;
        public Func<object, object, object, object, object> Proc4;
        public Func<object, object, object, object, object, object> Proc5;
        public Func<object, object, object, object, object, object, object> Proc6;

        #endregion Fields

        #region Constructors

        public AccessorLambda(bool nullable, string members)
        {
            Members = members;
            Nullable = nullable;
        }

        #endregion Constructors

        #region Methods

        object IApply.Apply(object[] args)
        {
            if (args.Length > 6)
            {
                var args2 = args.Select(x => (Expression)Expression.Constant(x)).ToArray();
                var expr = AccessorLambdaMetaObject.MakeExpression(Nullable, Members, args2);
                var proc = Runtime.CompileToFunction(expr);
                var val = proc();
                return val;
            }
            else
            {
                switch (args.Length)
                {
                    case 0:
                    {
                        if (Proc0 == null)
                        {
                            Proc0 = (Func<object>)MakeExpressionProc(0);
                        }
                        return Proc0();
                    }
                    case 1:
                    {
                        if (Proc1 == null)
                        {
                            Proc1 = (Func<object, object>)MakeExpressionProc(1);
                        }
                        return Proc1(args[0]);
                    }
                    case 2:
                    {
                        if (Proc2 == null)
                        {
                            Proc2 = (Func<object, object, object>)MakeExpressionProc(2);
                        }
                        return Proc2(args[0], args[1]);
                    }
                    case 3:
                    {
                        if (Proc3 == null)
                        {
                            Proc3 = (Func<object, object, object, object>)MakeExpressionProc(3);
                        }
                        return Proc3(args[0], args[1], args[2]);
                    }
                    case 4:
                    {
                        if (Proc4 == null)
                        {
                            Proc4 = (Func<object, object, object, object, object>)MakeExpressionProc(4);
                        }
                        return Proc4(args[0], args[1], args[2], args[3]);
                    }
                    case 5:
                    {
                        if (Proc5 == null)
                        {
                            Proc5 = (Func<object, object, object, object, object, object>)MakeExpressionProc(1);
                        }
                        return Proc5(args[0], args[1], args[2], args[3], args[4]);
                    }
                    case 6:
                    {
                        if (Proc6 == null)
                        {
                            Proc6 = (Func<object, object, object, object, object, object, object>)MakeExpressionProc(6);
                        }
                        return Proc6(args[0], args[1], args[2], args[3], args[4], args[5]);
                    }
                    default:
                    {
                        throw new NotImplementedException("Apply supports up to 6 arguments");
                    }
                }
            }
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new AccessorLambdaMetaObject(parameter, this);
        }

        public Delegate MakeExpressionProc(int argCount)
        {
            var args = new ParameterExpression[ argCount ];
            for (var i = 0; i < argCount; ++i)
            {
                args[i] = Expression.Parameter(typeof(object));
            }
            var code = AccessorLambdaMetaObject.MakeExpression(Nullable, Members, args);
            var proc = Runtime.CompileToDelegate(code, args);
            return proc;
        }

        public override string ToString()
        {
            return String.Format("AccessorLambda Name=\"{0}\" Nullable=\"{1}\"", Members, Nullable);
        }

        #endregion Methods
    }

    public class AccessorLambdaMetaObject : DynamicMetaObject
    {
        #region Fields

        public AccessorLambda Lambda;

        #endregion Fields

        #region Constructors

        public AccessorLambdaMetaObject(Expression parameter, AccessorLambda lambda)
            : base(parameter, BindingRestrictions.Empty, lambda)
        {
            this.Lambda = lambda;
        }

        #endregion Constructors

        #region Methods

        public static Expression MakeExpression(bool nullable, string members, Expression[] args)
        {
            // Warning: modifies args

            if (args.Length == 0)
            {
                throw new LispException("Member accessor invoked without a target: {0}", members);
            }

            var names = members.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            var code = args[0];

            if (nullable)
            {
                var temp = Expression.Parameter(typeof(object));

                for (var i = 0; i < names.Length; ++i)
                {
                    Expression code2;

                    if (i < names.Length - 1)
                    {
                        var binder = Runtime.GetInvokeMemberBinder(new InvokeMemberBinderKey(names[i], 0));
                        code2 = Runtime.CompileDynamicExpression(binder, typeof(object), new Expression[] { code });
                    }
                    else
                    {
                        var binder = Runtime.GetInvokeMemberBinder(new InvokeMemberBinderKey(names[i], args.Length - 1));
                        args[0] = code;
                        code2 = Runtime.CompileDynamicExpression(binder, typeof(object), args);
                    }

                    code = Expression.Condition(Runtime.WrapBooleanTest(Expression.Assign(temp, code)), code2, Expression.Constant(null));
                }

                code = Expression.Block(typeof(object), new ParameterExpression[] { temp }, code);
            }
            else
            {
                for (var i = 0; i < names.Length; ++i)
                {
                    if (i < names.Length - 1)
                    {
                        var binder = Runtime.GetInvokeMemberBinder(new InvokeMemberBinderKey(names[i], 0));
                        code = Runtime.CompileDynamicExpression(binder, typeof(object), new Expression[] { code });
                    }
                    else
                    {
                        var binder = Runtime.GetInvokeMemberBinder(new InvokeMemberBinderKey(names[i], args.Length - 1));
                        args[0] = code;
                        code = Runtime.CompileDynamicExpression(binder, typeof(object), args);
                    }
                }
            }

            return code;
        }

        public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
        {
            var args2 = args.Select(x => x.Expression).ToArray();
            var code = MakeExpression(false, Lambda.Members, args2);
            var restrictions = BindingRestrictions.GetInstanceRestriction(this.Expression, this.Value);
            return new DynamicMetaObject(code, restrictions);
        }

        #endregion Methods
    }

    public class ImportedConstructor : IDynamicMetaObjectProvider, IApply
    {
        #region Fields

        public Type DeclaringType;
        public ConstructorInfo[] Members;
        public dynamic Proc0;
        public dynamic Proc1;
        public dynamic Proc10;
        public dynamic Proc11;
        public dynamic Proc12;
        public dynamic Proc2;
        public dynamic Proc3;
        public dynamic Proc4;
        public dynamic Proc5;
        public dynamic Proc6;
        public dynamic Proc7;
        public dynamic Proc8;
        public dynamic Proc9;

        #endregion Fields

        #region Constructors

        public ImportedConstructor(ConstructorInfo[] members)
        {
            DeclaringType = ((ConstructorInfo)members[0]).DeclaringType;
            Members = members;
            Proc0 = this;
            Proc1 = this;
            Proc2 = this;
            Proc3 = this;
            Proc4 = this;
            Proc5 = this;
            Proc6 = this;
            Proc7 = this;
            Proc8 = this;
            Proc9 = this;
            Proc10 = this;
            Proc11 = this;
            Proc12 = this;
        }

        #endregion Constructors

        #region Properties

        public string FullName
        {
            get
            {
                return String.Format("{0}.{1}", DeclaringType, DeclaringType.Name);
            }
        }

        public bool HasKiezelMethods
        {
            get
            {
                return Members.Any(x => x.DeclaringType.FullName.IndexOf("Kiezel") != -1);
            }
        }

        #endregion Properties

        #region Methods

        object IApply.Apply(object[] args)
        {
            if (args.Length > 12)
            {
                var binder = Runtime.GetInvokeBinder(args.Length);
                var exprs = new List<Expression>();
                exprs.Add(Expression.Constant(this));
                exprs.AddRange(args.Select(x => Expression.Constant(x)));
                var code = Runtime.CompileDynamicExpression(binder, typeof(object), exprs);
                var proc = Runtime.CompileToFunction(code);
                return proc();
            }
            else
            {
                switch (args.Length)
                {
                    case 0:
                    {
                        return Proc0();
                    }
                    case 1:
                    {
                        return Proc1(args[0]);
                    }
                    case 2:
                    {
                        return Proc2(args[0], args[1]);
                    }
                    case 3:
                    {
                        return Proc3(args[0], args[1], args[2]);
                    }
                    case 4:
                    {
                        return Proc4(args[0], args[1], args[2], args[3]);
                    }
                    case 5:
                    {
                        return Proc5(args[0], args[1], args[2], args[3], args[4]);
                    }
                    case 6:
                    {
                        return Proc6(args[0], args[1], args[2], args[3], args[4], args[5]);
                    }
                    case 7:
                    {
                        return Proc7(args[0], args[1], args[2], args[3], args[4], args[5], args[6]);
                    }
                    case 8:
                    {
                        return Proc8(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]);
                    }
                    case 9:
                    {
                        return Proc9(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8]);
                    }
                    case 10:
                    {
                        return Proc9(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9]);
                    }
                    case 11:
                    {
                        return Proc9(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10]);
                    }
                    case 12:
                    {
                        return Proc9(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11]);
                    }
                    default:
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new ImportedConstructorMetaObject(parameter, this);
        }

        public override string ToString()
        {
            return String.Format("BuiltinConstructor Method=\"{0}.{1}\"", Members[0].DeclaringType, Members[0].Name);
        }

        #endregion Methods
    }

    public class ImportedConstructorMetaObject : DynamicMetaObject
    {
        #region Fields

        public ImportedConstructor runtimeModel;

        #endregion Fields

        #region Constructors

        public ImportedConstructorMetaObject(Expression objParam, ImportedConstructor runtimeModel)
            : base(objParam, BindingRestrictions.Empty, runtimeModel)
        {
            this.runtimeModel = runtimeModel;
        }

        #endregion Constructors

        #region Methods

        public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
        {
            bool createdParamArray;
            var ctors = new List<CandidateMethod<ConstructorInfo>>();

            foreach (ConstructorInfo m in runtimeModel.Members)
            {
                if (m.IsStatic)
                {
                    continue;
                }

                if (Runtime.ParametersMatchArguments(m.GetParameters(), args, out createdParamArray))
                {
                    Runtime.InsertInMostSpecificOrder(ctors, m, createdParamArray);
                }
            }

            if (ctors.Count == 0)
            {
                throw new MissingMemberException("No (suitable) constructor found: new " + runtimeModel.FullName + Runtime.CollectParameterInfo(args));
            }

            var ctor = ctors[0].Method;
            var restrictions = Runtime.GetTargetArgsRestrictions(this, args, true);
            var callArgs = Runtime.ConvertArguments(args, ctor.GetParameters());

            return new DynamicMetaObject(Runtime.EnsureObjectResult(Expression.New(ctor, callArgs)), restrictions);
        }

        #endregion Methods

        #region Other

        //public override DynamicMetaObject BindConvert( ConvertBinder binder )
        //{
        //    var expr = Expression.Constant( RuntimeHelpers.CreateDelegate( runtimeModel.Runtime, runtimeModel.Members[ 0 ] ) );
        //    return new DynamicMetaObject( expr, this.Restrictions );
        //}

        #endregion Other
    }

    public class ImportedFunction : IDynamicMetaObjectProvider, IApply, ISyntax
    {
        #region Fields

        public MethodInfo[] BuiltinExtensionMembers;
        public Type DeclaringType;
        public MethodInfo[] ExternalExtensionMembers;
        public MethodInfo[] Members;
        public string Name;
        public dynamic Proc0;
        public dynamic Proc1;
        public dynamic Proc10;
        public dynamic Proc11;
        public dynamic Proc12;
        public dynamic Proc2;
        public dynamic Proc3;
        public dynamic Proc4;
        public dynamic Proc5;
        public dynamic Proc6;
        public dynamic Proc7;
        public dynamic Proc8;
        public dynamic Proc9;
        public bool Pure;

        #endregion Fields

        #region Constructors

        public ImportedFunction(string name, Type declaringType)
        {
            Init();
            Name = name;
            DeclaringType = declaringType;
            BuiltinExtensionMembers = new MethodInfo[ 0 ];
            Members = new MethodInfo[ 0 ];
            ExternalExtensionMembers = new MethodInfo[ 0 ];
            Pure = false;
        }

        public ImportedFunction(string name, Type declaringType, MethodInfo[] members, bool pure)
            : this(name, declaringType)
        {
            Members = members;
            Pure = pure;
        }

        #endregion Constructors

        #region Properties

        public string FullName
        {
            get
            {
                return String.Format("{0}.{1}", DeclaringType, Name);
            }
        }

        public bool HasKiezelMethods
        {
            get
            {
                return Members.Any(x => x.DeclaringType.FullName.IndexOf("Kiezel") != -1);
            }
        }

        #endregion Properties

        #region Methods

        object IApply.Apply(object[] args)
        {
            if (args.Length > 12)
            {
                var binder = Runtime.GetInvokeBinder(args.Length);
                var exprs = new List<Expression>();
                exprs.Add(Expression.Constant(this));
                exprs.AddRange(args.Select(Expression.Constant));
                var code = Runtime.CompileDynamicExpression(binder, typeof(object), exprs);
                var proc = Runtime.CompileToFunction(code);
                return proc();
            }
            else
            {
                switch (args.Length)
                {
                    case 0:
                    {
                        return Proc0();
                    }
                    case 1:
                    {
                        return Proc1(args[0]);
                    }
                    case 2:
                    {
                        return Proc2(args[0], args[1]);
                    }
                    case 3:
                    {
                        return Proc3(args[0], args[1], args[2]);
                    }
                    case 4:
                    {
                        return Proc4(args[0], args[1], args[2], args[3]);
                    }
                    case 5:
                    {
                        return Proc5(args[0], args[1], args[2], args[3], args[4]);
                    }
                    case 6:
                    {
                        return Proc6(args[0], args[1], args[2], args[3], args[4], args[5]);
                    }
                    case 7:
                    {
                        return Proc7(args[0], args[1], args[2], args[3], args[4], args[5], args[6]);
                    }
                    case 8:
                    {
                        return Proc8(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]);
                    }
                    case 9:
                    {
                        return Proc9(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8]);
                    }
                    case 10:
                    {
                        return Proc9(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9]);
                    }
                    case 11:
                    {
                        return Proc9(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10]);
                    }
                    case 12:
                    {
                        return Proc9(args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7], args[8], args[9], args[10], args[11]);
                    }
                    default:
                    {
                        throw new NotImplementedException("Apply supports up to 12 arguments");
                    }
                }
            }
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
        {
            return new ImportedFunctionMetaObject(parameter, this);
        }

        public void Init()
        {
            Proc0 = this;
            Proc1 = this;
            Proc2 = this;
            Proc3 = this;
            Proc4 = this;
            Proc5 = this;
            Proc6 = this;
            Proc7 = this;
            Proc8 = this;
            Proc9 = this;
            Proc10 = this;
            Proc11 = this;
            Proc12 = this;
        }

        public bool IsProperOrExtensionInstanceMethod(MethodInfo method)
        {
            if (!method.IsStatic)
            {
                return true;
            }
            if (method.GetCustomAttributes(typeof(ExtensionAttribute), true).Length != 0)
            {
                return true;
            }
            return false;
        }

        Cons ISyntax.GetSyntax(Symbol context)
        {
            var v = new Vector();
            foreach (var m in Members)
            {
                v.Add(Runtime.GetMethodSyntax(m, context));
            }
            return Runtime.AsList(Runtime.SeqBase.Distinct(v, Runtime.StructurallyEqualApply));
        }

        public override string ToString()
        {
            return String.Format("Function Name=\"{0}.{1}\"", DeclaringType, Name);
        }

        public bool TryBindInvokeBestInstanceMethod(bool restrictionOnTargetInstance, DynamicMetaObject target, DynamicMetaObject argsFirst, DynamicMetaObject[] argsRest, out DynamicMetaObject result)
        {
            return TryBindInvokeBestMethod(true, restrictionOnTargetInstance, target, null, argsFirst, argsRest, out result);
        }

        public bool TryBindInvokeBestMethod(bool restrictionOnTargetInstance, DynamicMetaObject target, DynamicMetaObject[] args, out DynamicMetaObject result)
        {
            DynamicMetaObject argsFirst = null;
            DynamicMetaObject[] argsRest = null;
            return TryBindInvokeBestMethod(false, restrictionOnTargetInstance, target, args, argsFirst, argsRest, out result);
        }

        public bool TryBindInvokeBestMethod(bool instanceMethodsOnly, bool restrictionOnTargetInstance, DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject argsFirst, DynamicMetaObject[] argsRest, out DynamicMetaObject result)
        {
            bool createdParamArray;
            var candidates = new List<CandidateMethod<MethodInfo>>();
            args = args ?? Runtime.GetCombinedTargetArgs(argsFirst, argsRest);

            foreach (MethodInfo m in BuiltinExtensionMembers)
            {
                if (instanceMethodsOnly && !IsProperOrExtensionInstanceMethod(m))
                {
                    continue;
                }

                if (m.IsStatic)
                {
                    if (Runtime.ParametersMatchArguments(m.GetParameters(), args, out createdParamArray))
                    {
                        Runtime.InsertInMostSpecificOrder(candidates, m, createdParamArray);
                    }
                }
                else
                {
                    if (argsRest == null)
                    {
                        Runtime.SplitCombinedTargetArgs(args, out argsFirst, out argsRest);
                    }

                    if (argsRest != null && Runtime.ParametersMatchArguments(m.GetParameters(), argsRest, out createdParamArray))
                    {
                        Runtime.InsertInMostSpecificOrder(candidates, m, createdParamArray);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                foreach (MethodInfo m in Members)
                {
                    if (instanceMethodsOnly && !IsProperOrExtensionInstanceMethod(m))
                    {
                        continue;
                    }

                    if (m.IsStatic)
                    {
                        if (Runtime.ParametersMatchArguments(m.GetParameters(), args, out createdParamArray))
                        {
                            Runtime.InsertInMostSpecificOrder(candidates, m, createdParamArray);
                        }
                    }
                    else
                    {
                        if (argsRest == null)
                        {
                            Runtime.SplitCombinedTargetArgs(args, out argsFirst, out argsRest);
                        }

                        if (argsRest != null && Runtime.ParametersMatchArguments(m.GetParameters(), argsRest, out createdParamArray))
                        {
                            Runtime.InsertInMostSpecificOrder(candidates, m, createdParamArray);
                        }
                    }
                }
            }

            if (candidates.Count == 0)
            {
                foreach (MethodInfo m in ExternalExtensionMembers)
                {
                    if (instanceMethodsOnly && !IsProperOrExtensionInstanceMethod(m))
                    {
                        continue;
                    }

                    if (m.IsStatic)
                    {
                        if (Runtime.ParametersMatchArguments(m.GetParameters(), args, out createdParamArray))
                        {
                            Runtime.InsertInMostSpecificOrder(candidates, m, createdParamArray);
                        }
                    }
                    else
                    {
                        if (argsRest == null)
                        {
                            Runtime.SplitCombinedTargetArgs(args, out argsFirst, out argsRest);
                        }

                        if (argsRest != null && Runtime.ParametersMatchArguments(m.GetParameters(), argsRest, out createdParamArray))
                        {
                            Runtime.InsertInMostSpecificOrder(candidates, m, createdParamArray);
                        }
                    }
                }
            }

            if (candidates.Count == 0)
            {
                result = null;
                return false;
            }

            var method = candidates[0].Method;

            if (method.IsStatic)
            {
                var restrictions = Runtime.GetTargetArgsRestrictions(target, args, true);
                var callArgs = Runtime.ConvertArguments(args, method.GetParameters());
                result = new DynamicMetaObject(Runtime.EnsureObjectResult(Expression.Call(method, callArgs)), restrictions);
            }
            else
            {
                if (argsRest == null)
                {
                    Runtime.SplitCombinedTargetArgs(args, out argsFirst, out argsRest);
                }

                // When called from FallbackInvokeMember we want to restrict on the type.
                var restrictions = Runtime.GetTargetArgsRestrictions(target, argsRest, restrictionOnTargetInstance);
                var targetInst = Expression.Convert(argsFirst.Expression, method.DeclaringType);
                var callArgs = Runtime.ConvertArguments(argsRest, method.GetParameters());
                result = new DynamicMetaObject(Runtime.EnsureObjectResult(Expression.Call(targetInst, method, callArgs)), restrictions);
            }

            return true;
        }

        #endregion Methods
    }

    public class ImportedFunctionMetaObject : DynamicMetaObject
    {
        #region Fields

        public ImportedFunction runtimeModel;

        #endregion Fields

        #region Constructors

        public ImportedFunctionMetaObject(Expression objParam, ImportedFunction runtimeModel)
            : base(objParam, BindingRestrictions.Empty, runtimeModel)
        {
            this.runtimeModel = runtimeModel;
        }

        #endregion Constructors

        #region Methods

        public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
        {
            DynamicMetaObject result;
            if (!runtimeModel.TryBindInvokeBestMethod(true, this, args, out result))
            {
                throw new MissingMemberException("No (suitable) method found: " + runtimeModel.FullName + Runtime.CollectParameterInfo(args));
            }
            return result;
        }

        #endregion Methods

        #region Other

        //public override DynamicMetaObject BindConvert( ConvertBinder binder )
        //{
        //    var expr = Expression.Constant( RuntimeHelpers.CreateDelegate( runtimeModel.Runtime, runtimeModel.Members[ 0 ] ) );
        //    return new DynamicMetaObject( expr, this.Restrictions );
        //}

        #endregion Other
    }
}