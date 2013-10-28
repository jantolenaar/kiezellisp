// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;

namespace Kiezel
{
    public class Package: IPrintsValue
    {
        public string Name
        {
            get;
            internal set;
        }

        internal Dictionary<string, Symbol> Dict
        {
            get;
            set;
        }

        internal Dictionary<string, Package> Aliases
        {
            get;
            set;
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

        public Cons ExternalNames
        {
            get
            {
                return Runtime.AsList( ExternalSymbols );
            }
        }

        internal HashSet<string> ExternalSymbols
        {
            get;
            set;
        }

        internal List<Package> UseList
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

        public Type ImportedType
        {
            get;
            internal set;
        }

        public bool RestrictedImport
        {
            get;
            set;
        }

        internal ImportedConstructor ImportedConstructor
        {
            get;
            set;
        }

        internal Package( string name )
        {
            Name = name;
            Dict = new Dictionary<string, Symbol>();
            ExternalSymbols = new HashSet<string>();
            UseList = new List<Package>();
            Reserved = false;
            Aliases = new Dictionary<string, Package>();
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

        
        internal Symbol Find( string key, bool useMissing = false )
        {
            return FindInternal( key, useMissing ) ?? FindInherited( key, useMissing );
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


        public override string ToString()
        {
            return String.Format( "<Package Name=\"{0}\">", Name );
        }

        internal void AddUsePackage( Package package )
        {
            if ( !UseList.Contains( package ) )
            {
                UseList.Add( package );
            }
        }

        internal void RemoveUsePackage( Package package )
        {
            if ( UseList.Contains( package ) )
            {
                UseList.Remove( package );
            }
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

        internal void Export( string key )
        {
            ExternalSymbols.Add( key );
        }

    }

    public partial class Runtime
    {
        internal static Dictionary<string, Package> Packages;

        [Lisp( "list-all-packages" )]
        public static Cons ListAllPackages()
        {
            return (Cons) Force( Sort( Packages.Keys ) );
        }

        internal static List<string> ListVisiblePackageNames()
        {
            var currentPackage = CurrentPackage();
            var aliases = currentPackage.Aliases.Keys.Cast<string>();
            var names = Packages.Keys.Cast<string>();
            return aliases.Concat( names ).ToList();
        }

        [Lisp("list-exported-symbols")]
        public static Cons ListExportedSymbols( object name )
        {
            var package = GetPackage( name );
            return (Cons) Force( Sort( package.ListExternals() ) );
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

        [Lisp( "in-package" )]
        public static Package InPackage( object name )
        {
            var package = GetPackage( name );
            SetDynamic( Symbols.Package, package );
            return package;
        }

        internal static Package CurrentPackage()
        {
            return ( Package ) GetDynamic( Symbols.Package );
        }

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

        [Lisp( "make-package" )]
        public static Package MakePackage( object name )
        {
            return MakePackage( name, false );
        }
        
        internal static Package MakePackage( object name, bool reserved )
        {
            var n = GetDesignatedString( name );
            if ( n.IndexOfAny( PackageSymbolSeparators ) != -1 )
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

        [Lisp( "use-package" )]
        public static Package UsePackage( object name )
        {
            var package = GetPackage( name );
            CurrentPackage().AddUsePackage( package );
            return package;
        }

        [Lisp( "unuse-package" )]
        public static Package UnusePackage( object name )
        {
            var package = GetPackage( name );
            CurrentPackage().RemoveUsePackage( package );
            return package;
        }

        [Lisp( "export-symbol" )]
        public static void ExportSymbol( string name )
        {
            var descr = ParseSymbol( name );
            CurrentPackage().Export( descr.SymbolName );
        }

        [Lisp( "shadow-symbol" )]
        public static void ShadowSymbol( string name )
        {
            var descr = ParseSymbol( name );
            CurrentPackage().InternNoInherit( descr.SymbolName );
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

        [Lisp( "set-package-alias" )]
        public static string SetPackageAlias( object packageName, object nickName )
        {
            var currentPackage = CurrentPackage();
            var n = GetDesignatedString( nickName );
            var package = GetPackage( packageName );
            currentPackage.Aliases[ n ] = package;
            return n;
        }

        internal class SymbolDescriptor
        {
            public string PackageName;
            public string SymbolName;
            public bool Internal;
            public Package Package;
        }


        internal static SymbolDescriptor ParseSymbol( string name )
        {
            var descr = new SymbolDescriptor();
            var index = name.LastIndexOfAny( PackageSymbolSeparators );

            if ( index == -1 )
            {
                if ( name[ 0 ] == ':' )
                {
                    descr.Package = KeywordPackage;
                    descr.PackageName = "keyword";
                    descr.Internal = false;
                    descr.SymbolName = name.Substring( 1 );
                }
                else
                {
                    descr.Package = CurrentPackage();
                    descr.PackageName = "";
                    descr.Internal = true;
                    descr.SymbolName = name;
                }

                return descr;
            }

            descr.Internal = name[ index ] == '!';
            descr.PackageName = name.Substring( 0, index );
            descr.SymbolName = name.Substring( index + 1 );

            if ( descr.SymbolName.Length == 0 )
            {
                throw new LispException( "Invalid symbol reference: {0}", name );
            }

            if ( descr.PackageName == "" )
            {
                descr.PackageName = "keyword";
            }

            descr.Package = FindPackage( descr.PackageName );

            return descr;
        }

        internal static char[] PackageSymbolSeparators = new char[] { '.', '!' };

        internal static Symbol FindSymbol( string name, bool creating )
        {
            var descr = ParseSymbol( name );

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
                else if ( IsReadSuppress() )
                {
                    return Symbols.Undefined;
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
                else if ( IsReadSuppress() )
                {
                    return Symbols.Undefined;
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

    }
}
