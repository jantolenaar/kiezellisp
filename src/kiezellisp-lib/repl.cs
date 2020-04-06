#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System.Collections.Generic;

    public partial class Runtime
    {
        #region Public Methods

        public static void FindCompletions(string prefix, HashSet<string> nameset)
        {
            if (prefix.StartsWith("#\\"))
            {
                foreach (var item in CharacterTable)
                {
                    var s = item.Name;
                    if (s != null && s.StartsWith(prefix.Substring(2)))
                    {
                        nameset.Add("#\\" + s);
                    }
                }

                return;
            }

            var currentPackage = CurrentPackage();

            foreach (var sym in currentPackage.Symbols)
            {
                var s = sym.Name;

                if (Definedp(sym) && s.StartsWith(prefix))
                {
                    nameset.Add(s);
                }
            }

            foreach (Package package in ToIter((Cons)GetDynamic(Symbols.UseList)))
            {
                foreach (var sym in package.PublicSymbols)
                {
                    var s = sym.Name;

                    if (s.StartsWith(prefix))
                    {
                        nameset.Add(s);
                    }
                }
            }

            var packageNames = ListAllPackages();

            foreach (string package in packageNames)
            {
                if (package.StartsWith(prefix))
                {
                    nameset.Add(package + ":");
                }
            }

            if (prefix.IndexOf("::") > 0)
            {
                foreach (var name in packageNames)
                {
                    var package = FindPackage(name);

                    if (package == null || package.Name == "")
                    {
                        continue;
                    }

                    // Show only internal symbols
                    foreach (var sym in package.PrivateSymbols)
                    {
                        var s = name + "::" + sym.Name;

                        if (s.StartsWith(prefix))
                        {
                            nameset.Add(s);
                        }
                    }
                }
            }
            else if (prefix.IndexOf(":") > 0)
            {
                foreach (var name in packageNames)
                {
                    var package = FindPackage(name);

                    if (package == null || package.Name == "")
                    {
                        continue;
                    }

                    foreach (var sym in package.PublicSymbols)
                    {
                        var s = name + ":" + sym.Name;

                        if (s.StartsWith(prefix))
                        {
                            nameset.Add(s);
                        }
                    }
                }
            }
        }

        public static bool LooksLikeFunction(Symbol head)
        {
            return (Functionp(head.Value) || head.SpecialFormValue != null || head.MacroValue != null) && !Prototypep(head.Value);
        }

        #endregion Public Methods
    }
}