// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

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