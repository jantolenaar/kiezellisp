// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace Kiezel
{
    internal enum Modifier
    {
        None = 0,
        EqlSpecializer = 1,
        TypeSpecializer = 2
    }

    public class LambdaSignature
    {
        internal Symbol ArgModifier;
        internal Symbol EnvArg = null;
        internal List<ParameterDef> flattenedParameters = null;
        internal LambdaKind Kind;
        internal List<Symbol> Names = new List<Symbol>();
        internal List<ParameterDef> Parameters = new List<ParameterDef>();
        internal int RequiredArgsCount = 0;
        internal Symbol WholeArg = null;
        internal LambdaSignature( LambdaKind kind )
        {
            Kind = kind;
        }

        internal List<ParameterDef> FlattenedParameters
        {
            get
            {
                if ( flattenedParameters == null )
                {
                    flattenedParameters = new List<ParameterDef>();
                    FlattenParameters( ref flattenedParameters );
                }

                return flattenedParameters;
            }
        }

        internal void FlattenParameters( ref List<ParameterDef> output )
        {
            foreach ( var parameter in Parameters )
            {
                if ( parameter.NestedParameters == null )
                {
                    output.Add( parameter );
                }
                else
                {
                    parameter.NestedParameters.FlattenParameters( ref output );
                }
            }
        }

        internal bool IsGetter()
        {
            return Kind == LambdaKind.Function && Parameters.Count == 1 && RequiredArgsCount == 1 && Parameters[ 0 ].Sym.Name == "this";
        }
        internal bool ParametersMatchArguments( object[] args )
        {
            int i = 0;

            foreach ( ParameterDef param in Parameters )
            {
                if ( i >= RequiredArgsCount )
                {
                    return true;
                }

                if ( param.HasEqlSpecializer )
                {
                    bool result = Runtime.Eql( args[ i ], param.EqlSpecializer );
                    if ( !result )
                    {
                        return false;
                    }
                }
                else if ( param.HasTypeSpecializer )
                {
                    bool result = Runtime.ToBool( Runtime.IsInstanceOf( args[ i ], param.TypeSpecializer ) );
                    if ( !result )
                    {
                        return false;
                    }
                }

                ++i;
            }

            return true;
        }

        internal bool ParametersMatchArguments( DynamicMetaObject[] args )
        {
            int i = 0;

            foreach ( ParameterDef param in Parameters )
            {
                if ( i >= RequiredArgsCount )
                {
                    return true;
                }

                if ( param.HasEqlSpecializer )
                {
                    bool result = Runtime.Eql( args[ i ].Value, param.EqlSpecializer );
                    if ( !result )
                    {
                        return false;
                    }
                }
                else if ( param.HasTypeSpecializer )
                {
                    bool result = Runtime.ToBool( Runtime.IsInstanceOf( args[ i ].Value, param.TypeSpecializer ) );
                    if ( !result )
                    {
                        return false;
                    }
                }

                ++i;
            }

            return true;
        }
    }
    internal class ParameterDef
    {
        internal bool Hidden;
        internal Expression InitForm;
        internal Func<object> InitFormProc;
        internal LambdaSignature NestedParameters;
        internal object Specializer;
        internal Symbol Sym;
        internal ParameterDef( Symbol sym, object specializer = null,
                                            Expression initForm = null,
                                            Func<object> initFormProc = null,
                                            LambdaSignature nestedParameters = null,
                                            bool hidden = false )
        {
            Sym = sym;
            Specializer = specializer;
            NestedParameters = nestedParameters;
            InitForm = initForm;
            InitFormProc = initFormProc;
            Hidden = hidden;

            if ( InitForm != null && InitFormProc == null )
            {
                InitFormProc = Runtime.CompileToFunction( initForm );
            }
        }

        internal object EqlSpecializer
        {
            get
            {
                if ( HasEqlSpecializer )
                {
                    return ( ( EqlSpecializer ) Specializer ).Value;
                }
                else
                {
                    return null;
                }
            }
        }

        internal bool HasEqlSpecializer
        {
            get
            {
                return Specializer != null && Specializer is EqlSpecializer;
            }
        }
        internal bool HasTypeSpecializer
        {
            get
            {
                return Specializer != null && !( Specializer is EqlSpecializer );
            }
        }

        internal object TypeSpecializer
        {
            get
            {
                if ( HasTypeSpecializer )
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
            return System.String.Format( "{0}", Sym );
        }
    }
}