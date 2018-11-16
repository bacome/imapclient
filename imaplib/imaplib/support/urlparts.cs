using System;
using work.bacome.imapclient;

namespace work.bacome.imapsupport
{
    [Flags]
    public enum fURLParts
    {
        scheme = 1 << 0,
        userid = 1 << 1,
        mechanismname = 1 << 2,
        host = 1 << 3,
        port = 1 << 4,
        mailboxname = 1 << 5,
        uidvalidity = 1 << 6,
        search = 1 << 7,
        uid = 1 << 8,
        section = 1 << 9,
        partial = 1 << 10,
        partiallength = 1 << 11,
        expire = 1 << 12,
        urlauth = 1 << 13,
        accessuserid = 1 << 14,
        token = 1 << 15
    }

    public struct sURLParts : IEquatable<sURLParts>
    {
        private fURLParts mParts;

        private string _UserId;
        private string _MechanismName;
        private string _Host;
        private string _DisplayHost;
        private int _Port;
        private string _MailboxPath;
        private uint? _UIDValidity;
        private string _Search;
        private uint? _UID;
        private string _Section;
        private uint? _PartialOffset;
        private uint? _PartialLength;
        private cTimestamp _Expire;
        private string _Application;
        private string _AccessUserId;
        private string _Token;

        public sURLParts(bool pDefaultPort)
        {
            mParts = 0;
            _UserId = null;
            _MechanismName = null;
            _Host = null;
            _DisplayHost = null;
            _Port = 143;
            _MailboxPath = null;
            _UIDValidity = null;
            _Search = null;
            _UID = null;
            _Section = null;
            _PartialOffset = null;
            _PartialLength = null;
            _Expire = null;
            _Application = null;
            _AccessUserId = null;
            TokenMechanism = null;
            _Token = null;
        }

        public void SetHasScheme() => mParts |= fURLParts.scheme;

        public string UserId
        {
            get => _UserId;

            set
            {
                _UserId = value;
                mParts |= fURLParts.userid;
            }
        }

        public string MechanismName
        {
            get => _MechanismName;

            set
            {
                _MechanismName = value;
                mParts |= fURLParts.mechanismname;
            }
        }

        public string Host
        {
            get => _Host;

            set
            {
                _Host = value;
                _DisplayHost = cTools.GetDisplayHost(value);
                mParts |= fURLParts.host;
            }
        }

        public string DisplayHost => _DisplayHost;

        public int Port
        {
            get => _Port;

            set
            {
                _Port = value;
                mParts |= fURLParts.port;
            }
        }

        public string MailboxPath
        {
            get => _MailboxPath;

            set
            {
                _MailboxPath = value;
                mParts |= fURLParts.mailboxname;
            }
        }

        public uint? UIDValidity
        {
            get => _UIDValidity;

            set
            {
                _UIDValidity = value;
                mParts |= fURLParts.uidvalidity;
            }
        }

        public string Search
        {
            get => _Search;

            set
            {
                _Search = value;
                mParts |= fURLParts.search;
            }
        }

        public uint? UID
        {
            get => _UID;

            set
            {
                _UID = value;
                mParts |= fURLParts.uid;
            }
        }

        public string Section
        {
            get => _Section;

            set
            {
                _Section = value;
                mParts |= fURLParts.section;
            }
        }

        public uint? PartialOffset
        {
            get => _PartialOffset;

            set
            {
                _PartialOffset = value;
                mParts |= fURLParts.partial;
            }
        }

        public uint? PartialLength
        {
            get => _PartialLength;

            set
            {
                _PartialLength = value;
                mParts |= fURLParts.partiallength;
            }
        }

        public cTimestamp Expire
        {
            get => _Expire;

            set
            {
                _Expire = value;
                mParts |= fURLParts.expire;
            }
        }

        public string Application
        {
            get => _Application;

            set
            {
                _Application = value;
                mParts |= fURLParts.urlauth;
            }
        }

        public string AccessUserId
        {
            get => _AccessUserId;

            set
            {
                _AccessUserId = value;
                mParts |= fURLParts.accessuserid;
            }
        }

        public string TokenMechanism { get; set; }

        public string Token
        {
            get => _Token;

            set
            {
                _Token = value;
                mParts |= fURLParts.token;
            }
        }

        public bool MustUseAnonymous => ((mParts & fURLParts.host) != 0) && (mParts & (fURLParts.userid | fURLParts.mechanismname)) == 0;

        public bool IsHomeServerReferral =>
            HasParts(
                fURLParts.scheme | fURLParts.host,
                fURLParts.userid | fURLParts.mechanismname | fURLParts.port
                ) &&
            !MustUseAnonymous;

        public bool IsMailboxReferral =>
            HasParts(
                fURLParts.scheme | fURLParts.host | fURLParts.mailboxname,
                fURLParts.userid | fURLParts.mechanismname | fURLParts.port | fURLParts.uidvalidity
                );

        public bool IsMailboxSearch =>
            HasParts(
                fURLParts.scheme | fURLParts.host | fURLParts.mailboxname | fURLParts.search,
                fURLParts.userid | fURLParts.mechanismname | fURLParts.port | fURLParts.uidvalidity
                );

        public bool IsMessageReference =>
            HasParts(
                fURLParts.scheme | fURLParts.host | fURLParts.mailboxname | fURLParts.uid,
                fURLParts.userid | fURLParts.mechanismname | fURLParts.port | fURLParts.uidvalidity | fURLParts.section | fURLParts.partial | fURLParts.partiallength
                );

        public bool IsPartial => !((mParts & (fURLParts.section | fURLParts.partial)) != 0);

