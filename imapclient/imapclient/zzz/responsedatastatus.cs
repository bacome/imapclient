using System;
using System.Diagnostics;
using work.bacome.imapclient.support;
using work.bacome.trace;

namespace work.bacome.imapclient.zzz
{
    /*
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cResponseDataStatus
            {

                public readonly string EncodedMailboxName; // NOT converted from Modified-UTF7 if it is in use
                public readonly cStatus Status;

                private cResponseDataStatus(string pEncodedMailboxName, cStatus pStatus)
                {
                    EncodedMailboxName = pEncodedMailboxName;
                    Status = pStatus;
                }

                public override string ToString() => $"{nameof(cResponseDataStatus)}({EncodedMailboxName},{Status})";

                public static bool Process(cBytesCursor pCursor, out cResponseDataStatus rResponseData, cTrace.cContext pParentContext)
                {
                    //  NOTE: this routine does not return the cursor to its original position if it fails

                    var lContext = pParentContext.NewMethod(nameof(cResponseDataStatus), nameof(Process));


                    pCursor.ParsedAs = rResponseData;

                    return rResponseData != null;
                }

                [Conditional("DEBUG")]
                public static void _Tests(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cResponseDataStatus),nameof(_Tests));

                    cBytesCursor.TryConstruct("blurdybloop (MESSAGES 231 UIDNEXT 44292)", out var lCursor);

                    if (!cResponseDataStatus.Process(lCursor, out var lStatus, lContext)) throw new cTestsException("status response 1");
                    if (lStatus.EncodedMailboxName != "blurdybloop") throw new cTestsException("status response 1.1");
                    if (!(lStatus.Status.Messages == 231 && lStatus.Status.UIDValidity == null && lStatus.Status.Unseen == null)) throw new cTestsException("status response 1.2");
                }
            }
        }
    }*/
}