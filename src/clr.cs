// Copyright (C) 2012-2013 Jan Tolenaar. See the file LICENSE for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Reflection;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.IO;

namespace Kiezel
{
    public partial class Runtime
    {
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

        [Lisp("reference")]
        public static void Reference( string assemblyName )
        {
            var a = assemblyName;
            if ( Path.GetExtension( a ) != ".dll" )
            {
                a = a + ".dll";
            }

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

        internal static bool NamespaceOrClassNameMatch( string pattern, Type type )
        {
            //if ( String.Compare( pattern, type.FullName, true ) == 0 )
            //{
            //    return true;
            //}
            //else 
            if ( type.FullName != null && type.FullName.WildcardMatch( pattern ) != null )
            {
                return true;
            }
            return false;
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

        internal static Type GetTypeForImport( string typeName, Type[] typeParameters )
        {
            if ( typeParameters != null && typeParameters.Length != 0 )
            {
                typeName += "`" + typeParameters.Length.ToString();
            }

            Type type = null;

            foreach ( Assembly asm in AppDomain.CurrentDomain.GetAssemblies() )
            {
                type = asm.GetType( typeName, false, true );
                
                if ( type != null )
                {
                    break;
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

        internal static BindingFlags ImportBindingFlags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;

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

        [Lisp( "import" )]
        public static Package Import( string typeName, params object[] args )
        {
            var kwargs = ParseKwargs( args, new string[] { "package-name", "extends-package-name", "export", "type-parameters" }, null, null, true, null );
            var typeParameters = ToIter( ( Cons ) kwargs[ 3 ] ).Cast<Symbol>().Select( GetType ).Cast<Type>().ToArray();
            var type = GetTypeForImport( typeName, typeParameters );
            var packageName = GetDesignatedString( kwargs[ 0 ] ?? type.Name.LispName() );
            var packageName2 = GetDesignatedString( kwargs[ 1 ] );
            var export = ToBool( kwargs[ 2 ] );

            return Import( type, packageName, packageName2, export );
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
                                  .Select( x => ( MethodInfo ) x )
                                  .Where( x => ExtendsType( x, extendedType))
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

                Symbol sym = package.InternNoInherit( name.LispName() );

                if ( sym.Value is ImportedFunction )
                {
                    if ( isbuiltin )
                    {
                        // Designed to go before other methods.
                        methods.AddRange( ( ( ImportedFunction ) sym.Value ).Members );
                        methods = methods.Distinct().ToList();
                    }
                    else
                    {
                        // todo: change order
                        // Goes after other methods as in c#.
                        methods.AddRange( ( ( ImportedFunction ) sym.Value ).Members );
                        methods = methods.Distinct().ToList();
                    }
                }

                var builtin = new ImportedFunction( methods.ToArray(), false );
                sym.FunctionValue = builtin;
                sym.Package.Export( sym.Name );
            }
        }

        internal static void ImportIntoPackage( Package package, Type type )
        {
            var isbuiltin = type.Assembly == Assembly.GetExecutingAssembly();
            var restrictedImport = type.GetCustomAttributes( typeof( RestrictedImportAttribute ), true ).Length != 0;
            var names = type.GetMembers( ImportBindingFlags ).Select( x => x.Name ).Distinct().ToArray();

            if ( true )
            {
                package.ImportedType = type;
                Symbol sym = package.InternNoInherit( "T" );
                sym.ConstantValue = type;
                sym.Package.Export( sym.Name );
                sym.Documentation = MakeList( String.Format( "The .NET type <{0}> imported in this package.", type ) );
            }

            foreach ( var name in names )
            {
                var members = type.GetMember( name, ImportBindingFlags ).ToArray();
                var importable = restrictedImport ? members[ 0 ].GetCustomAttributes( typeof( LispAttribute ), true ).Length != 0 : true;

                if ( !importable )
                {
                    continue;
                }

                var fields = members.Select( x => x as FieldInfo ).Where( x => x != null ).ToArray();

                if ( fields.Length != 0 )
                {
                    var field = fields[ 0 ];
                    if ( field.IsLiteral || ( field.IsStatic && field.IsInitOnly ) )
                    {
                        Symbol sym = package.InternNoInherit( name.LispName().ToUpper() );
                        sym.ConstantValue = field.GetValue( null );
                        sym.Package.Export( sym.Name );
                    }
                    continue;
                }

                var events = members.Select( x => x as EventInfo ).Where( x => x != null ).ToArray();

                if ( events.Length != 0 )
                {
                    var evt = events[ 0 ];
                    Symbol sym = package.InternNoInherit( /*"event-" +*/ name.LispName() );
                    sym.ConstantValue = evt;
                    sym.Package.Export( sym.Name );
                    continue;
                }

                var constructors = members.Select( x => x as ConstructorInfo ).Where( x => x != null ).ToArray();

                if ( constructors.Length != 0 )
                {
                    var builtin = new ImportedConstructor( constructors );
                    Symbol sym = package.InternNoInherit( "new" );
                    sym.FunctionValue = builtin;
                    sym.Package.Export( sym.Name );
                    package.ImportedConstructor = builtin;
                    continue;
                }

                var methods = members.Select( x => x as MethodInfo ).Where( x => x != null ).ToArray();

                if ( methods.Length != 0 && !name.StartsWith( "get_" ) && !name.StartsWith("set_") )
                {
                    var sym = package.InternNoInherit( name.LispName() );
                    var builtin = new ImportedFunction( methods, false );
                    sym.FunctionValue = builtin;
                    sym.Package.Export( sym.Name );
                    continue;
                }

                var properties = members.Select( x => x as PropertyInfo ).Where( x => x != null ).ToArray();
                
                if ( properties.Length != 0 )
                {
                    var getters = properties.Select( x => x.GetGetMethod() ).Where( x => x != null ).ToArray();
                    var setters = properties.Select( x => x.GetSetMethod() ).Where( x => x != null ).ToArray();

                    if ( getters.Length != 0 )
                    {
                        Symbol sym = package.InternNoInherit( name.LispName() );
                        var builtin = new ImportedFunction( getters, false );
                        sym.FunctionValue = builtin;
                        sym.Package.Export( sym.Name );
                    }

                    if ( setters.Length != 0 )
                    {
                        // use set-xxx
                        Symbol sym = package.InternNoInherit( "set-" + name.LispName() );
                        var builtin = new ImportedFunction( setters, false );
                        sym.FunctionValue = builtin;
                        sym.Package.Export( sym.Name );
                    }
                    continue;
                }
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

        internal static object InvokeMember( string name, params object[] args )
        {
            var binder = GetInvokeMemberBinder( new InvokeMemberBinderKey( name, args.Length ) );
            var exprs = new List<Expression>();
            exprs.AddRange( args.Select( x => Expression.Constant( x ) ) );
            var proc = CompileToFunction( CompileDynamicExpression( binder, typeof( object ), exprs ) );
            return proc();
        }

        [Lisp( "add-event-handler" )]
        public static void AddEventHandler( System.Reflection.EventInfo eventinfo, object target, object func )
        {
            var type = eventinfo.EventHandlerType;
            var closure = GetClosure( func );
            var dlg = ConvertToDelegate( type, closure );
            eventinfo.AddEventHandler( target, dlg );
        }

  
    }
}
