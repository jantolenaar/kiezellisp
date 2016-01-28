// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

namespace Kiezel
{
    public class DefaultValue : IPrintsValue
    {
        public static readonly DefaultValue Value = new DefaultValue();

        public override string ToString()
        {
            return "#default-value";
        }
    }
}