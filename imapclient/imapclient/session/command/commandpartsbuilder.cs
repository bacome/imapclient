using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private enum eListBracketing { none, bracketed, ifany, ifmorethanone }

            private class cCommandPartsBuilder
            {
                private readonly Stack<cList> mLists = new Stack<cList>();
                private cList mList = null; // the current list
                private readonly List<cCommandPart> mParts = new List<cCommandPart>();

                public readonly ReadOnlyCollection<cCommandPart> Parts;

                public cCommandPartsBuilder()
                {
                    Parts = mParts.AsReadOnly();
                }

                public void Add(cCommandPart pPart)
                {
                    if (mList == null)
                    {
                        mParts.Add(pPart);
                        return;
                    }

                    mList.Add(pPart);
                }

                public void Add(IEnumerable<cCommandPart> pParts)
                {
                    if (mList == null)
                    {
                        mParts.AddRange(pParts);
                        return;
                    }

                    mList.Add(pParts);
                }

                public void Add(params cCommandPart[] pParts)
                {
                    if (mList == null)
                    {
                        mParts.AddRange(pParts);
                        return;
                    }

                    mList.Add(pParts);
                }

                public void BeginList(eListBracketing pBracketing, cCommandPart pPrefix = null, cCommandPart pPostfix = null)
                {
                    if (mList != null) mLists.Push(mList);
                    mList = new cList(pBracketing, pPrefix, pPostfix);
                }

                public void EndList()
                {
                    var lList = mList;

                    if (mLists.Count == 0) mList = null;
                    else mList = mLists.Pop();

                    if (lList.Bracketing == eListBracketing.bracketed || (lList.Bracketing == eListBracketing.ifany && lList.AddCount > 0) || (lList.Bracketing == eListBracketing.ifmorethanone && lList.AddCount > 1))
                    {
                        List<cCommandPart> lParts = new List<cCommandPart>();

                        if (lList.Prefix != null) lParts.Add(lList.Prefix);
                        lParts.Add(cCommandPart.LParen);
                        lParts.AddRange(lList.Parts);
                        lParts.Add(cCommandPart.RParen);
                        if (lList.Postfix != null) lParts.Add(lList.Postfix);
                        Add(lParts);
                    }
                    else if (lList.Parts.Count > 0)
                    {
                        if (lList.Prefix != null || lList.Postfix != null)
                        {
                            List<cCommandPart> lParts = new List<cCommandPart>();
                            if (lList.Prefix != null) lParts.Add(lList.Prefix);
                            lParts.AddRange(lList.Parts);
                            if (lList.Postfix != null) lParts.Add(lList.Postfix);
                            Add(lParts);
                        }
                        else Add(lList.Parts);
                    }
                }

                public override string ToString()
                {
                    var lBuilder = new cListBuilder(nameof(cCommandPartsBuilder));
                    foreach (var lPart in Parts) lBuilder.Append(lPart);
                    return lBuilder.ToString();
                }

                private class cList
                {
                    public readonly eListBracketing Bracketing;
                    public readonly cCommandPart Prefix;
                    public readonly cCommandPart Postfix;
                    private readonly List<cCommandPart> mParts;
                    private int mAddCount;
                    public readonly ReadOnlyCollection<cCommandPart> Parts;

                    public cList(eListBracketing pBracketing, cCommandPart pPrefix, cCommandPart pPostix)
                    {
                        Bracketing = pBracketing;
                        Prefix = pPrefix;
                        Postfix = pPostix;
                        mParts = new List<cCommandPart>();
                        mAddCount = 0;
                        Parts = new ReadOnlyCollection<cCommandPart>(mParts);
                    }

                    public void Add(cCommandPart pPart)
                    {
                        if (mAddCount != 0) mParts.Add(cCommandPart.Space);
                        mParts.Add(pPart);
                        mAddCount++;
                    }

                    public void Add(IEnumerable<cCommandPart> pParts)
                    {
                        if (mAddCount != 0) mParts.Add(cCommandPart.Space);
                        mParts.AddRange(pParts);
                        mAddCount++;
                    }

                    public int AddCount => mAddCount;
                }
            }
        }
    }
}