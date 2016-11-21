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
            VerifyNoMissingSymbols(currentPackage);

            foreach (var sym in currentPackage.Dict.Values)
            {
                var s = sym.Name;

                if (Definedp(sym) && s.StartsWith(prefix))
                {
                    nameset.Add(s);
                }
            }

            foreach (var package in currentPackage.UseList)
            {
                VerifyNoMissingSymbols(package);

                foreach (var sym in package.Dict.Values)
                {
                    var s = sym.Name;

                    if (s.StartsWith(prefix) && package.IsExported(s))
                    {
                        nameset.Add(s);
                    }
                }
            }

            var packageNames = ListVisiblePackageNames();

            foreach (var package in packageNames)
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

                    VerifyNoMissingSymbols(package);

                    // Show only internal symbols
                    foreach (var sym in package.Dict.Values)
                    {
                        if (!package.IsExported(sym.Name))
                        {
                            var s = name + "::" + sym.Name;

                            if (s.StartsWith(prefix))
                            {
                                nameset.Add(s);
                            }
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

                    VerifyNoMissingSymbols(package);

                    // Show only external symbols
                    foreach (var sym in package.Dict.Values)
                    {
                        if (package.IsExported(sym.Name))
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
        }

        public static bool LooksLikeFunction(Symbol head)
        {
            return (Functionp(head.Value) || head.SpecialFormValue != null || head.MacroValue != null) && !Prototypep(head.Value);
        }

        #endregion Public Methods
    }
}