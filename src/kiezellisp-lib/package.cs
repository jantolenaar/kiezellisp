#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;

    public class Package : IPrintsValue
    {
        #region Constructors

        public Package(string name)
        {
            Name = name;
            Dict = new Dictionary<string, Symbol>();
            Reserved = false;
            AutoCreate = false;
        }

        #endregion Constructors

        #region Public Properties

        public Dictionary<string,Symbol> Dict { get; set; }

        public Type ImportedType { get; set; }

        public string Name { get; set; }

        public bool AutoCreate { get; set; }

        public bool Reserved { get; set; }

        public bool RestrictedImport { get; set; }

        #endregion Public Properties

        #region Public Methods

        public Symbol MakePrivate(string key)
        {
            var sym = Create(key, false);
            return sym;
        }

        public Symbol MakePublic(string key)
        {
            var sym = Create(key, true);
            return sym;
        }

        public Symbol Find(string key)
        {
            Symbol sym;

            if (Dict.TryGetValue(key, out sym))
            {
                return sym;
            }

            return null;
        }

        public Symbol FindExported(string key)
        {
            var sym = Find(key);
            if (sym != null && sym.IsPublic)
            {
                return sym;
            }
            return null;
        }

        public Symbol Create(string key)
        {
            // SetupMode takes care of lisp function visibility.
            // Existing symbols are unchanged.
            Symbol sym = Find(key);

            if (sym == null)
            {
                Dict[key] = sym = new Symbol(key, this);
                sym.IsPublic = Runtime.SetupMode;
            }

            return sym;
        }

        public Symbol Create(string key, bool ispublic)
        {
            // Existing symbols are changed to match visibility.
            Symbol sym = Find(key);

            if (sym == null)
            {
                Dict[key] = sym = new Symbol(key, this);
            }

            sym.IsPublic = ispublic;

            return sym;
        }

        public List<Symbol> Symbols
        {
            get
            {
                var seq = new List<Symbol>();
                foreach (Symbol item in Runtime.ToIter(Runtime.Sort(Dict.Values)))
                {
                    seq.Add(item);
                }
                return seq;
            }
        }

        public List<Symbol> PublicSymbols
        {
            get
            {
                var seq = new List<Symbol>();
                foreach (var sym in Symbols)
                {
                    if (sym.IsPublic)
                    {
                        seq.Add(sym);
                    }
                }
                return seq;
            }
        }

        public List<Symbol> PrivateSymbols
        {
            get
            {
                var seq = new List<Symbol>();
                foreach (var sym in Symbols)
                {
                    if (!sym.IsPublic)
                    {
                        seq.Add(sym);
                    }
                }
                return seq;
            }
        }

        public override string ToString()
        {
            return string.Format("<Package Name=\"{0}\">", Name);
        }

        #endregion Public Methods
    }

    public partial class Runtime
    {
        #region Static Fields

        public static char PackageSymbolSeparator = ':';

        #endregion Static Fields

        #region Private Methods

        static Package MakePackage3(object name, bool reserved = false, bool automatic = false)
        {
            var n = GetDesignatedString(name);
            if (n.IndexOf(PackageSymbolSeparator) != -1)
            {
                throw new LispException("Invalid package name: {0}", n);
            }

            var package = FindPackage(name);

            if (package != null)
            {
                //package.Reserved = reserved;
                //package.AutoCreate = package.AutoExport = automatic;
                return package;
            }

            Packages[n] = package = new Package(n);
            package.Reserved = reserved;
            package.AutoCreate = automatic;

            return package;
        }

        #endregion Private Methods

        #region Public Methods

        public static void AddPackageByType(Type type, Package package)
        {
            PackagesByType[type] = package;
        }

        public static Symbol CreateDocSymbol(string key)
        {
            var sym = LispDocPackage.Create(key, true);
            return sym;
        }

        public static Package CurrentPackage()
        {
            if (SetupMode || Symbols.Package == null)
            {
                // This happens during Symbols.Create()
                return LispPackage;
            }
            else
            {
                return (Package)GetDynamic(Symbols.Package);
            }
        }

        [Lisp("delete-package")]
        public static Package DeletePackage(object name)
        {
            var n = GetDesignatedString(name);
            Package package;
            if (Packages.TryGetValue(n, out package))
            {
                Packages.Remove(n);
            }
            return package;
        }

        public static ImportedFunction FindImportedFunction(Type type, string name)
        {
            var package = FindPackageByType(type);
            if (package == null)
            {
                return null;
            }
            var sym = package.Find(name);
            if (sym == null)
            {
                return null;
            }
            var builtin = sym.Value as ImportedFunction;
            return builtin;
        }

        public static Symbol FindInUseList(string key)
        {
            Symbol sym;
            bool ok = false;
            
            foreach (Package package in ToIter(GetDynamic(Symbols.UseList)))
            {
                if (package == LispPackage) ok = true;
                if ((sym = package.FindExported(key)) != null)
                {
                    return sym;
                }
            }
            if (!ok)
            {
                ThrowError("Missing lisp in uselist");
            }
            return null;
        }

        [Lisp("find-package")]
        public static Package FindPackage(object name)
        {
            var name2 = GetDesignatedString(name);
            Package package;
            Packages.TryGetValue(name2, out package);
            return package;
        }

        public static Package FindPackageByType(Type type)
        {
            Package package = null;
            if (PackagesByType.TryGetValue(type, out package))
            {
                return package;
            }
            return null;
        }


        //[Lisp("find-symbol")]
        public static Symbol FindSymbol(string name)
        {
            var sym = ResolveSymbol(name, calledByFindSymbol: true);
            return sym;
        }

        [Lisp("make-symbol")]
        public static Symbol MakeSymbol(params object[] args)
        {
            var name = MakeString(args);
            var sym = ResolveSymbol(name, calledByMakeSymbol: true);
            return sym;
        }


        [Lisp("get-package")]
        public static Package GetPackage(object name)
        {
            var package = FindPackage(name);
            if (package == null)
            {
                throw new LispException("Undefined package: {0}", name);
            }
            return package;
        }

        [Lisp("list-all-packages")]
        public static Cons ListAllPackages()
        {
            return (Cons)Force(Sort(Packages.Keys));
        }

        [Lisp("list-public-symbols")]
        public static Cons ListPublicSymbols(object name)
        {
            var package = GetPackage(name);
            return AsList(package.PublicSymbols);
        }

        [Lisp("list-private-symbols")]
        public static Cons ListPrivateSymbols(object name)
        {
            var package = GetPackage(name);
            return AsList(package.PrivateSymbols);
        }

        [Lisp("list-symbols")]
        public static Cons ListSymbols(object name)
        {
            var package = GetPackage(name);
            return AsList(package.Symbols);
        }

        [Lisp("make-package")]
        public static Package MakePackage(object name)
        {
            return MakePackage3(name, reserved: false);
        }


        public static SymbolDescriptor ParseSymbol(string name)
        {
            var descr = new SymbolDescriptor();
            var index = name.IndexOf(PackageSymbolSeparator);

            if (index == 0)
            {
                descr.Package = KeywordPackage;
                descr.PackageName = "keyword";
                descr.IsPublic = true;
                descr.SymbolName = name.Substring(1);
                descr.SymbolName = descr.SymbolName.TrimStart(PackageSymbolSeparator);
                if (descr.SymbolName.Length == 0)
                {
                    throw new LispException("Keyword name cannot be null or blank");
                }
                return descr;
            }

            if (index == -1)
            {
                if (SetupMode)
                {
                    descr.Package = LispPackage;
                    descr.PackageName = "lisp";
                    descr.IsPublic = true;
                }
                else
                {
                    descr.Package = CurrentPackage();
                    descr.PackageName = "";
                    descr.IsPublic = false;
                }

                descr.SymbolName = name;
                if (descr.SymbolName.Length == 0)
                {
                    throw new LispException("Symbol name cannot be null or blank");
                }
                return descr;
            }

            if (index + 1 < name.Length && name[index + 1] == PackageSymbolSeparator)
            {
                // two consecutives colons
                descr.IsPublic = false;
                descr.PackageName = name.Substring(0, index);
                descr.SymbolName = name.Substring(index + 2);
            }
            else
            {
                // one colon
                descr.IsPublic = true;
                descr.PackageName = name.Substring(0, index);
                descr.SymbolName = name.Substring(index + 1);
            }

            if (descr.SymbolName.Length == 0)
            {
                throw new LispException("Symbol name cannot be null or blank: {0}", name);
            }

            if (descr.PackageName == "")
            {
                descr.PackageName = "keyword";
            }

            descr.Package = FindPackage(descr.PackageName);

            if (SetupMode && descr.PackageName == null)
            {
                descr.Package = MakePackage(descr.PackageName);
            }

            return descr;
        }

        [Lisp("package")]
        public static Package InPackage(object name)
        {
            var package = MakePackage(name);
            SetDynamic(Symbols.Package, package);
            return package;
        }


        [Lisp("private")]
        public static void MakePrivateSymbols(params string[] names)
        {
            var package = CurrentPackage();

            foreach (var name in names)
            {
                package.MakePrivate(name);
            }
        }

        [Lisp("public")]
        public static void MakePublicSymbols(params string[] names)
        {
            var package = CurrentPackage();

            foreach (var name in names)
            {
                package.MakePublic(name);
            }
        }

        public static Symbol ResolveSymbol(string longname, bool calledByFindSymbol = false, bool calledByReader = false, bool calledByMakeSymbol = false )
        {
            // calledByFindSymbol: no new packages or symbols.
            // calledByMakeSymbol: new packages and symbols.
            // calledByReader: no new packages, new symbols in current/default package and some others.
            var descr = Runtime.ParseSymbol(longname);
            var name = descr.SymbolName;
            var package = descr.Package;

            if (package == null)
            {
                if (calledByReader || calledByFindSymbol)
                {
                    throw new LispException("Undefined package: {0}", descr.PackageName);
                }
                else
                {
                    package = MakePackage(descr.PackageName);
                }
            }

            Symbol sym = package.Find(name);
            
            if (sym == null && descr.PackageName == "")
            {
                sym = FindInUseList(name);
            }

            if (sym != null)
            {
                if (descr.IsPublic && !sym.IsPublic)
                {
                    throw new LispException("Symbol {0} not public", longname);
                }
                return sym;
            }

            if (calledByFindSymbol)
            {
                throw new LispException("Symbol {0} not found", longname);
            }
            
            if (descr.PackageName == "" || package.AutoCreate || calledByMakeSymbol)
            {
                sym = package.Create(name, descr.IsPublic);
            }

            return sym;

        }

        [Lisp("shadow")]
        public static void ShadowSymbols(params string[] names)
        {
            var package = CurrentPackage();
      
            foreach (var name in names)
            {
                package.Create(name);
            }
        }

        [Lisp("use-package-symbols")]
        public static Package UsePackageSymbols(object name)
        {
            var package = GetPackage(name);
            var list = (Cons)GetDynamic(Symbols.UseList);
            var seq = AsVector(list);
            seq.Remove(package);
            seq.Insert(0,package);
            SetDynamic(Symbols.UseList, AsList(seq));
            return package;
        }

        [Lisp("use-package-alias")]
        public static string UsePackageAlias(object packageName, object nickName)
        {
            var name = GetDesignatedString(nickName);
            var package = GetPackage(packageName);
            Packages[name] = package;
            return name;
        }

        #endregion Public Methods

        #region Other

        /*public*/
        public class SymbolDescriptor
        {
            #region Fields

            public bool IsPublic;
            public Package Package;
            public string PackageName;
            public string SymbolName;

            #endregion Fields
        }

        #endregion Other
    }
}