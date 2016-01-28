// Copyright (C) Jan Tolenaar. See the file LICENSE for details.
using System.IO;

namespace Kiezel
{
    public interface IApply
    {
        object Apply(object[] args);
    }

    public interface IPrintsValue
    {
    }

    public interface ISyntax
    {
        Cons GetSyntax(Symbol context);
    }

    public interface IHasTextWriter
    {
        TextWriter GetTextWriter();
    }

    public interface ILogWriter
    {
        void WriteLog(string style, string msg);
    }
}