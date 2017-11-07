using System;
using System.Linq;

namespace work.bacome.imapclient
{
    /// <summary>
    /// <para>A set of server capabilities.</para>
    /// <para>The elements of this enum are all the capabilities that the library understands in some way.</para>
    /// <para>The full list of server capabilities can be found in a <see cref="cCapabilities"/> instance.</para>
    /// </summary>
    [Flags]
    public enum fCapabilities
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
        condstore = 1 << 22,
        qresync = 1 << 23

        /* deimplemented pending a requirement to complete it
        threadorderedsubject = 1 << 22,
        threadreferences = 1 << 23, */
    }

    /// <summary>
    /// A set of server capabilities.
    /// </summary>
    public class cCapabilities
    {
        /// <summary>
        /// The capabilities as presented by the server.
        /// </summary>
        public readonly cStrings Capabilities;

        /// <summary>
        /// The authentication mechanisms supported by the server.
        /// </summary>
        public readonly cStrings AuthenticationMechanisms;

        /// <summary>
        /// <para>The set of server capabilities that the instance is assuming.</para>
        /// <para>This is the set of recognised capabilities less the set that the instance has been told to ignore (see <see cref="cIMAPClient.IgnoreCapabilities"/>).</para>
        /// </summary>
        public readonly fCapabilities EffectiveCapabilities;

        public cCapabilities(cStrings pCapabilities, cStrings pAuthenticationMechanisms, fCapabilities pIgnoreCapabilities)
        {
            Capabilities = pCapabilities ?? throw new ArgumentNullException(nameof(pCapabilities));
            AuthenticationMechanisms = pAuthenticationMechanisms ?? throw new ArgumentNullException(nameof(pAuthenticationMechanisms));

            fCapabilities lCapabilities = 0;

            if (pCapabilities.Contains("LoginDisabled", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.logindisabled;
            if (pCapabilities.Contains("StartTLS", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.starttls;
            if (pCapabilities.Contains("Idle", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.idle;
            if (pCapabilities.Contains("Literal+", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.literalplus;
            if (pCapabilities.Contains("Literal-", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.literalminus;
            if (pCapabilities.Contains("Enable", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.enable;
            if (pCapabilities.Contains("UTF8=Accept", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.utf8accept;
            if (pCapabilities.Contains("UTF8=Only", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.utf8only;
            if (pCapabilities.Contains("List-Extended", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.listextended;
            if (pCapabilities.Contains("Children", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.children;
            if (pCapabilities.Contains("SASL-IR", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.sasl_ir;
            if (pCapabilities.Contains("Login-Referrals", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.loginreferrals;
            if (pCapabilities.Contains("Mailbox-Referrals", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.mailboxreferrals;
            if (pCapabilities.Contains("Id", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.id;
            if (pCapabilities.Contains("Binary", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.binary;
            if (pCapabilities.Contains("Namespace", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.namespaces;
            if (pCapabilities.Contains("List-Status", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.liststatus;
            if (pCapabilities.Contains("Special-Use", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.specialuse;
            if (pCapabilities.Contains("ESearch", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.esearch;
            if (pCapabilities.Contains("Sort", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.sort;
            if (pCapabilities.Contains("Sort=Display", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.sortdisplay;
            if (pCapabilities.Contains("ESort", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.esort;
            //if (pCapabilities.Contains("Thread=OrderedSubject", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.threadorderedsubject;
            //if (pCapabilities.Contains("Thread=References", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.threadreferences;
            if (pCapabilities.Contains("CondStore", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.condstore;
            if (pCapabilities.Contains("QResync", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.qresync | fCapabilities.condstore;

            EffectiveCapabilities = lCapabilities & ~pIgnoreCapabilities;
        }

        public bool LoginDisabled => (EffectiveCapabilities & fCapabilities.logindisabled) != 0;
        public bool StartTLS => (EffectiveCapabilities & fCapabilities.starttls) != 0;
        public bool Idle => (EffectiveCapabilities & fCapabilities.idle) != 0;
        public bool LiteralPlus => (EffectiveCapabilities & fCapabilities.literalplus) != 0;
        public bool LiteralMinus => (EffectiveCapabilities & fCapabilities.literalminus) != 0;
        public bool Enable => (EffectiveCapabilities & fCapabilities.enable) != 0;
        public bool UTF8Accept => (EffectiveCapabilities & fCapabilities.utf8accept) != 0;
        public bool UTF8Only => (EffectiveCapabilities & fCapabilities.utf8only) != 0;
        public bool ListExtended => (EffectiveCapabilities & fCapabilities.listextended) != 0;
        public bool Children => (EffectiveCapabilities & fCapabilities.children) != 0;
        public bool SASL_IR => (EffectiveCapabilities & fCapabilities.sasl_ir) != 0;
        public bool LoginReferrals => (EffectiveCapabilities & fCapabilities.loginreferrals) != 0;
        public bool MailboxReferrals => (EffectiveCapabilities & fCapabilities.mailboxreferrals) != 0;
        public bool Id => (EffectiveCapabilities & fCapabilities.id) != 0;
        public bool Binary => (EffectiveCapabilities & fCapabilities.binary) != 0;
        public bool Namespace => (EffectiveCapabilities & fCapabilities.namespaces) != 0;
        public bool ListStatus => (EffectiveCapabilities & fCapabilities.liststatus) != 0;
        public bool SpecialUse => (EffectiveCapabilities & fCapabilities.specialuse) != 0;
        public bool ESearch => (EffectiveCapabilities & fCapabilities.esearch) != 0;
        public bool Sort => (EffectiveCapabilities & fCapabilities.sort) != 0;
        public bool SortDisplay => (EffectiveCapabilities & fCapabilities.sortdisplay) != 0;
        public bool ESort => (EffectiveCapabilities & fCapabilities.esort) != 0;
        //public bool ThreadOrderedSubject => (EffectiveCapabilities & fCapabilities.threadorderedsubject) != 0;
        //public bool ThreadReferences => (EffectiveCapabilities & fCapabilities.threadreferences) != 0;
        public bool CondStore => (EffectiveCapabilities & fCapabilities.condstore) != 0;
        public bool QResync => (EffectiveCapabilities & fCapabilities.qresync) != 0;

        public override string ToString() => $"{nameof(cCapabilities)}({Capabilities},{AuthenticationMechanisms},{EffectiveCapabilities})";
    }
}