#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Package : IPrintsValue
    {
        #region Constructors

        public Package(string name)
        {
            Name = name;
            Dict = new Dictionary<string, Symbol>();
            ExportedSymbols = new HashSet<string>();
            UseList = new List<Package>();
            Reserved = false;
            Aliases = new Dictionary<string, Package>();
            ImportMissingDone = false;
        }

        #endregion Constructors

        #region Public Properties

        public Dictionary<string, Package> Aliases { get; set; }

        public Dictionary<string, Symbol> Dict { get; set; }

        public HashSet<string> ExportedSymbols { get; set; }

        public Cons ExternalNames
        {
            get
            {
                return Runtime.AsList(ExportedSymbols);
            }
        }

        public ImportedConstructor ImportedConstructor { get; set; }

        public Type ImportedType { get; set; }

        public bool ImportMissingDone { get; set; }

        public string Name { get; set; }

        public Cons Names
        {
            get
            {
                return Runtime.AsList(Dict.Keys);
            }
        }

        public bool Reserved { get; set; }

        public bool RestrictedImport { get; set; }

        public List<Package> UseList { get; set; }

        public Cons Uses
        {
            get
            {
                return Runtime.AsList(UseList);
            }
        }

        #endregion Public Properties

        #region Public Methods

        public void AddUsePackage(Package package)
        {
            if (!UseList.Contains(package))
            {
                UseList.Insert(0, package);
            }
        }

        public void Export(string key)
        {
            ExportedSymbols.Add(key);
        }

        public Symbol Find(string key, bool useMissing = false, bool excludeUseList = false)
        {
            var sym = FindInternal(key, useMissing);

            if (sym == null && !excludeUseList)
            {
                sym = FindInUseList(key, useMissing);
            }

            return sym;
        }

        public Symbol FindExported(string key, bool useMissing = false)
        {
            if (IsExported(key))
            {
                return FindInternal(key);
            }
            else if (useMissing && ImportedType != null)
            {
                Runtime.ImportMissingSymbol(key, this);
                if (IsExported(key))
                {
                    return FindInternal(key);
                }
            }

            return null;
        }

        public Symbol FindInternal(string key, bool useMissing = false)
        {
            Symbol sym;

            if (Dict.TryGetValue(key, out sym))
            {
                return sym;
            }
            else if (useMissing && ImportedType != null && !ImportMissingDone)
            {
                Runtime.ImportMissingSymbol(key, this);
                if (Dict.TryGetValue(key, out sym))
                {
                    return sym;
                }
            }

            return null;
        }

        public Symbol FindInUseList(string key, bool useMissing = false)
        {
            Symbol sym;

            foreach (var package in UseList)
            {
                if ((sym = package.FindExported(key, useMissing)) != null)
                {
                    return sym;
                }
            }

            return null;
        }

        public Symbol FindOrCreate(string key, bool useMissing = false, bool excludeUseList = false, bool export = false)
        {
            Symbol sym = Find(key, useMissing: useMissing, excludeUseList: excludeUseList);

            if (sym == null)
            {
                Dict[key] = sym = new Symbol(key, this);
            }

            if (export || Runtime.SetupMode)
            {
                Export(key);
            }

            return sym;
        }

        public void Import(Symbol sym)
        {
            if (sym.Package == null)
            {
                sym.Package = this;
            }

            Dict[sym.Name] = sym;
        }

        public bool IsExported(string key)
        {
            return ExportedSymbols.Contains(key);
        }

        public IEnumerable<Symbol> ListExportedSymbols()
        {
            foreach (string s in ExportedSymbols)
            {
                var sym = FindInternal(s);
                if (sym != null)
                {
                    yield return sym;
                }
            }
        }

        public void RemoveUsePackage(Package package)
        {
            if (UseList.Contains(package))
            {
                UseList.Remove(package);
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

        public static Dictionary<string, Package> Packages;
        public static Dictionary<Type, Package> PackagesByType;
        public static char PackageSymbolSeparator = ':';

        #endregion Static Fields

        #region Private Methods

        static Package MakePackage3(object name, bool reserved = false, bool useLisp = false)
        {
            var n = GetDesignatedString(name);
            if (n.IndexOf(PackageSymbolSeparator) != -1)
            {
                throw new LispException("Invalid package name: {0}", n);
            }

            var package = FindPackage(name);

            if (package == null)
            {
                Packages[n] = package = new Package(n);
            }

            package.Reserved = reserved;

            if (useLisp)
            {
                package.AddUsePackage(LispPackage);
            }

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
            var sym = LispDocPackage.FindOrCreate(key, export: true);
            return sym;
        }

        public static Package CurrentPackage()
        {
            if (SetupMode || Symbols.Package == null)
            {
                // This happens during Symbols.Create()
                return LispPackage;
            }
            else {
                return (Package)GetDynamic(Symbols.Package);
            }
        }

        [Lisp("delete-package")]
        public static void DeletePackage(object name)
        {
            var n = GetDesignatedString(name);
            Package package;
            if (Packages.TryGetValue(n, out package))
            {
                Packages.Remove(n);
            }
        }

        [Lisp("export-symbol")]
        public static void ExportSymbol(string name)
        {
            var descr = ParseSymbol(name);
            CurrentPackage().Export(descr.SymbolName);
        }

        public static ImportedFunction FindImportedFunction(Type type, string name)
        {
            var package = FindPackageByType(type);
            if (package == null)
            {
                return null;
            }
            var sym = package.FindInternal(name, useMissing: true);
            if (sym == null)
            {
                return null;
            }
            var builtin = sym.Value as ImportedFunction;
            return builtin;
        }

        [Lisp("find-package")]
        public static Package FindPackage(object name)
        {
            var currentPackage = CurrentPackage();
            var n = GetDesignatedString(name);
            Package package = null;
            if (currentPackage != null && currentPackage.Aliases.TryGetValue(n, out package))
            {
                return package;
            }
            if (Packages.TryGetValue(n, out package))
            {
                return package;
            }
            return null;
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

        [Lisp("find-symbol")]
        public static Symbol FindSymbol(string name)
        {
            var descr = ParseSymbol(name);
            var sym = descr.Package.FindOrCreate(descr.SymbolName, useMissing: true, export: descr.Exported);

            if (sym == null)
            {
                throw new LispException("Symbol {0} not found", name);
            }

            if (descr.Exported)
            {
                descr.Package.Export(sym.Name);
            }

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

        [Lisp("import-symbol")]
        public static Symbol ImportSymbol(string name)
        {
            var package = CurrentPackage();
            var descr = ParseSymbol(name);
            Symbol sym;
            if (descr.PackageName == "")
            {
                throw new LispException("Package name not specified: {0}", name);
            }
            if (descr.Exported)
            {
                sym = descr.Package.FindExported(descr.SymbolName);
            }
            else {
                sym = descr.Package.Find(descr.SymbolName);
            }
            if (sym == null)
            {
                throw new LispException("Symbol not found: {0}", name);
            }
            package.Import(sym);
            return sym;
        }

        [Lisp("in-package")]
        public static Package InPackage(object name)
        {
            var package = GetPackage(name);
            SetDynamic(Symbols.Package, package);
            return package;
        }

        [Lisp("list-all-packages")]
        public static Cons ListAllPackages()
        {
            return (Cons)Force(Sort(Packages.Keys));
        }

        [Lisp("list-exported-symbols")]
        public static Cons ListExportedSymbols(object name)
        {
            var package = GetPackage(name);
            return (Cons)Force(Sort(package.ListExportedSymbols()));
        }

        public static List<string> ListVisiblePackageNames()
        {
            var currentPackage = CurrentPackage();
            var aliases = currentPackage.Aliases.Keys.Cast<string>();
            var names = Packages.Keys.Cast<string>();
            return aliases.Concat(names).ToList();
        }

        [Lisp("make-package")]
        public static Package MakePackage(object name)
        {
            return MakePackage3(name, reserved: false, useLisp: false);
        }

        [Lisp("make-symbol")]
        public static Symbol MakeSymbol(params object[] args)
        {
            var str = MakeString(args);
            var sym = FindSymbol(str);
            return sym;
        }

        public static SymbolDescriptor ParseSymbol(string name)
        {
            var descr = new SymbolDescriptor();
            var index = name.IndexOf(PackageSymbolSeparator);

            if (index == 0)
            {
                descr.Package = KeywordPackage;
                descr.PackageName = "keyword";
                descr.Exported = true;
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
                    descr.Exported = true;
                }
                else {
                    descr.Package = CurrentPackage();
                    descr.PackageName = "";
                    descr.Exported = false;
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
                descr.Exported = false;
                descr.PackageName = name.Substring(0, index);
                descr.SymbolName = name.Substring(index + 2);
            }
            else {
                // one colon
                descr.Exported = true;
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

            descr.Package = MakePackage(descr.PackageName);

            return descr;
        }

        [Lisp("shadow-symbol")]
        public static void ShadowSymbol(string name)
        {
            var descr = ParseSymbol(name);
            CurrentPackage().FindOrCreate(descr.SymbolName, excludeUseList: true);
        }

        [Lisp("use-package")]
        public static Package UsePackage(object name)
        {
            var package = GetPackage(name);
            CurrentPackage().AddUsePackage(package);
            return package;
        }

        [Lisp("use-package-alias")]
        public static string UsePackageAlias(object packageName, object nickName)
        {
            var currentPackage = CurrentPackage();
            var n = GetDesignatedString(nickName);
            var package = GetPackage(packageName);
            currentPackage.Aliases[n] = package;
            return n;
        }

        #endregion Public Methods

        #region Other

        /*public*/
        public class SymbolDescriptor
        {
            #region Fields

            public bool Exported;
            public Package Package;
            public string PackageName;
            public string SymbolName;

            #endregion Fields
        }

        #endregion Other
    }
}