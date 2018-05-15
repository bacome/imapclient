using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using work.bacome.imapclient.support;
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
        public readonly int Tries;

        private Dictionary<cPersistentKey, cInfo> mItems = new Dictionary<cPersistentKey, cInfo>();

        private readonly List<string> mFullNamesToDelete = new List<string>();

        public cDirectorySectionCache(string pInstanceName, int pMaintenanceFrequency, string pDirectory, long pByteCountBudget, int pFileCountBudget, int pTries) : base(pInstanceName, pMaintenanceFrequency)
        {
            if (pByteCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pByteCountBudget));
            if (pFileCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pFileCountBudget));
            if (pTries < 1) throw new ArgumentOutOfRangeException(nameof(pTries));

            DirectoryInfo = new DirectoryInfo(pDirectory);
            ByteCountBudget = pByteCountBudget;
            FileCountBudget = pFileCountBudget;
            Tries = pTries;

            StartMaintenance();
        }

        protected override cSectionCacheItem YGetNewItem(cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cDirectorySectionCache), nameof(YGetNewItem));
            ZNewFile(kData, out var lItemKey, out var lFullName, out var lStream);
            var lFileInfo = new FileInfo(lFullName);
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

        protected override void Maintenance(CancellationToken pCancellationToken, cTrace.cContext pParentContext)
        {
            var lContext = pParentContext.NewMethod(nameof(cDirectorySectionCache), nameof(Maintenance));

            foreach (var lFullName in mFullNamesToDelete) File.Delete(lFullName);
            mFullNamesToDelete.Clear();

            var lFileInfos1 = DirectoryInfo.GetFiles("*.sc?", SearchOption.TopDirectoryOnly);

            Dictionary<string, FileInfo> lItemKeyToDataFileInfo = new Dictionary<string, FileInfo>();
            List<string> lListFileFullNames = new List<string>();
            Dictionary<string, cInfo> lNewList = new Dictionary<string, cInfo>();

            foreach (var lFileInfo in lFileInfos1)
            {
                var lExtension = lFileInfo.Extension;

                if (lExtension == kData)
                {
                    var lItemKey = Path.GetFileNameWithoutExtension(lFileInfo.Name);
                    lItemKeyToDataFileInfo.Add(lItemKey, lFileInfo);
                }
                else if (lExtension == kInfo)
                {
                    ;?;
                }
                else if (lExtension == kList) lListFileFullNames.Add(lFileInfo.FullName);
            }

            BinaryFormatter lFormatter = new BinaryFormatter();

            foreach (var lListFileFullName in lListFileFullNames)
            {
                try
                {
                    using (var lStream = new FileStream(lListFileFullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var lListing = lFormatter.Deserialize(lStream) as List<cInfo>;

                        foreach (var lInfo in lListing)
                        {
                            if (lItemKeyToDataFileInfo.TryGetValue(lInfo.ItemKey, out var lDataFileInfo))
                            {
                                if (lDataFileInfo.CreationTimeUtc == lInfo.CreationTimeUTC && lDataFileInfo.Length == lInfo.Length)
                                {
                                    lNewList[lInfo.ItemKey] = lInfo;
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            // second dir here

            foreach (var linfo in infos)
            {
                // check if there is an entry in the newlist already (indexkey), if so skip it
                // check that there is a datafile in either 1 or 2 (indexkey)
                //  if not, delete it (fullname)
                // read the content
                //  check that the data file matches the content, if so 
            }




            var lFileInfos2 = DirectoryInfo.GetFiles("*.sc?", SearchOption.TopDirectoryOnly);
            var lFileInfos2 = 









































        }

        private void ZNewFile(string pExtension, out string rItemKey, out string rFullName, out Stream rStream)
        {
            int lTries = 0;

            while (true)
            {
                try
                {
                    rItemKey = ZRandomItemKey();
                    rFullName = Path.Combine(DirectoryInfo.FullName, rItemKey + pExtension);
                    rStream = new FileStream(rFullName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
                    return;
                }
                catch (IOException) { }

                if (++lTries == Tries) throw new IOException();
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
            private long mLength;

            public cItem(cDirectorySectionCache pCache, string pItemKey, DateTime pCreationTimeUTC, long pLength) : base(pCache, pItemKey)
            {
                mDataFileFullName = Path.Combine(pCache.DirectoryInfo.FullName, pItemKey + kData);
                mInfoFileFullName = Path.Combine(pCache.DirectoryInfo.FullName, pItemKey + kInfo);
                mCreationTimeUTC = pCreationTimeUTC;
                mLength = pLength;
            }

            public cItem(cDirectorySectionCache pCache, string pItemKey, Stream pReadWriteStream, DateTime pCreationTimeUTC) : base(pCache, pItemKey, pReadWriteStream)
            {
                mDataFileFullName = Path.Combine(pCache.DirectoryInfo.FullName, pItemKey + kData);
                mInfoFileFullName = Path.Combine(pCache.DirectoryInfo.FullName, pItemKey + kInfo);
                mCreationTimeUTC = pCreationTimeUTC;
                mLength = 0;
            }

            protected override Stream YGetReadStream(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(YGetReadStream));
                var lStream = new FileStream(mDataFileFullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                var lFileInfo = new FileInfo(mDataFileFullName);
                if (lFileInfo.CreationTimeUtc == mCreationTimeUTC && lFileInfo.Length == mLength && lStream.Length == mLength) return lStream;
                lStream.Dispose();
                return null;
            }

            protected override void YDelete(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(YDelete));
                File.Delete(mDataFileFullName);
                File.Delete(mInfoFileFullName);
            }

            protected override void ItemCached(long pLength, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(ItemCached), pLength);
                mLength = pLength;
                if (PersistentKey == null || PersistentKeyAssigned) return;
                ZRecordInfo(false, lContext);
            }

            protected override void Touch(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(Touch));

                if (!PersistentKeyAssigned)
                {
                    ZRecordInfo(false, lContext);
                    if (PersistentKeyAssigned) return;
                }

                if (PersistentKeyAssigned)
                {
                    var lFileInfo = new FileInfo(mInfoFileFullName);

                    if (lFileInfo.Exists)
                    {
                        lFileInfo.CreationTimeUtc = DateTime.UtcNow;
                        return;
                    }
                }

                ZRecordInfo(true, lContext);
            }

            protected override void AssignPersistentKey(bool pItemClosed, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(Touch));
                ZRecordInfo(false, lContext);
            }

            private void ZRecordInfo(bool pTouch, cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(ZRecordInfo), pTouch);

                if (PersistentKey == null)
                {
                    if (pTouch) ZTouchDataFile(lContext);
                    return;
                }

                var lPersistentKey = ZPersistentKey(PersistentKey);

                if (lPersistentKey == null)
                {
                    if (pTouch) ZTouchDataFile(lContext);
                    return;
                }

                using (var lStream = new FileStream(mInfoFileFullName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    BinaryFormatter lFormatter = new BinaryFormatter();
                    lFormatter.Serialize(lStream, new cInfo(ItemKey, lPersistentKey, mCreationTimeUTC, mLength));
                }

                SetPersistentKeyAssigned(lContext);
            }

            private void ZTouchDataFile(cTrace.cContext pParentContext)
            {
                var lContext = pParentContext.NewMethod(nameof(cItem), nameof(ZTouchDataFile));

                var lFileInfo = new FileInfo(mDataFileFullName);

                if (lFileInfo.Exists)
                {
                    lFileInfo.CreationTimeUtc = DateTime.UtcNow;
                    return;
                }
            }
        }

        [Serializable]
        [DataContract]
        private class cInfo
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
                ItemKey = pItemKey;
                PersistentKey = pPersistentKey;
                CreationTimeUTC = pCreationTimeUTC;
                Length = pLength;
            }

            [OnDeserialized]
            private void OnDeserialised(StreamingContext pSC)
            {
                if (ItemKey == null) new cDeserialiseException($"{nameof(cInfo)}.{nameof(ItemKey)}.null");
                if (PersistentKey == null) new cDeserialiseException($"{nameof(cInfo)}.{nameof(PersistentKey)}.null");
            }
        }

        [Serializable]
        [DataContract]
        private class cPersistentKey : IEquatable<cPersistentKey>
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
                CredentialId = pCredentialId;
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
    }
}