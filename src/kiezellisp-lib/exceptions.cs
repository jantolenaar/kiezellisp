#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;

    public class AbortedDebuggerException : Exception
    {
    }

    public class AbortingDebuggerException : Exception
    {
    }

    public class AssertFailedException : Exception
    {
        #region Constructors

        public AssertFailedException(string msg)
            : base(msg)
        {
        }

        public AssertFailedException(string fmt, params object[] args)
            : base(String.Format(fmt, args))
        {
        }

        #endregion Constructors
    }

    public class ContinueFromBreakpointException : Exception
    {
    }

    public class ExitOnCloseException : Exception
    {
    }

    public class InterruptException : Exception
    {
    }

    public class LispException : Exception
    {
        #region Constructors

        public LispException(string msg)
            : base(msg)
        {
        }

        public LispException(string msg, Exception innerException)
            : base(msg, innerException)
        {
        }

        public LispException(string fmt, params object[] args)
            : base(String.Format(fmt, args))
        {
        }

        #endregion Constructors
    }

    public class ReturnFromLoadException : Exception
    {
    }
}