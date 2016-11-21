#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public partial class Runtime
    {
        #region Public Methods

        public static object ConvertToEnumType(Type enumType, object value)
        {
            var result = TryConvertToEnumType(enumType, value);
            if (result == null)
            {
                throw new LispException("Cannot convert {0} to enumeration type {1}", value, enumType);
            }
            return result;
        }

        public static object DefDynamic(Symbol sym, object value)
        {
            // used by compiler generated code
            CurrentThreadContext.SpecialStack = new SpecialVariables(sym, false, value, CurrentThreadContext.SpecialStack);
            return value;
        }

        public static object DefDynamicConst(Symbol sym, object value)
        {
            // used by compiler generated code
            CurrentThreadContext.SpecialStack = new SpecialVariables(sym, true, value, CurrentThreadContext.SpecialStack);
            return value;
        }

        public static object GetDynamic(Symbol sym)
        {
            // used by compiler generated code
            for (SpecialVariables entry = CurrentThreadContext.SpecialStack; entry != null; entry = entry.Link)
            {
                if (entry.Sym == sym)
                {
                    return entry.Value;
                }
            }

            return sym.CheckedValue;
        }

        public static object GetLexical(int depth, int index)
        {
            // used by compiler generated code
            return CurrentThreadContext.Frame.GetValueAt(depth, index);
        }

        public static object[] ParseKwargs(object[] args, string[] names, params object[] defaults)
        {
            return ParseKwargs(false, args, names, defaults);
        }

        public static object[] ParseKwargs(bool allowOtherArgs, object[] args, string[] names,
            params object[] defaults)
        {
            int count = names.Length;
            var kwargs = new object[count];
            if (defaults != null)
            {
                Array.Copy(defaults, kwargs, defaults.Length);
            }

            for (var i = 0; i < args.Length; i += 2)
            {
                if (!Keywordp(args[i]))
                {
                    throw new LispException("Not a keyword: {0}", ToPrintString(args[i]));
                }
                var k = (Symbol)args[i];
                object v = args[i + 1];
                int j = 0;
                while (j < count && k.Name != names[j])
                {
                    ++j;
                }
                if (j < count)
                {
                    kwargs[j] = v;
                }
                else if (!allowOtherArgs)
                {
                    throw new LispException("Illegal keyword argument: {0}", k);
                }
            }
            return kwargs;
        }

        public static AnalysisScope ReconstructAnalysisScope(Frame context, AnalysisScope consoleScope = null)
        {
            if (consoleScope == null)
            {
                consoleScope = new AnalysisScope();
            }

            var stack = new Stack<AnalysisScope>();
            stack.Push(consoleScope);

            var scope = consoleScope;
            scope.Names = null;

            for (var frame = context; frame != null; frame = frame.Link)
            {
                if (frame.Names == null)
                {
                    continue;
                }

                if (scope == null)
                {
                    scope = new AnalysisScope();
                    stack.Push(scope);
                }

                scope.Names = new List<Symbol>();

                foreach (var key in frame.Names)
                {
                    scope.DefineFrameLocal(key, ScopeFlags.All);
                }

                scope = null;
            }

            scope = null;

            while (stack.Count > 0)
            {
                var top = stack.Pop();
                top.Parent = scope;
                scope = top;
            }

            return scope;
        }

        public static AnalysisScope ReplGetCurrentAnalysisScope()
        {
            var scope = ReconstructAnalysisScope(CurrentThreadContext.Frame);
            return scope;
        }

        public static object RestoreFrame(ThreadContextState saved)
        {
            // used by compiler generated code
            CurrentThreadContext.RestoreFrame(saved);
            return null;
        }

        public static object RestoreStackAndFrame(ThreadContextState saved)
        {
            // used by compiler generated code
            CurrentThreadContext.RestoreStackAndFrame(saved);
            return null;
        }

        public static ThreadContextState SaveStackAndFrame()
        {
            // used by compiler generated code
            return SaveStackAndFrameWith(null, null);
        }

        public static ThreadContextState SaveStackAndFrameWith(Frame frame, Cons form)
        {
            // used by compiler generated code
            return CurrentThreadContext.SaveStackAndFrame(frame, form);
        }

        public static object SetDynamic(Symbol sym, object value)
        {
            // used by compiler generated code
            for (SpecialVariables entry = CurrentThreadContext.SpecialStack; entry != null; entry = entry.Link)
            {
                if (entry.Sym == sym)
                {
                    entry.CheckedValue = value;
                    return value;
                }
            }

            sym.CheckedValue = value;

            return value;
        }

        public static object SetLexical(int depth, int index, object value)
        {
            // used by compiler generated code
            return CurrentThreadContext.Frame.SetValueAt(depth, index, value);
        }

        public static object TryConvertToEnumType(Type enumType, object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value.GetType() == enumType)
            {
                return value;
            }

            if (value is Symbol || value is string)
            {
                var name = GetDesignatedString(value).LispToPascalCaseName();
                var field = enumType.GetFields().First(f => string.Compare(f.Name, name, true) == 0);
                if (field == null)
                {
                    return null;
                }
                else {
                    return field.GetValue(null);
                }
            }
            else {
                var i = Convert.ToInt32(value);
                if (enumType.IsEnumDefined(i))
                {
                    return i;
                }
                else {
                    return null;
                }
            }
        }

        public static object TryGetLexicalSymbol(Symbol sym)
        {
            // used by compiler generated code
            return CurrentThreadContext.Frame.TryGetValue(sym);
        }

        #endregion Public Methods
    }
}