using System;
using work.bacome.imapsupport;

namespace work.bacome.imapclient_tests
{
    internal static class kTrace
    {
        private const string kTraceSourceName = "work.bacome.imapinternalstests";
        private static readonly cTrace mTrace = new cTrace(kTraceSourceName);
        public static readonly cTrace.cContext Root = mTrace.NewRoot("global");
    }
}