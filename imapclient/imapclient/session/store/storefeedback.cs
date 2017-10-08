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

                public bool Fetched(uint pUInt)
                {
                    if (mDictionary.TryGetValue(pUInt, out var lItem))
                    {
                        lItem.Fetched = true;
                        return true;
                    }

                    return false;
                }

                public bool Modified(uint pUInt)
                {
                    if (mDictionary.TryGetValue(pUInt, out var lItem))
                    {
                        lItem.Modified = true;
                        return true;
                    }

                    return false;
                }

                public IEnumerable<uint> UInts => mDictionary.Keys;

                public override string ToString()
                {
                    var lBuilder = new cListBuilder(nameof(cStoreFeedback));

                    bool lFirst = true;

                    foreach (var lItem in mDictionary.Values)
                    {
                        if (lFirst)
                        {
                            lFirst = false;
                            if (lItem.UID == null) lBuilder.Append(lItem.Handle.Cache);
                            else lBuilder.Append(lItem.UID.UIDValidity);
                        }

                        if (lItem.UID == null) lBuilder.Append(lItem.Handle.CacheSequence);
                        else lBuilder.Append(lItem.UID.UID);
                    }

                    return lBuilder.ToString();
                }
            }



        }
    }
}