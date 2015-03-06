// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System.Linq.Expressions;

namespace Kiezel
{
    public partial class Runtime
    {
        [Lisp( "%attr" )]
        public static object Attr( object target, object attr )
        {
            if ( target is Prototype )
            {
                var proto = ( Prototype ) target;
                var result = proto.GetValue( attr );
                return result;
            }
            else
            {
                var name = GetDesignatedString( attr );
                var binder = GetGetMemberBinder( name );
                var code = CompileDynamicExpression( binder, typeof( object ), new Expression[] { Expression.Constant( target ) } );
                var result = Execute( code );
                return result;
            }
        }

        // Handled by compiler if used as function in function call; otherwise
        // accessor creates a lambda.
        [Lisp( "." )]
        public static object MemberAccessor( string members )
        {
            return new AccessorLambda( false, members );
        }

        [Lisp( "?" )]
        public static object NullableMemberAccessor( string members )
        {
            return new AccessorLambda( true, members );
        }
        [Lisp( "%set-attr" )]
        public static object SetAttr( object target, object attr, object value )
        {
            if ( target is Prototype )
            {
                var proto = ( Prototype ) target;
                return proto.SetValue( attr, value );
            }
            else
            {
                var name = GetDesignatedString( attr );
                var binder = GetSetMemberBinder( name );
                var code = CompileDynamicExpression( binder, typeof( object ), new Expression[] { Expression.Constant( target ), Expression.Constant( value ) } );
                var result = Execute( code );
                return result;
            }
        }
    }
}