// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Kiezel
{
    public partial class Runtime
    {
        internal static BindingFlags ImportBindingFlags = BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;

        internal static Assembly LastUsedAssembly = null;

        [Lisp( "add-event-handler" )]
        public static void AddEventHandler( System.Reflection.EventInfo eventinfo, object target, IApply func )
        {
            var type = eventinfo.EventHandlerType;
            var dlg = ConvertToDelegate( type, func );
            eventinfo.AddEventHandler( target, dlg );
        }

        //[Lisp("get-namespace-types")]
        public static List<Type> GetNamespaceTypes( string pattern )
        {
            var allTypes = new List<Type>();
            foreach ( Assembly asm in AppDomain.CurrentDomain.GetAssemblies() )
            {
                var types = asm.GetTypes();
                allTypes.AddRange( types.Where( t => NamespaceOrClassNameMatch( pattern, t ) ) );
            }
            return allTypes;
        }

        [Lisp( "import" )]
        public static Package Import( string typeName, params object[] args )
        {
            var kwargs = ParseKwargs( args, new string[] { "package-name", "package-name-prefix", "extends-package-name", "export", "type-parameters" }, null, null, null, true, null );
            var typeParameters = ToIter( ( Cons ) kwargs[ 4 ] ).Cast<Symbol>().Select( GetType ).Cast<Type>().ToArray();
            var type = GetTypeForImport( typeName, typeParameters );
            var prefix = GetDesignatedString( kwargs[ 1 ] ?? GetDynamic( Symbols.PackageNamePrefix ) );
            var packageName = GetDesignatedString( kwargs[ 0 ] ?? prefix + type.Name.LispName() );
            var packageName2 = GetDesignatedString( kwargs[ 2 ] );
            var export = ToBool( kwargs[ 3 ] );

            return Import( type, packageName, packageName2, export );
        }

        //[Lisp( "import-namespace" )]
        public static void ImportNamespace( string namespaceName, params object[] args )
        {
            var kwargs = ParseKwargs( args, new string[] { "package-name-prefix" } );
            var packageNamePrefix = ( string ) kwargs[ 0 ] ?? "";
            var types = GetNamespaceTypes( namespaceName );
            foreach ( var type in types )
            {
                var packageName = packageNamePrefix + type.Name.LispName();
                Import( type, packageName, null, true );
            }
        }

        [Lisp( "reference" )]
        public static void Reference( string assemblyName )
        {
            var a = assemblyName;
            if ( Path.GetExtension( a ) != ".dll" )
            {
                a = a + ".dll";
            }

            try
            {
                a = FindFileInPath( a );
                Assembly.LoadFile( a );
            }
            catch
            {
                try
                {
                    Assembly.LoadFrom( a );
                }
                catch
                {
                    try
                    {
                        Assembly.Load( a );
                    }
                    catch
                    {
                        try
                        {
                            Assembly.LoadFile( a );
                        }
                        catch
                        {
                            a = FindFileInPath( a );
                            Assembly.LoadFile( a );
                        }
                    }
                }
            }
        }

        internal static bool ExtendsType( MethodInfo method, Type type )
        {
            var ext = method.GetCustomAttributes( typeof( ExtendsAttribute ), true );

            if ( ext.Length != 0 && ( ( ExtendsAttribute ) ext[ 0 ] ).Type == type )
            {
                return true;
            }

            if ( method.GetCustomAttributes( typeof( ExtensionAttribute ), true ).Length == 0 )
            {
                return false;
            }

            var pars = method.GetParameters();
            if ( pars.Length == 0 )
            {
                return false;
            }

            if ( type != pars[ 0 ].ParameterType )
            {
                // maybe test for subclass
                return false;
            }

            return true;
        }

        internal static string FindFileInPath( string fileName )
        {
            string[] folders = System.Environment.GetEnvironmentVariable( "path" ).Split( ";" );
            foreach ( var s in folders )
            {
                var p = PathExtensions.Combine( s, fileName );
                if ( File.Exists( p ) )
                {
                    return p;
                }
            }
            return fileName;
        }

        internal static Cons GetMethodSyntax( MethodInfo method, Symbol context )
        {
            var name = context;
            if ( name == null )
            {
                var attrs = method.GetCustomAttributes( typeof( LispAttribute ), false );
                if ( attrs.Length != 0 )
                {
                    name = MakeSymbol( ( ( LispAttribute ) attrs[ 0 ] ).Names[ 0 ], LispDocPackage );
                }
                else
                {
                    name = MakeSymbol( method.Name.LispName(), LispDocPackage );
                }
            }

            var buf = new Vector();
            if ( !method.IsStatic )
            {
                buf.Add( MakeSymbol( "object", LispDocPackage ) );
            }
            foreach ( var arg in method.GetParameters() )
            {
                bool hasParamArray = arg.IsDefined( typeof( ParamArrayAttribute ), false );
                if ( hasParamArray )
                {
                    buf.Add( Symbols.Rest );
                }
                buf.Add( MakeSymbol( arg.Name.LispName(), LispDocPackage ) );
            }
            return MakeListStar( name, AsList( buf ) );
        }
        internal static Type GetTypeForImport( string typeName, Type[] typeParameters )
        {
            if ( typeParameters != null && typeParameters.Length != 0 )
            {
                typeName += "`" + typeParameters.Length.ToString();
            }

            Type type = null;

            if ( LastUsedAssembly == null || ( type = LastUsedAssembly.GetType( typeName, false, true ) ) == null )
            {
                LastUsedAssembly = null;

                foreach ( Assembly asm in AppDomain.CurrentDomain.GetAssemblies() )
                {
                    type = asm.GetType( typeName, false, true );

                    if ( type != null )
                    {
                        LastUsedAssembly = asm;
                        break;
                    }
                }
            }

            if ( type == null )
            {
                throw new LispException( "Undefined type: {0}", typeName );
            }

            if ( typeParameters != null && typeParameters.Length != 0 )
            {
                type = type.MakeGenericType( typeParameters );
            }

            return type;
        }

        internal static Package Import( Type type, string packageName, string extendsPackageName, bool export )
        {
            if ( !String.IsNullOrEmpty( extendsPackageName ) )
            {
                var package = GetPackage( extendsPackageName );
                ImportExtensionMethodsIntoPackage( package, type );
                return package;
            }
            else
            {
                var package = FindPackage( packageName );

                if ( package != null )
                {
                    if ( package.Reserved )
                    {
                        throw new LispException( "Cannot import into a reserved package name" );
                    }

                    var typeSym = package.FindInternal( "T" );
                    if ( typeSym != null && Eq( typeSym.Value, type ) )
                    {
                        // silently do nothing
                    }
                    else
                    {
                        Packages.Remove( packageName );
                        package = MakePackage( packageName );
                        ImportIntoPackage( package, type );
                    }
                }
                else
                {
                    package = MakePackage( packageName );
                    ImportIntoPackage( package, type );
                }

                var classSymbol = MakeSymbol( packageName, CurrentPackage(), export );
                SetFindType( classSymbol, type );

                return package;
            }
        }

        internal static void ImportExtensionMethodsIntoPackage( Package package, Type type )
        {
            var isbuiltin = type.Assembly == Assembly.GetExecutingAssembly();
            var extendedType = ( Type ) package.Dict[ "T" ].CheckedValue;
            var restrictedImport = type.GetCustomAttributes( typeof( RestrictedImportAttribute ), true ).Length != 0;
            var names = type.GetMethods( ImportBindingFlags ).Select( x => x.Name ).Distinct().ToArray();

            foreach ( var name in names )
            {
                var methods = type.GetMember( name, ImportBindingFlags )
                                  .Where( x => x is MethodInfo )
                                  .Select( x => ( MethodInfo ) ResolveGenericMethod( ( MethodInfo ) x ) )
                                  .Where( x => ExtendsType( x, extendedType ) )
                                  .ToList();

                if ( methods.Count == 0 )
                {
                    continue;
                }

                var importable = restrictedImport ? methods[ 0 ].GetCustomAttributes( typeof( LispAttribute ), true ).Length != 0 : true;

                if ( !importable )
                {
                    continue;
                }

                Symbol sym = package.InternNoInherit( name.LispName(), useMissing: true );
                var builtin = sym.Value as ImportedFunction;

                if ( builtin == null )
                {
                    sym.FunctionValue = builtin = new ImportedFunction( name, type );
                }

                if ( isbuiltin )
                {
                    // Designed to go before other methods.
                    methods.AddRange( builtin.BuiltinExtensionMembers );
                    builtin.BuiltinExtensionMembers = methods.Distinct().ToArray();
                }
                else
                {
                    // todo: change order
                    // Goes after other methods as in c#.
                    methods.AddRange( builtin.ExternalExtensionMembers );
                    builtin.ExternalExtensionMembers = methods.Distinct().ToArray();
                }

                sym.Package.Export( sym.Name );
            }
        }

        internal static void ImportIntoPackage( Package package, Type type )
        {
            AddPackageByType( type, package );

            var isbuiltin = type.Assembly == Assembly.GetExecutingAssembly();
            var restrictedImport = type.GetCustomAttributes( typeof( RestrictedImportAttribute ), true ).Length != 0;

            package.ImportedType = type;
            package.RestrictedImport = restrictedImport;
            Symbol sym = package.InternNoInherit( "T" );
            sym.ConstantValue = type;
            sym.Package.Export( sym.Name );
            sym.Documentation = MakeList( String.Format( "The .NET type <{0}> imported in this package.", type ) );

            if ( !ToBool( Symbols.LazyImport.Value ) )
            {
                VerifyNoMissingSymbols( package );
            }
        }

        internal static void ImportMissingSymbol( string name, Package package )
        {
            name = name.LispToPascalCaseName();

            if ( name.StartsWith( "set-" ) )
            {
                name = name.Substring( 4 );
            }
            else if ( name == "New" )
            {
                name = ".ctor";
            }

            var members = package.ImportedType.GetMember( name, ImportBindingFlags ).Select( x => ResolveGenericMethod( x ) ).ToArray();

            if ( members.Length == 0 )
            {
                return;
            }

            var importable = package.RestrictedImport ? members[ 0 ].GetCustomAttributes( typeof( LispAttribute ), true ).Length != 0 : true;

            if ( !importable )
            {
                return;
            }

            var field = members[ 0 ] as FieldInfo;

            if ( field != null )
            {
                if ( field.IsLiteral || ( field.IsStatic && field.IsInitOnly ) )
                {
                    Symbol sym = package.InternNoInherit( members[ 0 ].Name.LispName().ToUpper() );
                    sym.ConstantValue = field.GetValue( null );
                    sym.Package.Export( sym.Name );
                }
                return;
            }

            if ( members[ 0 ] is EventInfo )
            {
                Symbol sym = package.InternNoInherit( members[ 0 ].Name.LispName() );
                sym.ConstantValue = members[ 0 ];
                sym.Package.Export( sym.Name );
                return;
            }

            if ( members[ 0 ] is ConstructorInfo )
            {
                var builtin = new ImportedConstructor( members.Cast<ConstructorInfo>().ToArray() );
                Symbol sym = package.InternNoInherit( "new" );
                sym.FunctionValue = builtin;
                sym.Package.Export( sym.Name );
                package.ImportedConstructor = builtin;
                return;
            }

            if ( members[ 0 ] is MethodInfo )
            {
                if ( !name.StartsWith( "get_" ) && !name.StartsWith( "set_" ) )
                {
                    var sym = package.InternNoInherit( members[ 0 ].Name.LispName() );
                    var builtin = new ImportedFunction( members[ 0 ].Name, members[ 0 ].DeclaringType, members.Cast<MethodInfo>().ToArray(), false );
                    sym.FunctionValue = builtin;
                    sym.Package.Export( sym.Name );
                }

                return;
            }

            if ( members[ 0 ] is PropertyInfo )
            {
                var properties = members.Cast<PropertyInfo>().ToArray();
                var getters = properties.Select( x => x.GetGetMethod() ).Where( x => x != null ).ToArray();
                var setters = properties.Select( x => x.GetSetMethod() ).Where( x => x != null ).ToArray();

                if ( getters.Length != 0 )
                {
                    Symbol sym = package.InternNoInherit( members[ 0 ].Name.LispName() );
                    var builtin = new ImportedFunction( members[ 0 ].Name, members[ 0 ].DeclaringType, getters, false );
                    sym.FunctionValue = builtin;
                    sym.Package.Export( sym.Name );
                }

                if ( setters.Length != 0 )
                {
                    // use set-xxx
                    Symbol sym = package.InternNoInherit( "set-" + members[ 0 ].Name.LispName() );
                    var builtin = new ImportedFunction( members[ 0 ].Name, members[ 0 ].DeclaringType, setters, false );
                    sym.FunctionValue = builtin;
                    sym.Package.Export( sym.Name );
                }
                return;
            }
        }

        internal static object InvokeMember( object target, string name, params object[] args )
        {
            var binder = GetInvokeMemberBinder( new InvokeMemberBinderKey( name, args.Length ) );
            var exprs = new List<Expression>();
            exprs.Add( Expression.Constant( target ) );
            exprs.AddRange( args.Select( x => Expression.Constant( x ) ) );
            var proc = CompileToFunction( CompileDynamicExpression( binder, typeof( object ), exprs ) );
            return proc();
        }

        internal static bool NamespaceOrClassNameMatch( string pattern, Type type )
        {
            if ( type.FullName != null && type.FullName.WildcardMatch( pattern ) != null )
            {
                return true;
            }
            return false;
        }

        internal static MemberInfo ResolveGenericMethod( MemberInfo member )
        {
            if ( member is MethodInfo )
            {
                var method = ( MethodInfo ) member;
                if ( method.ContainsGenericParameters )
                {
                    var parameters = method.GetGenericArguments();
                    var types = new Type[ parameters.Length ];
                    for ( var i = 0; i < types.Length; ++i )
                    {
                        types[ i ] = typeof( object );
                    }
                    try
                    {
                        member = method.MakeGenericMethod( types );
                    }
                    catch ( Exception )
                    {
                    }
                }
            }

            return member;
        }

        internal static string TypeNameAlias( string name )
        {
            var index = name.LastIndexOf( "." );
            if ( index == -1 )
            {
                return "";
            }
            else
            {
                return name.Substring( 0, index ) + "+" + name.Substring( index + 1 );
            }
        }
        internal static void VerifyNoMissingSymbols( Package package )
        {
            if ( package.ImportedType != null && !package.ImportMissingDone )
            {
                package.ImportMissingDone = true;
                var names = package.ImportedType.GetMembers( ImportBindingFlags ).Select( x => x.Name ).Distinct().ToArray();
                foreach ( var name in names )
                {
                    ImportMissingSymbol( name, package );
                }
            }
        }
    }
}