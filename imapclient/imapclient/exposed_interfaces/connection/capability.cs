using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fKnownCapabilities
    {
        LoginDisabled = 1 << 0,
        StartTLS = 1 << 1,
        Idle = 1 << 2,
        LiteralPlus = 1 << 3,
        LiteralMinus = 1 << 4,
        Enable = 1 << 5,
        UTF8Accept = 1 << 6,
        UTF8Only = 1 << 7,
        ListExtended = 1 << 8,
        Children = 1 << 9,
        SASL_IR = 1 << 10,
        LoginReferrals = 1 << 11,
        MailboxReferrals = 1 << 12,
        Id = 1 << 13,
        Binary = 1 << 14,
        Namespace = 1 << 15,
        ListStatus = 1 << 16,
        SpecialUse = 1 << 17,
        ESearch = 1 << 18,
        Sort = 1 << 19,
        SortDisplay = 1 << 20,
        ESort = 1 << 21,
        ThreadOrderedSubject = 1 << 22,
        ThreadReferences = 1 << 23,
        ThreadRefs = 1 << 24,
        CondStore = 1 << 25,
        QResync = 1 << 26
    }

    public class cCapabilities
    {
        public readonly fKnownCapabilities KnownCapabilities;
        private readonly cUniqueIgnoreCaseStringList mCapabilities;
        private readonly cUniqueIgnoreCaseStringList mAuthenticationMechanisms;
        public readonly fKnownCapabilities IgnoreCapabilities;
        public readonly fKnownCapabilities EffectiveCapabilities;

        public cCapabilities(cUniqueIgnoreCaseStringList pCapabilities, cUniqueIgnoreCaseStringList pAuthenticationMechanisms, fKnownCapabilities pIgnoreCapabilities)
        {
            if (pCapabilities == null) throw new ArgumentNullException(nameof(pCapabilities));
            if (pAuthenticationMechanisms == null) throw new ArgumentNullException(nameof(pAuthenticationMechanisms));

            KnownCapabilities = 0;

            if (pCapabilities.Contains("LoginDisabled")) KnownCapabilities |= fKnownCapabilities.LoginDisabled;
            if (pCapabilities.Contains("StartTLS")) KnownCapabilities |= fKnownCapabilities.StartTLS;
            if (pCapabilities.Contains("Idle")) KnownCapabilities |= fKnownCapabilities.Idle;
            if (pCapabilities.Contains("Literal+")) KnownCapabilities |= fKnownCapabilities.LiteralPlus;
            if (pCapabilities.Contains("Literal-")) KnownCapabilities |= fKnownCapabilities.LiteralMinus;
            if (pCapabilities.Contains("Enable")) KnownCapabilities |= fKnownCapabilities.Enable;
            if (pCapabilities.Contains("UTF8=Accept")) KnownCapabilities |= fKnownCapabilities.UTF8Accept;
            if (pCapabilities.Contains("UTF8=Only")) KnownCapabilities |= fKnownCapabilities.UTF8Only;
            if (pCapabilities.Contains("List-Extended")) KnownCapabilities |= fKnownCapabilities.ListExtended;
            if (pCapabilities.Contains("Children")) KnownCapabilities |= fKnownCapabilities.Children;
            if (pCapabilities.Contains("SASL-IR")) KnownCapabilities |= fKnownCapabilities.SASL_IR;
            if (pCapabilities.Contains("Login-Referrals")) KnownCapabilities |= fKnownCapabilities.LoginReferrals;
            if (pCapabilities.Contains("Mailbox-Referrals")) KnownCapabilities |= fKnownCapabilities.MailboxReferrals;
            if (pCapabilities.Contains("Id")) KnownCapabilities |= fKnownCapabilities.Id;
            if (pCapabilities.Contains("Binary")) KnownCapabilities |= fKnownCapabilities.Binary;
            if (pCapabilities.Contains("Namespace")) KnownCapabilities |= fKnownCapabilities.Namespace;
            if (pCapabilities.Contains("List-Status")) KnownCapabilities |= fKnownCapabilities.ListStatus;
            if (pCapabilities.Contains("Special-Use")) KnownCapabilities |= fKnownCapabilities.SpecialUse;
            if (pCapabilities.Contains("ESearch")) KnownCapabilities |= fKnownCapabilities.ESearch;
            if (pCapabilities.Contains("Sort")) KnownCapabilities |= fKnownCapabilities.Sort;
            if (pCapabilities.Contains("Sort=Display")) KnownCapabilities |= fKnownCapabilities.SortDisplay;
            if (pCapabilities.Contains("ESort")) KnownCapabilities |= fKnownCapabilities.ESort;
            if (pCapabilities.Contains("Thread=OrderedSubject")) KnownCapabilities |= fKnownCapabilities.ThreadOrderedSubject;
            if (pCapabilities.Contains("Thread=References")) KnownCapabilities |= fKnownCapabilities.ThreadReferences;
            if (pCapabilities.Contains("Thread=Refs")) KnownCapabilities |= fKnownCapabilities.ThreadRefs;
            if (pCapabilities.Contains("CondStore")) KnownCapabilities |= fKnownCapabilities.CondStore;
            if (pCapabilities.Contains("QResync")) KnownCapabilities |= fKnownCapabilities.QResync | fKnownCapabilities.CondStore;

            mCapabilities = pCapabilities;
            mAuthenticationMechanisms = pAuthenticationMechanisms;

            IgnoreCapabilities = pIgnoreCapabilities;

            EffectiveCapabilities = KnownCapabilities & ~pIgnoreCapabilities;
        }

        public ICollection<string> Capabilities => mCapabilities.AsReadOnly();
        public ICollection<string> AuthenticationMechanisms => mAuthenticationMechanisms.AsReadOnly();

        public bool LoginDisabled => (EffectiveCapabilities & fKnownCapabilities.LoginDisabled) != 0;
        public bool StartTLS => (EffectiveCapabilities & fKnownCapabilities.StartTLS) != 0;
        public bool Idle => (EffectiveCapabilities & fKnownCapabilities.Idle) != 0;
        public bool LiteralPlus => (EffectiveCapabilities & fKnownCapabilities.LiteralPlus) != 0;
        public bool LiteralMinus => (EffectiveCapabilities & fKnownCapabilities.LiteralMinus) != 0;
        public bool Enable => (EffectiveCapabilities & fKnownCapabilities.Enable) != 0;
        public bool UTF8Accept => (EffectiveCapabilities & fKnownCapabilities.UTF8Accept) != 0;
        public bool UTF8Only => (EffectiveCapabilities & fKnownCapabilities.UTF8Only) != 0;
        public bool ListExtended => (EffectiveCapabilities & fKnownCapabilities.ListExtended) != 0;
        public bool Children => (EffectiveCapabilities & fKnownCapabilities.Children) != 0;
        public bool SASL_IR => (EffectiveCapabilities & fKnownCapabilities.SASL_IR) != 0;
        public bool LoginReferrals => (EffectiveCapabilities & fKnownCapabilities.LoginReferrals) != 0;
        public bool MailboxReferrals => (EffectiveCapabilities & fKnownCapabilities.MailboxReferrals) != 0;
        public bool Id => (EffectiveCapabilities & fKnownCapabilities.Id) != 0;
        public bool Binary => (EffectiveCapabilities & fKnownCapabilities.Binary) != 0;
        public bool Namespace => (EffectiveCapabilities & fKnownCapabilities.Namespace) != 0;
        public bool ListStatus => (EffectiveCapabilities & fKnownCapabilities.ListStatus) != 0;
        public bool SpecialUse => (EffectiveCapabilities & fKnownCapabilities.SpecialUse) != 0;
        public bool ESearch => (EffectiveCapabilities & fKnownCapabilities.ESearch) != 0;
        public bool Sort => (EffectiveCapabilities & fKnownCapabilities.Sort) != 0;
        public bool SortDisplay => (EffectiveCapabilities & fKnownCapabilities.SortDisplay) != 0;
        public bool ESort => (EffectiveCapabilities & fKnownCapabilities.ESort) != 0;
        public bool ThreadOrderedSubject => (EffectiveCapabilities & fKnownCapabilities.ThreadOrderedSubject) != 0;
        public bool ThreadReferences => (EffectiveCapabilities & fKnownCapabilities.ThreadReferences) != 0;
        public bool ThreadRefs => (EffectiveCapabilities & fKnownCapabilities.ThreadRefs) != 0;
        public bool CondStore => (EffectiveCapabilities & fKnownCapabilities.CondStore) != 0;
        public bool QResync => (EffectiveCapabilities & fKnownCapabilities.QResync) != 0;

        public override string ToString() => $"{nameof(cCapabilities)}([{KnownCapabilities}],{mCapabilities},{mAuthenticationMechanisms},[{IgnoreCapabilities}],[{EffectiveCapabilities}])";
    }
}