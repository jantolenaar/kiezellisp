using System;
using System.Reflection;
using System.Runtime.InteropServices;

// This file is shared by all framework and netcore assemblies of Kiezellisp.

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Kiezellisp")]
#if DEBUG
[assembly: AssemblyDescription("Debug Build")]
#else
[assembly: AssemblyDescription("Release Build")]
#endif
[assembly: AssemblyProduct("Kiezellisp")]
[assembly: AssemblyCopyright("Copyright \u00a9 Jan Tolenaar 2009-2020")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("da6eeacf-da3a-44ed-98f0-925c70ed3017")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
//
// In framework, the build numbers are inserted in the file version and the product version.
// In net core, the build numbers are inserted in the product version only.

[assembly: AssemblyVersion("4.0.*")]
