// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;

namespace Kiezel
{
    [Flags]
    internal enum ScopeFlags
    {
        Initialized = 1,
        Referenced = 2,
        Assigned = 4,
        Ignore = 8,
        Ignorable = 16,
        All = 31
    }

    internal class ScopeEntry
    {
        public int Index;
        public ParameterExpression Parameter;
        public ScopeFlags Flags;
        public Symbol Key;

        public ScopeEntry( Symbol key, int index, ScopeFlags flags )
        {
            Key = key;
            Index = index;
            Parameter = null;
            Flags = flags;
        }

        public ScopeEntry( Symbol key, ParameterExpression parameter, ScopeFlags flags )
        {
            Key = key;
            Index = -1;
            Parameter = parameter;
            Flags = flags;
        }

        public bool Initialized
        {
            get
            {
                return ( Flags & ScopeFlags.Initialized ) != 0;
            }
        }

        public bool Referenced
        {
            get
            {
                return ( Flags & ScopeFlags.Referenced ) != 0;
            }
        }

        public bool Assigned
        {
            get
            {
                return ( Flags & ScopeFlags.Assigned ) != 0;
            }
        }

        public bool Ignore
        {
            get
            {
                return ( Flags & ScopeFlags.Ignore ) != 0;
            }
        }

        public bool Ignorable
        {
            get
            {
                return ( Flags & ScopeFlags.Ignorable ) != 0;
            }
        }

    }

    internal class AnalysisScope
    {
        public AnalysisScope()
        {
#if DEBUG
            Ident = Runtime.GenTemp( "scope" );
#endif
        }

        public AnalysisScope( AnalysisScope parent, string name )
            :this()
        {
            Parent = parent;
            Name = name;
        }

#if DEBUG
        public Symbol Ident;
#endif
        public bool IsFileScope;
        public bool IsBlockScope;
        public bool IsTagBodyScope;
        public string Name;
        public AnalysisScope Parent;
        public bool IsLambda;
        public bool UsesTilde;
        public bool UsesReturn;
        public LabelTarget ReturnLabel;

        public List<LabelTarget> Tags = new List<LabelTarget>();
        public List<ScopeEntry> Variables = new List<ScopeEntry>();

        public List<ParameterExpression> Parameters
        {
            get
            {
                return Variables.Where( x => x.Parameter != null ).Select( x => x.Parameter ).ToList();
            }
        }

        public List<Symbol> Names = null;
        public bool UsesDynamicVariables = false;
        public bool UsesFramedVariables = false;
        public HashSet<Symbol> FreeVariables;

        public ParameterExpression DefineNativeLocal( Symbol sym, ScopeFlags flags )
        {
            var parameter = Expression.Parameter( typeof( object ), sym.Name );
            Variables.Add( new ScopeEntry( sym, parameter, flags ) );

            return parameter;
        }

        public int DefineFrameLocal( Symbol sym, ScopeFlags flags )
        {
            if ( Names == null )
            {
                Names = new List<Symbol>();
            }

            UsesFramedVariables = true;
            Names.Add( sym );
            Variables.Add( new ScopeEntry( sym, Names.Count - 1, flags ) );

            return Names.Count - 1;
        }

        public bool FindLocal( Symbol sym, ScopeFlags reason )
        {
            int depth;
            int index;
            ParameterExpression parameter;
            int realDepth;
            return FindLocal( sym, reason, out realDepth, out depth, out index, out parameter );
        }

        public bool FindLocal( Symbol sym, ScopeFlags reason, out int depth, out int index, out ParameterExpression parameter )
        {
            int realDepth;
            return FindLocal( sym, reason, out realDepth, out depth, out index, out parameter );
        }

        public bool FindLocal( Symbol sym, ScopeFlags reason, out int realDepth, out int depth, out int index, out ParameterExpression parameter )
        {
            bool noCapturedNativeParametersBeyondThisPoint = false;

            realDepth = 0;
            depth = 0;
            index = 0;
            parameter = null;

            for ( AnalysisScope sc = this; sc != null; sc = sc.Parent )
            {
                ScopeEntry item;

                if ( sc.IsBlockScope && sym.Name[0] == '~' )
                {
                    // If a block uses ~ we must recompile because the tilde variable is
                    // not defined in the first compile pass.
                    UsesTilde = true;
                }

                for ( int i = sc.Variables.Count - 1; i >= 0; --i )
                {
                    item = sc.Variables[ i ];

                    // Looking for exact match or the most recent tilde
                    if ( item.Key == sym || ( sym == Symbols.Tilde && item.Key.Name[ 0 ] == '~' ) )
                    {
                        if ( item.Index != -1 )
                        {
                            index = item.Index;
                            item.Flags |= reason;
                        }
                        else if ( reason != 0 && noCapturedNativeParametersBeyondThisPoint && sym.Name[ 0 ] != '~' )
                        {
                            // Linq.Expression closures do not support native variables defined
                            // outside the LambdaExpression. Whenever we encounter such a variable
                            // it is added to the free variables and also added as frame variable
                            // to keep the compiler happy.
                            // The recompile that comes later uses the list of free variables to
                            // choose the correct implementation scheme.
                            sc.FreeVariables.Add( sym );
                            index = sc.DefineFrameLocal( sym, reason );
                        }
                        else
                        {
                            parameter = item.Parameter;
                            item.Flags |= reason;
                        }

                        return true;
                    }
                }

                if ( sc.IsBlockScope && sym.Name[0] == '~' )
                {
                    // boundary for ~ variable which is tightly coupled to its DO block.
                    break;
                }

                if ( sc.IsLambda )
                {
                    // boundary for native variables in closures.
                    noCapturedNativeParametersBeyondThisPoint = true;
                }

                if ( sc.Names != null )
                {
                    ++depth;
                }

                ++realDepth;
            }

            depth = 0;
            index = -1;

            return false;
        }

        public bool HasLocalVariable( Symbol name, int maxDepth )
        {
            int realDepth;
            int depth;
            int index;
            ParameterExpression parameter;
            return FindLocal( name, 0, out realDepth, out depth, out index, out parameter ) && realDepth <= maxDepth;
        }

        public void CheckVariables()
        {
            string context = null;

            for ( AnalysisScope sc = this; sc != null; sc = sc.Parent )
            {
                if ( sc.IsLambda && sc.Name != null )
                {
                    context = sc.Name;
                    break;
                }
            }

            foreach ( var v in Variables )
            {
                if ( v.Ignorable )
                {

                }
                else if ( !v.Referenced )
                {
                    if ( !v.Key.Name.StartsWith( "__" ) && !v.Key.Name.StartsWith( "~" ) && !v.Ignore )
                    {
                        PrintWarning( context, "unreferenced variable", v.Key );
                    }
                }
                else if ( !v.Initialized && !v.Assigned )
                {
                    PrintWarning( context, "uninitialized variable", v.Key );
                }
            }
        }

        internal void PrintWarning( string context, string error, Symbol sym )
        {
            if ( context == null )
            {
                Runtime.PrintWarning( error, " ", sym.Name );
            }
            else
            {
                Runtime.PrintWarning( error, " ", sym.Name, " in ", context );
            }
        }

    }

}
