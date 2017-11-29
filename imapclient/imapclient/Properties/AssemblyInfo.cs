using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("work.bacome.imapclient")]
[assembly: AssemblyDescription(
    "Simple IMAP mail access with the ability to get complex if/ when required.\r\n" +
    "\r\n" +
    "Synchronous and asynchronous APIs with timeouts and cancellation," +
    "automatic idle processing to keep the client in sync with the server, " +
    "conditional-store support for 'mailbox as a message queue' applications.\r\n" +
    "\r\n" +
    "This is the initial alpha release: all the APIs are subject to change, however most aren't likely to.\r\n" +
    "SIGNIFICANT MISSING FEATURE: Append: the ability to upload mail to an IMAP server.\r\n" +
    "\r\n" +
    "High level documentation, API level documentation and source code are available via the project site.\r\n" +
    "")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("bacome")]
[assembly: AssemblyProduct("imapclient")]
[assembly: AssemblyCopyright("Copyright © bacome 2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("99043948-67b0-4136-b81f-b5f52d4259a3")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("0.5.*")]
[assembly: AssemblyFileVersion("0.5")]
[assembly: AssemblyInformationalVersion("0.5.0-alpha01")]
