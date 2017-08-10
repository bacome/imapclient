using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    [Flags]
    public enum fKnownCapabilities
    {
        logindisabled = 1 << 0,
        starttls = 1 << 1,
        idle = 1 << 2,
        literalplus = 1 << 3,
        literalminus = 1 << 4,
        enable = 1 << 5,
        utf8accept = 1 << 6,
        utf8only = 1 << 7,
        listextended = 1 << 8,
        children = 1 << 9,
        sasl_ir = 1 << 10,
        loginreferrals = 1 << 11,
        mailboxreferrals = 1 << 12,
        id = 1 << 13,
        binary = 1 << 14,
        namespaces = 1 << 15,
        liststatus = 1 << 16,
        specialuse = 1 << 17,
        esearch = 1 << 18,
        sort = 1 << 19,
        sortdisplay = 1 << 20,
        esort = 1 << 21,
        threadorderedsubject = 1 << 22,
        threadreferences = 1 << 23,
        condstore = 1 << 24,
        qresync = 1 << 25
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

            if (pCapabilities.Contains("LoginDisabled")) KnownCapabilities |= fKnownCapabilities.logindisabled;
            if (pCapabilities.Contains("StartTLS")) KnownCapabilities |= fKnownCapabilities.starttls;
            if (pCapabilities.Contains("Idle")) KnownCapabilities |= fKnownCapabilities.idle;
            if (pCapabilities.Contains("Literal+")) KnownCapabilities |= fKnownCapabilities.literalplus;
            if (pCapabilities.Contains("Literal-")) KnownCapabilities |= fKnownCapabilities.literalminus;
            if (pCapabilities.Contains("Enable")) KnownCapabilities |= fKnownCapabilities.enable;
            if (pCapabilities.Contains("UTF8=Accept")) KnownCapabilities |= fKnownCapabilities.utf8accept;
            if (pCapabilities.Contains("UTF8=Only")) KnownCapabilities |= fKnownCapabilities.utf8only;
            if (pCapabilities.Contains("List-Extended")) KnownCapabilities |= fKnownCapabilities.listextended;
            if (pCapabilities.Contains("Children")) KnownCapabilities |= fKnownCapabilities.children;
            if (pCapabilities.Contains("SASL-IR")) KnownCapabilities |= fKnownCapabilities.sasl_ir;
            if (pCapabilities.Contains("Login-Referrals")) KnownCapabilities |= fKnownCapabilities.loginreferrals;
            if (pCapabilities.Contains("Mailbox-Referrals")) KnownCapabilities |= fKnownCapabilities.mailboxreferrals;
            if (pCapabilities.Contains("Id")) KnownCapabilities |= fKnownCapabilities.id;
            if (pCapabilities.Contains("Binary")) KnownCapabilities |= fKnownCapabilities.binary;
            if (pCapabilities.Contains("Namespace")) KnownCapabilities |= fKnownCapabilities.namespaces;
            if (pCapabilities.Contains("List-Status")) KnownCapabilities |= fKnownCapabilities.liststatus;
            if (pCapabilities.Contains("Special-Use")) KnownCapabilities |= fKnownCapabilities.specialuse;
            if (pCapabilities.Contains("ESearch")) KnownCapabilities |= fKnownCapabilities.esearch;
            if (pCapabilities.Contains("Sort")) KnownCapabilities |= fKnownCapabilities.sort;
            if (pCapabilities.Contains("Sort=Display")) KnownCapabilities |= fKnownCapabilities.sortdisplay;
            if (pCapabilities.Contains("ESort")) KnownCapabilities |= fKnownCapabilities.esort;
            if (pCapabilities.Contains("Thread=OrderedSubject")) KnownCapabilities |= fKnownCapabilities.threadorderedsubject;
            if (pCapabilities.Contains("Thread=References")) KnownCapabilities |= fKnownCapabilities.threadreferences;
            if (pCapabilities.Contains("CondStore")) KnownCapabilities |= fKnownCapabilities.condstore;
            if (pCapabilities.Contains("QResync")) KnownCapabilities |= fKnownCapabilities.qresync | fKnownCapabilities.condstore;

            mCapabilities = pCapabilities;
            mAuthenticationMechanisms = pAuthenticationMechanisms;

            IgnoreCapabilities = pIgnoreCapabilities;

            EffectiveCapabilities = KnownCapabilities & ~pIgnoreCapabilities;
        }

        public ICollection<string> Capabilities => mCapabilities.AsReadOnly();
        public ICollection<string> AuthenticationMechanisms => mAuthenticationMechanisms.AsReadOnly();

        public bool LoginDisabled => (EffectiveCapabilities & fKnownCapabilities.logindisabled) != 0;
        public bool StartTLS => (EffectiveCapabilities & fKnownCapabilities.starttls) != 0;
        public bool Idle => (EffectiveCapabilities & fKnownCapabilities.idle) != 0;
        public bool LiteralPlus => (EffectiveCapabilities & fKnownCapabilities.literalplus) != 0;
        public bool LiteralMinus => (EffectiveCapabilities & fKnownCapabilities.literalminus) != 0;
        public bool Enable => (EffectiveCapabilities & fKnownCapabilities.enable) != 0;
        public bool UTF8Accept => (EffectiveCapabilities & fKnownCapabilities.utf8accept) != 0;
        public bool UTF8Only => (EffectiveCapabilities & fKnownCapabilities.utf8only) != 0;
        public bool ListExtended => (EffectiveCapabilities & fKnownCapabilities.listextended) != 0;
        public bool Children => (EffectiveCapabilities & fKnownCapabilities.children) != 0;
        public bool SASL_IR => (EffectiveCapabilities & fKnownCapabilities.sasl_ir) != 0;
        public bool LoginReferrals => (EffectiveCapabilities & fKnownCapabilities.loginreferrals) != 0;
        public bool MailboxReferrals => (EffectiveCapabilities & fKnownCapabilities.mailboxreferrals) != 0;
        public bool Id => (EffectiveCapabilities & fKnownCapabilities.id) != 0;
        public bool Binary => (EffectiveCapabilities & fKnownCapabilities.binary) != 0;
        public bool Namespace => (EffectiveCapabilities & fKnownCapabilities.namespaces) != 0;
        public bool ListStatus => (EffectiveCapabilities & fKnownCapabilities.liststatus) != 0;
        public bool SpecialUse => (EffectiveCapabilities & fKnownCapabilities.specialuse) != 0;
        public bool ESearch => (EffectiveCapabilities & fKnownCapabilities.esearch) != 0;
        public bool Sort => (EffectiveCapabilities & fKnownCapabilities.sort) != 0;
        public bool SortDisplay => (EffectiveCapabilities & fKnownCapabilities.sortdisplay) != 0;
        public bool ESort => (EffectiveCapabilities & fKnownCapabilities.esort) != 0;
        public bool ThreadOrderedSubject => (EffectiveCapabilities & fKnownCapabilities.threadorderedsubject) != 0;
        public bool ThreadReferences => (EffectiveCapabilities & fKnownCapabilities.threadreferences) != 0;
        public bool CondStore => (EffectiveCapabilities & fKnownCapabilities.condstore) != 0;
        public bool QResync => (EffectiveCapabilities & fKnownCapabilities.qresync) != 0;

        public override string ToString() => $"{nameof(cCapabilities)}([{KnownCapabilities}],{mCapabilities},{mAuthenticationMechanisms},[{IgnoreCapabilities}],[{EffectiveCapabilities}])";
    }
}