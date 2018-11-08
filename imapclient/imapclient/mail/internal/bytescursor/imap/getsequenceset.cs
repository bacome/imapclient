﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using work.bacome.imapclient;
using work.bacome.mailclient.support;

namespace work.bacome.mailclient
{
    internal partial class cBytesCursor
    {
        public bool GetSequenceSet(bool pAsteriskAllowed, out cSequenceSet rSequenceSet)
        {
            var lBookmark = Position;

            List<cSequenceSetItem> lItems = new List<cSequenceSetItem>();

            while (true)
            {
                if (!ZGetSequenceSetItem(pAsteriskAllowed, out var lItem))
                {
                    Position = lBookmark;
                    rSequenceSet = null;
                    return false;
                }

                lItems.Add(lItem);

                if (!SkipByte(cASCII.COMMA))
                {
                    rSequenceSet = new cSequenceSet(lItems);
                    return true;
                }
            }
        }

        private bool ZGetSequenceSetItem(bool pAsteriskAllowed, out cSequenceSetItem rItem)
        {
            uint lNumber;
            cSequenceSetRangePart lItem;

            if (pAsteriskAllowed && SkipByte(cASCII.ASTERISK)) lItem = cSequenceSetItem.Asterisk;
            else
            {
                if (!GetNZNumber(out _, out lNumber)) { rItem = null; return false; }
                lItem = new cSequenceSetNumber(lNumber);
            }

            var lBookmark = Position;

            if (!SkipByte(cASCII.COLON))
            {
                rItem = lItem;
                return true;
            }

            if (pAsteriskAllowed && SkipByte(cASCII.ASTERISK))
            {
                rItem = new cSequenceSetRange(lItem, cSequenceSetItem.Asterisk);
                return true;
            }

            if (GetNZNumber(out _, out lNumber)) rItem = new cSequenceSetRange(lItem, new cSequenceSetNumber(lNumber));
            else
            {
                Position = lBookmark;
                rItem = lItem;
            }

            return true;
        }

        [Conditional("DEBUG")]
        private static void _Tests_SequenceSet(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cBytesCursor), nameof(_Tests_SequenceSet));

        }
    }
}