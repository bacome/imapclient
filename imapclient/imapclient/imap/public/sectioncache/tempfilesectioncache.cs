using System;
using System.Collections.Generic;
using System.IO;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    public abstract partial class cSectionCachexx
    {

        // for use in cache trim

        // for internal use of the cache NO!: trygetreadstream, getreadwritestream
        protected internal bool TryGetItem(cSectionCachePersistentKey pKey, out cSectionCacheItem rItem)
        {
            lock (mItemsLock)
            {
                if (mDisposing) { rItem = null; return false; } // <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< check this

                if (mPersistentKeyItems.TryGetValue(pKey, out rItem)) return true;

                foreach (var lPair in mNonPersistentKeyItems)
                {

                }



                foreach ()


                // find in pk
                // find in npk, moving to pk if it has one [note that the item isn't marked as having the pk set yet]
                // call trygetexistingitem (and add to pk items, marking the item as having the pk set and marking as in the cache)
                // if (not disposing)
                //  call gettempitem (and add to mTempItems)
                //   
            }
        }

        // for internal use of the cache
        internal bool TryGetItem(cSectionCacheNonPersistentKey pKey, out cSectionCacheItem rItem)
        {
            lock (mItemsLock)
            {
                if (mDisposing) { rItem = null; return false; } // <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< check this

                // find in pk if it has one
                // find in npk, moving to pk if it has one [note that the item isn't marked as having the pk set yet]
                // if the item has a pk, call trygetexisting ...
                // if (not disposing)
                //  call gettempitem (and add to mTempItems)
            }
        }

        internal void AddItem(cSectionCacheItem pItem)
        {
            // this marks it as in the cache and adds it to the cache: so the key has to come along with it

        }

        internal void ItemClosed(cSectionCacheItem pItem)
        {
            lock (mItemsLock)
            {
                // if it isn't marked as in the cache, delete it, remove it from the tempitems, 
                //  sad eh? if the item isn't fully written delete it?
                // 


                if (mDisposing)
                {

                }
                else
                {
                    // 
                }
            }
        }


















        internal bool TryGetReadStream(cSectionCachePersistentKey pKey, out Stream rStream)
        {
            // look in the pkey first before the npkey
            //  => if found in the npkey it should be added to the pkey for quicker location next time
            //  if find an item but it is deleted can't use it
            //  if can't find in my arrays, call getexistingitem to see if it is cached but I don't know
            //   (the cache may choose to convert (decode) an existing item if it wishes, or extract the info from a larger aggregate, whatever)

            lock (mItemsLock)
            {
                if (mPersistentKeyItems.TryGetValue(pKey, out var lPKItem) && lPKItem.TryGetReadStreamWrapper(out var )
            }


        }

        internal bool TryGetReadStream(cSectionCacheNonPersistentKey pKey, out Stream rStream)
        {
            // look in the pkey first before the npkey
        }

        internal cSectionCacheItemWrapper GetNewCacheItemWrapper(cSectionCachePersistentKey pKey)
        {
            var lItem = GetNewItem();

        }

        internal cSectionCacheItemWrapper GetNewCacheItemWrapper(cSectionCacheNonPersistentKey pKey)
        {

        }

        internal void TryAdd(cSectionCachePersistentKey pKey, cSectionCacheItemWrapper pItem)
        {
            // mark the item as in the cache if it is successfully added
            //  note that adding items can be done from the getreadstream accessors above 
            //  if an item is found but it is deleted, replace it
            //  do not mark the item as in cache unless it is added
            //  remove the item from the list of new items

            lock (mItemsLock)
            {
                if (mPersistentKeyItems)
            }

            ;?;

        }

        internal void TryAdd(cSectionCacheNonPersistentKey pKey, cSectionCacheItemWrapper pItem)
        {
            ;?;
        }
    }

    public abstract partial class cSectionCacheItemxx
    {
        private readonly cSectionCacheItem mItem;
        private cSectionCachePersistentKey mPersistentKey; // may be set initally or by tryassignkey
        private readonly cSectionCacheNonPersistentKey mNonPersistentKey; // may be null (if null, mPersistentKey can't be)

        private bool mInCache; // may be set on construction

        ;?; // all private/internal


        //private 


        ;?;

        public cSectionCacheItemWrapper(cSectionCache pCache, cSectionCacheItem pItem, cSectionCachePersistentKey pKey, bool p)
        {
            mCache = pCache ?? throw new ArgumentNullException(nameof(pCache));
            mItem = pItem ?? throw new ArgumentNullException(nameof(pItem));
            mPersistentKey = pKey ?? throw new ArgumentNullException(nameof(pKey));
            mNonPersistentKey = null;
            ;?;
        }

        public bool TryGetReadStream(out Stream rStream)
        {
            ;?;
            // inside the lock
        }

        public bool TryGetWriteStream(out Stream rStream)
        {
            ;?;
            // inside the lock

        }

        public bool TryAssignKey(cSectionCachePersistentKey pKey)
        {
            // inside the lock ...
            // must check if the key is already assigned, if so return false;
            // must check is deleted, if so return false
            // must check if not open, and must only do the assign in a lock ensuring that it is not open

            ;?;

            ;?;
        }

        public bool TryDelete()
        {
            ;?;

            // inside the lock
            //  must check if it is deleted -> return false
            //  ust check if not open, ...
        }

        public bool IsDeleted => mDeleted;

        // when count goes to zero
        //  if the item is not in the cache, delete it and remove it from the list of new items
        //  otherwise
        //   if the item has a persistentkey and we haven't set it yet call the items setpersistentkey
        //   (this includes checking that the non-persistent key may now have a UID)
    }

    internal class cSectionCacheStream : Stream
    {
        private bool mDisposed = false;
        private readonly cSectionCacheItemWrapper mItem;
        private readonly Stream mStream; // underlying stream
        private readonly 


    }
}