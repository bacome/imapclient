using System;
using System.Diagnostics;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        /// <summary>
        /// Internal library tests.
        /// </summary>
        /// <param name="pParentContext"></param>
        [Conditional("DEBUG")]
        public static void _Tests(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cIMAPClient), nameof(_Tests));
            cBytesCursor._Tests(lContext);
            cModifiedUTF7._Tests(cTrace.cContext.None);
            cBase64._Tests(cTrace.cContext.None);
            cSession._Tests(cTrace.cContext.None);
            cCredentials._Tests(cTrace.cContext.None);
            cURI._Tests(lContext);
            cURLParts._Tests(lContext);
            cURIParts._Tests(lContext);
            cMailboxPathPattern._Tests(lContext);
            cCulturedString._Tests(lContext);
            cMailboxName._Tests(lContext);
            cBatchSizer._Tests(lContext);
            //cHeaderFieldNames._Tests(lContext);
            cHeaderFieldNameList._Tests(lContext);
            cHeaderFields._Tests(lContext);
            cStorableFlagList._Tests(lContext);
        }

        private partial class cSession
        {
            [Conditional("DEBUG")]
            public static void _Tests(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cSession), nameof(_Tests));
                cCommandPipeline._Tests(lContext);
                cIdDataProcessor._Tests(lContext);
                cNamespaceDataProcessor._Tests(lContext);
                //cCommandHookList._Tests(lContext);
                //cCommandHookLSub._Tests(lContext);
                //cListExtendedCommandHook._Tests(lContext);
                cResponseDataParserFetch._Tests(lContext);
                cResponseDataParserESearch._Tests(lContext);
                //_Tests_ListExtendedCommandParts(lContext);
                cCommandDetailsBuilder._Tests(lContext);
                cQuotedPrintableDecoder._Tests(lContext);
            }
        }
    }
}