using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if !XBOX360
using System.Security.Permissions;
#endif

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("SharpSteer")]
[assembly: AssemblyProduct("SharpSteer")]
[assembly: AssemblyDescription("Steering behaviors for autonomous characters")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyCopyright("Copyright © 2007 Björn Graf")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: CLSCompliant(true)]
#if !XBOX360
//FIXME: [assembly: SecurityPermission(SecurityAction.RequestMinimum, Assertion = true, Execution = true, Unrestricted = true)]
#endif

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  
// This should never be true for Xbox 360 assemblies.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("7bf5711c-0447-40e7-9b7e-916cf8cbd74b")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
[assembly: AssemblyVersion("1.0.0.0")]