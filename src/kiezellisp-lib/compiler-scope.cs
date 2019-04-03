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

        public bool IsBlockScope;
        public bool IsFileScope;
        public Symbol Name;
        public AnalysisScope Parent;
        public AnalysisScope LambdaParent;
        public ParameterExpression SavedState = Expression.Parameter(typeof(ThreadContextState), "%state");
        public bool UsesDynamicVariables;
        public List<ScopeEntry> Variables = new List<ScopeEntry>();
        public List<ScopeEntry> HoistVariables = new List<ScopeEntry>();
        public ParameterExpression HoistedArgs;
        // named block
        public ParameterExpression ResultVar;
        public LabelTarget RedoLabel;
        public LabelTarget LeaveLabel;
        public Symbol BlockName;
        public bool RedoLabelUsed;
        public bool LeaveLabelUsed;

        #endregion Fields

        #region Constructors

        public AnalysisScope(AnalysisScope parent = null)
        {
            Parent = parent;
            LambdaParent = parent == null ? null : parent.LambdaParent;
            Name = parent == null ? Symbols.Anonymous : parent.Name;
        }

        #endregion Constructors

        #region Public Properties

        public bool IsLambda
        {
            get
            {
                return LambdaParent == this;
            }
        }

        public List<Symbol> Names
        {
            get
            {
                return Variables.Where(x => x.Parameter != null).Select(x => x.Key).ToList();
            }
        }

        public List<ParameterExpression> Parameters
        {
            get
            {
                return Variables.Where(x => x.Parameter != null).Select(x => x.Parameter).ToList();
            }
        }

        public List<Symbol> HoistNames
        {
            get
            {
                return HoistVariables.Where(x => x.Parameter != null).Select(x => x.Key).ToList();
            }
        }

        public List<ParameterExpression> HoistParameters
        {
            get
            {
                return HoistVariables.Where(x => x.Parameter != null).Select(x => x.Parameter).ToList();
            }
        }

        public bool UsesLabels
        {
            get
            {
                return BlockName != null;
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void CheckVariables()
        {
            string context = null;

            for (AnalysisScope sc = this; sc != null; sc = sc.Parent)
            {
                if (sc.IsLambda && sc.Name != null)
                {
                    context = sc.Name.ContextualName;
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

        public void DefineMacro(Symbol sym, object macro, ScopeFlags flags)
        {
            Variables.Add(new ScopeEntry(this, sym, macro, flags));
        }

        public ParameterExpression DefineVariable(Symbol sym, ScopeFlags flags, Type type = null)
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

        public ScopeEntry FindLocal(Symbol sym, ScopeFlags reason = 0)
        {
            for (var sc = this; sc != null; sc = sc.Parent)
            {
                foreach (var item in sc.Variables)
                {
                    if (item.Key == sym)
                    {
                        item.Flags |= reason;

                        if (item.Parameter != null && LambdaParent != sc.LambdaParent)
                        {
                            // Linq.Expression closures do not support native variables defined
                            // outside a separately compiled lambda.
                            var index = LambdaParent.HoistVariables.Count;
                            var hoistedItem = new ScopeEntry(item, index);
                            LambdaParent.HoistVariables.Add(hoistedItem);
                            return hoistedItem;
                        }
                        else
                        {
                            return item;
                        }
                    }
                }

                if (LambdaParent == sc)
                {
                    foreach (var item in sc.HoistVariables)
                    {
                        if (item.Key == sym)
                        {
                            item.Flags |= reason;
                            item.HoistOriginal.Flags |= reason;
                            return item;
                        }
                    }
                }

            }

            return null;
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

        #endregion Public Methods
    }

    public class ScopeEntry
    {
        #region Fields

        public ScopeFlags Flags;
        public Symbol Key;
        public AnalysisScope Scope;
        public object Value;
        public int HoistIndex;
        public ScopeEntry HoistOriginal;

        #endregion Fields

        #region Constructors

        public ScopeEntry(AnalysisScope scope, Symbol key, object value, ScopeFlags flags)
        {
            Scope = scope;
            Key = key;
            Value = value;
            Flags = flags;
            HoistIndex = -1;
            HoistOriginal = null;
        }

        public ScopeEntry(ScopeEntry item, int index)
        : this(item.Scope, item.Key, item.Value, item.Flags)
        {
            HoistIndex = index;
            HoistOriginal = item;
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