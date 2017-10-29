using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;
using work.bacome.trace;

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
                    private const int kTraceMaxLength = 1000;

                    private readonly cCallbackSynchroniser mSynchroniser;
                    private readonly cConnection mConnection;
                    private readonly CancellationToken mCancellationToken;

                    private readonly List<byte> mSendBuffer = new List<byte>();
                    private readonly List<cByteList> mTraceBuffers = new List<cByteList>();

                    private cByteList mCurrentTraceBuffer = null;
                    private bool mContainsSecrets = false;
                    private bool mLastByteWasSecret = false;

                    public cSendBuffer(cCallbackSynchroniser pSynchroniser, cConnection pConnection, CancellationToken pCancellationToken)
                    {
                        mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                        mConnection = pConnection ?? throw new ArgumentNullException(nameof(pConnection));
                        mCancellationToken = pCancellationToken;
                    }

                    public void AddTag(cCommandTag pTag)
                    {
                        if (pTag == null) throw new ArgumentNullException(nameof(pTag));

                        mSendBuffer.AddRange(pTag);
                        mSendBuffer.Add(cASCII.SPACE);

                        if (mCurrentTraceBuffer != null) mTraceBuffers.Add(mCurrentTraceBuffer);
                        mCurrentTraceBuffer = new List<byte>(pTag);
                        mCurrentTraceBuffer.Add(cASCII.SPACE);

                        mLastByteWasSecret = false;
                    }

                    public void AddLiteralHeader(bool pSecret, bool pBinary, int pLength, bool pSynchronising)
                    {
                        if (pBinary)
                        {
                            mSendBuffer.Add(cASCII.TILDA);
                            mCurrentTraceBuffer.Add(cASCII.TILDA);
                        }

                        cByteList lLengthBytes = cTools.IntToBytesReverse(pLength);
                        lLengthBytes.Reverse();

                        mSendBuffer.Add(cASCII.LBRACE);
                        mSendBuffer.AddRange(lLengthBytes);

                        mCurrentTraceBuffer.Add(cASCII.LBRACE);
                        if (pSecret) mCurrentTraceBuffer.Add(cASCII.NUL);
                        else mCurrentTraceBuffer.AddRange(lLengthBytes);

                        if (!pSynchronising)
                        {
                            mSendBuffer.Add(cASCII.PLUS);
                            mCurrentTraceBuffer.Add(cASCII.PLUS);
                        }

                        mSendBuffer.Add(cASCII.RBRACE);
                        mSendBuffer.Add(cASCII.CR);
                        mSendBuffer.Add(cASCII.LF);

                        mCurrentTraceBuffer.Add(cASCII.RBRACE);
                        mCurrentTraceBuffer.Add(cASCII.CR);
                        mCurrentTraceBuffer.Add(cASCII.LF);

                        if (pSecret) mContainsSecrets = true;
                        mLastByteWasSecret = false;
                    }

                    public void AddCRLF()
                    {
                        mSendBuffer.Add(cASCII.CR);
                        mSendBuffer.Add(cASCII.LF);

                        mCurrentTraceBuffer.Add(cASCII.CR);
                        mCurrentTraceBuffer.Add(cASCII.LF);

                        mLastByteWasSecret = false;
                    }

                    public void AddByte(bool pSecret, byte pByte)
                    {
                        mSendBuffer.Add(pByte);

                        if (pSecret)
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

                    public int Count => mSendBuffer.Count;

                    public async Task WriteAsync(cTrace.cContext pParentContext)
                    {
                        var lContext = pParentContext.NewMethod(nameof(cSendBuffer), nameof(WriteAsync));

                        if (mSendBuffer.Count == 0) throw new InvalidOperationException();

                        if (lContext.EmitsVerbose)
                        {
                            if (!mContainsSecrets) lContext.TraceVerbose("sending {0} bytes", mSendBuffer.Count);

                            foreach (var lTraceBuffer in mTraceBuffers)
                            {
                                if (lTraceBuffer.Count < kTraceMaxLength) lContext.TraceVerbose()
                            }




                            for (int i = 0; i < mStartPoints.Count; i++)
                            {
                                int lStart = mStartPoints[i];

                                int lSegmentEnd;
                                if (i == mStartPoints.Count - 1) lSegmentEnd = mTraceBuffer.Count;
                                else lSegmentEnd = mStartPoints[i + 1];

                                int lMaxLengthEnd = lStart + kTraceMaxLength;

                                int lEnd = Math.Min(lSegmentEnd, lMaxLengthEnd);

                                int lPos = lStart;

                                while (lPos < lEnd) lBytes.Add(mTraceBuffer[lPos++]);
                                if (lPos < lSegmentEnd) for (int j = 0; j < 3; j++) lBytes.Add(cASCII.DOT);

                                lContext.TraceVerbose(
                            }





                            // note that the buffer may be huge ...
                            ;?;


                            lContext.TraceVerbose("sending {0} bytes: {1}", mSendBuffer.Count, mTraceBuffer);
                        }

                        if (mContainsSecrets) mSynchroniser.InvokeNetworkActivity(-1, mStartPoints, mTraceBuffer, lContext);
                        else mSynchroniser.InvokeNetworkActivity(mSendBuffer.Count, mStartPoints, mTraceBuffer, lContext);

                        await mConnection.WriteAsync(mSendBuffer.ToArray(), mCancellationToken, lContext).ConfigureAwait(false);

                        mSendBuffer.Clear();
                        mTraceBuffers.Clear();
                        mCurrentTraceBuffer = null;
                        mContainsSecrets = false;
                        mLastByteWasSecret = false;
                    }
                }
            }
        }
    }
}