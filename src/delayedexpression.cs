// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

using System;

namespace Kiezel
{

	public class DelayedExpression
	{
		internal Func<object> Recipe;
		internal object Result;

		internal DelayedExpression( Func<object> code )
		{
			Recipe = code;
            Result = null;
        }

		internal object GetValue()
		{
			if ( Recipe != null )
			{
				Result = Recipe();
				Recipe = null;
			}
			return Result;
		}


        public override string ToString()
		{
            return System.String.Format( "DelayedExpr Result={0}", Runtime.ToPrintString( Result ) );
		}
	}

    public partial class Runtime
    {
        [Lisp( "force" )]
        public static object Force( object expr )
        {
            if ( expr is DelayedExpression )
            {
                return ( ( DelayedExpression ) expr ).GetValue();
            }
            else if ( expr is Cons )
            {
                foreach ( var obj in ( Cons ) expr )
                {
                    Force( obj );
                }
                return expr;
            }
            else
            {
                return expr;
            }
        }

        [Lisp( "forced?" )]
        public static object Forced( object expr )
        {
            if ( expr is DelayedExpression )
            {
                return false;
            }
            else if ( expr is Cons )
            {
                return ( ( Cons ) expr ).Forced;
            }
            else
            {
                return true;
            }
        }

        [Lisp("system.create-delayed-expression")]
        public static DelayedExpression CreateDelayedExpression( object func )
        {
            var f = GetThreadFunc( func );
            return new DelayedExpression( f );
        }

    }
}
