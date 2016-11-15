#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System.IO;

    public interface IApply
    {
        #region Methods

        object Apply(object[] args);

        #endregion Methods
    }

    public interface IHasTextWriter
    {
        #region Methods

        TextWriter GetTextWriter();

        #endregion Methods
    }

    public interface IHtmlWriter
    {
        #region Methods

        string Format(string style, string msg);

        #endregion Methods
    }

    public interface ILogWriter
    {
        #region Methods

        void WriteLog(string style, string msg);

        #endregion Methods
    }

    public interface IPrintsValue
    {
    }

    public interface ISyntax
    {
        #region Methods

        Cons GetSyntax(Symbol context);

        #endregion Methods
    }
}