// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;

namespace Kiezel
{
    internal class ExtendsAttribute : Attribute
    {
        public Type Type;

        public ExtendsAttribute( Type type )
        {
            Type = type;
        }
    }

    internal class LispAttribute : Attribute
    {
        public string[] Names;

        public LispAttribute( params string[] names )
        {
            Names = names;
        }
    }

    internal class PureAttribute : Attribute
    {
    }

    internal class RestrictedImportAttribute : Attribute
    {
    }
}