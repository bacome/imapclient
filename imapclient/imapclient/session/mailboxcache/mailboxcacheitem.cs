using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cMailboxCache
            {
                private class cItem : iMailboxHandle
                {
                    private object mMailboxCache;
                    private string mEncodedMailboxName;
                    private cMailboxName mMailboxName = null;
                    private bool? mExists = null;
                    private cMailboxFlags mMailboxFlags = null;
                    private cLSubFlags mLSubFlags = null;
                    private cMailboxFlags mMergedFlags = null; // the merge between the mailbox and the lsub flags
                    private cStatus mStatus = null;
                    private cMailboxStatus mMailboxStatus = null;
                    private cMailboxSelectedProperties mMailboxSelectedProperties = cMailboxSelectedProperties.NeverBeenSelected;

                    public cItem(object pMailboxCache, string pEncodedMailboxName)
                    {
                        mMailboxCache = pMailboxCache;
                        mEncodedMailboxName = pEncodedMailboxName;
                    }

                    public object MailboxCache => mMailboxCache;
                    public string EncodedMailboxName => mEncodedMailboxName;

                    public cMailboxName MailboxName
                    {
                        get => mMailboxName;
                        set => mMailboxName = value ?? throw new ArgumentNullException();
                    }

                    public bool? Exists => mExists;

                    public fMailboxProperties ResetExists(cMailboxNamePattern pPattern, int pMailboxFlagsSequence)
                    {
                        if (mExists == false) return 0; // already done
                        if (mMailboxName == null) return 0; // can't tell
                        if (mMailboxFlags != null && mMailboxFlags.Sequence > pMailboxFlagsSequence) return 0; // been refreshed recently => probably still exists
                        if (!pPattern.Matches(mMailboxName.Name)) return 0; // don't expect that it should have been refreshed
                        return ZResetExists();
                    }

                    public fMailboxProperties ResetExists(int pMailboxStatusSequence)
                    {
                        if (mExists == false) return 0; // already done
                        if (mMailboxStatus != null && mMailboxStatus.Sequence > pMailboxStatusSequence) return 0; // been refreshed recently => probably still exists
                        return ZResetExists();
                    }

                    public cMailboxFlags MailboxFlags
                    {
                        get
                        {
                            if (mMergedFlags == null && mMailboxFlags != null) mMergedFlags = mMailboxFlags.Merge(mLSubFlags);
                            return mMergedFlags;
                        }
                    }

                    public fMailboxProperties SetMailboxFlags(cMailboxFlags pMailboxFlags)
                    {
                        if (pMailboxFlags == null) throw new ArgumentNullException(nameof(pMailboxFlags));
                        fMailboxProperties lDifferences = ZSetExists() | cMailboxFlags.Differences(mMailboxFlags, pMailboxFlags);
                        mMailboxFlags = pMailboxFlags;
                        mMergedFlags = null;
                        return lDifferences;
                    }

                    public fMailboxProperties SetLSubFlags(cLSubFlags pLSubFlags)
                    {
                        if (pLSubFlags == null) throw new ArgumentNullException(nameof(pLSubFlags));
                        var lDifferences = ZSetExists() | cLSubFlags.Differences(mLSubFlags, pLSubFlags);
                        mLSubFlags = pLSubFlags;
                        mMergedFlags = null;
                        return lDifferences;
                    }

                    public fMailboxProperties ClearLSubFlags(cMailboxNamePattern pPattern, int pLSubFlagsSequence)
                    {
                        if (mLSubFlags == null) return 0;
                        if (mMailboxName == null) return 0; // can't tell
                        if (mLSubFlags.Sequence > pLSubFlagsSequence) return 0; // been refreshed recently
                        if (!pPattern.Matches(mMailboxName.Name)) return 0; // don't expect that it should have been refreshed

                        fMailboxProperties lDifferences = cLSubFlags.Differences(mLSubFlags, null);
                        mLSubFlags = null;
                        mMergedFlags = null;
                        return lDifferences;
                    }

                    public cStatus Status
                    {
                        get => mStatus;
                        set => mStatus = value ?? throw new ArgumentNullException();
                    }

                    public cMailboxStatus MailboxStatus => mMailboxStatus;

                    public fMailboxProperties SetMailboxStatus(cMailboxStatus pMailboxStatus)
                    {
                        if (pMailboxStatus == null) throw new ArgumentNullException(nameof(pMailboxStatus));
                        fMailboxProperties lDifferences = ZSetExists() | cMailboxStatus.Differences(mMailboxStatus, pMailboxStatus);
                        mMailboxStatus = pMailboxStatus;
                        return lDifferences;
                    }

                    public cMailboxSelectedProperties MailboxSelectedProperties => mMailboxSelectedProperties;

                    public fMailboxProperties UpdateMailboxSelectedProperties(cMessageFlags pMessageFlags, bool pSelectedForUpdate, cMessageFlags pPermanentFlags)
                    {
                        if (pMessageFlags == null) throw new ArgumentNullException(nameof(pMessageFlags));
                        if (pPermanentFlags == null) throw new ArgumentNullException(nameof(pPermanentFlags));
                        var lMailboxSelectedProperties = mMailboxSelectedProperties.Update(pMessageFlags, pSelectedForUpdate, pPermanentFlags);
                        fMailboxProperties lDifferences = ZSetExists() | cMailboxSelectedProperties.Differences(mMailboxSelectedProperties, lMailboxSelectedProperties);
                        mMailboxSelectedProperties = lMailboxSelectedProperties;
                        return lDifferences;
                    }

                    private fMailboxProperties ZResetExists()
                    {
                        mExists = false;
                        mMailboxFlags = null;
                        mLSubFlags = null;
                        mMergedFlags = null;
                        mStatus = null;
                        mMailboxStatus = null;
                        mMailboxSelectedProperties = cMailboxSelectedProperties.NeverBeenSelected;
                        return fMailboxProperties.exists;
                    }

                    private fMailboxProperties ZSetExists()
                    {
                        if (mExists == true) return 0;
                        mExists = true;
                        return fMailboxProperties.exists;
                    }
                }
            }
        }
    }
}