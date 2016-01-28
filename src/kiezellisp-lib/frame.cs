// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System.Collections;
using System.Collections.Generic;

namespace Kiezel
{
    public class Frame : IEnumerable
    {
        public Frame Link;
        public List<Symbol> Names;
        public List<object> Values;

        public Frame()
        {
        }

        public Frame(List<Symbol> names)
        {
            Link = null;
            Names = names ?? new List<Symbol>();
            Values = null;
        }

        public Frame(Frame template, List<object> args)
        {
            Link = template.Link;
            Names = template.Names;
            Values = args;
        }

        public IEnumerator GetEnumerator()
        {
            for (int i = 0; i < Names.Count; ++i)
            {
                if (Values != null && i < Values.Count)
                {
                    yield return new KeyValuePair<Symbol, object>(Names[i], Values[i]);
                }
                else
                {
                    yield return new KeyValuePair<Symbol, object>(Names[i], null);
                }
            }
        }

        public object GetValueAt(int depth, int index)
        {
            int d = depth;
            for (Frame frame = this; frame != null; frame = frame.Link, --d)
            {
                if (d == 0)
                {
                    if (frame.Values == null || index >= frame.Values.Count)
                    {
                        return null;
                        //throw new LispException( "Undefined lexical variable: ({0},{1})", depth, index );
                    }

                    return frame.Values[index];
                }
            }

            throw new LispException("No lexical variable at ({0},{1})", depth, index);
        }

        public object SetValueAt(int depth, int index, object val)
        {
            int d = depth;
            for (Frame frame = this; frame != null; frame = frame.Link, --d)
            {
                if (d == 0)
                {
                    if (frame.Values == null)
                    {
                        frame.Values = new List<object>();
                    }
                    while (frame.Values.Count <= index)
                    {
                        frame.Values.Add(null);
                    }
                    frame.Values[index] = val;
                    return val;
                }
            }

            throw new LispException("No lexical variable at ({0},{1})", depth, index);
        }

        public object TryGetValue(Symbol sym)
        {
            for (Frame frame = this; frame != null; frame = frame.Link)
            {
                if (frame.Names != null)
                {
                    int index = frame.Names.IndexOf(sym);
                    if (index != -1)
                    {
                        return frame.Values[index];
                    }
                }
            }

            return null;
        }
    }

    public class FrameAndScope
    {
        public Frame Frame;
        public AnalysisScope Scope;

        public FrameAndScope()
        {
            Frame = new Frame();
            Scope = new AnalysisScope();
            Scope.IsFileScope = true;
            Scope.IsBlockScope = true;
        }
    }
}