        public bool IsAuthorisable =>
            HasParts(
                fURLParts.scheme | fURLParts.userid | fURLParts.host | fURLParts.mailboxname | fURLParts.uid | fURLParts.urlauth,
                fURLParts.mechanismname | fURLParts.port | fURLParts.uidvalidity | fURLParts.section | fURLParts.partial | fURLParts.partiallength | fURLParts.expire | fURLParts.accessuserid
                );

        public bool IsAuthorised =>
            HasParts(
                fURLParts.scheme | fURLParts.userid | fURLParts.host | fURLParts.mailboxname | fURLParts.uid | fURLParts.urlauth | fURLParts.token,
                fURLParts.mechanismname | fURLParts.port | fURLParts.uidvalidity | fURLParts.section | fURLParts.partial | fURLParts.partiallength | fURLParts.expire | fURLParts.accessuserid
                );

        public bool HasParts(fURLParts pMustHave, fURLParts pMayHave = 0)
        {
            if ((mParts & pMustHave) != pMustHave) return false;
            if ((mParts & ~(pMustHave | pMayHave)) != 0) return false;
            return true;
        }

        public bool Equals(sURLParts pObject) => this == pObject;

        public override bool Equals(object pObject) => pObject is sURLParts && this == (sURLParts)pObject;

        public override int GetHashCode()
        {
            unchecked
            {
                int lHash = 17;
                lHash = lHash * 23 + mParts.GetHashCode();
                if (_UserId != null) lHash = lHash * 23 + _UserId.GetHashCode();
                if (_MechanismName != null) lHash = lHash * 23 + _MechanismName.GetHashCode();
                if (_Host != null) lHash = lHash * 23 + _Host.GetHashCode();
                lHash = lHash * 23 + _Port.GetHashCode();
                if (_MailboxPath != null) lHash = lHash * 23 + _MailboxPath.GetHashCode();
                if (_UIDValidity != null) lHash = lHash * 23 + _UIDValidity.GetHashCode();
                if (_Search != null) lHash = lHash * 23 + _Search.GetHashCode();
                if (_UID != null) lHash = lHash * 23 + _UID.GetHashCode();
                if (_Section != null) lHash = lHash * 23 + _Section.GetHashCode();
                if (_PartialOffset != null) lHash = lHash * 23 + _PartialOffset.GetHashCode();
                if (_PartialLength != null) lHash = lHash * 23 + _PartialLength.GetHashCode();
                if (_Expire != null) lHash = lHash * 23 + _Expire.GetHashCode();
                if (_Application != null) lHash = lHash * 23 + _Application.GetHashCode();
                if (_AccessUserId != null) lHash = lHash * 23 + _AccessUserId.GetHashCode();
                if (TokenMechanism != null) lHash = lHash * 23 + TokenMechanism.GetHashCode();
                if (_Token != null) lHash = lHash * 23 + _Token.GetHashCode();
                return lHash;
            }
        }

        public override string ToString()
        {
            var lBuilder = new cListBuilder(nameof(sURLParts));

            lBuilder.Append(mParts);
            if (_UserId != null) lBuilder.Append(nameof(UserId), _UserId);
            if (_MechanismName != null) lBuilder.Append(nameof(MechanismName), _MechanismName);
            if (_Host != null) lBuilder.Append(nameof(Host), _Host);
            lBuilder.Append(nameof(Port), _Port);
            if (_MailboxPath != null) lBuilder.Append(nameof(MailboxPath), _MailboxPath);
            if (_UIDValidity != null) lBuilder.Append(nameof(UIDValidity), _UIDValidity);
            if (_Search != null) lBuilder.Append(nameof(Search), _Search);
            if (_UID != null) lBuilder.Append(nameof(UID), _UID);
            if (_Section != null) lBuilder.Append(nameof(Section), _Section);
            if (_PartialOffset != null) lBuilder.Append(nameof(PartialOffset), _PartialOffset);
            if (_PartialLength != null) lBuilder.Append(nameof(PartialLength), _PartialLength);
            if (_Expire != null) lBuilder.Append(nameof(Expire), _Expire);
            if (_Application != null) lBuilder.Append(nameof(Application), _Application);
            if (_AccessUserId != null) lBuilder.Append(nameof(AccessUserId), _AccessUserId);
            if (TokenMechanism != null) lBuilder.Append(nameof(TokenMechanism), TokenMechanism);
            if (_Token != null) lBuilder.Append(nameof(Token), _Token);

            return lBuilder.ToString();
        }

        public static bool operator ==(sURLParts pA, sURLParts pB)
        {
            if (ReferenceEquals(pA, pB)) return true;
            if (ReferenceEquals(pA, null)) return false;
            if (ReferenceEquals(pB, null)) return false;

            return
                pA.mParts == pB.mParts &&
                pA._UserId == pB._UserId &&
                pA._MechanismName == pB._MechanismName &&
                pA._Host == pB._Host &&
                pA._Port == pB._Port &&
                pA._MailboxPath == pB._MailboxPath &&
                pA._UIDValidity == pB._UIDValidity &&
                pA._Search == pB._Search &&
                pA._UID == pB._UID &&
                pA._Section == pB._Section &&
                pA._PartialOffset == pB._PartialOffset &&
                pA._PartialLength == pB._PartialLength &&
                pA._Expire == pB._Expire &&
                pA._Application == pB._Application &&
                pA._AccessUserId == pB._AccessUserId &&
                pA.TokenMechanism == pB.TokenMechanism &&
                pA._Token == pB._Token;
        }

        public static bool operator !=(sURLParts pA, sURLParts pB) => !(pA == pB);
    }
}