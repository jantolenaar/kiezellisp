// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Kiezel
{
    [Flags]
    public enum ScopeFlags
    {
        Initialized = 1,
        Referenced = 2,
        Assigned = 4,
        Ignore = 8,
        Ignorable = 16,
        Future = 32,
        Lazy = 64,
        Constant = 128,
        Macro = 256,
        SymbolMacro = 512,
        All = 31
    }

    public class AnalysisScope
    {
        public HashSet<Symbol> FreeVariables;

        public bool IsBlockScope;

        public bool IsFileScope;

        public bool IsLambda;

        public string Name;

        public List<Symbol> Names = null;

        public AnalysisScope Parent;

        public ParameterExpression TagBodySaved;

        public List<LabelTarget> Tags = new List<LabelTarget>();

        public ParameterExpression Tilde;

        public bool UsesDynamicVariables = false;

        public bool UsesFramedVariables = false;

        public bool UsesTilde = false;

        public List<ScopeEntry> Variables = new List<ScopeEntry>();

        public AnalysisScope()
        {
        }

        public AnalysisScope(AnalysisScope parent, string name)
            : this()
        {
            Parent = parent;
            Name = name;
        }

        public List<ParameterExpression> Parameters
        {
            get
            {
                return Variables.Where(x => x.Parameter != null).Select(x => x.Parameter).ToList();
            }
        }

        public bool UsesLabels
        {
            get
            {
                return Tags.Count != 0;
            }
        }

        public void CheckVariables()
        {
            string context = null;

            for (AnalysisScope sc = this; sc != null; sc = sc.Parent)
            {
                if (sc.IsLambda && sc.Name != null)
                {
                    context = sc.Name;
                    break;
                }
            }

            foreach (var v in Variables)
            {
                if (v.Ignorable)
                {
                }
                else if (!v.Referenced)
                {
                    if (!v.Key.Name.StartsWith("_") && !v.Key.Name.StartsWith("~") && !v.Key.Name.StartsWith("%") && !v.Ignore)
                    {
                        PrintWarning(context, "unreferenced variable", v.Key);
                    }
                }
                else if (!v.Initialized && !v.Assigned)
                {
                    PrintWarning(context, "uninitialized variable", v.Key);
                }
            }
        }

        public void DefineMacro(Symbol sym, object macro, ScopeFlags flags)
        {
            Variables.Add(new ScopeEntry(sym, macro, flags));
        }

        public int DefineFrameLocal(Symbol sym, ScopeFlags flags)
        {
            if (Names == null)
            {
                Names = new List<Symbol>();
            }

            UsesFramedVariables = true;
            Names.Add(sym);
            Variables.Add(new ScopeEntry(sym, Names.Count - 1, flags));

            return Names.Count - 1;
        }

        public ParameterExpression DefineNativeLocal(Symbol sym, ScopeFlags flags, Type type = null)
        {
            var parameter = Expression.Parameter(type ?? typeof(object), sym.Name);
            Variables.Add(new ScopeEntry(sym, parameter, flags));

            return parameter;
        }

        public bool FindLocal(Symbol sym, ScopeFlags reason)
        {
            int depth;
            ScopeEntry entry;
            return FindLocal(sym, reason, out depth, out entry);
        }

        bool LexicalSymEqual(Symbol sym1, Symbol sym2)
        {
            if (sym1 == sym2)
            {
                return true;
            }
            return false;
        }


        public bool FindLocal(Symbol sym, ScopeFlags reason, out int depth, out ScopeEntry entry)
        {
            bool noCapturedNativeParametersBeyondThisPoint = false;

            depth = 0;
            entry = null;

            for (AnalysisScope sc = this; sc != null; sc = sc.Parent)
            {
                ScopeEntry item;

                for (int i = sc.Variables.Count - 1; i >= 0; --i)
                {
                    item = sc.Variables[i];

                    // Looking for exact match
                    if (LexicalSymEqual(item.Key, sym))
                    {
                        entry = item;

                        if (LexicalSymEqual(sym, Symbols.Tilde))
                        {
                            UsesTilde = true;
                        }

                        if (item.Index != -1 || item.MacroValue != null || item.SymbolMacroValue != null)
                        {
                            item.Flags |= reason;
                        }
                        else if (reason != 0 && noCapturedNativeParametersBeyondThisPoint && !LexicalSymEqual(sym, Symbols.Tilde))
                        {
                            // Linq.Expression closures do not support native variables defined
                            // outside the LambdaExpression. Whenever we encounter such a variable
                            // it is added to the free variables and also added as frame variable
                            // to keep the compiler happy.
                            // The recompile that comes later uses the list of free variables to
                            // choose the correct implementation scheme.
                            sc.FreeVariables.Add(sym);
                            var index = sc.DefineFrameLocal(sym, reason);
                            entry = Variables[index];
                        }
                        else
                        {
                            item.Flags |= reason;
                        }

                        return true;
                    }
                }

                if (sc.IsBlockScope && sym == Symbols.Tilde)
                {
                    // boundary for ~ variable which is tightly coupled to its DO block.
                    break;
                }

                if (sc.IsLambda)
                {
                    // boundary for native variables in closures.
                    noCapturedNativeParametersBeyondThisPoint = true;
                }

                if (sc.Names != null)
                {
                    ++depth;
                }

            }

            depth = 0;

            return false;
        }

        public ScopeEntry FindLocal(Symbol name, int maxDepth = int.MaxValue)
        {
            int depth;
            ScopeEntry entry;
            if (FindLocal(name, 0, out depth, out entry) && depth <= maxDepth)
            {
                return entry;
            }
            else
            {
                return null;
            }
        }

        public void PrintWarning(string context, string error, Symbol sym)
        {
            if (context == null)
            {
                Runtime.PrintWarning(error, " ", sym.Name);
            }
            else
            {
                Runtime.PrintWarning(error, " ", sym.Name, " in ", context);
            }
        }
    }

    public class ScopeEntry
    {
        public ScopeFlags Flags;
        public Symbol Key;
        public object Value;

        public ScopeEntry(Symbol key, object value, ScopeFlags flags)
        {
            Key = key;
            Value = value;
            Flags = flags;
        }

        public int Index
        {
            get
            { 
                return (Value is Int32) ? (Int32)Value : -1;
            }
        }

        public LambdaClosure MacroValue
        {
            get
            { 
                return Value as LambdaClosure; 
            }
        }

        public SymbolMacro SymbolMacroValue
        {
            get
            { 
                return Value as SymbolMacro; 
            }
        }

        public ParameterExpression Parameter
        {
            get
            {
                return Value as ParameterExpression;
            }
        }

        public bool Assigned
        {
            get
            {
                return (Flags & ScopeFlags.Assigned) != 0;
            }
        }

        public bool Ignorable
        {
            get
            {
                return (Flags & ScopeFlags.Ignorable) != 0;
            }
        }

        public bool Ignore
        {
            get
            {
                return (Flags & ScopeFlags.Ignore) != 0;
            }
        }

        public bool Initialized
        {
            get
            {
                return (Flags & ScopeFlags.Initialized) != 0;
            }
        }

        public bool Referenced
        {
            get
            {
                return (Flags & ScopeFlags.Referenced) != 0;
            }
        }
    }
}