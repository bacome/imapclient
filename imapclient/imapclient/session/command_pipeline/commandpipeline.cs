using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public partial class cIMAPClient
    {
        private partial class cSession
        {
            private sealed partial class cCommandPipeline : IDisposable
            {
                private static readonly cBytes kPlusSpace = new cBytes("+ ");

                private bool mDisposed = false;

                // state
                private enum eState { notconnected, connecting, connected, enabled, stopped }
                private eState mState = eState.notconnected;

                // stuff
                private readonly cCallbackSynchroniser mSynchroniser;
                private readonly Action<cTrace.cContext> mDisconnected;
                private readonly cConnection mConnection;
                private cIdleConfiguration mIdleConfiguration;

                // response text processing
                private readonly cResponseTextProcessor mResponseTextProcessor;

                // used to control idling
                private readonly cExclusiveAccess mIdleBlock = new cExclusiveAccess("idleblock", 100); 

                // installable components
                private readonly List<iResponseDataParser> mResponseDataParsers = new List<iResponseDataParser>();
                private readonly List<cUnsolicitedDataProcessor> mUnsolicitedDataProcessors = new List<cUnsolicitedDataProcessor>();

                // required background task objects
                private readonly CancellationTokenSource mBackgroundCancellationTokenSource = new CancellationTokenSource(); 
                private readonly cReleaser mBackgroundReleaser;
                private readonly cAwaiter mBackgroundAwaiter;

                // background send buffer
                private readonly cSendBuffer mBackgroundSendBuffer;

                // background task
                private Task mBackgroundTask = null; // background task
                private Exception mBackgroundTaskException = null;

                // capability data
                private cStrings mCapabilities = null;
                private cStrings mAuthenticationMechanisms = null;

                // can be set only before and on enable
                private bool mLiteralPlus = false;
                private bool mLiteralMinus = false;

                // set on enable
                private cMailboxCache mMailboxCache = null;
                private bool mIdleCommandSupported = false;

                // commands
                private readonly object mPipelineLock = new object(); // access to commands is protected by locking this
                private readonly Queue<cCommand> mQueuedCommands = new Queue<cCommand>();
                private cCommand mCurrentCommand = null;
                private readonly cActiveCommands mActiveCommands = new cActiveCommands();

                public cCommandPipeline(cCallbackSynchroniser pSynchroniser, Action<cTrace.cContext> pDisconnected, cBatchSizerConfiguration pNetworkWriteConfiguration, cIdleConfiguration pIdleConfiguration, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewObject(nameof(cCommandPipeline), pIdleConfiguration);

                    mSynchroniser = pSynchroniser ?? throw new ArgumentNullException(nameof(pSynchroniser));
                    mDisconnected = pDisconnected ?? throw new ArgumentNullException(nameof(pDisconnected));
                    if (pNetworkWriteConfiguration == null) throw new ArgumentNullException(nameof(pNetworkWriteConfiguration));
                    mConnection = new cConnection(pNetworkWriteConfiguration);
                    mIdleConfiguration = pIdleConfiguration;

                    mResponseTextProcessor = new cResponseTextProcessor(pSynchroniser);

                    // these depend on the cancellationtokensource being constructed
                    mBackgroundReleaser = new cReleaser("commandpipeline_background", mBackgroundCancellationTokenSource.Token);
                    mBackgroundAwaiter = new cAwaiter(mBackgroundCancellationTokenSource.Token);

                    mBackgroundSendBuffer = new cSendBuffer(pSynchroniser, mConnection, mBackgroundCancellationTokenSource.Token);

                    // plumbing
                    mIdleBlock.Released += mBackgroundReleaser.Release; // when the idle block is removed, kick the background process
                }

                public cStrings Capabilities => mCapabilities;
                public cStrings AuthenticationMechanisms => mAuthenticationMechanisms;

                public void SetIdleConfiguration(cIdleConfiguration pConfiguration, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(SetIdleConfiguration), pConfiguration);
                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));
                    mIdleConfiguration = pConfiguration;
                    mBackgroundReleaser.Release(lContext);
                }

                public async Task<cExclusiveAccess.cToken> GetIdleBlockTokenAsync(cMethodControl pMC, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(GetIdleBlockTokenAsync));
                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));
                    var lTask = mIdleBlock.GetTokenAsync(pMC, lContext);
                    mBackgroundReleaser.Release(lContext);
                    return await lTask.ConfigureAwait(false);
                }

                private void ZIdleBlockReleased(cTrace.cContext pParentContext) => mBackgroundReleaser.Release(pParentContext);

                public async Task<cCommandResult> ExecuteAsync(cMethodControl pMC, sCommandDetails pCommandDetails, cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(ExecuteAsync), pMC, pCommandDetails);

                    if (mDisposed)
                    {
                        pCommandDetails.Disposables?.Dispose();
                        throw new ObjectDisposedException(nameof(cCommandPipeline));
                    }

                    if (mState < eState.connected)
                    {
                        pCommandDetails.Disposables?.Dispose();
                        throw new InvalidOperationException(kInvalidOperationExceptionMessage.NotConnected);
                    }

                    cCommand lCommand;

                    lock (mPipelineLock)
                    {
                        if (mState == eState.stopped)
                        {
                            pCommandDetails.Disposables.Dispose();
                            throw mBackgroundTaskException;
                        }

                        lCommand = new cCommand(pCommandDetails);
                        mQueuedCommands.Enqueue(lCommand);

                        mBackgroundReleaser.Release(lContext);
                    }

                    try { return await lCommand.WaitAsync(pMC, lContext).ConfigureAwait(false); }
                    finally
                    {
                        lock (mPipelineLock)
                        {
                            if (lCommand.State == eCommandState.queued) lCommand.SetCancelled(lContext);
                        }
                    }
                }

                public void RequestStop(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cCommandPipeline), nameof(RequestStop));
                    if (mDisposed) throw new ObjectDisposedException(nameof(cCommandPipeline));
                    mBackgroundCancellationTokenSource.Cancel();
                }

                public void Dispose()
                {
                    if (mDisposed) return;

                    if (mBackgroundCancellationTokenSource != null && !mBackgroundCancellationTokenSource.IsCancellationRequested)
                    {
                        try { mBackgroundCancellationTokenSource.Cancel(); }
                        catch { }
                    }

                    // must dispose first as the background task uses the other objects to be disposed
                    if (mBackgroundTask != null)
                    {
                        // wait for the task to exit before disposing it
                        try { mBackgroundTask.Wait(); }
                        catch { }

                        try { mBackgroundTask.Dispose(); }
                        catch { }
                    }

                    if (mIdleBlock != null)
                    {
                        try { mIdleBlock.Dispose(); }
                        catch { }
                    }

                    if (mBackgroundReleaser != null)
                    {
                        try { mBackgroundReleaser.Dispose(); }
                        catch { }
                    }

                    if (mBackgroundAwaiter != null)
                    {
                        try { mBackgroundAwaiter.Dispose(); }
                        catch { }
                    }

                    if (mBackgroundCancellationTokenSource != null)
                    {
                        try { mBackgroundCancellationTokenSource.Dispose(); }
                        catch { }
                    }

                    if (mConnection != null)
                    {
                        try { mConnection.Dispose(); }
                        catch { }
                    }

                    mDisposed = true;
                }

                [Conditional("DEBUG")]
                public static void _Tests(cTrace.cContext pParentContext)
                {
                    var lContext = pParentContext.NewMethod(nameof(cConnection), nameof(_Tests));
                    cConnection._Tests(lContext);
                }
            }
        }
    }
}
