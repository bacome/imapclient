using System;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fCapabilities
    {
        LoginDisabled = 1,
        Idle = 1 << 1,
        LiteralPlus = 1 << 2,
        LiteralMinus = 1 << 3,
        Enable = 1 << 4,
        UTF8Accept = 1 << 5,
        UTF8Only = 1 << 6,
        ListExtended = 1 << 7,
        Children = 1 << 8,
        SASL_IR = 1 << 9,
        LoginReferrals = 1 << 10,
        MailboxReferrals = 1 << 11,
        Id = 1 << 12,
        Binary = 1 << 13,
        Namespace = 1 << 14,
        ListStatus = 1 << 15,
        SpecialUse = 1 << 16,
        ESearch = 1 << 17,
        Sort = 1 << 18,
        SortDisplay = 1 << 19,
        ESort = 1 << 20,
        ThreadOrderedSubject = 1 << 21,
        ThreadReferences = 1 << 22,
        ThreadRefs = 1 << 23,
        CondStore = 1 << 24,
        QResync = 1 << 25
    }

    public class cCapability
    {
        public readonly fCapabilities Capabilities;
        public readonly cStrings ServerCapabilities;
        public readonly cStrings AuthenticationMechanisms;
        public readonly fCapabilities IgnoreCapabilities;
        public readonly fCapabilities EffectiveCapabilities;

        public cCapability(cCapabilities pServerCapabilities, cCapabilities pAuthenticationMechanisms, fCapabilities pIgnoreCapabilities)
        {
            if ((pIgnoreCapabilities & fCapabilities.LoginDisabled) != 0) throw new ArgumentOutOfRangeException(nameof(pIgnoreCapabilities), "cannot ignore login disabled");

            Capabilities = 0;

            if (pServerCapabilities.Has("LoginDisabled")) Capabilities |= fCapabilities.LoginDisabled;
            if (pServerCapabilities.Has("Idle")) Capabilities |= fCapabilities.Idle;
            if (pServerCapabilities.Has("Literal+")) Capabilities |= fCapabilities.LiteralPlus;
            if (pServerCapabilities.Has("Literal-")) Capabilities |= fCapabilities.LiteralMinus;
            if (pServerCapabilities.Has("Enable")) Capabilities |= fCapabilities.Enable;
            if (pServerCapabilities.Has("UTF8=Accept")) Capabilities |= fCapabilities.UTF8Accept;
            if (pServerCapabilities.Has("UTF8=Only")) Capabilities |= fCapabilities.UTF8Only;
            if (pServerCapabilities.Has("List-Extended")) Capabilities |= fCapabilities.ListExtended;
            if (pServerCapabilities.Has("Children")) Capabilities |= fCapabilities.Children;
            if (pServerCapabilities.Has("SASL-IR")) Capabilities |= fCapabilities.SASL_IR;
            if (pServerCapabilities.Has("Login-Referrals")) Capabilities |= fCapabilities.LoginReferrals;
            if (pServerCapabilities.Has("Mailbox-Referrals")) Capabilities |= fCapabilities.MailboxReferrals;
            if (pServerCapabilities.Has("Id")) Capabilities |= fCapabilities.Id;
            if (pServerCapabilities.Has("Binary")) Capabilities |= fCapabilities.Binary;
            if (pServerCapabilities.Has("Namespace")) Capabilities |= fCapabilities.Namespace;
            if (pServerCapabilities.Has("List-Status")) Capabilities |= fCapabilities.ListStatus;
            if (pServerCapabilities.Has("Special-Use")) Capabilities |= fCapabilities.SpecialUse;
            if (pServerCapabilities.Has("ESearch")) Capabilities |= fCapabilities.ESearch;
            if (pServerCapabilities.Has("Sort")) Capabilities |= fCapabilities.Sort;
            if (pServerCapabilities.Has("Sort=Display")) Capabilities |= fCapabilities.SortDisplay;
            if (pServerCapabilities.Has("ESort")) Capabilities |= fCapabilities.ESort;
            if (pServerCapabilities.Has("Thread=OrderedSubject")) Capabilities |= fCapabilities.ThreadOrderedSubject;
            if (pServerCapabilities.Has("Thread=References")) Capabilities |= fCapabilities.ThreadReferences;
            if (pServerCapabilities.Has("Thread=Refs")) Capabilities |= fCapabilities.ThreadRefs;
            if (pServerCapabilities.Has("CondStore")) Capabilities |= fCapabilities.CondStore;
            if (pServerCapabilities.Has("QResync")) Capabilities |= fCapabilities.QResync | fCapabilities.CondStore;

            ServerCapabilities = new cStrings(pServerCapabilities.AsUpperList());
            AuthenticationMechanisms = new cStrings(pAuthenticationMechanisms.AsUpperList());
            IgnoreCapabilities = pIgnoreCapabilities;

            EffectiveCapabilities = Capabilities & ~IgnoreCapabilities;
        }

        public bool LoginDisabled => (EffectiveCapabilities & fCapabilities.LoginDisabled) != 0;
        public bool Idle => (EffectiveCapabilities & fCapabilities.Idle) != 0;
        public bool LiteralPlus => (EffectiveCapabilities & fCapabilities.LiteralPlus) != 0;
        public bool LiteralMinus => (EffectiveCapabilities & fCapabilities.LiteralMinus) != 0;
        public bool Enable => (EffectiveCapabilities & fCapabilities.Enable) != 0;
        public bool UTF8Accept => (EffectiveCapabilities & fCapabilities.UTF8Accept) != 0;
        public bool UTF8Only => (EffectiveCapabilities & fCapabilities.UTF8Only) != 0;
        public bool ListExtended => (EffectiveCapabilities & fCapabilities.ListExtended) != 0;
        public bool Children => (EffectiveCapabilities & fCapabilities.Children) != 0;
        public bool SASL_IR => (EffectiveCapabilities & fCapabilities.SASL_IR) != 0;
        public bool LoginReferrals => (EffectiveCapabilities & fCapabilities.LoginReferrals) != 0;
        public bool MailboxReferrals => (EffectiveCapabilities & fCapabilities.MailboxReferrals) != 0;
        public bool Id => (EffectiveCapabilities & fCapabilities.Id) != 0;
        public bool Binary => (EffectiveCapabilities & fCapabilities.Binary) != 0;
        public bool Namespace => (EffectiveCapabilities & fCapabilities.Namespace) != 0;
        public bool ListStatus => (EffectiveCapabilities & fCapabilities.ListStatus) != 0;
        public bool SpecialUse => (EffectiveCapabilities & fCapabilities.SpecialUse) != 0;
        public bool ESearch => (EffectiveCapabilities & fCapabilities.ESearch) != 0;
        public bool Sort => (EffectiveCapabilities & fCapabilities.Sort) != 0;
        public bool SortDisplay => (EffectiveCapabilities & fCapabilities.SortDisplay) != 0;
        public bool ESort => (EffectiveCapabilities & fCapabilities.ESort) != 0;
        public bool ThreadOrderedSubject => (EffectiveCapabilities & fCapabilities.ThreadOrderedSubject) != 0;
        public bool ThreadReferences => (EffectiveCapabilities & fCapabilities.ThreadReferences) != 0;
        public bool ThreadRefs => (EffectiveCapabilities & fCapabilities.ThreadRefs) != 0;
        public bool CondStore => (EffectiveCapabilities & fCapabilities.CondStore) != 0;
        public bool QResync => (EffectiveCapabilities & fCapabilities.QResync) != 0;

        public bool SupportsAuthenticationMechanism(string pMechanismName)
        {
            foreach (string lMechanismName in AuthenticationMechanisms) if (lMechanismName.Equals(pMechanismName, StringComparison.InvariantCultureIgnoreCase)) return true;
            return false;
        }

        public override string ToString()
        {
            return $"{nameof(cCapability)}([{Capabilities}],{ServerCapabilities},{AuthenticationMechanisms},[{IgnoreCapabilities}],[{EffectiveCapabilities}])";
        }
    }
}