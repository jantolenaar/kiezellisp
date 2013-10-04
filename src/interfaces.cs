// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.


using System.IO;

namespace Kiezel
{
    interface IPrintsValue
    {
    }

    public interface IApply
    {
        object Apply( object[] args );
    }

    interface ISyntax
    {
        Cons GetSyntax( Symbol context );
    }
}

