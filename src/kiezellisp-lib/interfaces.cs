// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

namespace Kiezel
{
    public interface IApply
    {
        object Apply( object[] args );
    }

    internal interface IPrintsValue
    {
    }

    internal interface ISyntax
    {
        Cons GetSyntax( Symbol context );
    }
}