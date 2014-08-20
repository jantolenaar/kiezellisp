// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace Kiezel
{

    public class LambdaSignature
    {
        internal LambdaKind Kind;
        internal int RequiredArgsCount = 0;
        internal Symbol ArgModifier;
        internal List<ParameterDef> Parameters = new List<ParameterDef>();
        internal List<Symbol> Names = new List<Symbol>();
        internal List<ParameterDef> flattenedParameters = null;
        internal Symbol WholeArg = null;
        internal Symbol EnvArg = null;

        internal LambdaSignature( LambdaKind kind )
        {
            Kind = kind;
        }

        internal bool IsGetter()
        {
            return Kind == LambdaKind.Function && Parameters.Count == 1 && RequiredArgsCount == 1 && Parameters[ 0 ].Sym.Name == "this";
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


    internal enum Modifier
    {
        None = 0,
        EqlSpecializer = 1,
        TypeSpecializer = 2
    }

    internal class ParameterDef
    {
        internal Symbol Sym;
        internal object Specializer;
        internal LambdaSignature NestedParameters;
        internal Expression InitForm;
        internal Func<object> InitFormProc;
        internal bool Hidden;

        internal ParameterDef( Symbol sym,  object specializer = null, 
                                            Expression initForm = null, 
                                            Func<object> initFormProc = null,
                                            LambdaSignature nestedParameters = null,
                                            bool hidden = false     )
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

        internal bool HasEqlSpecializer
        {
            get
            {
                return Specializer != null && Specializer is EqlSpecializer;
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

