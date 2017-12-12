using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private partial class cCommandPipeline
            {
                private class cSendBuffer
                {
                    private readonly cCallbackSynchroniser mSynchroniser;
                    private readonly cConnection mConnection;
                    private readonly CancellationToken mCancellationToken;

                    private readonly List<byte> mSendBuffer = new List<byte>();

                    private bool mTracing = false;
                    private readonly List<cBytes> mTraceBuffers = new List<cBytes>();
                    private bool mContainsSecrets = false;
                    private bool mLastByteWasSecret = false;
                    private List<byte> mCurrentTraceBuffer = null;

                    private bool mSecret = false;
                    private cIncrementAccumulator mCurrentIncrementAccumulator = null;
                    private readonly List<cIncrementAccumulator> mPendingIncrements = new List<cIncrementAccumulator>();

                    public cSendBuffer(cCallbackSynchroniser pSynchroniser, cConnection pConnection, CancellationToken pCancellationToken)
                    {
                        mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                        mConnection = pConnection ?? throw new ArgumentNullException(nameof(pConnection));
                        mCancellationToken = pCancellationToken;
                    }

                    public void SetTracing(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cSendBuffer), nameof(SetTracing));
                        mTracing = (lContext.EmitsVerbose || mSynchroniser.NetworkSendSubscriptionCount > 0);
                    }

                    public void AddTag(cCommandTag pTag)
                    {
                        if (pTag == null) throw new ArgumentNullException(nameof(pTag));

                        if (mTracing)
                        {
                            if (mCurrentTraceBuffer != null) mTraceBuffers.Add(new cBytes(mCurrentTraceBuffer));
                            mCurrentTraceBuffer = new List<byte>(pTag);
                            mCurrentTraceBuffer.Add(cASCII.SPACE);

                            mLastByteWasSecret = false;
                        }

                        mSendBuffer.AddRange(pTag);
                        mSendBuffer.Add(cASCII.SPACE);
                    }

                    public void AddLiteralHeader(bool pSecret, bool pBinary, int pLength, bool pSynchronising)
                    {
                        cByteList lLengthBytes = cTools.IntToBytesReverse(pLength);
                        lLengthBytes.Reverse();

                        if (mTracing)
                        {
                            if (pBinary) mCurrentTraceBuffer.Add(cASCII.TILDA);

                            mCurrentTraceBuffer.Add(cASCII.LBRACE);

                            if (pSecret) mCurrentTraceBuffer.Add(cASCII.NUL);
                            else mCurrentTraceBuffer.AddRange(lLengthBytes);

                            if (!pSynchronising) mCurrentTraceBuffer.Add(cASCII.PLUS);

                            mCurrentTraceBuffer.Add(cASCII.RBRACE);
                            mCurrentTraceBuffer.Add(cASCII.CR);
                            mCurrentTraceBuffer.Add(cASCII.LF);

                            if (pSecret) mContainsSecrets = true;
                            mLastByteWasSecret = false;
                        }

                        if (pBinary) mSendBuffer.Add(cASCII.TILDA);
                        mSendBuffer.Add(cASCII.LBRACE);
                        mSendBuffer.AddRange(lLengthBytes);
                        if (!pSynchronising) mSendBuffer.Add(cASCII.PLUS);
                        mSendBuffer.Add(cASCII.RBRACE);
                        mSendBuffer.Add(cASCII.CR);
                        mSendBuffer.Add(cASCII.LF);
                    }

                    public void AddCRLF()
                    {
                        if (mTracing)
                        {
                            mCurrentTraceBuffer.Add(cASCII.CR);
                            mCurrentTraceBuffer.Add(cASCII.LF);

                            mLastByteWasSecret = false;
                        }

                        mSendBuffer.Add(cASCII.CR);
                        mSendBuffer.Add(cASCII.LF);
                    }

                    public void BeginAddBytes(bool pSecret, Action<int> pIncrement)
                    {
                        if (mCurrentIncrementAccumulator != null) throw new InvalidOperationException();
                        mSecret = pSecret;
                        mCurrentIncrementAccumulator = new cIncrementAccumulator(pIncrement);
                    }

                    public void AddByte(byte pByte)
                    {
                        if (mCurrentIncrementAccumulator == null) throw new InvalidOperationException();

                        mSendBuffer.Add(pByte);

                        if (mTracing)
                        {
                            if (mSecret)
                            {
                                if (mLastByteWasSecret) return;
                                mLastByteWasSecret = true;
                                mContainsSecrets = true;
                                pByte = cASCII.NUL;
                            }
                            else mLastByteWasSecret = false;

                            if (mCurrentTraceBuffer == null) mCurrentTraceBuffer = new List<byte>();
                            mCurrentTraceBuffer.Add(pByte);
                        }

                        if (!mSecret) mCurrentIncrementAccumulator.Accumulate();
                    }

                    public void EndAddBytes()
                    {
                        if (mCurrentIncrementAccumulator == null) throw new InvalidOperationException();
                        if (mCurrentIncrementAccumulator.Accumulated > 0) mPendingIncrements.Add(mCurrentIncrementAccumulator);
                        mCurrentIncrementAccumulator = null;
                    }

                    public int Count => mSendBuffer.Count;

                    public async Task WriteAsync(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cSendBuffer), nameof(WriteAsync));

                        if (mSendBuffer.Count == 0) throw new InvalidOperationException();

                        if (mTracing)
                        {
                            mTraceBuffers.Add(new cBytes(mCurrentTraceBuffer));

                            if (lContext.EmitsVerbose)
                            {
                                lContext.TraceVerbose("sending;");
                                if (!mContainsSecrets) lContext.TraceVerbose(" {0} bytes", mSendBuffer.Count);
                                foreach (var lTraceBuffer in mTraceBuffers) lContext.TraceVerbose("  {0}", lTraceBuffer.ToString(1000));
                            }

                            if (mContainsSecrets) mSynchroniser.InvokeNetworkSend(null, mTraceBuffers, lContext);
                            else mSynchroniser.InvokeNetworkSend(mSendBuffer.Count, mTraceBuffers, lContext);

                            mTraceBuffers.Clear();
                            mContainsSecrets = false;
                            mLastByteWasSecret = false;
                            mCurrentTraceBuffer = null;
                        }

                        await mConnection.WriteAsync(mSendBuffer.ToArray(), mCancellationToken, lContext).ConfigureAwait(false);
                        mSendBuffer.Clear();

                        foreach (var lAccumulator in mPendingIncrements) lAccumulator.Increment(mSynchroniser, lContext);
                        mPendingIncrements.Clear();

                        if (mCurrentIncrementAccumulator != null) mCurrentIncrementAccumulator.Increment(mSynchroniser, lContext);
                    }
                
                    private class cIncrementAccumulator
                    {
                        private readonly Action<int> mIncrement; // can be null
                        private int mAccumulated = 0;

                        public cIncrementAccumulator(Action<int> pIncrement)
                        {
                            mIncrement = pIncrement;
                        }

                        public void Accumulate()
                        {
                            mAccumulated++;
                        }

                        public int Accumulated => mAccumulated;

                        public void Increment(cCallbackSynchroniser pSynchroniser, cTrace.cContext pContext)
                        {
                            if (mAccumulated > 0) pSynchroniser.InvokeActionInt(mIncrement, mAccumulated, pContext);
                            mAccumulated = 0;
                        }
                    }
                }
            }
        }
    }
}