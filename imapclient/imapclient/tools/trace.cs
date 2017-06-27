using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace work.bacome.trace
{
    public class cTrace
    {
        private TraceSource mTraceSource = null;
        private TraceEventType mLevel = 0;
        private bool mContextTraceMustBeDelayed = true;
        private TraceEventType mContextTraceEventType;

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

            if (Emits(TraceEventType.Verbose) || Emits(TraceEventType.Information))
            {
                mContextTraceMustBeDelayed = false;
                mContextTraceEventType = TraceEventType.Information;
            }
            else
            {
                if (Emits(TraceEventType.Warning)) mContextTraceEventType = TraceEventType.Warning;
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

        public cContext NewRoot(string pInstanceName, bool pContextTraceDelay = false)
        {
            if (mTraceSource == null) return cContext.Null;
            return new cContext.cRoot(this, pInstanceName, pContextTraceDelay);
        }

        public abstract class cContext
        {
            public readonly static cContext Null = new cNull();

            public abstract cContext NewRoot(string pInstanceName, bool pContextTraceDelay = false);

            public abstract cContext NewGeneric(bool pContextTraceDelay, string pMessage, params object[] pArgs);
            public abstract cContext NewObjectV(bool pContextTraceDelay, string pClass, int pVersion, params object[] pArgs);
            public abstract cContext NewSetProp(bool pContextTraceDelay, string pClass, string pProperty, object pValue);
            public abstract cContext NewMethodV(bool pContextTraceDelay, string pClass, string pMethod, int pVersion, params object[] pArgs);
            public abstract cContext NewRootMethod(bool pContextTraceDelay, string pClass, string pMethod);

            public virtual cContext NewGeneric(string pMessage, params object[] pArgs) => NewGeneric(false, pMessage, pArgs);
            public virtual cContext NewObject(string pClass, params object[] pArgs) => NewObjectV(false, pClass, 1, pArgs);
            public virtual cContext NewObject(bool pContextTraceDelay, string pClass, params object[] pArgs) => NewObjectV(pContextTraceDelay, pClass, 1, pArgs);
            public virtual cContext NewObjectV(string pClass, int pVersion, params object[] pArgs) => NewObjectV(false, pClass, pVersion, pArgs);
            public virtual cContext NewSetProp(string pClass, string pProperty, object pValue) => NewSetProp(false, pClass, pProperty, pValue);
            public virtual cContext NewMethod(string pClass, string pMethod, params object[] pArgs) => NewMethodV(false, pClass, pMethod, 1, pArgs);
            public virtual cContext NewMethod(bool pContextTraceDelay, string pClass, string pMethod, params object[] pArgs) => NewMethodV(pContextTraceDelay, pClass, pMethod, 1, pArgs);
            public virtual cContext NewMethodV(string pClass, string pMethod, int pVersion, params object[] pArgs) => NewMethodV(false, pClass, pMethod, pVersion, pArgs);
            public virtual cContext NewRootMethod(string pClass, string pMethod) => NewRootMethod(false, pClass, pMethod);

            public abstract bool ContextTraceDelay { get; }
            protected abstract void TraceContext();

            [Conditional("TRACE")]
            public abstract void TraceEvent(TraceEventType pTraceEventType, string pMessage, params object[] pArgs);

            public abstract bool EmitsVerbose { get; }

            [Conditional("TRACE")]
            public void TraceCritical(string pMessage, params object[] pArgs) => TraceEvent(TraceEventType.Critical, pMessage, pArgs);

            [Conditional("TRACE")]
            public void TraceError(string pMessage, params object[] pArgs) => TraceEvent(TraceEventType.Error, pMessage, pArgs);

            [Conditional("TRACE")]
            public void TraceWarning(string pMessage, params object[] pArgs) => TraceEvent(TraceEventType.Warning, pMessage, pArgs);

            [Conditional("TRACE")]
            public void TraceInformation(string pMessage, params object[] pArgs) => TraceEvent(TraceEventType.Information, pMessage, pArgs);

            [Conditional("TRACE")]
            public void TraceVerbose(string pMessage, params object[] pArgs) => TraceEvent(TraceEventType.Verbose, pMessage, pArgs);

            // these methods for tracing exceptions as they 'fly by' using a line like this: catch (exception e) when (x.TraceException(e))
            public bool TraceException(Exception e) { TraceEvent(TraceEventType.Error, "Exception\n{0}", e); return false; }
            public bool TraceException(string pMessage, Exception e) { TraceEvent(TraceEventType.Error, "{0}\n{1}", pMessage, e); return false; }
            public bool TraceException(TraceEventType pTraceEventType, Exception e) { TraceEvent(pTraceEventType, "Exception\n{0}", e); return false; }
            public bool TraceException(TraceEventType pTraceEventType, string pMessage, Exception e) { TraceEvent(pTraceEventType, "{0}\n{1}", pMessage, e); return false; }

            private class cNull : cContext
            {
                public cNull() { }

                public override cContext NewRoot(string pInstanceName, bool pContextTraceDelay) => this;

                public override cContext NewGeneric(bool pContextTraceDelay, string pMessage, params object[] pArgs) => this;
                public override cContext NewObject(bool pContextTraceDelay, string pClass, params object[] pArgs) => this;
                public override cContext NewSetProp(bool pContextTraceDelay, string pClass, string pProperty, object pValue) => this;
                public override cContext NewMethod(bool pContextTraceDelay, string pClass, string pMethod, params object[] pArgs) => this;
                public override cContext NewRootMethod(bool pContextTraceDelay, string pClass, string pMethod) => this;

                public override cContext NewGeneric(string pMessage, params object[] pArgs) => this;
                public override cContext NewObject(string pClass, params object[] pArgs) => this;
                public override cContext NewObjectV(bool pContextTraceDelay, string pClass, int pVersion, params object[] pArgs) => this;
                public override cContext NewObjectV(string pClass, int pVersion, params object[] pArgs) => this;
                public override cContext NewSetProp(string pClass, string pProperty, object pValue) => this;
                public override cContext NewMethod(string pClass, string pMethod, params object[] pArgs) => this;
                public override cContext NewMethodV(bool pContextTraceDelay, string pClass, string pMethod, int pVersion, params object[] pArgs) => this;
                public override cContext NewMethodV(string pClass, string pMethod, int pVersion, params object[] pArgs) => this;
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
