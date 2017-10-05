using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cStoreFeedback
            {
                public readonly bool UID;
                private Dictionary<uint, cStoreFeedbackItem> mDictionary = new Dictionary<uint, cStoreFeedbackItem>();

                public cStoreFeedback(bool pUID)
                {
                    UID = pUID;
                }

                public void Add(cUID pUID)
                {
                    if (!UID) throw new InvalidOperationException();
                    mDictionary[pUID.UID] = new cStoreFeedbackItem(pUID);
                }

                public void Add(uint pUInt, iMessageHandle pHandle) => mDictionary[pUInt] = new cStoreFeedbackItem(pHandle); // either mapping from a UID to a handle, or an MSN to a handle

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
                public IEnumerable<cStoreFeedbackItem> Items => mDictionary.Values;

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

            private class cStoreFeedbackItem
            {
                public readonly cUID UID;
                public readonly iMessageHandle Handle;
                public bool Fetched = false;
                public bool Modified = false;

                public cStoreFeedbackItem(cUID pUID)
                {
                    UID = pUID ?? throw new ArgumentNullException(nameof(pUID));
                    Handle = null;
                }

                public cStoreFeedbackItem(iMessageHandle pHandle)
                {
                    UID = null;
                    Handle = pHandle ?? throw new ArgumentNullException(nameof(pHandle));
                }

                public override string ToString() => $"{nameof(cStoreFeedbackItem)}({UID},{Handle},{Fetched},{Modified})";
            }
        }
    }
}