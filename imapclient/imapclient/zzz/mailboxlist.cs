using System;
using System.Collections;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient.zzz
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private class cMailboxList : IEnumerable<cMailboxList.cItem>
            {
                private Dictionary<string, cItem> mDictionary = new Dictionary<string, cItem>();

                public cMailboxList() { }

                public void Store(string pEncodedMailboxName, cMailboxName pMailboxName, cMailboxFlags pFlags)
                {
                    if (!mDictionary.TryGetValue(pEncodedMailboxName, out var lItem))
                    {
                        lItem = new cItem(pMailboxName);
                        mDictionary.Add(pEncodedMailboxName, lItem);
                    }

                    lItem.Flags |= pFlags;
                }

                public bool Store(string pEncodedMailboxName, cStatus pStatus)
                {
                    if (mDictionary.TryGetValue(pEncodedMailboxName, out var lItem))
                    {
                        lItem.Status = cMailboxStatus.Combine(pStatus, lItem.Status);
                        return true;
                    }

                    return false;
                }

                public int Count => mDictionary.Count;

                public IEnumerator<cItem> GetEnumerator()
                {
                    foreach (var lItem in mDictionary) yield return lItem.Value;
                }

                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

                public cItem FirstItem()
                {
                    foreach (var lItem in mDictionary) return lItem.Value;
                    throw new InvalidOperationException();
                }

                public override string ToString()
                {
                    cListBuilder lBuilder = new cListBuilder(nameof(cMailboxList));
                    foreach (var lItem in mDictionary) lBuilder.Append(lItem.Value);
                    return lBuilder.ToString();
                }

                public class cItem
                {
                    public readonly cMailboxName MailboxName;
                    public cMailboxFlags Flags = null;
                    public cStatus Status = null;

                    public cItem(cMailboxName pMailboxName) { MailboxName = pMailboxName; }
                }
            }
        }
    }
}