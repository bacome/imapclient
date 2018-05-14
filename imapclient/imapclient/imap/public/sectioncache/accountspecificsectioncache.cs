using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using work.bacome.mailclient.support;

namespace work.bacome.imapclient
{
    public class cAccountSpecificDirectorySectionCache : cSectionCache
    {
        private static readonly char[] kChars =
            new char[]
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
                'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
                'U', 'V', 'W', 'X', 'Y', 'Z'
            };

        private readonly Random mRandom = new Random();

        public readonly string Directory;
        public readonly long ByteCountBudget;
        public readonly int FileCountBudget;
        public readonly int Tries;

        private readonly ConcurrentDictionary<cSectionCachePersistentKey, FileInfo> mItems = new ConcurrentDictionary<cSectionCachePersistentKey, FileInfo>;

        public cAccountSpecificDirectorySectionCache(string pInstanceName, int pMaintenanceFrequency, string pDirectory, long pByteCountBudget, int pFileCountBudget, int pTries) : base(pInstanceName, pMaintenanceFrequency)
        {
            if (pByteCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pByteCountBudget));
            if (pFileCountBudget < 0) throw new ArgumentOutOfRangeException(nameof(pFileCountBudget));
            if (pTries < 1) throw new ArgumentOutOfRangeException(nameof(pTries));

            mDirectory = Path.GetFullPath(pDirectory);
            ByteCountBudget = pByteCountBudget;
            FileCountBudget = pFileCountBudget;
            Tries = pTries;
        }


    }
}