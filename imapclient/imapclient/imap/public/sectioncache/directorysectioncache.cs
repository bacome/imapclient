using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using work.bacome.mailclient;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public class cDirectorySectionCache : cSectionCache
    {
        private const string kData = ".scd"; // data file extension
        private const string kInfo = ".sci"; // info file extension
        private const string kList = ".scl"; // list file extension

        private static readonly char[] kChars =
            new char[]
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
                'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
                'U', 'V', 'W', 'X', 'Y', 'Z'
            };

        private readonly Random mRandom = new Random();

        public readonly DirectoryInfo DirectoryInfo;
        public readonly long ByteCountBudget;
        public readonly int FileCountBudget;
        public readonly TimeSpan RecentFileAge;

        private readonly ConcurrentDictionary<cPersistentKey, cInfo> mPersistentKeyToInfo = new ConcurrentDictionary<cPersistentKey, cInfo>();
        private readonly ConcurrentQueue<cInfo> mNewInfos = new ConcurrentQueue<cInfo>();

            
        private List<cInfoUID> mInfoUIDs = null; // sorted


        ;?; // need a list of new pk items here [for copy]
        ;?; // and a list of UIDs to delete
        ;?;

        // info retained from one iteration of maintenance to the next
        private readonly List<string> mOldListFileFullNames = new List<string>();
        private List<string> mItemKeysWithInfoButNoDataFile = new List<string>();
        private List<string> mItemKeysWithDataButNoInfoFile = new List<string>();

        public cDirectorySectionCache(string pInstanceName, int pMaintenanceFrequency, string pDirectory, long pByteCountBudget, int pFileCountBudget, TimeSpan pRecentFileAge) : base(pInstanceName, pMaintenanceFrequency)
        {
            if (pDirectory == null) throw new ArgumentNullException(nameof(pDirectory));
            DirectoryInfo = new DirectoryInfo(pDirectory);
            if (!DirectoryInfo.Exists) throw new ArgumentOutOfRangeException(nameof(pDirectory));

            if (pByteCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pByteCountBudget));
            if (pFileCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pFileCountBudget));
            if (pRecentFileAge < TimeSpan.FromSeconds(1)) throw new ArgumentOutOfRangeException(nameof(pRecentFileAge));

            ByteCountBudget = pByteCountBudget;
            FileCountBudget = pFileCountBudget;
            RecentFileAge = pRecentFileAge;

            StartMaintenance();
        }

        protected override cSectionCacheItem YGetNewItem(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cDirectorySectionCache), nameof(YGetNewItem));

            ZNewFile(kData, FileShare.Read, out var lItemKey, out var lDataFileFullName, out var lStream);

            var lInfoFileFullName = ZFullName(lItemKey, kInfo);
            try { File.Delete(lInfoFileFullName); }
            catch (Exception e) { lContext.TraceException($"failed to delete old info file: {lInfoFileFullName}", e); }

            var lFileInfo = new FileInfo(lDataFileFullName);
            return new cItem(this, lItemKey, lStream, lFileInfo.CreationTimeUtc);
        }

        protected override bool TryGetExistingItem(cSectionCachePersistentKey pKey, out cSectionCacheItem rItem, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cDirectorySectionCache), nameof(TryGetExistingItem), pKey);

            var lPersistentKey = ZPersistentKey(pKey);

            if (lPersistentKey != null && mItems.TryGetValue(lPersistentKey, out var lInfo))
            {
                rItem = new cItem(this, lInfo.ItemKey, lInfo.CreationTimeUTC, lInfo.Length);
                return true;
            }

            rItem = null;
            return false;
        }

        protected override void Copy(string pHost, string pCredentialId, cMailboxName pSourceMailboxName, cMailboxName pDestinationMailboxName, cCopyFeedback pCopyFeedback, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cDirectorySectionCache), nameof(Copy), pAccountId, pSourceMailboxName, pDestinationMailboxName, pCopyFeedback);

            if (pAccountId == null) throw new ArgumentNullException(nameof(pAccountId));
            if (pSourceMailboxName == null) throw new ArgumentNullException(nameof(pSourceMailboxName));
            if (pDestinationMailboxName == null) throw new ArgumentNullException(nameof(pDestinationMailboxName));
            if (pCopyFeedback == null) throw new ArgumentNullException(nameof(pCopyFeedback));
            if (pCopyFeedback.Count == 0) return;

            var lInfos = new List<iComparableByUID>(mItems.Values);
            lInfos.Sort();

            foreach (var lItem in pCopyFeedback)
            {
                cSearchByUID lUID = new cSearchByUID(pHost, pCredentialId, pSourceMailboxName, lItem.SourceMessageUID);

                var lIndex = lInfos.BinarySearch(lUID);

                if (lIndex < 0) continue;

                // generic routine for finding the start and end of the group (for uidval and uid)
                ;?;
            }
        }

        protected override void Maintenance(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cDirectorySectionCache), nameof(Maintenance));

            // manage new items
            var lNewInfos = mNewInfos.Count;

            // delete the index files discovered in the last maintenance run

            foreach (var lOldListFileFullName in mOldListFileFullNames)
            {
                try { File.Delete(lOldListFileFullName); }
                catch (Exception e) { lContext.TraceException($"failed to delete old list file: {lOldListFileFullName}", e); }
                if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();
            }

            mOldListFileFullNames.Clear();

            // get directory content

            var lFileInfos1 = DirectoryInfo.GetFiles("*.sc?", SearchOption.TopDirectoryOnly);
            if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();

            Dictionary<string, FileInfo> lItemKeyToDataFileFileInfo = new Dictionary<string, FileInfo>();
            Dictionary<string, FileInfo> lItemKeyToInfoFileFileInfo = new Dictionary<string, FileInfo>();
            List<string> lListFileFullNames = new List<string>();

            foreach (var lFileInfo in lFileInfos1)
            {
                var lExtension = lFileInfo.Extension;

                if (lExtension == kData)
                {
                    var lItemKey = Path.GetFileNameWithoutExtension(lFileInfo.Name);
                    lItemKeyToDataFileFileInfo.Add(lItemKey, lFileInfo);
                }
                else if (lExtension == kInfo)
                {
                    var lItemKey = Path.GetFileNameWithoutExtension(lFileInfo.Name);
                    lItemKeyToInfoFileFileInfo.Add(lItemKey, lFileInfo);
                }
                else if (lExtension == kList)
                {
                    if (lFileInfo.Length == 0) mOldListFileFullNames.Add(lFileInfo.FullName);
                    else lListFileFullNames.Add(lFileInfo.FullName);
                }
            }

            if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();

            // build a list of files with a known key

            Dictionary<string, cInfo> lItemKeyToInfo = new Dictionary<string, cInfo>();

            BinaryFormatter lFormatter = new BinaryFormatter();

            foreach (var lListFileFullName in lListFileFullNames)
            {
                List<cInfo> lListFileInfos;

                try
                {
                    using (var lListReadStream = new FileStream(lListFileFullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        try
                        {
                            lListFileInfos = lFormatter.Deserialize(lListReadStream) as List<cInfo>;

                            if (lListFileInfos == null)
                            {
                                lContext.TraceError("corrupt list file (1): {0}", lListFileFullName);

                                try { File.Delete(lListFileFullName); }
                                catch (Exception e) { lContext.TraceException($"failed to delete corrupt list file (1): {lListFileFullName}", e); }

                                lListFileInfos = null;
                            }
                        }
                        catch (Exception e)
                        {
                            lContext.TraceException($"corrupt list file (2): {lListFileFullName}", e);

                            try { File.Delete(lListFileFullName); }
                            catch (Exception e2) { lContext.TraceException($"failed to delete corrupt list file (2): {lListFileFullName}", e2); }

                            lListFileInfos = null;
                        }
                    }
                }
                catch (Exception e) // in case the file is being written/ deleted
                {
                    lContext.TraceException($"failed to open list file: {lListFileFullName}", e);
                    lListFileInfos = null;
                }

                if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();

                if (lListFileInfos != null)
                {
                    foreach (var lInfo in lListFileInfos) if (lItemKeyToDataFileFileInfo.TryGetValue(lInfo.ItemKey, out var lDataFileFileInfo) && FileTimesAreTheSame(lDataFileFileInfo.CreationTimeUtc, lInfo.CreationTimeUTC) && lDataFileFileInfo.Length == lInfo.Length) lItemKeyToInfo[lInfo.ItemKey] = lInfo;
                    mOldListFileFullNames.Add(lListFileFullName);
                }

                if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();
            }

            var lItemKeysWithInfoButNoDataFile = mItemKeysWithInfoButNoDataFile;
            mItemKeysWithInfoButNoDataFile = new List<string>();
            lItemKeysWithInfoButNoDataFile.Sort();

            foreach (var lPair in lItemKeyToInfoFileFileInfo)
            {
                var lItemKey = lPair.Key;

                if (lItemKeyToInfo.ContainsKey(lItemKey)) continue; // already have the details

                if (!lItemKeyToDataFileFileInfo.TryGetValue(lItemKey, out var lDataFileFileInfo))
                {
                    if (lItemKeysWithInfoButNoDataFile.BinarySearch(lItemKey) < 0)
                    {
                        lContext.TraceVerbose("found an info file with no data file (1): {0}", lItemKey);
                        mItemKeysWithInfoButNoDataFile.Add(lItemKey);
                    }
                    else
                    {
                        lContext.TraceVerbose("found an info file with no data file (2): {0}", lItemKey);

                        var lInfoFileFullName = ZFullName(lItemKey, kInfo);

                        try { File.Delete(lInfoFileFullName); }
                        catch (Exception e)
                        {
                            lContext.TraceException($"failed to delete orphaned info file: {lInfoFileFullName}", e);
                            mItemKeysWithInfoButNoDataFile.Add(lItemKey);
                        }

                        if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();
                    }

                    continue;
                }

                var lInfoFileFileInfo = lPair.Value;

                if (lInfoFileFileInfo.Length > 0)
                {
                    var lInfoFileFullName = ZFullName(lItemKey, kInfo);

                    cInfo lInfo;

                    try
                    {
                        using (var lInfoReadStream = new FileStream(lInfoFileFullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            try
                            {
                                lInfo = lFormatter.Deserialize(lInfoReadStream) as cInfo;

                                if (lInfo == null || lInfo.ItemKey != lItemKey)
                                {
                                    lContext.TraceError("corrupt info file (1): {0}", lInfoFileFullName);

                                    try { File.Delete(lInfoFileFullName); }
                                    catch (Exception e) { lContext.TraceException($"failed to delete corrupt info file (1): {lInfoFileFullName}", e); }

                                    lInfo = null;
                                }
                            }
                            catch (Exception e)
                            {
                                lContext.TraceException($"corrupt info file (2): {lInfoFileFullName}", e);

                                try { File.Delete(lInfoFileFullName); }
                                catch (Exception e2) { lContext.TraceException($"failed to delete corrupt info file (2): {lInfoFileFullName}", e2); }

                                lInfo = null;
                            }
                        }
                    }
                    catch (Exception e) // in case the file is being written/ deleted
                    {
                        lContext.TraceException($"failed to open info file: {lInfoFileFullName}", e);
                        lInfo = null;
                    }

                    if (lInfo != null && FileTimesAreTheSame(lDataFileFileInfo.CreationTimeUtc, lInfo.CreationTimeUTC) && lDataFileFileInfo.Length == lInfo.Length) lItemKeyToInfo.Add(lItemKey, lInfo);

                    if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();
                }
            }

            if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();

            // delete files that contain duplicate data, update the map from key to info

            List<cInfo> lItemKeyInfos = new List<cInfo>(lItemKeyToInfo.Values);
            lItemKeyInfos.Sort();

            if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();

            cPersistentKey lLastPersistentKey = null;

            foreach (var lInfo in lItemKeyInfos)
            {
                if (lInfo.PersistentKey == lLastPersistentKey)
                {
                    string lDataFileFullName = ZFullName(lInfo.ItemKey, kData);

                    try
                    {
                        lContext.TraceInformation("found a duplicate data file: {0}", lDataFileFullName);
                        File.Delete(lDataFileFullName);

                        // remove from the list of data files and the list of keyed data files
                        lItemKeyToDataFileFileInfo.Remove(lInfo.ItemKey);
                        lItemKeyToInfo.Remove(lInfo.ItemKey);

                        // remove the info file
                        string lInfoFileFullName = ZFullName(lInfo.ItemKey, kInfo);
                        try { File.Delete(lInfoFileFullName); }
                        catch (Exception e) { lContext.TraceException($"failed to delete duplicate's info file: {lInfoFileFullName}", e); }
                    }
                    catch (Exception e) { lContext.TraceException($"failed to delete duplicate data file: {lDataFileFullName}", e); }

                    if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();
                }
                else
                {
                    mPersistentKeyToInfo[lInfo.PersistentKey] = lInfo;
                    lLastPersistentKey = lInfo.PersistentKey;
                }
            }

            if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();

            // delete data files that look abandoned

            var lItemKeysWithDataButNoInfoFile = mItemKeysWithDataButNoInfoFile;
            mItemKeysWithDataButNoInfoFile = new List<string>();
            lItemKeysWithDataButNoInfoFile.Sort();

            if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();

            var lRecentFileLimit = DateTime.UtcNow - RecentFileAge;

            List<string> lItemKeysToRemove = new List<string>();

            foreach (var lPair in lItemKeyToDataFileFileInfo)
            {
                var lItemKey = lPair.Key;
                if (lItemKeyToInfoFileFileInfo.ContainsKey(lItemKey) || lItemKeyToInfo.ContainsKey(lItemKey)) continue; // there is an info file or we know what it in the file

                var lFileInfo = lPair.Value;
                if (lFileInfo.CreationTimeUtc > lRecentFileLimit) continue; // the file is recent

                if (lItemKeysWithDataButNoInfoFile.BinarySearch(lItemKey) < 0)
                {
                    try { new FileStream(lFileInfo.FullName, FileMode.Open, FileAccess.Write, FileShare.Read).Dispose(); }
                    catch (Exception e)
                    {
                        // can't open it for write => likely still being written to => the recentfileage might be too small
                        lContext.TraceException($"failed to open data file with no info file: {lFileInfo.FullName}", e);
                        continue; 
                    }

                    lContext.TraceVerbose("found a closed data file with no info file: {0}", lFileInfo.FullName);
                    mItemKeysWithDataButNoInfoFile.Add(lItemKey);
                }
                else
                {
                    lContext.TraceVerbose("found a data file with no info file: {0}", lFileInfo.FullName);

                    try { File.Delete(lFileInfo.FullName); }
                    catch (Exception e)
                    {
                        // hmmm ... maybe it is open
                        lContext.TraceException($"failed to delete data file with no info file: {lFileInfo.FullName}", e);
                        continue;
                    }

                    // mark this item as needing to be removed from the list of data files
                    lItemKeysToRemove.Add(lItemKey);
                }

                if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();
            }

            if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();

            // remove the deleted items from the list of data files
            foreach (var lItemKey in lItemKeysToRemove) lItemKeyToDataFileFileInfo.Remove(lItemKey);
            if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();

            // tidy up

            long lByteCount = 0;
            foreach (var lPair in lItemKeyToDataFileFileInfo) lByteCount += lPair.Value.Length;
            if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();

            if (lByteCount > ByteCountBudget || lItemKeyToDataFileFileInfo.Count > FileCountBudget)
            {
                int lFileCount = lItemKeyToDataFileFileInfo.Count;

                var lItemsToDelete = new List<cItemToDelete>();

                foreach (var lPair in lItemKeyToDataFileFileInfo)
                {
                    var lItemKey = lPair.Key;
                    var lFileInfo = lPair.Value;

                    DateTime lTouchTimeUTC;
                    if (lItemKeyToInfoFileFileInfo.TryGetValue(lItemKey, out var lInfoFileFileInfo) && lInfoFileFileInfo.CreationTimeUtc > lFileInfo.CreationTimeUtc) lTouchTimeUTC = lInfoFileFileInfo.CreationTimeUtc;
                    else lTouchTimeUTC = lFileInfo.CreationTimeUtc;

                    lItemsToDelete.Add(new cItemToDelete(lItemKey, lFileInfo.FullName, ZFullName(lItemKey, kInfo), lTouchTimeUTC, lFileInfo.Length));
                }

                if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();

                lItemsToDelete.Sort();

                if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();

                foreach (var lItemToDelete in lItemsToDelete)
                {
                    try
                    { 
                        File.Delete(lItemToDelete.DataFileFullName);

                        // remove it from the list of keyed data files (if it is there)
                        lItemKeyToInfo.Remove(lItemToDelete.ItemKey);

                        // decrement the counts
                        lByteCount -= lItemToDelete.Length;
                        lFileCount--;
                    }
                    catch (Exception e)
                    {
                        lContext.TraceException($"failed to delete data file: {lItemToDelete.DataFileFullName}", e);
                        continue;
                    }

                    // try to get rid of any index file that exists
                    try { File.Delete(lItemToDelete.InfoFileFullName); }
                    catch (Exception e) { lContext.TraceException($"failed to delete info file: {lItemToDelete.InfoFileFullName}", e); }

                    if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();

                    // check if we need to carry on
                    if (lByteCount <= ByteCountBudget || lFileCount <= FileCountBudget) break;
                }
            }

            // write out new index file

            var lInfos = new List<cInfo>(lItemKeyToInfo.Values);
            if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();

            ZNewFile(kList, FileShare.None, out _, out var lNewListFileFullName, out var lListWriteStream);

            try
            {
                if (pCancellationToken.IsCancellationRequested) throw new OperationCanceledException();
                lFormatter.Serialize(lListWriteStream, lInfos);
            }
            catch (Exception e)
            {
                lContext.TraceException($"failed to write out new list file: {lNewListFileFullName}", e);
                mOldListFileFullNames.Clear();
            }
            finally { lListWriteStream.Dispose(); }

            // update the list of files we know about
            var lInfoUIDs = new List<cInfoUID>(lInfos);
            lInfoUIDs.Sort();
            mInfoUIDs = lInfoUIDs;

            // remove new items we saw at the start (they should now be in the mInfoUIDs list)
            for (int i = 0; i < lNewInfos; i++) if (!mNewInfos.TryDequeue(out _)) throw new cInternalErrorException(lContext);
        }

        private void ZNewFile(string pExtension, FileShare pFileShare, out string rItemKey, out string rFullName, out Stream rStream)
        {
            while (true)
            {
                try
                { 
                    rItemKey = ZRandomItemKey();
                    rFullName = ZFullName(rItemKey, pExtension);
                    rStream = new FileStream(rFullName, FileMode.CreateNew, FileAccess.ReadWrite, pFileShare);
                    return;
                }
                catch (IOException) { }
            }
        }

        private string ZRandomItemKey()
        {
            byte[] lRandomBytes = new byte[4];
            mRandom.NextBytes(lRandomBytes);
            uint lNumber = BitConverter.ToUInt32(lRandomBytes, 0);

            var lBuilder = new StringBuilder();

            do
            {
                int lChar = (int)(lNumber % 36);
                lBuilder.Append(kChars[lChar]);
                lNumber = lNumber / 36;
            } while (lNumber > 0);

            return lBuilder.ToString();
        }

        private string ZFullName(string pItemKey, string pExtension) => Path.Combine(DirectoryInfo.FullName, pItemKey + pExtension);

        private static cPersistentKey ZPersistentKey(cSectionCachePersistentKey pKey)
        {
            if (pKey == null) return null;

            string lCredentialId;

            if (pKey.AccountId.CredentialId is string lTemp) lCredentialId = lTemp;
            else if (pKey.AccountId.CredentialId == cSASLAnonymous.AnonymousCredentialId) lCredentialId = cIMAPLogin.Anonymous;
            else return null;

            return new cPersistentKey(pKey, lCredentialId);
        }

        private class cItem : cSectionCacheItem
        {
            private readonly string mDataFileFullName;
            private readonly string mInfoFileFullName;
            private readonly DateTime mCreationTimeUTC;
            private bool mInfoFileExpectedToExist;

            public cItem(cDirectorySectionCache pCache, string pItemKey, DateTime pCreationTimeUTC, long pLength) : base(pCache, pItemKey, pLength)
            {
                mDataFileFullName = pCache.ZFullName(pItemKey, kData);
                mInfoFileFullName = pCache.ZFullName(pItemKey, kInfo);
                mCreationTimeUTC = pCreationTimeUTC;
                mInfoFileExpectedToExist = true;
            }

            public cItem(cDirectorySectionCache pCache, string pItemKey, Stream pReadWriteStream, DateTime pCreationTimeUTC) : base(pCache, pItemKey, pReadWriteStream)
            {
                mDataFileFullName = pCache.ZFullName(pItemKey, kData);
                mInfoFileFullName = pCache.ZFullName(pItemKey, kInfo);
                mCreationTimeUTC = pCreationTimeUTC;
                mInfoFileExpectedToExist = false;
            }

            protected override Stream YGetReadStream(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(YGetReadStream));
                var lStream = new FileStream(mDataFileFullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                var lFileInfo = new FileInfo(mDataFileFullName);
                if (FileTimesAreTheSame(lFileInfo.CreationTimeUtc, mCreationTimeUTC)) return lStream;
                lStream.Dispose();
                return null;
            }

            protected override void YDelete(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(YDelete));
                File.Delete(mDataFileFullName);
                File.Delete(mInfoFileFullName);
                mInfoFileExpectedToExist = false;
            }

            protected override void ItemCached(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(ItemCached));
                if (PersistentKey == null || PersistentKeyAssigned) return;
                ZCreateInfoFileIfPossible(lContext);
            }

            protected override eItemState Touch(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(Touch));

                if (!PersistentKeyAssigned)
                {
                    ZCreateInfoFileIfPossible(lContext);
                    if (PersistentKeyAssigned) return eItemState.exists;
                }

                if (mInfoFileExpectedToExist)
                {
                    var lFileInfo = new FileInfo(mInfoFileFullName);

                    if (lFileInfo.Exists)
                    {
                        lFileInfo.CreationTimeUtc = DateTime.UtcNow;
                        return eItemState.exists;
                    }
                }

                // create an empty file
                File.Create(mInfoFileFullName).Dispose();
                mInfoFileExpectedToExist = true;

                return eItemState.exists;
            }

            protected override void AssignPersistentKey(bool pItemClosed, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(Touch));
                ZCreateInfoFileIfPossible(lContext);
            }

            private void ZCreateInfoFileIfPossible(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(ZCreateInfoFileIfPossible));

                if (PersistentKey == null) return;
                var lPersistentKey = ZPersistentKey(PersistentKey);
                if (lPersistentKey == null) return;

                var lInfo = new cInfo(ItemKey, lPersistentKey, mCreationTimeUTC, Length);

                using (var lStream = new FileStream(mInfoFileFullName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    BinaryFormatter lFormatter = new BinaryFormatter();
                    lFormatter.Serialize(lStream, lInfo);
                }

                mInfoFileExpectedToExist = true;
                SetPersistentKeyAssigned(lContext);

                ((cDirectorySectionCache)Cache).mNewInfos.Enqueue(lInfo);
            }
        }

        private abstract class cInfoUID : IComparable<cInfoUID>
        {
            public abstract string Host { get; }
            public abstract string CredentialId { get; }
            public abstract cMailboxName MailboxName { get; }
            public abstract uint UIDValidity { get; }
            public abstract uint UID { get; }

            public int CompareTo(cInfoUID pOther)
            {
                if (pOther == null) return 1;

                int lCompareTo;
                if ((lCompareTo = Host.CompareTo(pOther.Host)) != 0) return lCompareTo;
                if ((lCompareTo = CredentialId.CompareTo(pOther.CredentialId)) != 0) return lCompareTo;
                if ((lCompareTo = MailboxName.CompareTo(pOther.MailboxName)) != 0) return lCompareTo;
                if ((lCompareTo = UIDValidity.CompareTo(pOther.UIDValidity)) != 0) return lCompareTo;

                return UID.CompareTo(pOther.UID);
            }
        }

        private class cMessageID : cInfoUID
        {
            private readonly string mHost;
            private readonly string mCredentialId;
            private readonly cMailboxName mMailboxName;
            private readonly uint mUIDValidity;
            private readonly uint mUID;

            public cMessageID(string pHost, string pCredentialId, cMailboxName pMailboxName, uint pUIDValidity, uint pUID = 0)
            {
                mHost = pHost ?? throw new ArgumentNullException(nameof(pHost));
                mCredentialId = pCredentialId ?? throw new ArgumentNullException(nameof(pCredentialId));
                mMailboxName = pMailboxName ?? throw new ArgumentNullException(nameof(pMailboxName));
                mUIDValidity = pUIDValidity;
                mUID = pUID;
            }

            public override string Host => mHost;
            public override string CredentialId => mCredentialId;
            public override cMailboxName MailboxName => mMailboxName;
            public override uint UIDValidity => mUIDValidity;
            public override uint UID => mUID;

            public override string ToString() => $"{nameof(cMessageID)}({mHost},{mCredentialId},{mMailboxName},{mUIDValidity},{mUID})";
        }

        [Serializable]
        [DataContract]
        private class cInfo : cInfoUID, IComparable<cInfo>
        {
            [DataMember]
            public readonly string ItemKey; 
            [DataMember]
            public readonly cPersistentKey PersistentKey;
            [DataMember]
            public readonly DateTime CreationTimeUTC;
            [DataMember]
            public readonly long Length;

            public cInfo(string pItemKey, cPersistentKey pPersistentKey, DateTime pCreationTimeUTC, long pLength)
            {
                ItemKey = pItemKey ?? throw new ArgumentNullException(nameof(pItemKey));
                PersistentKey = pPersistentKey ?? throw new ArgumentNullException(nameof(pPersistentKey));
                CreationTimeUTC = pCreationTimeUTC;
                Length = pLength;
            }

            [OnDeserialized]
            private void OnDeserialised(StreamingContext pSC)
            {
                if (ItemKey == null) new cDeserialiseException($"{nameof(cInfo)}.{nameof(ItemKey)}.null");
                if (PersistentKey == null) new cDeserialiseException($"{nameof(cInfo)}.{nameof(PersistentKey)}.null");
            }

            // sort by key then creation time: for deleting duplicates
            public int CompareTo(cInfo pOther)
            {
                if (pOther == null) return 1;

                int lCompareTo;
                if ((lCompareTo = PersistentKey.CompareTo(pOther.PersistentKey)) != 0) return lCompareTo;
                if ((lCompareTo = CreationTimeUTC.CompareTo(pOther.CreationTimeUTC)) != 0) return lCompareTo;

                return ItemKey.CompareTo(pOther.ItemKey);
            }

            public override string Host => PersistentKey.Host;
            public override string CredentialId => PersistentKey.CredentialId;
            public override cMailboxName MailboxName => PersistentKey.MailboxName;
            public override uint UIDValidity => PersistentKey.UID.UIDValidity;
            public override uint UID => PersistentKey.UID.UID;

            public override string ToString() => $"{nameof(cInfo)}({ItemKey},{PersistentKey},{CreationTimeUTC},{Length})";
        }

        [Serializable]
        [DataContract]
        private class cPersistentKey : IEquatable<cPersistentKey>, IComparable<cPersistentKey>
        {
            [DataMember]
            public readonly string Host;
            [DataMember]
            public readonly string CredentialId;
            [DataMember]
            public readonly cMailboxName MailboxName;
            [DataMember]
            public readonly cUID UID;
            [DataMember]
            public readonly cSection Section;
            [DataMember]
            public readonly eDecodingRequired Decoding;

            public cPersistentKey(cSectionCachePersistentKey pKey, string pCredentialId)
            {
                Host = pKey.AccountId.Host;
                CredentialId = pCredentialId ?? throw new ArgumentNullException(nameof(pCredentialId));
                MailboxName = pKey.MailboxName;
                UID = pKey.UID;
                Section = pKey.Section;
                Decoding = pKey.Decoding;
            }

            [OnDeserialized]
            private void OnDeserialised(StreamingContext pSC)
            {
                if (Host == null) new cDeserialiseException($"{nameof(cPersistentKey)}.{nameof(Host)}.null");
                if (CredentialId == null) new cDeserialiseException($"{nameof(cPersistentKey)}.{nameof(CredentialId)}.null");
                if (MailboxName == null) new cDeserialiseException($"{nameof(cPersistentKey)}.{nameof(MailboxName)}.null");
                if (UID == null) new cDeserialiseException($"{nameof(cPersistentKey)}.{nameof(UID)}.null");
                if (Section == null) new cDeserialiseException($"{nameof(cPersistentKey)}.{nameof(Section)}.null");
            }

            public bool Equals(cPersistentKey pObject) => this == pObject;

            public int CompareTo(cPersistentKey pOther)
            {
                if (pOther == null) return 1;

                int lCompareTo;
                if ((lCompareTo = Host.CompareTo(pOther.Host)) != 0) return lCompareTo;
                if ((lCompareTo = CredentialId.CompareTo(pOther.CredentialId)) != 0) return lCompareTo;
                if ((lCompareTo = MailboxName.CompareTo(pOther.MailboxName)) != 0) return lCompareTo;
                if ((lCompareTo = UID.CompareTo(pOther.UID)) != 0) return lCompareTo;
                if ((lCompareTo = Section.CompareTo(pOther.Section)) != 0) return lCompareTo;

                return Decoding.CompareTo(pOther.Decoding);
            }

            public override bool Equals(object pObject) => this == pObject as cPersistentKey;

            public override int GetHashCode()
            {
                unchecked
                {
                    int lHash = 17;

                    lHash = lHash * 23 + Host.GetHashCode();
                    lHash = lHash * 23 + CredentialId.GetHashCode();
                    lHash = lHash * 23 + MailboxName.GetHashCode();
                    lHash = lHash * 23 + UID.GetHashCode();
                    lHash = lHash * 23 + Section.GetHashCode();
                    lHash = lHash * 23 + Decoding.GetHashCode();

                    return lHash;
                }
            }

            public override string ToString() => $"{nameof(cPersistentKey)}({Host},{CredentialId},{MailboxName},{UID},{Section},{Decoding})";

            public static bool operator ==(cPersistentKey pA, cPersistentKey pB)
            {
                if (ReferenceEquals(pA, pB)) return true;
                if (ReferenceEquals(pA, null)) return false;
                if (ReferenceEquals(pB, null)) return false;
                return pA.Host == pB.Host && pA.CredentialId == pB.CredentialId && pA.MailboxName == pB.MailboxName && pA.UID == pB.UID && pA.Section == pB.Section && pA.Decoding == pB.Decoding;
            }

            public static bool operator !=(cPersistentKey pA, cPersistentKey pB) => !(pA == pB);
        }

        private class cItemToDelete : IComparable<cItemToDelete>
        {
            public readonly string ItemKey;
            public readonly string DataFileFullName;
            public readonly string InfoFileFullName;
            public readonly DateTime TouchTimeUTC;
            public readonly long Length;

            public cItemToDelete(string pItemKey, string pDataFileFullName, string pInfoFileFullName, DateTime pTouchTimeUTC, long pLength)
            {
                ItemKey = pItemKey;
                DataFileFullName = pDataFileFullName;
                InfoFileFullName = pInfoFileFullName;
                TouchTimeUTC = pTouchTimeUTC;
                Length = pLength;
            }

            public int CompareTo(cItemToDelete pOther)
            {
                if (pOther == null) return 1;
                return TouchTimeUTC.CompareTo(pOther.TouchTimeUTC);
            }
        }
    }
}