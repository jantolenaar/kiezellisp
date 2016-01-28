// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;

namespace Kiezel
{
    public class ExtendsAttribute : Attribute
    {
        public Type Type;

        public ExtendsAttribute(Type type)
        {
            Type = type;
        }
    }

    public class LispAttribute : Attribute
    {
        public string[] Names;

        public LispAttribute(params string[] names)
        {
            Names = names;
        }
    }

    public class PureAttribute : Attribute
    {
    }

    public class RestrictedImportAttribute : Attribute
    {
    }
}