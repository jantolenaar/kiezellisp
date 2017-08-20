#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    public class Frame
    {
        #region Fields

        public Frame Link;
        public List<Symbol> Names;
        public IRuntimeVariables Values;

        #endregion Fields

        #region Constructors

        public Frame()
        {
        }

        public Frame(List<Symbol> names, IRuntimeVariables values)
        {
            Link = null;
            Names = names;
            Values = values;
        }

        #endregion Constructors

        #region Public Methods

        public bool Modify(Symbol name, object value)
        {
            for (var f = this; f != null; f = f.Link)
            {
                var i = f.Names.IndexOf(name);
                if (i != -1)
                {
                    f.Values[i] = value;
                    return true;
                }
            }
            return false;
        }

        public static Frame MakeFrame(List<Symbol> names, IRuntimeVariables values)
        {
            return new Frame(names, values);
        }

        public PrototypeDictionary GetDictionary()
        {
            var dict = new PrototypeDictionary();
            for (var f = this; f != null; f = f.Link)
            {
                if (f.Names != null)
                {
                    for (var i = 0; i < f.Names.Count; ++i)
                    {
                        var n = f.Names[i];
                        var v = f.Values[i];

                        if (!n.IsReservedName && !dict.ContainsKey(n))
                        {
                            dict[n] = v;
                        }
                    }
                }
            }
            return dict;
        }

        #endregion Public Methods
    }


    public class FrameAndScope
    {
        #region Fields

        public Frame Frame;
        public AnalysisScope Scope;

        #endregion Fields

        #region Constructors

        public FrameAndScope()
        {
            Frame = new Frame();
            Scope = new AnalysisScope();
            Scope.IsFileScope = true;
        }

        #endregion Constructors
    }


}