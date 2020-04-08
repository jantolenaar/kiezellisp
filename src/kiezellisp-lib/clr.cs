﻿#region Header

// Copyright (C) Jan Tolenaar. See the file LICENSE for details.

#endregion Header

namespace Kiezel
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    public partial class Runtime
    {
        #region Static Fields

        public static BindingFlags ImportBindingFlags = BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;
        public static Assembly LastUsedAssembly;

        #endregion Static Fields

        #region Public Methods

        [Lisp("add-event-handler")]
        public static void AddEventHandler(EventInfo eventinfo, object target, IApply func)
        {
            var type = eventinfo.EventHandlerType;
            var dlg = ConvertToDelegate(type, func);
            eventinfo.AddEventHandler(target, dlg);
        }

        public static bool ExtendsType(MethodInfo method, Type type)
        {
            var ext = method.GetCustomAttributes(typeof(ExtendsAttribute), true);

            if (ext.Length != 0 && ((ExtendsAttribute)ext[0]).Type == type)
            {
                return true;
            }

            if (method.GetCustomAttributes(typeof(ExtensionAttribute), true).Length == 0)
            {
                return false;
            }

            var pars = method.GetParameters();
            if (pars.Length == 0)
            {
                return false;
            }

            if (type != pars[0].ParameterType)
            {
                // maybe test for subclass
                return false;
            }

            return true;
        }

        public static string FindFileInPath(string fileName)
        {
            var folders = (Cons)GetDynamic(Symbols.AssemblyPath);
            foreach (string s in ToIter(folders))
            {
                var p = PathExtensions.Combine(s, fileName);
                if (File.Exists(p))
                {
                    return p;
                }
            }
            return fileName;
        }

        public static Cons GetMethodSyntax(MethodInfo method, Symbol context)
        {
            var name = context;
            if (name == null)
            {
                var attrs = method.GetCustomAttributes(typeof(LispAttribute), false);
                if (attrs.Length != 0)
                {
                    name = CreateDocSymbol(((LispAttribute)attrs[0]).Names[0]);
                }
                else
                {
                    name = CreateDocSymbol(method.Name.LispName());
                }
            }

            var buf = new Vector();
            if (!method.IsStatic)
            {
                buf.Add(CreateDocSymbol("object"));
            }
            foreach (var arg in method.GetParameters())
            {
                bool hasParamArray = arg.IsDefined(typeof(ParamArrayAttribute), false);
                if (hasParamArray)
                {
                    buf.Add(Symbols.Rest);
                }
                buf.Add(CreateDocSymbol(arg.Name.LispName()));
            }
            return MakeListStar(name, AsList(buf));
        }

        //[Lisp("get-namespace-types")]
        public static List<Type> GetNamespaceTypes(string pattern)
        {
            var allTypes = new List<Type>();
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var types = asm.GetTypes();
                allTypes.AddRange(types.Where(t => NamespaceOrClassNameMatch(pattern, t)));
            }
            return allTypes;
        }

        public static object GetStaticPropertyValue(Type type, object ident)
        {
            var flags = BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public;
            var name = GetDesignatedString(ident).LispToPascalCaseName();
            var member = type.GetProperty(name, flags);
            if (member != null)
            {
                return member.GetValue(null);
            }
            else
            {
                return null;
            }
        }

        public static string GetFirstAssemblyLocation()
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies().First();
            return asm.Location;
        }

        [Lisp("list-assemblies")]
        public static void ListAssemblies()
        {
            var i = 0;

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                ++i;
                PrintLine(i, ": ", asm.FullName);
            }

            PrintLine();
            i = 0;

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                ++i;
                if (asm.IsDynamic)
                {
                    PrintLine(i, ": ", "(dynamic)");
                }
                else
                {
                    PrintLine(i, ": ", asm.Location);
                }
            }
        }

        public static Type GetTypeForImport(string typeName, Type[] typeParameters)
        {
            if (typeParameters != null && typeParameters.Length != 0)
            {
                typeName += "`" + typeParameters.Length;
            }

            Type type = null;

            if (LastUsedAssembly == null || (type = LastUsedAssembly.GetType(typeName, false, true)) == null)
            {
                LastUsedAssembly = null;

                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType(typeName, false, true);

                    if (type != null)
                    {
                        LastUsedAssembly = asm;
                        break;
                    }
                }
            }

            if (type == null)
            {
                throw new LispException("Undefined type: {0}", typeName);
            }

            if (typeParameters != null && typeParameters.Length != 0)
            {
                type = type.MakeGenericType(typeParameters);
            }

            return type;
        }

        [Lisp("import")]
        public static Package Import(string typeName, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "package-name", "package-name-prefix", "extends-package-name", "type-parameters" }, null, null, null, null);
            var typeParameters = ToIter((Cons)kwargs[3]).Cast<Symbol>().Select(GetType).Cast<Type>().ToArray();
            var type = GetTypeForImport(typeName, typeParameters);
            var prefix = GetDesignatedString(kwargs[1] ?? GetDynamic(Symbols.PackageNamePrefix));
            var packageName = GetDesignatedString(kwargs[0] ?? prefix + type.Name.LispName());
            var packageName2 = GetDesignatedString(kwargs[2]);

            return Import(type, packageName, packageName2);
        }

        public static Package Import(Type type, string packageName, string extendsPackageName)
        {
            if (!string.IsNullOrEmpty(extendsPackageName))
            {
                var package = GetPackage(extendsPackageName);
                ImportExtensionMethodsIntoPackage(package, type);
                return package;
            }
            else
            {
                var package = FindPackage(packageName);

                if (package != null)
                {
                    if (package.Reserved)
                    {
                        throw new LispException("Cannot import into a reserved package name");
                    }

                    var typeSym = package.Find("T");
                    if (typeSym != null && Eq(typeSym.Value, type))
                    {
                        // silently do nothing
                    }
                    else
                    {
                        Packages.Remove(packageName);
                        package = MakePackage3(packageName);
                        ImportIntoPackage(package, type);
                    }
                }
                else
                {
                    package = MakePackage3(packageName);
                    ImportIntoPackage(package, type);
                }

                var classSymbol = MakeSymbol("lisp:", packageName);
                SetFindType(classSymbol, type);

                return package;
            }
        }

        public static void ImportExtensionMethodsIntoPackage(Package package, Type type)
        {
            var isbuiltin = type.Assembly == Assembly.GetExecutingAssembly();
            var extendedType = (Type)package.Dict["T"].CheckedValue;
            var restrictedImport = type.GetCustomAttributes(typeof(RestrictedImportAttribute), true).Length != 0;
            var names = type.GetMethods(ImportBindingFlags).Select(x => x.Name).Distinct().ToArray();

            foreach (var name in names)
            {
                var methods = type.GetMember(name, ImportBindingFlags)
                                  .Where(x => x is MethodInfo)
                                  .Select(x => (MethodInfo)ResolveGenericMethod((MethodInfo)x))
                                  .Where(x => ExtendsType(x, extendedType))
                                  .ToList();

                if (methods.Count == 0)
                {
                    continue;
                }

                var importable = !restrictedImport || methods[0].GetCustomAttributes(typeof(LispAttribute), true).Length != 0;

                if (!importable)
                {
                    continue;
                }

                var sym = package.Create(name.LispName(), ispublic: true);
                var builtin = sym.Value as ImportedFunction;

                if (builtin == null)
                {
                    sym.FunctionValue = builtin = new ImportedFunction(name, type);
                }

                if (isbuiltin)
                {
                    // Designed to go before other methods.
                    methods.AddRange(builtin.BuiltinExtensionMembers);
                    builtin.BuiltinExtensionMembers = methods.Distinct().ToArray();
                }
                else
                {
                    // todo: change order
                    // Goes after other methods as in c#.
                    methods.AddRange(builtin.ExternalExtensionMembers);
                    builtin.ExternalExtensionMembers = methods.Distinct().ToArray();
                }

            }
        }

        public static void ImportIntoPackage(Package package, Type type)
        {
            AddPackageByType(type, package);

            var restrictedImport = type.GetCustomAttributes(typeof(RestrictedImportAttribute), true).Length != 0;

            package.ImportedType = type;
            package.RestrictedImport = restrictedImport;
            var sym = package.Create("T", ispublic: true);
            sym.ConstantValue = type;
            sym.Documentation = string.Format("The .NET type <{0}> imported in this package.", type);

            ImportMembers(package);
        }

        public static void ImportMembers(Package package)
        {
            var type = package.ImportedType;
            var names = type.GetMembers(Runtime.ImportBindingFlags).Select(x => x.Name).Distinct().ToArray();
            foreach (var name in names)
            {
                var members = type.GetMember(name, ImportBindingFlags).Select(x => ResolveGenericMethod(x)).ToArray();
                Runtime.ImportMembers(members, package);
            }              
        }

        public static bool ImportMembers(MemberInfo[] members,Package package)
        {
            if (members.Length == 0)
            {
                return false;
            }

            var member = members[0];
            var importable = !package.RestrictedImport || member.GetCustomAttributes(typeof(LispAttribute), true).Length != 0;

            if (!importable)
            {
                return false;
            }

            var name = member.Name;
            var ucName = name.LispName().ToUpper();
            var lcName = name.LispName();

            if (member is FieldInfo)
            {
                var field = member as FieldInfo;
                if (field.IsLiteral || (field.IsStatic && field.IsInitOnly))
                {
                    var sym = package.Create(ucName, ispublic: true);
                    sym.ConstantValue = field.GetValue(null);
                }
                return true;
            }

            if (member is EventInfo)
            {
                var sym = package.Create(lcName, ispublic: true);
                sym.ConstantValue = member;
                return true;
            }

            if (member is ConstructorInfo)
            {
                var builtin = new ImportedConstructor(members.Cast<ConstructorInfo>().ToArray());
                var sym = package.Create("new", ispublic: true);
                sym.FunctionValue = builtin;
                return true;
            }

            if (member is MethodInfo)
            {
                var sym = package.Create(lcName, ispublic: true);
                var builtin = new ImportedFunction(name, member.DeclaringType, members.Cast<MethodInfo>().ToArray(), false);
                sym.FunctionValue = builtin;
                return true;
            }

            if (member is PropertyInfo)
            {
                var properties = members.Cast<PropertyInfo>().ToArray();
                var getters = properties.Select(x => x.GetGetMethod()).Where(x => x != null).ToArray();
                var setters = properties.Select(x => x.GetSetMethod()).Where(x => x != null).ToArray();

                if (getters.Length != 0)
                {
                    var sym = package.Create(lcName, ispublic: true);
                    var builtin = new ImportedFunction(name, member.DeclaringType, getters, false);
                    sym.FunctionValue = builtin;
                }

                if (setters.Length != 0)
                {
                    // create getter symbol for setf/setq
                    package.Create(lcName, ispublic: true);
                    // use set-xxx
                    var sym = package.Create("set-" + lcName, ispublic: true);
                    var builtin = new ImportedFunction(name, member.DeclaringType, setters, false);
                    sym.FunctionValue = builtin;
                }

                return true;
            }

            return false;
        }

        [Lisp("import-namespace")]
        public static void ImportNamespace(string namespaceName, params object[] args)
        {
            var kwargs = ParseKwargs(args, new string[] { "package-name-prefix" });
            var packageNamePrefix = (string)kwargs[0] ?? "";
            var types = GetNamespaceTypes(namespaceName);
            foreach (var type in types)
            {
                var packageName = packageNamePrefix + type.Name.LispName();
                Import(type, packageName, null);
            }
        }

        public static object InvokeMember(object target, string name, params object[] args)
        {
            var binder = GetInvokeMemberBinder(new InvokeMemberBinderKey(name, args.Length));
            var exprs = new List<Expression>();
            exprs.Add(Expression.Constant(target));
            exprs.AddRange(args.Select(x => Expression.Constant(x)));
            var proc = CompileToFunction(CompileDynamicExpression(binder, typeof(object), exprs));
            return proc();
        }

        public static bool NamespaceOrClassNameMatch(string pattern, Type type)
        {
            if (type.FullName != null && type.FullName.WildcardMatch(pattern) != null)
            {
                return true;
            }
            return false;
        }

        [Lisp("reference")]
        public static void Reference(string assemblyName)
        {
            var a = assemblyName;
            if (a.IndexOf(",") == -1 && Path.GetExtension(a) != ".dll")
            {
                a = a + ".dll";
            }

            Assembly asm = null;

            try
            {
                a = FindFileInPath(a);
                asm = Assembly.LoadFile(a);
            }
            catch
            {
                try
                {
                    asm = Assembly.LoadFrom(a);
                }
                catch
                {
                    try
                    {
                        asm = Assembly.Load(a);
                    }
                    catch
                    {
                        try
                        {
                            asm = Assembly.LoadFile(a);
                        }
                        catch
                        {
                            a = FindFileInPath(a);
                            asm = Assembly.LoadFile(a);
                        }
                    }
                }
            }
        }

        public static MemberInfo ResolveGenericMethod(MemberInfo member)
        {
            if (member is MethodInfo)
            {
                var method = (MethodInfo)member;
                if (method.ContainsGenericParameters)
                {
                    var parameters = method.GetGenericArguments();
                    var types = new Type[parameters.Length];
                    for (var i = 0; i < types.Length; ++i)
                    {
                        types[i] = typeof(object);
                    }
                    try
                    {
                        member = method.MakeGenericMethod(types);
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return member;
        }

        public static string TypeNameAlias(string name)
        {
            var index = name.LastIndexOf(".");
            if (index == -1)
            {
                return "";
            }
            else
            {
                return name.Substring(0, index) + "+" + name.Substring(index + 1);
            }
        }


        #endregion Public Methods
    }
}