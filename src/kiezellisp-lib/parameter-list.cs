// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace Kiezel
{
    public enum Modifier
    {
        None = 0,
        EqlSpecializer = 1,
        TypeSpecializer = 2
    }

    public class LambdaSignature
    {
        public Symbol ArgModifier;
        public Symbol EnvArg = null;
        public List<ParameterDef> flattenedParameters = null;
        public LambdaKind Kind;
        public List<Symbol> Names = new List<Symbol>();
        public List<ParameterDef> Parameters = new List<ParameterDef>();
        public int RequiredArgsCount = 0;
        public Symbol WholeArg = null;

        public LambdaSignature(LambdaKind kind)
        {
            Kind = kind;
        }

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

        public void FlattenParameters(ref List<ParameterDef> output)
        {
            foreach (var parameter in Parameters)
            {
                if (parameter.NestedParameters == null)
                {
                    output.Add(parameter);
                }
                else
                {
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
    }

    public class ParameterDef
    {
        public bool Hidden;
        public Expression InitForm;
        public Func<object> InitFormProc;
        public LambdaSignature NestedParameters;
        public object Specializer;
        public Symbol Sym;

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

        public object EqlSpecializer
        {
            get
            {
                if (HasEqlSpecializer)
                {
                    return ((EqlSpecializer)Specializer).Value;
                }
                else
                {
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
                else
                {
                    return null;
                }
            }
        }

        public override string ToString()
        {
            return System.String.Format("{0}", Sym);
        }
    }
}