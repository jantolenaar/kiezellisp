#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;

    #region Enumerations

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

    #endregion Enumerations

    public class AnalysisScope
    {
        #region Fields

        public bool AnyLabelsCreated;
        public HashSet<Symbol> FreeVariables;
        public bool IsBlockScope;
        public bool IsFileScope;
        public bool IsLambda;
        public List<LabelTarget> Labels = new List<LabelTarget>();
        public string Name;
        public List<Symbol> Names;
        public AnalysisScope Parent;
        public ParameterExpression TagBodySaved;
        public ParameterExpression Tilde;
        public bool UsesDynamicVariables;
        public bool UsesFramedVariables;
        public List<ScopeEntry> Variables = new List<ScopeEntry>();

        #endregion Fields

        #region Constructors

        public AnalysisScope()
        {
        }

        public AnalysisScope(AnalysisScope parent, string name)
            : this()
        {
            Parent = parent;
            Name = name;
        }

        #endregion Constructors

        #region Public Properties

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
                return Labels.Count != 0;
            }
        }

        #endregion Public Properties

        #region Private Methods

        bool LexicalSymEqual(Symbol sym1, Symbol sym2)
        {
            if (sym1 == sym2)
            {
                return true;
            }
            return false;
        }

        #endregion Private Methods

        #region Public Methods

        public void ChangeNativeToFrameLocal(ScopeEntry entry)
        {
            if (Names == null)
            {
                Names = new List<Symbol>();
            }

            UsesFramedVariables = true;
            Names.Add(entry.Key);
            entry.Value = Names.Count - 1;
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
                    if (!v.Key.SuppressWarnings && !v.Ignore)
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

        public int DefineFrameLocal(Symbol sym, ScopeFlags flags)
        {
            if (Names == null)
            {
                Names = new List<Symbol>();
            }

            UsesFramedVariables = true;
            Names.Add(sym);
            Variables.Add(new ScopeEntry(this, sym, Names.Count - 1, flags));

            return Names.Count - 1;
        }

        public void DefineMacro(Symbol sym, object macro, ScopeFlags flags)
        {
            Variables.Add(new ScopeEntry(this, sym, macro, flags));
        }

        public ParameterExpression DefineNativeLocal(Symbol sym, ScopeFlags flags, Type type = null)
        {
            var parameter = Expression.Parameter(type ?? typeof(object), sym.Name);
            Variables.Add(new ScopeEntry(this, sym, parameter, flags));

            return parameter;
        }

        public bool FindDuplicate(Symbol name)
        {
            var entry = FindLocal(name);
            return entry != null && entry.Scope == this;
        }

        public bool FindLocal(Symbol sym, ScopeFlags reason)
        {
            int depth;
            ScopeEntry entry;
            return FindLocal(sym, reason, out depth, out entry);
        }

        public bool FindLocal(Symbol sym, ScopeFlags reason, out int depth, out ScopeEntry entry)
        {
            bool noCapturedNativeParametersBeyondThisPoint = false;

            depth = 0;
            entry = null;

            for (AnalysisScope sc = this; sc != null; sc = sc.Parent)
            {
                ScopeEntry item;

                if (sc.IsBlockScope && sym == Symbols.Tilde)
                {
                    if (sc.Tilde == null)
                    {
                        sc.Tilde = sc.DefineNativeLocal(Symbols.Tilde, ScopeFlags.All);
                    }
                }

                for (var i = sc.Variables.Count - 1; i >= 0; --i)
                {
                    item = sc.Variables[i];

                    // Looking for exact match
                    if (LexicalSymEqual(item.Key, sym))
                    {
                        entry = item;
                        item.Flags |= reason;

                        if (item.Index != -1 || item.MacroValue != null || item.SymbolMacroValue != null)
                        {
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
                            sc.ChangeNativeToFrameLocal(item);
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

        public ScopeEntry FindLocal(Symbol name)
        {
            int depth;
            ScopeEntry entry;
            if (FindLocal(name, 0, out depth, out entry))
            {
                return entry;
            }
            else {
                return null;
            }
        }

        public void PrintWarning(string context, string error, Symbol sym)
        {
            if (context == null)
            {
                Runtime.PrintWarning(error, " ", sym.Name);
            }
            else {
                Runtime.PrintWarning(error, " ", sym.Name, " in ", context);
            }
        }

        #endregion Public Methods
    }

    public class ScopeEntry
    {
        #region Fields

        public ScopeFlags Flags;
        public Symbol Key;
        public AnalysisScope Scope;
        public object Value;

        #endregion Fields

        #region Constructors

        public ScopeEntry(AnalysisScope scope, Symbol key, object value, ScopeFlags flags)
        {
            Scope = scope;
            Key = key;
            Value = value;
            Flags = flags;
        }

        #endregion Constructors

        #region Public Properties

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

        public int Index
        {
            get
            {
                return (Value is int) ? (int)Value : -1;
            }
        }

        public bool Initialized
        {
            get
            {
                return (Flags & ScopeFlags.Initialized) != 0;
            }
        }

        public LambdaClosure MacroValue
        {
            get
            {
                return Value as LambdaClosure;
            }
        }

        public ParameterExpression Parameter
        {
            get
            {
                return Value as ParameterExpression;
            }
        }

        public bool Referenced
        {
            get
            {
                return (Flags & ScopeFlags.Referenced) != 0;
            }
        }

        public SymbolMacro SymbolMacroValue
        {
            get
            {
                return Value as SymbolMacro;
            }
        }

        #endregion Public Properties
    }
}