// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Kiezel
{
    public class Package : IPrintsValue
    {
        internal Package( string name )
        {
            Name = name;
            Dict = new Dictionary<string, Symbol>();
            ExternalSymbols = new HashSet<string>();
            UseList = new List<Package>();
            Reserved = false;
            Aliases = new Dictionary<string, Package>();
            ImportMissingDone = false;
        }

        public Cons ExternalNames
        {
            get
            {
                return Runtime.AsList( ExternalSymbols );
            }
        }

        public Type ImportedType
        {
            get;
            internal set;
        }

        public bool ImportMissingDone
        {
            get;
            set;
        }

        public string Name
        {
            get;
            internal set;
        }

        public Cons Names
        {
            get
            {
                return Runtime.AsList( Dict.Keys );
            }
        }

        public bool Reserved
        {
            get;
            internal set;
        }

        public bool RestrictedImport
        {
            get;
            set;
        }

        public Cons Uses
        {
            get
            {
                return Runtime.AsList( UseList );
            }
        }

        internal Dictionary<string, Package> Aliases
        {
            get;
            set;
        }

        internal Dictionary<string, Symbol> Dict
        {
            get;
            set;
        }
        internal HashSet<string> ExternalSymbols
        {
            get;
            set;
        }

        internal ImportedConstructor ImportedConstructor
        {
            get;
            set;
        }

        internal List<Package> UseList
        {
            get;
            set;
        }
        public override string ToString()
        {
            return String.Format( "<Package Name=\"{0}\">", Name );
        }

        internal void AddUsePackage( Package package )
        {
            if ( !UseList.Contains( package ) )
            {
                UseList.Insert( 0, package );
            }
        }

        internal void Export( string key )
        {
            ExternalSymbols.Add( key );
        }

        internal Symbol Find( string key, bool useMissing = false )
        {
            return FindInternal( key, useMissing ) ?? FindInherited( key, useMissing );
        }

        internal Symbol FindExternal( string key, bool useMissing = false )
        {
            if ( ExternalSymbols.Contains( key ) )
            {
                return FindInternal( key );
            }
            else if ( useMissing && ImportedType != null )
            {
                Runtime.ImportMissingSymbol( key, this );
                if ( ExternalSymbols.Contains( key ) )
                {
                    return FindInternal( key );
                }
            }

            return null;
        }

        internal Symbol FindInherited( string key, bool useMissing = false )
        {
            Symbol sym;

            foreach ( var package in UseList )
            {
                if ( ( sym = package.FindExternal( key, useMissing ) ) != null )
                {
                    return sym;
                }
            }

            return null;
        }

        internal Symbol FindInternal( string key, bool useMissing = false )
        {
            Symbol sym;

            if ( Dict.TryGetValue( key, out sym ) )
            {
                return sym;
            }
            else if ( useMissing && ImportedType != null )
            {
                Runtime.ImportMissingSymbol( key, this );
                if ( Dict.TryGetValue( key, out sym ) )
                {
                    return sym;
                }
            }

            return null;
        }

        internal void Import( Symbol sym )
        {
            if ( sym.Package == null )
            {
                sym.Package = this;
            }

            Dict[ sym.Name ] = sym;

            if ( Runtime.SetupMode )
            {
                ExternalSymbols.Add( sym.Name );
            }
        }

        internal Symbol Intern( string key, bool useMissing = false )
        {
            Symbol sym = Find( key, useMissing );

            if ( sym == null )
            {
                Dict[ key ] = sym = new Symbol( key, this );
            }

            if ( Runtime.SetupMode && !ExternalSymbols.Contains( key ) )
            {
                ExternalSymbols.Add( key );
            }

            return sym;
        }

        internal Symbol InternNoInherit( string key, bool useMissing = false )
        {
            Symbol sym = FindInternal( key, useMissing );

            if ( sym == null )
            {
                Dict[ key ] = sym = new Symbol( key, this );
            }

            if ( Runtime.SetupMode && !ExternalSymbols.Contains( key ) )
            {
                ExternalSymbols.Add( key );
            }

            return sym;
        }
        internal IEnumerable<Symbol> ListExternals()
        {
            foreach ( string s in ExternalSymbols )
            {
                var sym = FindInternal( s );
                if ( sym != null )
                {
                    yield return sym;
                }
            }
        }
        internal void RemoveUsePackage( Package package )
        {
            if ( UseList.Contains( package ) )
            {
                UseList.Remove( package );
            }
        }
    }

    public partial class Runtime
    {
        internal static Dictionary<string, Package> Packages;
        internal static Dictionary<Type, Package> PackagesByType;

        internal static char PackageSymbolSeparator = ':';

        [Lisp( "delete-package" )]
        public static void DeletePackage( object name )
        {
            var n = GetDesignatedString( name );
            Package package;
            if ( Packages.TryGetValue( n, out package ) )
            {
                Packages.Remove( n );
            }
        }

        [Lisp( "export-symbol" )]
        public static void ExportSymbol( string name )
        {
            var descr = ParseSymbol( name );
            CurrentPackage().Export( descr.SymbolName );
        }

        [Lisp( "find-package" )]
        public static Package FindPackage( object name )
        {
            var currentPackage = CurrentPackage();
            var n = GetDesignatedString( name );
            Package package = null;
            if ( currentPackage.Aliases.TryGetValue( n, out package ) )
            {
                return package;
            }
            if ( Packages.TryGetValue( n, out package ) )
            {
                return package;
            }
            return null;
        }

        [Lisp( "get-package" )]
        public static Package GetPackage( object name )
        {
            var package = FindPackage( name );
            if ( package == null )
            {
                throw new LispException( "Undefined package: {0}", name );
            }
            return package;
        }

        [Lisp( "import-symbol" )]
        public static Symbol ImportSymbol( string name )
        {
            var package = CurrentPackage();
            var descr = ParseSymbol( name );
            Symbol sym;
            if ( descr.PackageName == "" )
            {
                throw new LispException( "Package name not specified: {0}", name );
            }
            if ( descr.Package == null )
            {
                throw new LispException( "Symbol not found: {0}", name );
            }
            if ( descr.Internal )
            {
                sym = descr.Package.Find( descr.SymbolName );
            }
            else
            {
                sym = descr.Package.FindExternal( descr.SymbolName );
            }
            if ( sym == null )
            {
                throw new LispException( "Symbol not found: {0}", name );
            }
            package.Import( sym );
            return sym;
        }

        [Lisp( "in-package" )]
        public static Package InPackage( object name )
        {
            var package = GetPackage( name );
            SetDynamic( Symbols.Package, package );
            return package;
        }

        [Lisp( "list-all-packages" )]
        public static Cons ListAllPackages()
        {
            return ( Cons ) Force( Sort( Packages.Keys ) );
        }

        [Lisp( "list-exported-symbols" )]
        public static Cons ListExportedSymbols( object name )
        {
            var package = GetPackage( name );
            return ( Cons ) Force( Sort( package.ListExternals() ) );
        }

        [Lisp( "make-package" )]
        public static Package MakePackage( object name )
        {
            return MakePackage( name, false );
        }

        [Lisp( "parse-symbol" )]
        public static SymbolDescriptor ParseSymbol( string name )
        {
            var descr = new SymbolDescriptor();
            var index = name.LastIndexOf( PackageSymbolSeparator );

            if ( index == 0 )
            {
                descr.Package = KeywordPackage;
                descr.PackageName = "keyword";
                descr.Internal = false;
                descr.SymbolName = name.Substring( 1 );
                if ( descr.SymbolName.Length == 0 )
                {
                    throw new LispException( "Keyword name cannot be null or blank" );
                }
                return descr;
            }

            if ( index == -1 )
            {
                descr.Package = CurrentPackage();
                descr.PackageName = "";
                descr.Internal = true;
                descr.SymbolName = name;
                if ( descr.SymbolName.Length == 0 )
                {
                    throw new LispException( "Symbol name cannot be null or blank" );
                }
                return descr;
            }

            if ( index > 0 && name[ index - 1 ] == PackageSymbolSeparator )
            {
                // two consecutives colons
                descr.Internal = true;
                descr.PackageName = name.Substring( 0, index - 1 );
                descr.SymbolName = name.Substring( index + 1 );
            }
            else
            {
                // one colon
                descr.Internal = false;
                descr.PackageName = name.Substring( 0, index );
                descr.SymbolName = name.Substring( index + 1 );
            }

            if ( descr.SymbolName.Length == 0 )
            {
                throw new LispException( "Symbol name cannot be null or blank: {0}", name );
            }

            if ( descr.PackageName == "" )
            {
                descr.PackageName = "keyword";
            }

            descr.Package = FindPackage( descr.PackageName );

            return descr;
        }

        [Lisp( "shadow-symbol" )]
        public static void ShadowSymbol( string name )
        {
            var descr = ParseSymbol( name );
            CurrentPackage().InternNoInherit( descr.SymbolName );
        }

        [Lisp( "use-package" )]
        public static Package UsePackage( object name )
        {
            var package = GetPackage( name );
            CurrentPackage().AddUsePackage( package );
            return package;
        }

        [Lisp( "use-package-alias" )]
        public static string UsePackageAlias( object packageName, object nickName )
        {
            var currentPackage = CurrentPackage();
            var n = GetDesignatedString( nickName );
            var package = GetPackage( packageName );
            currentPackage.Aliases[ n ] = package;
            return n;
        }

        internal static void AddPackageByType( Type type, Package package )
        {
            PackagesByType[ type ] = package;
        }

        internal static Package CurrentPackage()
        {
            return ( Package ) GetDynamic( Symbols.Package );
        }

        internal static ImportedFunction FindImportedFunction( Type type, string name )
        {
            var package = FindPackageByType( type );
            if ( package == null )
            {
                return null;
            }
            var sym = package.FindInternal( name, true );
            if ( sym == null )
            {
                return null;
            }
            var builtin = sym.Value as ImportedFunction;
            return builtin;
        }

        internal static Package FindPackageByType( Type type )
        {
            Package package = null;
            if ( PackagesByType.TryGetValue( type, out package ) )
            {
                return package;
            }
            return null;
        }

        internal static Symbol FindSymbol( string name, bool creating = false, bool prettyPrinting = false )
        {
            SymbolDescriptor descr;

            if ( prettyPrinting )
            {
                // Ignore package specifier since package may not exist anyway.
                descr = new SymbolDescriptor();
                descr.Internal = false;
                descr.PackageName = "";
                descr.Package = CurrentPackage();
                descr.SymbolName = name;
            }
            else
            {
                descr = ParseSymbol( name );
            }

            if ( descr.PackageName == "" || descr.Package == KeywordPackage )
            {
                return descr.Package.Intern( descr.SymbolName );
            }

            if ( descr.Package == null )
            {
                if ( creating )
                {
                    descr.Package = MakePackage( descr.PackageName );
                }
                else
                {
                    throw new LispException( "Undefined package: {0}", descr.PackageName );
                }
            }

            var sym = ( creating || descr.Internal ) ? descr.Package.Find( descr.SymbolName, useMissing: true ) : descr.Package.FindExternal( descr.SymbolName, useMissing: true );

            if ( sym == null )
            {
                if ( creating )
                {
                    return descr.Package.Intern( descr.SymbolName );
                }
                else
                {
                    throw new LispException( "Symbol {0} not found", name );
                }
            }
            else
            {
                return sym;
            }
        }

        internal static List<string> ListVisiblePackageNames()
        {
            var currentPackage = CurrentPackage();
            var aliases = currentPackage.Aliases.Keys.Cast<string>();
            var names = Packages.Keys.Cast<string>();
            return aliases.Concat( names ).ToList();
        }

        internal static Package MakePackage( object name, bool reserved )
        {
            var n = GetDesignatedString( name );
            if ( n.IndexOf( PackageSymbolSeparator ) != -1 )
            {
                throw new LispException( "Invalid package name: {0}", n );
            }

            Package package;
            if ( !Packages.TryGetValue( n, out package ) )
            {
                Packages[ n ] = package = new Package( n );
                package.Reserved = reserved;
                if ( LispPackage != null )
                {
                    // every package uses the lisp package
                    // until specifically unused
                    package.AddUsePackage( LispPackage );
                }
            }
            return package;
        }

        public /*internal*/ class SymbolDescriptor
        {
            public bool Internal;
            public Package Package;
            public string PackageName;
            public string SymbolName;
        }
    }
}