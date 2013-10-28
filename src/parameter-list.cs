// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

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
        internal Modifier LastArgModifier;
        internal List<ParameterDef> Parameters = new List<ParameterDef>();
        internal List<Symbol> Names = new List<Symbol>();
        internal List<ParameterDef> flattenedParameters = null;

        internal LambdaSignature( LambdaKind kind )
        {
            Kind = kind;
        }

        internal bool IsGetter()
        {
            return Kind == LambdaKind.Function && Parameters.Count == 1 && RequiredArgsCount == 1 && Parameters[ 0 ].Sym.Name == "this";
        }

        internal LambdaSignature EvalSpecializers()
        {
            if ( Kind == LambdaKind.Function || Kind == LambdaKind.Method )
            {
                var result = new LambdaSignature( Kind );
                result.LastArgModifier = LastArgModifier;
                result.RequiredArgsCount = RequiredArgsCount;
                result.Parameters = Parameters.Select( x => x.EvalSpecializer() ).ToList();
                result.Names = Names;
                return result;
            }
            else
            {
                return this;
            }
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
                    bool result = Runtime.Eql( args[ i ], param.Specializer );
                    if ( !result )
                    {
                        return false;
                    }
                }
                else if ( param.HasTypeSpecializer )
                {
                    bool result = Runtime.ToBool( Runtime.InstanceOfp( args[ i ], param.Specializer ) );
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


    [Flags]
    [Serializable]
    internal enum Modifier
    {
        None = 0,
        EqlSpecializer = 1,
        TypeSpecializer = 2,
        MaskSpecializer = 3,
        Optional = 4,
        Key = 8,
        Params = 16,
        Rest = 32,
        Vector = 64,
        Whole = 128
    }

    internal class ParameterDef
    {
        internal Symbol Sym;
        internal object Specializer;
        internal LambdaSignature NestedParameters;
        internal Modifier Modifiers;
        internal object InitForm;
        internal Symbol Key;

        internal ParameterDef( Modifier modifiers, Symbol sym, object specializer, object initForm )
        {
            Modifiers = modifiers;
            Sym = sym;
            Specializer = specializer;
            if ( IsKey )
            {
                Key = Runtime.KeywordPackage.Intern( Sym.Name );
            }
            NestedParameters = null;
            InitForm = initForm;
            if ( initForm is Vector || initForm is Prototype || ( initForm is Cons && Runtime.First( initForm ) == Symbols.Quote ) )
            {
                Runtime.PrintWarning( "Bad style: using literals of type vector, prototype or list as default value." );
            }
        }

        internal ParameterDef( Modifier modifiers, LambdaSignature nestedArgs )
        {
            Modifiers = modifiers;
            Sym = null;
            Specializer = null;
            NestedParameters = nestedArgs;
            InitForm = null;
        }

        internal bool IsWhole
        {
            get
            {
                return ( Modifiers & Modifier.Whole ) != 0;
            }
        }

        internal bool IsRequired
        {
            get
            {
                return ( int ) Modifiers < 4;
            }
        }

        internal bool IsParams
        {
            get
            {
                return ( Modifiers & Modifier.Params ) != 0;
            }
        }

        internal bool IsVector
        {
            get
            {
                return ( Modifiers & Modifier.Vector ) != 0;
            }
        }

        internal bool IsRest
        {
            get
            {
                return ( Modifiers & Modifier.Rest ) != 0;
            }
        }

        internal bool IsKey
        {
            get
            {
                return ( Modifiers & Modifier.Key ) != 0;
            }
        }

        internal bool IsOptional
        {
            get
            {
                return ( Modifiers & Modifier.Optional ) != 0;
            }
        }

        internal bool HasEqlSpecializer
        {
            get
            {
                return ( Modifiers & Modifier.EqlSpecializer ) != 0;
            }
        }

        internal bool HasTypeSpecializer
        {
            get
            {
                return ( Modifiers & Modifier.TypeSpecializer ) != 0;
            }
        }

        public override string ToString()
        {
            return System.String.Format( "{0}", Sym );
        }

        internal ParameterDef EvalSpecializer()
        {
            if ( Specializer is Expression )
            {
                Func<object> proc = Runtime.CompileToFunction( ( Expression ) Specializer );
                var specializer = proc();
                return new ParameterDef( Modifiers, Sym, specializer, InitForm );
            }
            else if ( Specializer is Symbol )
            {
                var type = Runtime.GetType( ( Symbol ) Specializer );
                return new ParameterDef( Modifiers, Sym, type, InitForm );
            }
            else
            {
                return this;
            }
        }

    }

    public class LambdaBinder
    {
        internal Lambda Lambda;
        internal object[] Input;
        internal object[] Output;
        internal Exception Error;

        public LambdaBinder( Lambda lambda, object expr )
        {
            // compile time constructor
            Lambda = lambda;
            Input = Runtime.AsArray( ( IEnumerable ) expr );
            Error = FillDataFrame( lambda.Signature, Input, out Output );
            if ( Error != null )
            {
                throw Error;
            }
        }

        public object Elt( int index, IApply defaultValue )
        {
            if ( Output[ index ] == DefaultValue.Value )
            {
                // A default may reference variables to its left.
                if ( defaultValue == null )
                {
                    Output[ index ] = null;
                }
                else
                {
                    Output[ index ] = Runtime.Funcall( defaultValue );
                }
            }
            return Output[ index ];
        }

        internal Exception FillDataFrame( LambdaSignature signature, object[] input, out object[] output )
        {
            if ( signature.Kind != LambdaKind.Macro && signature.RequiredArgsCount == input.Length && signature.Names.Count == input.Length )
            {
                // fast track if all arguments (no nested parameters) are accounted for.
                output = input;
                return null;
            }

            output = new object[ signature.Names.Count ];
            return FillDataFrame( signature, input, output, 0 );
            
        }

        internal Exception FillDataFrame( LambdaSignature signature, object[] input, object[] output, int offsetOutput )
        {            
            int offset = 0;
            int firstKey = -1;
            int usedKeys = 0;
            bool haveAll = false;
            int firstArg = 0;

            if ( signature.Kind != LambdaKind.Macro && signature.RequiredArgsCount > 0 )
            {
                var n = signature.RequiredArgsCount;

                if ( input.Length < n )
                {
                    return new LispException( "Missing required parameters" );
                }
                Array.Copy( input, 0, output, offsetOutput, n );
                offsetOutput += n;
                firstArg = n;
                offset = n;
            }

            for ( int iArg = firstArg; iArg < signature.Parameters.Count; ++iArg )
            {
                var arg = signature.Parameters[ iArg ];
                object val;

                if ( arg.IsWhole )
                {
                    val = Runtime.AsList( input );
                }
                else if ( arg.IsParams )
                {
                    var buf = new object[ input.Length - offset ];
                    Array.Copy( input, offset, buf, 0, buf.Length );
                    val = buf;
                    haveAll = true;
                }
                else if ( arg.IsRest )
                {
                    Cons list = null;
                    for ( int i = input.Length - 1; i >= offset; --i )
                    {
                        list = new Cons( input[ i ], list );
                    }
                    val = list;
                    haveAll = true;
                }
                else if ( arg.IsVector )
                {
                    var v = new Vector( input.Length - offset );
                    for ( int i = offset; i < input.Length; ++i )
                    {
                        v.Add( input[ i ] );
                    }
                    val = v;
                    haveAll = true;
                }
                else if ( arg.IsKey )
                {
                    if ( firstKey == -1 )
                    {
                        firstKey = offset;
                        for ( int i = firstKey; i < input.Length; i += 2 )
                        {
                            if ( !Runtime.Keywordp( input[ i ] ) || i + 1 == input.Length )
                            {
                                return new LispException( "Invalid keyword/value list" );
                            }
                        }
                    }

                    val = DefaultValue.Value;

                    for ( int i = firstKey; i + 1 < input.Length; i += 2 )
                    {
                        if ( Object.ReferenceEquals( arg.Key, input[ i ] ) )
                        {
                            val = input[ i + 1 ];
                            ++usedKeys;
                            break;
                        }
                    }

                }
                else if ( offset < input.Length )
                {
                    val = input[ offset ];
                    ++offset;
                }
                else if ( arg.IsOptional )
                {
                    val = DefaultValue.Value;
                }
                else
                {
                    return new LispException( "Missing required argument: {0}", arg.Sym );
                }

                if ( arg.NestedParameters != null )
                {
                    // required macro parameter
                    var nestedInput = Runtime.AsArray( ( IEnumerable ) val );
                    var ex = FillDataFrame( arg.NestedParameters, nestedInput, output, offsetOutput );
                    offsetOutput += arg.NestedParameters.Names.Count;
                    if ( ex != null )
                    {
                        return ex;
                    }
                }
                else
                {
                    output[ offsetOutput++ ] = val;
                }
            }

            if ( offset < input.Length && !haveAll && firstKey == -1 )
            {
                return new LispException( "Too many parameters supplied" );
            }

            return null;
        }

    }

    public partial class Runtime
    {
        [Lisp( "system.make-lambda-parameter-binder" )]
        public static LambdaBinder MakeLambdaParameterBinder( Lambda lambda, object expr )
        {
            return new LambdaBinder( lambda, expr );
        }
    }

}

