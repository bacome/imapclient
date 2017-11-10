using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace work.bacome.trace
{
    /// <summary>
    /// Provides services for tracing to a <see cref="TraceSource"/> with trace message indenting and context information.
    /// </summary>
    /// <remarks>
    /// <para>The concept is that trace messages are written in a context. Root-contexts can be established independently and sub-contexts can be created from root-contexts and sub-contexts.</para>
    /// <para>If a new sub-context is created for each call then call stack information can be built and included in the trace.</para>
    /// <para>
    /// Writing of context trace messages can be delayed until a non-context trace message is written, or context trace messages can be written as contexts are created.
    /// Note that if the writing is delayed then the generation of the context trace message is also delayed.
    /// If there are mutable objects to be included in the context trace message then this may lead to a misleading context trace message.
    /// (It is done like this for efficiency reasons.)
    /// </para>
    /// <para>
    /// Tracing can be disabled.
    /// When tracing is disabled contexts are not created and trace messages are not emitted, so most of the tracing overhead is eliminated.
    /// Tracing is disabled under the following circumstances;
    /// <list type="bullet">
    /// <item><description>The assembly is compiled without the "TRACE" conditional attribute.</description></item>
    /// <item><description>If there aren't any listeners attached to the <see cref="TraceSource"/> when the instance is created.</description></item>
    /// <item><description>The <see cref="TraceSource"/> isn't configured to emit critical messages when the instance is created.</description></item>
    /// </list>
    /// </para>
    /// <para>Root-contexts have a name and a number. The name is programmer assigned, the number is internally assigned and is unique for an exe.</para>
    /// <para>Trace messages are indented by a number of spaces that equals the context stack depth.</para>
    /// <para>Trace messages are written in a tab delimited form, the tab delimited 'columns' contain;
    /// <list type="bullet">
    /// <item><description>The <see cref="System.Diagnostics.TraceSource"/> defined data.</description></item>
    /// <item><description>The date and time that the message was written.</description></item>
    /// <item><description>The name and number of the root-context of this trace message.</description></item>
    /// <item><description>The thread number on which the trace message was written.</description></item>
    /// <item><description>The space indented trace message.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class cTrace
    {
        private TraceSource mTraceSource = null;
        private TraceEventType mLevel = 0;
        private bool mContextTraceMustBeDelayed = true;
        private TraceEventType mContextTraceEventType;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pTraceSourceName">The trace source name to use.</param>
        public cTrace(string pTraceSourceName)
        {
            ZCtor(pTraceSourceName);
        }

        [Conditional("TRACE")]
        private void ZCtor(string pTraceSourceName)
        {
            TraceSource lTraceSource = new TraceSource(pTraceSourceName);
            mLevel = (TraceEventType)lTraceSource.Switch.Level;
            if (!Emits(TraceEventType.Critical) || lTraceSource.Listeners.Count == 0) return;
            mTraceSource = lTraceSource;

            if (Emits(TraceEventType.Verbose))
            {
                mContextTraceMustBeDelayed = false;
                mContextTraceEventType = TraceEventType.Verbose;
            }
            else
            {
                if (Emits(TraceEventType.Information)) mContextTraceEventType = TraceEventType.Information;
                else if (Emits(TraceEventType.Warning)) mContextTraceEventType = TraceEventType.Warning;
                else if (Emits(TraceEventType.Error)) mContextTraceEventType = TraceEventType.Error;
                else mContextTraceEventType = TraceEventType.Critical;
            }
        }

        private bool Emits(TraceEventType pEventType) => (mLevel & pEventType) != 0;
        private bool ContextTraceMustBeDelayed => mContextTraceMustBeDelayed;

        private void TraceContext(string pInstanceName, int pInstanceNumber, int pLevel, string pMessage)
        {
            mTraceSource.TraceEvent(mContextTraceEventType, 1, "\t{0:yyyy-MM-dd HH:mm:ss}\t{1}({2})\t{3}\t{4}{5}", DateTime.Now, pInstanceName, pInstanceNumber, Thread.CurrentThread.ManagedThreadId, new string(' ', pLevel - 1), pMessage);
        }

        private void TraceEvent(TraceEventType pEventType, string pInstanceName, int pInstanceNumber, int pLevel, string pMessage)
        {
            mTraceSource.TraceEvent(pEventType, 1, "\t{0:yyyy-MM-dd HH:mm:ss}\t{1}({2})\t{3}\t{4}{5}", DateTime.Now, pInstanceName, pInstanceNumber, Thread.CurrentThread.ManagedThreadId, new string(' ', pLevel), pMessage);
        }

        /// <summary>
        /// Create a new independent root-context.
        /// </summary>
        /// <param name="pInstanceName">The name to give the context.</param>
        /// <param name="pContextTraceDelay">Whether writing of context trace messages should be delayed.</param>
        /// <returns>The new root-context.</returns>
        public cContext NewRoot(string pInstanceName, bool pContextTraceDelay = false)
        {
            if (mTraceSource == null) return cContext.Null;
            return new cContext.cRoot(this, pInstanceName, pContextTraceDelay);
        }

        /// <summary>
        /// A <see cref="cTrace"/> tracing context.
        /// </summary>
        /// <remarks>
        /// Will be either a root-context or a sub-context. See <see cref="cTrace"/> for more information.
        /// </remarks>
        public abstract class cContext
        {
            /**<summary>A tracing context that does not create contexts or emit messages. Used to suppress tracing.</summary>*/
            public readonly static cContext Null = new cNull();

            /// <summary>
            /// Creates a new root-context tied (in name only) to the root-context of this instance.
            /// </summary>
            /// <param name="pInstanceName">The name to use when creating the new the context.</param>
            /// <param name="pContextTraceDelay">Whether writing of context trace messages should be delayed. See <see cref="cTrace"/> for more information.</param>
            /// <returns>The new root-context with a name reflecting the root-context name of this instance and the specified name.</returns>
            public abstract cContext NewRoot(string pInstanceName, bool pContextTraceDelay = false);

            /// <summary>
            /// Creates a new sub-context with a free form trace message.
            /// </summary>
            /// <param name="pContextTraceDelay">Whether writing of context trace messages should be delayed. See <see cref="cTrace"/> for more information.</param>
            /// <param name="pMessage">The trace message in <see cref="String.Format(string, object[])"/> form.</param>
            /// <param name="pArgs">The objects to place in the trace message.</param>
            /// <returns>A new sub-context.</returns>
            public abstract cContext NewGeneric(bool pContextTraceDelay, string pMessage, params object[] pArgs);

            /// <summary>
            /// Creates a new sub-context with a trace message in 'object constructor' form.
            /// Use when creating a context for a constructor.
            /// </summary>
            /// <param name="pContextTraceDelay">Whether writing of context trace messages should be delayed. See <see cref="cTrace"/> for more information.</param>
            /// <param name="pClass">The name of the class.</param>
            /// <param name="pVersion">The version of the constructor.</param>
            /// <param name="pArgs">The parameters to the constructor that you want traced.</param>
            /// <returns>A new sub-context.</returns>
            public abstract cContext NewObjectV(bool pContextTraceDelay, string pClass, int pVersion, params object[] pArgs);

            /// <summary>
            /// Creates a new sub-context with a trace message in 'property setter' form.
            /// Use when creating a context for a property setter.
            /// </summary>
            /// <param name="pContextTraceDelay">Whether writing of context trace messages should be delayed. See <see cref="cTrace"/> for more information.</param>
            /// <param name="pClass">The name of the class.</param>
            /// <param name="pProperty">The name of the property.</param>
            /// <param name="pValue">The value being set.</param>
            /// <returns>A new sub-context.</returns>
            public abstract cContext NewSetProp(bool pContextTraceDelay, string pClass, string pProperty, object pValue);

            /// <summary>
            /// Creates a new sub-context with a trace message in 'method' form.
            /// Use when creating a context for a method.
            /// </summary>
            /// <param name="pContextTraceDelay">Whether writing of context trace messages should be delayed. See <see cref="cTrace"/> for more information.</param>
            /// <param name="pClass">The name of the class.</param>
            /// <param name="pMethod">The name of the method.</param>
            /// <param name="pVersion">The version of the method.</param>
            /// <param name="pArgs">The parameters to the method that you want traced.</param>
            /// <returns>A new sub-context.</returns>
            public abstract cContext NewMethodV(bool pContextTraceDelay, string pClass, string pMethod, int pVersion, params object[] pArgs);

            /// <summary>
            /// Creates a new root-context with a trace message in 'object constructor' form.
            /// Use when creating a new root-context in a constructor.
            /// </summary>
            /// <param name="pContextTraceDelay">Whether writing of context trace messages should be delayed. See <see cref="cTrace"/> for more information.</param>
            /// <param name="pClass">The name of the class.</param>
            /// <returns>A new root-context.</returns>
            public abstract cContext NewRootObject(bool pContextTraceDelay, string pClass);

            /// <summary>
            /// Creates a new root-context with a trace message in 'method' form.
            /// Use when creating a new root-context in a method.
            /// </summary>
            /// <param name="pContextTraceDelay">Whether writing of context trace messages should be delayed. See <see cref="cTrace"/> for more information.</param>
            /// <param name="pClass">The name of the class.</param>
            /// <param name="pMethod">The name of the method.</param>
            /// <returns>A new root-context.</returns>
            public abstract cContext NewRootMethod(bool pContextTraceDelay, string pClass, string pMethod);

            /**<summary>A version of <see cref="NewGeneric(bool, string, object[])"/> with delayed tracing set to false.</summary>*/
            public virtual cContext NewGeneric(string pMessage, params object[] pArgs) => NewGeneric(false, pMessage, pArgs);
            /**<summary>A version of <see cref="NewObjectV(bool, string, int, object[])"/> with delayed tracing set to false and the version number set to 1.</summary>*/
            public virtual cContext NewObject(string pClass, params object[] pArgs) => NewObjectV(false, pClass, 1, pArgs);
            /**<summary>A version of <see cref="NewObjectV(bool, string, int, object[])"/> with the version number set to 1.</summary>*/
            public virtual cContext NewObject(bool pContextTraceDelay, string pClass, params object[] pArgs) => NewObjectV(pContextTraceDelay, pClass, 1, pArgs);
            /**<summary>A version of <see cref="NewObjectV(bool, string, int, object[])"/> with delayed tracing set to false.</summary>*/
            public virtual cContext NewObjectV(string pClass, int pVersion, params object[] pArgs) => NewObjectV(false, pClass, pVersion, pArgs);
            /**<summary>A version of <see cref="NewSetProp(bool, string, string, object)"/> with delayed tracing set to false.</summary>*/
            public virtual cContext NewSetProp(string pClass, string pProperty, object pValue) => NewSetProp(false, pClass, pProperty, pValue);
            /**<summary>A version of <see cref="NewMethodV(bool, string, string, int, object[])"/> with delayed tracing set to false and the version number set to 1.</summary>*/
            public virtual cContext NewMethod(string pClass, string pMethod, params object[] pArgs) => NewMethodV(false, pClass, pMethod, 1, pArgs);
            /**<summary>A version of <see cref="NewMethodV(bool, string, string, int, object[])"/> with the version number set to 1.</summary>*/
            public virtual cContext NewMethod(bool pContextTraceDelay, string pClass, string pMethod, params object[] pArgs) => NewMethodV(pContextTraceDelay, pClass, pMethod, 1, pArgs);
            /**<summary>A version of <see cref="NewMethodV(bool, string, string, int, object[])"/> with delayed tracing set to false.</summary>*/
            public virtual cContext NewMethodV(string pClass, string pMethod, int pVersion, params object[] pArgs) => NewMethodV(false, pClass, pMethod, pVersion, pArgs);
            /**<summary>A version of <see cref="NewRootObject(bool, string)"/> with delayed tracing set to false.</summary>*/
            public virtual cContext NewRootObject(string pClass) => NewRootObject(false, pClass);
            /**<summary>A version of <see cref="NewRootMethod(bool, string, string)"/> with delayed tracing set to false.</summary>*/
            public virtual cContext NewRootMethod(string pClass, string pMethod) => NewRootMethod(false, pClass, pMethod);

            /**<summary>Indicates if context tracing being delayed. See <see cref="cTrace"/> for more information.</summary>*/
            public abstract bool ContextTraceDelay { get; }
            protected abstract void TraceContext();

            /// <summary>
            /// Writes a trace message.
            /// </summary>
            /// <param name="pTraceEventType">The trace event type.</param>
            /// <param name="pMessage">The trace message in <see cref="String.Format(string, object[])"/> form.</param>
            /// <param name="pArgs">The objects to place in the trace message.</param>
            [Conditional("TRACE")]
            public abstract void TraceEvent(TraceEventType pTraceEventType, string pMessage, params object[] pArgs);

            /**<summary>Indicates if the underlying context emits verbose trace messages.</summary>*/
            public abstract bool EmitsVerbose { get; }

            /// <summary>
            /// Writes a critcal trace message.
            /// </summary>
            /// <param name="pMessage">The trace message in <see cref="String.Format(string, object[])"/> form.</param>
            /// <param name="pArgs">The objects to place in the trace message.</param>
            [Conditional("TRACE")]
            public void TraceCritical(string pMessage, params object[] pArgs) => TraceEvent(TraceEventType.Critical, pMessage, pArgs);

            /// <summary>
            /// Writes an error trace message.
            /// </summary>
            /// <param name="pMessage">The trace message in <see cref="String.Format(string, object[])"/> form.</param>
            /// <param name="pArgs">The objects to place in the trace message.</param>
            [Conditional("TRACE")]
            public void TraceError(string pMessage, params object[] pArgs) => TraceEvent(TraceEventType.Error, pMessage, pArgs);

            /// <summary>
            /// Writes a warning trace message.
            /// </summary>
            /// <param name="pMessage">The trace message in <see cref="String.Format(string, object[])"/> form.</param>
            /// <param name="pArgs">The objects to place in the trace message.</param>
            [Conditional("TRACE")]
            public void TraceWarning(string pMessage, params object[] pArgs) => TraceEvent(TraceEventType.Warning, pMessage, pArgs);

            /// <summary>
            /// Writes an informational trace message.
            /// </summary>
            /// <param name="pMessage">The trace message in <see cref="String.Format(string, object[])"/> form.</param>
            /// <param name="pArgs">The objects to place in the trace message.</param>
            [Conditional("TRACE")]
            public void TraceInformation(string pMessage, params object[] pArgs) => TraceEvent(TraceEventType.Information, pMessage, pArgs);

            /// <summary>
            /// Writes a verbose trace message.
            /// </summary>
            /// <param name="pMessage">The trace message in <see cref="String.Format(string, object[])"/> form.</param>
            /// <param name="pArgs">The objects to place in the trace message.</param>
            [Conditional("TRACE")]
            public void TraceVerbose(string pMessage, params object[] pArgs) => TraceEvent(TraceEventType.Verbose, pMessage, pArgs);

            /**<summary>A version of <see cref="TraceException(TraceEventType, string, Exception)"/> with the event type set to <see cref="TraceEventType.Error"/> and a default message.</summary>*/
            public bool TraceException(Exception e) { TraceEvent(TraceEventType.Error, "Exception\n{0}", e); return false; }
            /**<summary>A version of <see cref="TraceException(TraceEventType, string, Exception)"/> with the event type set to <see cref="TraceEventType.Error"/>.</summary>*/
            public bool TraceException(string pMessage, Exception e) { TraceEvent(TraceEventType.Error, "{0}\n{1}", pMessage, e); return false; }
            /**<summary>A version of <see cref="TraceException(TraceEventType, string, Exception)"/> with a default message.</summary>*/
            public bool TraceException(TraceEventType pTraceEventType, Exception e) { TraceEvent(pTraceEventType, "Exception\n{0}", e); return false; }

            /// <summary>
            /// Writes a trace message reporting an exception.
            /// </summary>
            /// <remarks>
            /// Designed for use in a conditional catch clause to trace the exception as it 'flies by': e.g. <code>catch (Exception e) when (lContext.TraceException(e)) { }</code>.
            /// </remarks>
            /// <param name="pTraceEventType">The trace event type to use.</param>
            /// <param name="pMessage">A message to trace.</param>
            /// <param name="e">The exception to trace.</param>
            /// <returns>Always returns false.</returns>
            public bool TraceException(TraceEventType pTraceEventType, string pMessage, Exception e) { TraceEvent(pTraceEventType, "{0}\n{1}", pMessage, e); return false; }

            private class cNull : cContext
            {
                public cNull() { }

                public override cContext NewRoot(string pInstanceName, bool pContextTraceDelay) => this;

                public override cContext NewGeneric(bool pContextTraceDelay, string pMessage, params object[] pArgs) => this;
                public override cContext NewObject(bool pContextTraceDelay, string pClass, params object[] pArgs) => this;
                public override cContext NewSetProp(bool pContextTraceDelay, string pClass, string pProperty, object pValue) => this;
                public override cContext NewMethod(bool pContextTraceDelay, string pClass, string pMethod, params object[] pArgs) => this;
                public override cContext NewRootObject(bool pContextTraceDelay, string pClass) => this;
                public override cContext NewRootMethod(bool pContextTraceDelay, string pClass, string pMethod) => this;

                public override cContext NewGeneric(string pMessage, params object[] pArgs) => this;
                public override cContext NewObject(string pClass, params object[] pArgs) => this;
                public override cContext NewObjectV(bool pContextTraceDelay, string pClass, int pVersion, params object[] pArgs) => this;
                public override cContext NewObjectV(string pClass, int pVersion, params object[] pArgs) => this;
                public override cContext NewSetProp(string pClass, string pProperty, object pValue) => this;
                public override cContext NewMethod(string pClass, string pMethod, params object[] pArgs) => this;
                public override cContext NewMethodV(bool pContextTraceDelay, string pClass, string pMethod, int pVersion, params object[] pArgs) => this;
                public override cContext NewMethodV(string pClass, string pMethod, int pVersion, params object[] pArgs) => this;
                public override cContext NewRootObject(string pClass) => this;
                public override cContext NewRootMethod(string pClass, string pMethod) => this;

                public override bool ContextTraceDelay => true;
                protected override void TraceContext() { }
                public override void TraceEvent(TraceEventType pTraceEventType, string pMessage, params object[] pArgs) { }
                public override bool EmitsVerbose => false;
            }

            private string ZStringFormat(string pMessage, object[] pArgs)
            {
                if (pArgs.Length == 0) return pMessage;
                try { return string.Format(pMessage, pArgs); }
                catch { return $"malformed trace message: '{pMessage}' with {pArgs.Length} parameters"; }
            }

            /// <summary>
            /// A <see cref="cTrace"/> root-context.
            /// </summary>
            public class cRoot : cContext
            {
                private static int mInstanceNumberRoot = 7;

                private cTrace mTraceSource = null;
                private string mInstanceName;
                private int mInstanceNumber;
                private bool mContextTraceDelay;

                private object mLock;
                private volatile bool mLogged = false;

                public cRoot(cTrace pTraceSource, string pInstanceName, bool pContextTraceDelay)
                {
                    ZCtor(pTraceSource, pInstanceName, pContextTraceDelay);
                }

                [ConditionalAttribute("TRACE")]
                private void ZCtor(cTrace pTraceSource, string pInstanceName, bool pContextTraceDelay)
                {
                    if (pTraceSource.mTraceSource == null) return;

                    mTraceSource = pTraceSource;
                    mInstanceName = pInstanceName;
                    mInstanceNumber = Interlocked.Increment(ref mInstanceNumberRoot);

                    if (mTraceSource.ContextTraceMustBeDelayed) mContextTraceDelay = true;
                    else mContextTraceDelay = pContextTraceDelay;

                    mLock = new object();

                    if (!mContextTraceDelay) TraceContext();
                }

                public override cContext NewRoot(string pInstanceName, bool pContextTraceDelay)
                {
                    if (mTraceSource == null) return this;
                    return new cRoot(mTraceSource, $"{mInstanceName}.{pInstanceName}", pContextTraceDelay);
                }

                public override cContext NewGeneric(bool pContextTraceDelay, string pMessage, params object[] pArgs)
                {
                    if (mTraceSource == null) return this;
                    bool lContextTraceDelay = mContextTraceDelay || pContextTraceDelay;
                    var lResult = new cSubGeneric(this, this, 1, lContextTraceDelay, pMessage, pArgs);
                    if (!lContextTraceDelay) lResult.TraceContext();
                    return lResult;
                }

                public override cContext NewObjectV(bool pContextTraceDelay, string pClass, int pVersion, params object[] pArgs)
                {
                    if (mTraceSource == null) return this;
                    bool lContextTraceDelay = mContextTraceDelay || pContextTraceDelay;
                    var lResult = new cSubObjectV(this, this, 1, lContextTraceDelay, pClass, pVersion, pArgs);
                    if (!lContextTraceDelay) lResult.TraceContext();
                    return lResult;
                }

                public override cContext NewSetProp(bool pContextTraceDelay, string pClass, string pProperty, object pValue)
                {
                    if (mTraceSource == null) return this;
                    bool lContextTraceDelay = mContextTraceDelay || pContextTraceDelay;
                    var lResult = new cSubSetProp(this, this, 1, lContextTraceDelay, pClass, pProperty, pValue);
                    if (!lContextTraceDelay) lResult.TraceContext();
                    return lResult;
                }

                public override cContext NewMethodV(bool pContextTraceDelay, string pClass, string pMethod, int pVersion, params object[] pArgs)
                {
                    if (mTraceSource == null) return this;
                    bool lContextTraceDelay = mContextTraceDelay || pContextTraceDelay;
                    var lResult = new cSubMethodV(this, this, 1, lContextTraceDelay, pClass, pMethod, pVersion, pArgs);
                    if (!lContextTraceDelay) lResult.TraceContext();
                    return lResult;
                }

                public override cContext NewRootObject(bool pContextTraceDelay, string pClass)
                {
                    if (mTraceSource == null) return this;
                    return new cRoot(mTraceSource, $"{mInstanceName}.{pClass}", mContextTraceDelay || pContextTraceDelay);
                }

                public override cContext NewRootMethod(bool pContextTraceDelay, string pClass, string pMethod)
                {
                    if (mTraceSource == null) return this;
                    return new cRoot(mTraceSource, $"{mInstanceName}.{pClass}.{pMethod}", mContextTraceDelay || pContextTraceDelay);
                }

                public override bool ContextTraceDelay => mContextTraceDelay;

                protected override void TraceContext()
                {
                    if (mLogged) return;

                    lock (mLock) 
                    {
                        if (mLogged) return;
                        mTraceSource.TraceContext(mInstanceName, mInstanceNumber, 1, $"{mInstanceName}({mInstanceNumber})");
                        mLogged = true;
                    }
                }

                public override void TraceEvent(TraceEventType pTraceEventType, string pMessage, params object[] pArgs)
                {
                    if (mTraceSource == null) return;
                    if (!mTraceSource.Emits(pTraceEventType)) return;
                    TraceContext();
                    mTraceSource.TraceEvent(pTraceEventType, mInstanceName, mInstanceNumber, 1, ZStringFormat(pMessage, pArgs));
                }
            
                public override bool EmitsVerbose 
                {
                    get 
                    {
                        if (mTraceSource == null) return false;
                        return mTraceSource.Emits(TraceEventType.Verbose);
                    }
                }

                private abstract class cSub : cContext
                {
                    private cRoot mRoot;
                    private cContext mParent;
                    private int mLevel;
                    private bool mContextTraceDelay;

                    private object mLock = new object();
                    private volatile bool mLogged = false;

                    public cSub(cRoot pRoot, cContext pParent, int pParentLevel, bool pContextTraceDelay)
                    {
                        mRoot = pRoot;
                        mParent = pParent;
                        mLevel = pParentLevel + 1;
                        mContextTraceDelay = pParent.ContextTraceDelay || pContextTraceDelay;
                    }

                    public override cContext NewRoot(string pInstanceName, bool pContextTraceDelay) => new cRoot(mRoot.mTraceSource, $"{mRoot.mInstanceName}.{pInstanceName}", pContextTraceDelay);

                    public override cContext NewGeneric(bool pContextTraceDelay, string pMessage, params object[] pArgs)
                    {
                        bool lContextTraceDelay = mContextTraceDelay || pContextTraceDelay;
                        var lResult = new cSubGeneric(mRoot, this, mLevel, lContextTraceDelay, pMessage, pArgs);
                        if (!lContextTraceDelay) lResult.TraceContext();
                        return lResult;
                    }

                    public override cContext NewObjectV(bool pContextTraceDelay, string pClass, int pVersion, params object[] pArgs)
                    {
                        bool lContextTraceDelay = mContextTraceDelay || pContextTraceDelay;
                        var lResult = new cSubObjectV(mRoot, this, mLevel, lContextTraceDelay, pClass, pVersion, pArgs);
                        if (!lContextTraceDelay) lResult.TraceContext();
                        return lResult;
                    }

                    public override cContext NewSetProp(bool pContextTraceDelay, string pClass, string pProperty, object pValue)
                    {
                        bool lContextTraceDelay = mContextTraceDelay || pContextTraceDelay;
                        var lResult = new cSubSetProp(mRoot, this, mLevel, lContextTraceDelay, pClass, pProperty, pValue);
                        if (!lContextTraceDelay) lResult.TraceContext();
                        return lResult;
                    }

                    public override cContext NewMethodV(bool pContextTraceDelay, string pClass, string pMethod, int pVersion, params object[] pArgs)
                    {
                        bool lContextTraceDelay = mContextTraceDelay || pContextTraceDelay;
                        var lResult = new cSubMethodV(mRoot, this, mLevel, lContextTraceDelay, pClass, pMethod, pVersion, pArgs);
                        if (!lContextTraceDelay) lResult.TraceContext();
                        return lResult;
                    }

                    public override cContext NewRootObject(bool pContextTraceDelay, string pClass) => new cRoot(mRoot.mTraceSource, $"{mRoot.mInstanceName}.{pClass}", mContextTraceDelay || pContextTraceDelay);
                    public override cContext NewRootMethod(bool pContextTraceDelay, string pClass, string pMethod) => new cRoot(mRoot.mTraceSource, $"{mRoot.mInstanceName}.{pClass}.{pMethod}", mContextTraceDelay || pContextTraceDelay);

                    public override bool ContextTraceDelay => mContextTraceDelay;

                    protected override void TraceContext()
                    {
                        if (mLogged) return;

                        lock (mLock)
                        {
                            if (mLogged) return;
                            mParent.TraceContext();
                            mRoot.mTraceSource.TraceContext(mRoot.mInstanceName, mRoot.mInstanceNumber, mLevel, Context);
                            mLogged = true;
                        }
                    }

                    protected abstract string Context { get; }

                    public override void TraceEvent(TraceEventType pTraceEventType, string pMessage, params object[] pArgs)
                    {
                        if (!mRoot.mTraceSource.Emits(pTraceEventType)) return;
                        TraceContext();
                        mRoot.mTraceSource.TraceEvent(pTraceEventType, mRoot.mInstanceName, mRoot.mInstanceNumber, mLevel, ZStringFormat(pMessage, pArgs));
                    }

                    public override bool EmitsVerbose => mRoot.mTraceSource.Emits(TraceEventType.Verbose); 
                }

                private class cSubGeneric : cSub
                {
                    private string mMessage;
                    private object[] mArgs;

                    public cSubGeneric(cRoot pRoot, cContext pParent, int pParentLevel, bool pContextTraceDelay, string pMessage, params object[] pArgs) : base(pRoot, pParent, pParentLevel, pContextTraceDelay)
                    {
                        mMessage = pMessage;
                        mArgs = pArgs;
                    }

                    protected override string Context => ZStringFormat(mMessage, mArgs);
                }

                private abstract class cSubArgs : cSub
                {
                    protected object[] mArgs;

                    public cSubArgs(cRoot pRoot, cContext pParent, int pParentLevel, bool pContextTraceDelay, object[] pArgs) : base(pRoot, pParent, pParentLevel, pContextTraceDelay)
                    {
                        mArgs = pArgs;
                    }

                    public string Arguments
                    {
                        get
                        {
                            if (mArgs.Length == 0) return "()";

                            StringBuilder lArgs = new StringBuilder("(");

                            bool lComma = false;

                            foreach (object lArg in mArgs)
                            {
                                if (lComma) lArgs.Append(',');
                                else lComma = true;
                                try { lArgs.Append(lArg); }
                                catch { lArgs.Append('?'); }
                            }

                            lArgs.Append(')');

                            return lArgs.ToString();
                        }
                    }
                }

                private class cSubObject : cSubArgs
                {
                    protected string mClass;

                    public cSubObject(cRoot pRoot, cContext pParent, int pParentLevel, bool pContextTraceDelay, string pClass, params object[] pArgs) : base(pRoot, pParent, pParentLevel, pContextTraceDelay, pArgs)
                    {
                        mClass = pClass;
                    }

                    protected virtual string ClassName => $"{mClass}";

                    protected override string Context => ClassName + Arguments;
                }

                private class cSubObjectV : cSubObject
                {
                    private int mVersion;

                    public cSubObjectV(cRoot pRoot, cContext pParent, int pParentLevel, bool pContextTraceDelay, string pClass, int pVersion, params object[] pArgs) : base(pRoot, pParent, pParentLevel, pContextTraceDelay, pClass, pArgs)
                    {
                        mVersion = pVersion;
                    }

                    protected override string ClassName => $"{mClass}.{mVersion}";
                }

                private class cSubSetProp : cSub
                {
                    private string mClass;
                    private string mProperty;
                    private object mValue;

                    public cSubSetProp(cRoot pRoot, cContext pParent, int pParentLevel, bool pContextTraceDelay, string pClass, string pProperty, object pValue) : base(pRoot, pParent, pParentLevel, pContextTraceDelay)
                    {
                        mClass = pClass;
                        mProperty = pProperty;
                        mValue = pValue;
                    }

                    protected override string Context => $"{mClass}.{mProperty}={mValue}";
                }

                private class cSubMethod : cSubArgs
                {
                    protected string mClass;
                    protected string mMethod;

                    public cSubMethod(cRoot pRoot, cContext pParent, int pParentLevel, bool pContextTraceDelay, string pClass, string pMethod, params object[] pArgs) : base(pRoot, pParent, pParentLevel, pContextTraceDelay, pArgs)
                    {
                        mClass = pClass;
                        mMethod = pMethod;
                    }

                    protected virtual string MethodName => $"{mClass}.{mMethod}";

                    protected override string Context => MethodName + Arguments;
                }

                private class cSubMethodV : cSubMethod
                {
                    private int mVersion;

                    public cSubMethodV(cRoot pRoot, cContext pParent, int pParentLevel, bool pContextTraceDelay, string pClass, string pMethod, int pVersion, params object[] pArgs) : base(pRoot, pParent, pParentLevel, pContextTraceDelay, pClass, pMethod, pArgs)
                    {
                        mVersion = pVersion;
                    }

                    protected override string MethodName => $"{mClass}.{mMethod}.{mVersion}";
                }
            }
        }
    }
}
