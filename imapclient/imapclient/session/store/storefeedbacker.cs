using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cStoreFeedbacker
            {
                public enum eKeyType { msn, uid }

                public readonly eKeyType KeyType;
                private Dictionary<uint, cStoreFeedbackItemBase> mDictionary = new Dictionary<uint, cStoreFeedbackItemBase>();

                public cStoreFeedbacker()
                {
                    KeyType = eKeyType.msn;
                }

                public cStoreFeedbacker(cStoreFeedback pItems)
                {
                    KeyType = eKeyType.uid;

                    foreach (var lItem in pItems)
                    {
                        if (lItem.Handle.UID == null) throw new InvalidOperationException();
                        mDictionary[lItem.Handle.UID.UID] = lItem;
                    }
                }

                public cStoreFeedbacker(cUIDStoreFeedback pItems)
                {
                    KeyType = eKeyType.uid;
                    foreach (var lItem in pItems) mDictionary[lItem.UID.UID] = lItem;
                }

                public void Add(uint pMSN, cStoreFeedbackItem pItem)
                {
                    if (KeyType != eKeyType.msn) throw new InvalidOperationException();
                    mDictionary[pMSN] = pItem;
                }

                public int Count => mDictionary.Count;

                public bool ReceivedFlagsUpdate(uint pUInt)
                {
                    if (mDictionary.TryGetValue(pUInt, out var lItem))
                    {
                        lItem.ReceivedFlagsUpdate = true;
                        return true;
                    }

                    return false;
                }

                public bool WasNotUnchangedSince(uint pUInt)
                {
                    if (mDictionary.TryGetValue(pUInt, out var lItem))
                    {
                        lItem.WasNotUnchangedSince = true;
                        return true;
                    }

                    return false;
                }

                public IEnumerable<uint> UInts => mDictionary.Keys;

                public override string ToString()
                {
                    var lBuilder = new cListBuilder(nameof(cStoreFeedbacker));
                    foreach (var lItem in mDictionary) lBuilder.Append($"({lItem.Key},{lItem.Value.ReceivedFlagsUpdate},{lItem.Value.WasNotUnchangedSince})");
                    return lBuilder.ToString();
                }
            }
        }
    }
}