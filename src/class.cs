using System;
using System.Collections.Generic;

namespace Kiezel
{
    public partial class Runtime
    {
        internal static Dictionary<Symbol, object> Types;

        [Lisp( "find-type" )]
        public static object FindType( Symbol name )
        {
            object type;
            return Types.TryGetValue( name, out type ) ? type : null;
        }

        [Lisp( "get-type" )]
        public static object GetType( Symbol name )
        {
            var type = FindType( name );
            if ( type == null )
            {
                throw new LispException( "Undefined type name: {0}", name );
            }
            return type;
        }

        [Lisp( "list-all-types" )]
        public static Cons ListAllTypes()
        {
            return ( Cons ) Force( Sort( Types.Keys ) );
        }

        [Lisp( "set-find-type" )]
        public static Prototype SetFindType( Symbol name, Prototype type )
        {
            if ( Keywordp( name ) )
            {
                throw new LispException( "Type name cannot be a keyword" );
            }
            Types[ name ] = type;
            type.ClassName = name;
            return type;
        }

        [Lisp( "set-find-type" )]
        public static Type SetFindType( Symbol name, Type type )
        {
            if ( Keywordp( name ) )
            {
                throw new LispException( "Type name cannot be a keyword: {0}", name );
            }
            Types[ name ] = type;
            return type;
        }
    }
}