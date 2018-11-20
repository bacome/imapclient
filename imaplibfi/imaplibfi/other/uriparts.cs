using System;
using work.bacome.imapclient;

namespace work.bacome.imapinternals
{
    [Flags]
    public enum fURIParts
    {
        scheme = 1 << 0,
        userinfo = 1 << 1,
        host = 1 << 2,
        port = 1 << 3,
        pathroot = 1 << 4,
        path = 1 << 5,
        query = 1 << 6,
        fragment = 1 << 7
    }

    public class cURIParts : IEquatable<cURIParts>
    {
        private fURIParts mParts;

        private string _Scheme;
        private string _UserInfo;
        private string _Host;
        private string _DisplayHost;
        private string _Port;
        private string _Path;
        private string _Query;
        private string _Fragment;

        public string Scheme
        {
            get => _Scheme;

            set
            {
                _Scheme = value;
                mParts |= fURIParts.scheme;
            }
        }

        public string UserInfo
        {
            get => _UserInfo;

            set
            {
                _UserInfo = value;
                mParts |= fURIParts.userinfo;
            }
        }

        public string Host
        {
            get => _Host;

            set
            {
                _Host = value;
                _DisplayHost = cTools.GetDisplayHost(value);
                mParts |= fURIParts.host;
            }
        }

        public string DisplayHost => _DisplayHost;

        public string Port
        {
            get => _Port;

            set
            {
                _Port = value;
                mParts |= fURIParts.port;
            }
        }

        public void SetHasPathRoot() => mParts |= fURIParts.pathroot;

        public string Path
        {
            get => _Path;

            set
            {
                _Path = value;
                mParts |= fURIParts.path;
            }
        }

        public string Query
        {
            get => _Query;

            set
            {
                _Query = value;
                mParts |= fURIParts.query;
            }
        }

        public string Fragment
        {
            get => _Fragment;

            set
            {
                _Fragment = value;
                mParts |= fURIParts.fragment;
            }
        }

        public bool Equals(cURIParts pObject) => this == pObject;

        public override bool Equals(object pObject) => this == pObject as cURIParts;

        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + mParts.GetHashCode();
                if (_Scheme != null) lHash = lHash * 23 + _Scheme.GetHashCode();
                if (_UserInfo != null) lHash = lHash * 23 + _UserInfo.GetHashCode();
                if (_Host != null) lHash = lHash * 23 + _Host.GetHashCode();
                if (_Port != null) lHash = lHash * 23 + _Port.GetHashCode();
                if (_Path != null) lHash = lHash * 23 + _Path.GetHashCode();
                if (_Query != null) lHash = lHash * 23 + _Query.GetHashCode();
                if (_Fragment != null) lHash = lHash * 23 + _Fragment.GetHashCode();
                return lHash;
            }
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(cURIParts));

            lBuilder.Append(mParts);
            if (_Scheme != null) lBuilder.Append(nameof(Scheme), _Scheme);
            if (_UserInfo != null) lBuilder.Append(nameof(UserInfo), _UserInfo);
            if (_Host != null) lBuilder.Append(nameof(Host), _Host);
            if (_Port != null) lBuilder.Append(nameof(Port), _Port);
            if (_Path != null) lBuilder.Append(nameof(Path), _Path);
            if (_Query != null) lBuilder.Append(nameof(Query), _Query);
            if (_Fragment != null) lBuilder.Append(nameof(Fragment), _Fragment);

            return lBuilder.ToString();
        }


        public static bool operator ==(cURIParts pA, cURIParts pB)
        {
            return
                pA.mParts == pB.mParts &&
                pA._Scheme == pB._Scheme &&
                pA._UserInfo == pB._UserInfo &&
                pA._Host == pB._Host &&
                pA._Port == pB._Port &&
                pA._Path == pB._Path &&
                pA._Query == pB._Query &&
                pA._Fragment == pB._Fragment;
        }

        public static bool operator !=(cURIParts pA, cURIParts pB) => !(pA == pB);

        public bool HasParts(fURIParts pMustHave, fURIParts pMayHave = 0)
        {
            if ((mParts & pMustHave) != pMustHave) return false;
            if ((mParts & ~(pMustHave | pMayHave)) != 0) return false;
            return true;
        }
    }
}