using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("IMAP Client Library.")]
[assembly: AssemblyDescription(
    "Intended for use in projects that need to deal with email presented by an IMAP server where the programmer does not want to learn the finer points of IMAP.\r\n" +
    "\r\n" +
    "* Simple high level API with the ability to get low level if/when required.\r\n" +
    "* Complete API documentation.\r\n" +
    "* Synchronous and asynchronous APIs with timeout and cancellation.\r\n" +
    "* Automatic idle processing to keep the client in sync with the server.\r\n" +
    "* Condstore support for 'mailbox as a message queue' applications.\r\n" +
    "* Automatic safe IMAP command pipelining.\r\n" +
    "\r\n" +
    "As this is an alpha release;\r\n" +
    "* All the APIs are subject to change (however most aren't likely to do so), and\r\n" +
    "* There is a significant (depending on your use case) missing feature: Append - the ability to upload mail to an IMAP server.\r\n" +
    "\r\n" +
    "High level documentation, API level documentation, examples and source code are available on/via the project site.\r\n" +
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
[assembly: AssemblyVersion("0.6.*")]
[assembly: AssemblyFileVersion("0.6")]
[assembly: AssemblyInformationalVersion("0.6.0-alpha")]
