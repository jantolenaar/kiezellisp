#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq.Expressions;

    #region Enumerations

    public enum Modifier
    {
        None = 0,
        EqlSpecializer = 1,
        TypeSpecializer = 2
    }

    #endregion Enumerations

    public class LambdaSignature
    {
        #region Fields

        public Symbol ArgModifier;
        public Symbol EnvArg;
        public List<ParameterDef> flattenedParameters;
        public LambdaKind Kind;
        public List<Symbol> Names = new List<Symbol>();
        public List<ParameterDef> Parameters = new List<ParameterDef>();
        public int RequiredArgsCount;
        public Symbol WholeArg;

        #endregion Fields

        #region Constructors

        public LambdaSignature(LambdaKind kind)
        {
            Kind = kind;
        }

        #endregion Constructors

        #region Public Properties

        public List<ParameterDef> FlattenedParameters
        {
            get
            {
                if (flattenedParameters == null)
                {
                    flattenedParameters = new List<ParameterDef>();
                    FlattenParameters(ref flattenedParameters);
                }

                return flattenedParameters;
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void FlattenParameters(ref List<ParameterDef> output)
        {
            foreach (var parameter in Parameters)
            {
                if (parameter.NestedParameters == null)
                {
                    output.Add(parameter);
                }
                else {
                    parameter.NestedParameters.FlattenParameters(ref output);
                }
            }
        }

        public bool ParametersMatchArguments(object[] args)
        {
            int i = 0;

            foreach (ParameterDef param in Parameters)
            {
                if (i >= RequiredArgsCount)
                {
                    return true;
                }

                if (param.HasEqlSpecializer)
                {
                    bool result = Runtime.Eql(args[i], param.EqlSpecializer);
                    if (!result)
                    {
                        return false;
                    }
                }
                else if (param.HasTypeSpecializer)
                {
                    bool result = Runtime.ToBool(Runtime.IsInstanceOf(args[i], param.TypeSpecializer));
                    if (!result)
                    {
                        return false;
                    }
                }

                ++i;
            }

            return true;
        }

        public bool ParametersMatchArguments(DynamicMetaObject[] args)
        {
            int i = 0;

            foreach (ParameterDef param in Parameters)
            {
                if (i >= RequiredArgsCount)
                {
                    return true;
                }

                if (param.HasEqlSpecializer)
                {
                    bool result = Runtime.Eql(args[i].Value, param.EqlSpecializer);
                    if (!result)
                    {
                        return false;
                    }
                }
                else if (param.HasTypeSpecializer)
                {
                    bool result = Runtime.ToBool(Runtime.IsInstanceOf(args[i].Value, param.TypeSpecializer));
                    if (!result)
                    {
                        return false;
                    }
                }

                ++i;
            }

            return true;
        }

        #endregion Public Methods
    }

    public class ParameterDef
    {
        #region Fields

        public bool Hidden;
        public Expression InitForm;
        public Func<object> InitFormProc;
        public LambdaSignature NestedParameters;
        public object Specializer;
        public Symbol Sym;

        #endregion Fields

        #region Constructors

        public ParameterDef(Symbol sym, object specializer = null,
            Expression initForm = null,
            Func<object> initFormProc = null,
            LambdaSignature nestedParameters = null,
            bool hidden = false)
        {
            Sym = sym;
            Specializer = specializer;
            NestedParameters = nestedParameters;
            InitForm = initForm;
            InitFormProc = initFormProc;
            Hidden = hidden;

            if (InitForm != null && InitFormProc == null)
            {
                InitFormProc = Runtime.CompileToFunction(initForm);
            }
        }

        #endregion Constructors

        #region Public Properties

        public object EqlSpecializer
        {
            get
            {
                if (HasEqlSpecializer)
                {
                    return ((EqlSpecializer)Specializer).Value;
                }
                else {
                    return null;
                }
            }
        }

        public bool HasEqlSpecializer
        {
            get
            {
                return Specializer != null && Specializer is EqlSpecializer;
            }
        }

        public bool HasTypeSpecializer
        {
            get
            {
                return Specializer != null && !(Specializer is EqlSpecializer);
            }
        }

        public object TypeSpecializer
        {
            get
            {
                if (HasTypeSpecializer)
                {
                    return Specializer;
                }
                else {
                    return null;
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public override string ToString()
        {
            return string.Format("{0}", Sym);
        }

        #endregion Public Methods
    }
}