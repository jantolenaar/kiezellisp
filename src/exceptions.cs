// Copyright (C) Jan Tolenaar. See the file LICENSE for details.


using System;


namespace Kiezel
{
	public class LispException: Exception
	{
		public LispException( string msg ): base( msg )
		{
		}

        public LispException( string msg, Exception innerException )
            : base( msg, innerException )
        {
        }


        public LispException( string fmt, params object[] args )
            : base( String.Format( fmt, args ) )
        {
        }

    }

    public class CatchThrowException : Exception
    {

    }

    internal class ReturnFromLoadException : Exception
    {
    }

    internal class AbortingDebuggerException : Exception
    {
    }

    internal class AbortedDebuggerException : Exception
    {
    }

    internal class ContinueFromBreakpointException : Exception
    {
    }

    public class AssertFailedException : Exception
    {
		public AssertFailedException( string msg ): base( msg )
		{
		}

        public AssertFailedException( string fmt, params object[] args )
            : base( String.Format( fmt, args ) )
        {
        }

    }

    public class InterruptException : Exception
    {
    }

}
