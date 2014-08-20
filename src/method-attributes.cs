// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;

namespace Kiezel
{
    class LispAttribute : Attribute
    {
        public string[] Names;

        public LispAttribute( params string[] names )
        {
            Names = names;
        }
    }

    class PureAttribute : Attribute
    {

    }

    class RestrictedImportAttribute : Attribute
    {

    }

    class ExtendsAttribute : Attribute
    {
        public Type Type;

        public ExtendsAttribute( Type type )
        {
            Type = type;
        }
    }
}
