using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace work.bacome.imapclient.support
{
    /// <summary>
    /// Provides services for writing trace events with indenting and nested context information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instances of this class wrap a <see cref="TraceSource"/> and trace messages are written using its services.
    /// </para>
    /// <note type="note">
    /// The services provided by this class do not support the dynamic addition of trace listeners to the wrapped <see cref="TraceSource"/>.
    /// The trace messages that will be written by an instance are determined at the time of construction (by inspecting the state of the <see cref="TraceSource"/>).
    /// </note>
    /// <para>
    /// Trace messages are always written in a context.
    /// Context starts at an independent root-context.
    /// Sub-contexts can be established within root-contexts and within other sub-contexts.
    /// If a new sub-context is created for each call then call stack information can be built and included in the trace.
    /// </para>
    /// <para>
    /// Context creation itself can result in a trace message (the context-create trace message).
    /// Context-create trace messages can be written immediately (as the contexts are created),
    /// or they can be written only if, and only when, a non-context-create trace message is written in the context (or a sub-context of the context).
    /// This delayed context-create writing can minimise unnecessary and unhelpful tracing, whilst retaining the 
    /// benefits of tracing the full context when something interesting happens.
    /// </para>
    /// <note type="note" >
    /// If the writing of context-create trace messages is delayed then the generation of the trace message text is also delayed.
    /// If there are mutable objects to be included in the trace message text, this can lead to misleading context-create trace messages.
    /// </note>
    /// <para>
    /// Tracing can be disabled, either for an instance as a whole or for parts of the context tree.
    /// When tracing is disabled contexts are not created and trace messages are not written, so most of the tracing overhead is eliminated.
    /// Tracing is disabled under the following circumstances;
    /// <list type="bullet">
    /// <item>The assembly is compiled without the "TRACE" conditional attribute.</item>
    /// <item>There aren't any listeners attached to the wrapped <see cref="TraceSource"/> when the instance is constructed.</item>
    /// <item>The wrapped <see cref="TraceSource"/> isn't configured to emit <see cref="TraceEventType.Critical"/> messages when the instance is constructed.</item>
    /// <item>The <see cref="cContext.None"/> context is used as a root-context.</item>
    /// </list>
    /// </para>
    /// <para>Root-contexts have a name and a number. The name is programmer assigned, the number is assigned by the class and is unique within the program.</para>
    /// <para>Trace messages are written in a tab delimited form, the tab delimited columns contain;
    /// <list type="number">
    /// <item>The <see cref="System.Diagnostics.TraceSource"/> defined data.</item>
    /// <item>The date and time that the message was written.</item>
    /// <item>The name and number of the root-context associated with the trace message.</item>
    /// <item>The thread number on which the trace message was written.</item>
    /// <item>The space indented (by a number of spaces equal to the context-depth) trace message.</item>
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
        /// Initialises a new instance with the specified trace source name.
        /// </summary>
        /// <param name="pTraceSourceName">The <see cref="TraceSource"/> name to use.</param>
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
        /// Returns a new independent root-context.
        /// </summary>
        /// <param name="pInstanceName">The name to give the context.</param>
        /// <param name="pContextTraceDelay"></param>
        /// <returns></returns>
        public cContext NewRoot(string pInstanceName, bool pContextTraceDelay = false)
        {
            if (mTraceSource == null) return cContext.None;
            return new cContext.cRoot(this, pInstanceName, pContextTraceDelay);
        }

        /// <summary>
        /// Represents a tracing context.
        /// </summary>
        /// <remarks>
        /// Instances will be either a root-context or a sub-context.
        /// </remarks>
        /// <seealso cref="cTrace"/>
        public abstract class cContext
        {
            /**<summary>A tracing context that does not create contexts or emit messages. Can be used to suppress tracing.</summary>*/
            public readonly static cContext None = new cNone();

            internal cContext() { }

            /// <summary>
            /// Returns a new root-context tied (in name only) to the root-context of this instance.
            /// </summary>
            /// <param name="pInstanceName">A string to use when creating the name of the new context.</param>
            /// <param name="pContextTraceDelay"></param>
            /// <returns></returns>
            public abstract cContext NewRoot(string pInstanceName, bool pContextTraceDelay = false);

            /// <summary>
            /// Returns a new sub-context with a free format context-create trace message.
            /// </summary>
            /// <param name="pContextTraceDelay"></param>
            /// <param name="pMessage">The context-create trace message in <see cref="String.Format(string, object[])"/> format.</param>
            /// <param name="pArgs">The objects to place in the context-create trace message text.</param>
            /// <returns></returns>
            public abstract cContext NewGeneric(bool pContextTraceDelay, string pMessage, params object[] pArgs);

            /// <summary>
            /// Returns a new sub-context with a context-create trace message in 'object constructor' format.
            /// </summary>
            /// <param name="pContextTraceDelay"></param>
            /// <param name="pClass">The name of the class.</param>
            /// <param name="pVersion">The version of the constructor.</param>
            /// <param name="pArgs">The parameters to the constructor that should be in the trace message.</param>
            /// <returns></returns>
            /// <remarks>For use when creating a context for a constructor.</remarks>
            public abstract cContext NewObjectV(bool pContextTraceDelay, string pClass, int pVersion, params object[] pArgs);

            /// <summary>
            /// Returns a new sub-context with a context-create trace message in 'property setter' format.
            /// </summary>
            /// <param name="pContextTraceDelay"></param>
            /// <param name="pClass">The name of the class.</param>
            /// <param name="pProperty">The name of the property.</param>
            /// <param name="pValue">The value being set.</param>
            /// <returns></returns>
            /// <remarks>For use when creating a context for a property setter.</remarks>
            public abstract cContext NewSetProp(bool pContextTraceDelay, string pClass, string pProperty, object pValue);

            /// <summary>
            /// Returns a new sub-context with a context-create trace message in 'method' format.
            /// </summary>
            /// <param name="pContextTraceDelay"></param>
            /// <param name="pClass">The name of the class.</param>
            /// <param name="pMethod">The name of the method.</param>
            /// <param name="pVersion">The version of the method.</param>
            /// <param name="pArgs">The parameters to the method that should be in the trace message.</param>
            /// <returns></returns>
            /// <remarks>
            /// For use when creating a context for a method.
            /// </remarks>
            public abstract cContext NewMethodV(bool pContextTraceDelay, string pClass, string pMethod, int pVersion, params object[] pArgs);

            /// <summary>
            /// Returns a new root-context with a context-create trace message in 'object constructor' format.
            /// </summary>
            /// <param name="pContextTraceDelay"></param>
            /// <param name="pClass">The name of the class.</param>
            /// <returns></returns>
            /// <remarks>
            /// For use when creating a new root-context in a constructor.
            /// </remarks>
            public abstract cContext NewRootObject(bool pContextTraceDelay, string pClass);

            /// <summary>
            /// Returns a new root-context with a context-create trace message in 'method' format.
            /// </summary>
            /// <param name="pContextTraceDelay"></param>
            /// <param name="pClass">The name of the class.</param>
            /// <param name="pMethod">The name of the method.</param>
            /// <returns></returns>
            /// <remarks>
            /// For use when creating a new root-context in a method.
            /// </remarks>
            public abstract cContext NewRootMethod(bool pContextTraceDelay, string pClass, string pMethod);

            /// <summary>
            /// Returns a new sub-context with a free format context-create trace message.
            /// </summary>
            /// <param name="pMessage">The trace message in <see cref="String.Format(string, object[])"/> format.</param>
            /// <param name="pArgs">The objects to place in the context-create trace message text.</param>
            /// <returns></returns>
            public virtual cContext NewGeneric(string pMessage, params object[] pArgs) => NewGeneric(false, pMessage, pArgs);

            /// <summary>
            /// Returns a new sub-context with a context-create trace message in 'object constructor' format.
            /// </summary>
            /// <param name="pClass">The name of the class.</param>
            /// <param name="pArgs">The parameters to the constructor that should be in the trace message.</param>
            /// <returns></returns>
            /// <remarks>
            /// For use when creating a context for a constructor.
            /// </remarks>
            public virtual cContext NewObject(string pClass, params object[] pArgs) => NewObjectV(false, pClass, 1, pArgs);

            /// <summary>
            /// Returns a new sub-context with a context-create trace message in 'object constructor' format.
            /// </summary>
            /// <param name="pContextTraceDelay"></param>
            /// <param name="pClass">The name of the class.</param>
            /// <param name="pArgs">The parameters to the constructor that should be in the trace message.</param>
            /// <returns></returns>
            /// <remarks>
            /// For use when creating a context for a constructor.
            /// </remarks>
            public virtual cContext NewObject(bool pContextTraceDelay, string pClass, params object[] pArgs) => NewObjectV(pContextTraceDelay, pClass, 1, pArgs);

            /// <summary>
            /// Returns a new sub-context with a context-create trace message in 'object constructor' format.
            /// </summary>
            /// <param name="pClass">The name of the class.</param>
            /// <param name="pVersion">The version of the constructor.</param>
            /// <param name="pArgs">The parameters to the constructor that should be in the trace message.</param>
            /// <returns></returns>
            /// <remarks>
            /// For use when creating a context for a constructor.
            /// </remarks>
            public virtual cContext NewObjectV(string pClass, int pVersion, params object[] pArgs) => NewObjectV(false, pClass, pVersion, pArgs);

            /// <summary>
            /// Returns a new sub-context with a context-create trace message in 'property setter' format.
            /// </summary>
            /// <param name="pClass">The name of the class.</param>
            /// <param name="pProperty">The name of the property.</param>
            /// <param name="pValue">The value being set.</param>
            /// <returns></returns>
            /// <remarks>
            /// For use when creating a context for a property setter.
            /// </remarks>
            public virtual cContext NewSetProp(string pClass, string pProperty, object pValue) => NewSetProp(false, pClass, pProperty, pValue);

            /// <summary>
            /// Returns a new sub-context with a context-create trace message in 'method' format.
            /// </summary>
            /// <param name="pClass">The name of the class.</param>
            /// <param name="pMethod">The name of the method.</param>
            /// <param name="pArgs">The parameters to the method that should be in the trace message.</param>
            /// <returns></returns>
            /// <remarks>
            /// For use when creating a context for a method.
            /// </remarks>
            public virtual cContext NewMethod(string pClass, string pMethod, params object[] pArgs) => NewMethodV(false, pClass, pMethod, 1, pArgs);

            /// <summary>
            /// Returns a new sub-context with a context-create trace message in 'method' format.
            /// </summary>
            /// <param name="pContextTraceDelay"></param>
            /// <param name="pClass">The name of the class.</param>
            /// <param name="pMethod">The name of the method.</param>
            /// <param name="pArgs">The parameters to the method that should be in the trace message.</param>
            /// <returns></returns>
            /// <remarks>
            /// For use when creating a context for a method.
            /// </remarks>
            public virtual cContext NewMethod(bool pContextTraceDelay, string pClass, string pMethod, params object[] pArgs) => NewMethodV(pContextTraceDelay, pClass, pMethod, 1, pArgs);

            /// <summary>
            /// Returns a new sub-context with a context-create trace message in 'method' format.
            /// </summary>
            /// <param name="pClass">The name of the class.</param>
            /// <param name="pMethod">The name of the method.</param>
            /// <param name="pVersion">The version of the method.</param>
            /// <param name="pArgs">The parameters to the method that should be in the trace message.</param>
            /// <returns></returns>
            /// <remarks>
            /// For use when creating a context for a method.
            /// </remarks>
            public virtual cContext NewMethodV(string pClass, string pMethod, int pVersion, params object[] pArgs) => NewMethodV(false, pClass, pMethod, pVersion, pArgs);

            /// <summary>
            /// Returns a new root-context with a context-create trace message in 'object constructor' format.
            /// </summary>
            /// <param name="pClass">The name of the class.</param>
            /// <returns></returns>
            /// <remarks>
            /// For use when creating a new root-context in a constructor.
            /// </remarks>
            public virtual cContext NewRootObject(string pClass) => NewRootObject(false, pClass);

            /// <summary>
            /// Returns a new root-context with a context-create trace message in 'method' format.
            /// </summary>
            /// <param name="pClass">The name of the class.</param>
            /// <param name="pMethod">The name of the method.</param>
            /// <returns></returns>
            /// <remarks>
            /// For use when creating a new root-context in a method.
            /// </remarks>
            public virtual cContext NewRootMethod(string pClass, string pMethod) => NewRootMethod(false, pClass, pMethod);

            /**<summary>Indicates whether the writing of context-create trace messages is being delayed for the context and its sub-contexts.</summary>*/
            public abstract bool ContextTraceDelay { get; }
            /**<summary></summary>*/
            protected abstract void TraceContext();

            /// <summary>
            /// Writes a trace message.
            /// </summary>
            /// <param name="pTraceEventType"></param>
            /// <param name="pMessage">The trace message in <see cref="String.Format(string, object[])"/> format.</param>
            /// <param name="pArgs">The objects to place in the trace message text.</param>
            [Conditional("TRACE")]
            public abstract void TraceEvent(TraceEventType pTraceEventType, string pMessage, params object[] pArgs);

            /**<summary>Indicates whether the underlying <see cref="TraceSource"/> emits verbose trace messages. This value is determined at the time the containing <see cref="cTrace"/> is constructed.</summary>*/
            public abstract bool EmitsVerbose { get; }

            /// <summary>
            /// Writes a critcal trace message.
            /// </summary>
            /// <param name="pMessage">The trace message in <see cref="String.Format(string, object[])"/> format.</param>
            /// <param name="pArgs">The objects to place in the trace message text.</param>
            [Conditional("TRACE")]
            public void TraceCritical(string pMessage, params object[] pArgs) => TraceEvent(TraceEventType.Critical, pMessage, pArgs);

            /// <summary>
            /// Writes an error trace message.
            /// </summary>
            /// <param name="pMessage">The trace message in <see cref="String.Format(string, object[])"/> format.</param>
            /// <param name="pArgs">The objects to place in the trace message text.</param>
            [Conditional("TRACE")]
            public void TraceError(string pMessage, params object[] pArgs) => TraceEvent(TraceEventType.Error, pMessage, pArgs);

            /// <summary>
            /// Writes a warning trace message.
            /// </summary>
            /// <param name="pMessage">The trace message in <see cref="String.Format(string, object[])"/> format.</param>
            /// <param name="pArgs">The objects to place in the trace message text.</param>
            [Conditional("TRACE")]
            public void TraceWarning(string pMessage, params object[] pArgs) => TraceEvent(TraceEventType.Warning, pMessage, pArgs);

            /// <summary>
            /// Writes an informational trace message.
            /// </summary>
            /// <param name="pMessage">The trace message in <see cref="String.Format(string, object[])"/> format.</param>
            /// <param name="pArgs">The objects to place in the trace message text.</param>
            [Conditional("TRACE")]
            public void TraceInformation(string pMessage, params object[] pArgs) => TraceEvent(TraceEventType.Information, pMessage, pArgs);

            /// <summary>
            /// Writes a verbose trace message.
            /// </summary>
            /// <param name="pMessage">The trace message in <see cref="String.Format(string, object[])"/> format.</param>
            /// <param name="pArgs">The objects to place in the trace message text.</param>
            [Conditional("TRACE")]
            public void TraceVerbose(string pMessage, params object[] pArgs) => TraceEvent(TraceEventType.Verbose, pMessage, pArgs);

            /// <summary>
            /// Writes a trace message reporting an exception.
            /// </summary>
            /// <param name="pTraceEventType"></param>
            /// <param name="pMessage"></param>
            /// <param name="e"></param>
            /// <returns>Always returns <see langword="false"/>.</returns>
            /// <remarks>
            /// Designed for use in a conditional catch clause to trace the exception as it 'flies by': e.g.
            /// <code>catch (Exception e) when (lContext.TraceException(e)) { }</code>.
            /// </remarks>
            public bool TraceException(TraceEventType pTraceEventType, string pMessage, Exception e) { TraceEvent(pTraceEventType, "{0}\n{1}", pMessage, e); return false; }

            /// <param name="e"></param>
            /// <inheritdoc cref="TraceException(TraceEventType, string, Exception)" select="summary|returns|remarks"/>
            public bool TraceException(Exception e) { TraceEvent(TraceEventType.Error, "Exception\n{0}", e); return false; }

            /// <param name="pMessage"></param>
            /// <param name="e"></param>
            /// <inheritdoc cref="TraceException(TraceEventType, string, Exception)" select="summary|returns|remarks"/>
            public bool TraceException(string pMessage, Exception e) { TraceEvent(TraceEventType.Error, "{0}\n{1}", pMessage, e); return false; }

            /// <param name="pTraceEventType"></param>
            /// <param name="e"></param>
            /// <inheritdoc cref="TraceException(TraceEventType, string, Exception)" select="summary|returns|remarks"/>
            public bool TraceException(TraceEventType pTraceEventType, Exception e) { TraceEvent(pTraceEventType, "Exception\n{0}", e); return false; }

            private class cNone : cContext
            {
                public cNone() { }

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
            /// Represents a root-context.
            /// </summary>
            /// <seealso cref="cTrace"/>
            public class cRoot : cContext
            {
                private static int mInstanceNumberRoot = 7;

                private cTrace mTraceSource = null;
                private string mInstanceName;
                private int mInstanceNumber;
                private bool mContextTraceDelay;

                private object mLock;
                private volatile bool mLogged = false;

                internal cRoot(cTrace pTraceSource, string pInstanceName, bool pContextTraceDelay)
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

                /// <inheritdoc/>
                public override cContext NewRoot(string pInstanceName, bool pContextTraceDelay)
                {
                    if (mTraceSource == null) return this;
                    return new cRoot(mTraceSource, $"{mInstanceName}.{pInstanceName}", pContextTraceDelay);
                }

                /// <inheritdoc/>
                public override cContext NewGeneric(bool pContextTraceDelay, string pMessage, params object[] pArgs)
                {
                    if (mTraceSource == null) return this;
                    bool lContextTraceDelay = mContextTraceDelay || pContextTraceDelay;
                    var lResult = new cSubGeneric(this, this, 1, lContextTraceDelay, pMessage, pArgs);
                    if (!lContextTraceDelay) lResult.TraceContext();
                    return lResult;
                }

                /// <inheritdoc/>
                public override cContext NewObjectV(bool pContextTraceDelay, string pClass, int pVersion, params object[] pArgs)
                {
                    if (mTraceSource == null) return this;
                    bool lContextTraceDelay = mContextTraceDelay || pContextTraceDelay;
                    var lResult = new cSubObjectV(this, this, 1, lContextTraceDelay, pClass, pVersion, pArgs);
                    if (!lContextTraceDelay) lResult.TraceContext();
                    return lResult;
                }

                /// <inheritdoc/>
                public override cContext NewSetProp(bool pContextTraceDelay, string pClass, string pProperty, object pValue)
                {
                    if (mTraceSource == null) return this;
                    bool lContextTraceDelay = mContextTraceDelay || pContextTraceDelay;
                    var lResult = new cSubSetProp(this, this, 1, lContextTraceDelay, pClass, pProperty, pValue);
                    if (!lContextTraceDelay) lResult.TraceContext();
                    return lResult;
                }

                /// <inheritdoc/>
                public override cContext NewMethodV(bool pContextTraceDelay, string pClass, string pMethod, int pVersion, params object[] pArgs)
                {
                    if (mTraceSource == null) return this;
                    bool lContextTraceDelay = mContextTraceDelay || pContextTraceDelay;
                    var lResult = new cSubMethodV(this, this, 1, lContextTraceDelay, pClass, pMethod, pVersion, pArgs);
                    if (!lContextTraceDelay) lResult.TraceContext();
                    return lResult;
                }

                /// <inheritdoc/>
                public override cContext NewRootObject(bool pContextTraceDelay, string pClass)
                {
                    if (mTraceSource == null) return this;
                    return new cRoot(mTraceSource, $"{mInstanceName}.{pClass}", mContextTraceDelay || pContextTraceDelay);
                }

                /// <inheritdoc/>
                public override cContext NewRootMethod(bool pContextTraceDelay, string pClass, string pMethod)
                {
                    if (mTraceSource == null) return this;
                    return new cRoot(mTraceSource, $"{mInstanceName}.{pClass}.{pMethod}", mContextTraceDelay || pContextTraceDelay);
                }

                /// <inheritdoc/>
                public override bool ContextTraceDelay => mContextTraceDelay;

                /**<summary></summary>*/
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

                /// <inheritdoc/>
                public override void TraceEvent(TraceEventType pTraceEventType, string pMessage, params object[] pArgs)
                {
                    if (mTraceSource == null) return;
                    if (!mTraceSource.Emits(pTraceEventType)) return;
                    TraceContext();
                    mTraceSource.TraceEvent(pTraceEventType, mInstanceName, mInstanceNumber, 1, ZStringFormat(pMessage, pArgs));
                }

                /// <inheritdoc/>
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
