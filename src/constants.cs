// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.


using System;

namespace Kiezel
{
    internal class DefaultValue : IPrintsValue
    {
        internal static readonly DefaultValue Value = new DefaultValue();

        public override string ToString()
        {
            return "#default-value";
        }
    }

}
