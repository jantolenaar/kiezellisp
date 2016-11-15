#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;

    public class ExtendsAttribute : Attribute
    {
        #region Fields

        public Type Type;

        #endregion Fields

        #region Constructors

        public ExtendsAttribute(Type type)
        {
            Type = type;
        }

        #endregion Constructors
    }

    public class LispAttribute : Attribute
    {
        #region Fields

        public string[] Names;

        #endregion Fields

        #region Constructors

        public LispAttribute(params string[] names)
        {
            Names = names;
        }

        #endregion Constructors
    }

    public class PureAttribute : Attribute
    {
    }

    public class RestrictedImportAttribute : Attribute
    {
    }
}