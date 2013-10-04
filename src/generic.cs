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

    enum CompareClassResult
    {
        NotComparable,
        Equal,
        Less,
        Greater
    }

    class LambdaSignatureComparer : IComparer<LambdaSignature>
    {
        // if x more specific than y then return -1
        public int Compare( LambdaSignature x, LambdaSignature y )
        {
            if ( x.RequiredArgsCount > y.RequiredArgsCount )
            {
                // more arguments is more specific
                return -1;
            }

            if ( x.RequiredArgsCount < y.RequiredArgsCount )
            {
                // more arguments is more specific
                return 1;
            }

            for ( int i = 0; i < x.RequiredArgsCount; ++i )
            {
                var flags1 = x.Parameters[ i ].Modifiers & Modifier.MaskSpecializer;
                var class1 = x.Parameters[ i ].Specializer;

                var flags2 = y.Parameters[ i ].Modifiers & Modifier.MaskSpecializer;
                var class2 = y.Parameters[ i ].Specializer;


                var result = Runtime.CompareClass( flags1, class1, flags2, class2 );

                switch ( result )
                {
                    case CompareClassResult.NotComparable:
                    {
                        // implies both are not null
                        // anything will do but the value must be reproducible.
                        return Runtime.Compare( class1.GetHashCode(), class2.GetHashCode() );
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


    public class MultiMethod: DynamicObject, IApply, ISyntax
    {
        internal int RequiredArgsCount;
        internal List<Lambda> Lambdas = new List<Lambda>();

        public MultiMethod( int requiredArgsCount )
        {
            RequiredArgsCount = requiredArgsCount;
        }

        Cons ISyntax.GetSyntax( Symbol context )
        {
            return Runtime.AsList( Lambdas.Select( Runtime.GetSyntax ).Distinct() );
        }

        public void Add( Lambda method )
        {
            method.Generic = this;

            var comparer = new LambdaSignatureComparer();

            for ( int i = 0; i < Lambdas.Count; ++i )
            {
                int result = comparer.Compare( method.Signature, Lambdas[ i ].Signature );

                if ( result == -1 )
                {
                    Lambdas.Insert( i, method );
                    return;
                }
                else if ( result == 0 )
                {
                    Lambdas[ i ] = method;
                    return;
                }
            }

            Lambdas.Add( method );
        }

        object IApply.Apply( object[] args )
        {
            foreach ( var lambda in Lambdas )
            {
                if ( lambda.Signature.ParametersMatchArguments( args ) )
                {
                    return ( ( IApply ) lambda ).Apply( args );
                }
            }

            throw new LispException( "No matching multi-method found" );
        }

        internal object ApplyNext( Lambda current, object[] args )
        {
            foreach ( var lambda in Lambdas )
            {
                if ( current != null )
                {
                    if ( lambda == current )
                    {
                        current = null;
                    }
                }
                else if ( lambda.Signature.ParametersMatchArguments( args ) )
                {
                    return ( ( IApply ) lambda ).Apply( args );
                }
            }

            return null;
        }

        public override bool TryInvoke( InvokeBinder binder, object[] args, out object result )
        {
            result = ( ( IApply ) this ).Apply( args );
            return true;
        }
    }

    public partial class Runtime
    {
        internal static MultiMethod DefineMultiMethod( Symbol sym, int requiredArgsCount, string doc )
        {
            var func = new MultiMethod( requiredArgsCount );
            sym.FunctionValue = func;
            sym.Documentation = String.IsNullOrWhiteSpace( doc ) ? null : MakeList( doc );
            return func;
        }

        internal static Lambda DefineMethod( Symbol sym, Lambda lambda )
        {
            var container = sym.Value as MultiMethod;

            if ( container == null )
            {
                container = DefineMultiMethod( sym, lambda.Signature.RequiredArgsCount, null );
            }
            else if ( container.RequiredArgsCount != 0 && lambda.Signature.RequiredArgsCount != container.RequiredArgsCount )
            {
                throw new LispException( "Number of parameters of {0} and its multimethod are not the same", ToPrintString( lambda ) );
            }
            container.Add( lambda );
            return lambda;
        }

        [Lisp( "system.call-next-method" )]
        public static object CallNextMethod( Lambda currentMethod, object[] args )
        {
            if ( currentMethod != null && currentMethod.Generic != null )
            {
                return currentMethod.Generic.ApplyNext( currentMethod, args );
            }
            else
            {
                return null;
            }
        }

        internal static CompareClassResult CompareClass( Modifier flags1, object class1, Modifier flags2, object class2 )
        {
            //
            // The most specialized class is the 'smallest'.
            //

            if ( flags1 == 0 )
            {
                if ( flags2 == 0 )
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
            else if ( flags2 == 0 )
            {
                // lhs is more specialized
                return CompareClassResult.Less;
            }
            else if ( flags1 != flags2 )
            {
                if ( flags1 == Modifier.EqlSpecializer )
                {
                    return CompareClassResult.Less;
                }
                else
                {
                    return CompareClassResult.Greater;
                }
            }
            else if ( flags1 == Modifier.EqlSpecializer )
            {
                if ( Eql( class1, class2 ) )
                {
                    return CompareClassResult.Equal;
                }
                else
                {
                    return CompareClassResult.NotComparable;
                }
            }
            else if ( class1 is Type && class2 is Type )
            {
                var c1 = ( Type ) class1;
                var c2 = ( Type ) class2;

                if ( c1 == c2 )
                {
                    return CompareClassResult.Equal;
                }
                else if ( c1.IsSubclassOf( c2 ) )
                {
                    return CompareClassResult.Less;
                }
                else if ( c2.IsSubclassOf( c2 ) )
                {
                    return CompareClassResult.Greater;
                }
                else
                {
                    return CompareClassResult.NotComparable;
                }
            }
            else if ( class1 is Prototype && class2 is Prototype )
            {
                var c1 = ( Prototype ) class1;
                var c2 = ( Prototype ) class2;

                if ( c1 == c2 )
                {
                    return CompareClassResult.Equal;
                }
                else if ( c1.IsSubTypeOf( c2 ) )
                {
                    return CompareClassResult.Less;
                }
                else if ( c2.IsSubTypeOf( c2 ) )
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
    }
}


