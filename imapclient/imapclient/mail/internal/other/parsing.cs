using System;
using System.Collections.Generic;

namespace work.bacome.mailclient
{
    internal static class cParsing
    {
        // parses message data
        //  this code accepts and outputs obsolete rfc 5322 syntax (as opposed to the cValidation routines)
        //  this should only be used on existing message data, not to construct a new message

        public static bool TryParseMsgId(IList<byte> pValue, out string rMessageId)
        {
            cBytesCursor lCursor = new cBytesCursor(pValue);

            if (lCursor.GetRFC822MsgId(out var lIdLeft, out var lIdRight) && lCursor.Position.AtEnd)
            {
                rMessageId = cTools.MessageId(lIdLeft, lIdRight);
                return true;
            }

            rMessageId = null;
            return false;
        }

        public static bool TryParseMsgIds(IList<byte> pValue, out cStrings rMessageIds)
        {
            List<string> lMessageIds = new List<string>();

            cBytesCursor lCursor = new cBytesCursor(pValue);

            while (true)
            {
                if (!lCursor.GetRFC822MsgId(out var lIdLeft, out var lIdRight)) break;
                lMessageIds.Add(cTools.MessageId(lIdLeft, lIdRight));
            }

            if (lCursor.Position.AtEnd)
            {
                rMessageIds = new cStrings(lMessageIds);
                return true;
            }

            rMessageIds = null;
            return false;
        }
    }
}