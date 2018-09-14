using System;
using System.Linq;
using work.bacome.mailclient;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a set of IMAP capabilities.
    /// </summary>
    [Flags]
    public enum fIMAPCapabilities
    {
        /**<summary>IMAP LOGINDISABLED</summary>*/
        logindisabled = 1 << 0,
        /**<summary>IMAP STARTTLS</summary>*/
        starttls = 1 << 1,
        /**<summary>RFC 2177 IDLE</summary>*/
        idle = 1 << 2,
        /**<summary>RFC 7888 LITERAL+</summary>*/
        literalplus = 1 << 3,
        /**<summary>RFC 7888 LITERAL-</summary>*/
        literalminus = 1 << 4,
        /**<summary>RFC 5161 ENABLE</summary>*/
        enable = 1 << 5,
        /**<summary>RFC 6855 UTF8=ACCEPT</summary>*/
        utf8accept = 1 << 6,
        /**<summary>RFC 6855 UTF8=ONLY</summary>*/
        utf8only = 1 << 7,
        /**<summary>RFC 5258 LIST extensions</summary>*/
        listextended = 1 << 8,
        /**<summary>RFC 3348 Child mailboxes</summary>*/
        children = 1 << 9,
        /**<summary>RFC 4959 SASL initial client response</summary>*/
        sasl_ir = 1 << 10,
        /**<summary>RFC 2221 Login referrals</summary>*/
        loginreferrals = 1 << 11,
        /**<summary>RFC 2193 Mailbox referrals</summary>*/
        mailboxreferrals = 1 << 12,
        /**<summary>RFC 2971 Id</summary>*/
        id = 1 << 13,
        /**<summary>RFC 3516 Binary content</summary>*/
        binary = 1 << 14,
        /**<summary>RFC 2342 Namespaces</summary>*/
        namespaces = 1 << 15,
        /**<summary>RFC 5819 STATUS information in LIST</summary>*/
        liststatus = 1 << 16,
        /**<summary>RFC 6154 Special use</summary>*/
        specialuse = 1 << 17,
        /**<summary>RFC 4731 ESEARCH</summary>*/
        esearch = 1 << 18,
        /**<summary>RFC 5256 SORT</summary>*/
        sort = 1 << 19,
        /**<summary>RFC 5256 SORT=DISPLAY</summary>*/
        sortdisplay = 1 << 20,
        /**<summary>RFC 5267 ESORT</summary>*/
        esort = 1 << 21,
        /**<summary>RFC 7162 CONDSTORE</summary>*/
        condstore = 1 << 22,
        /**<summary>RFC 7162 QRESYNC</summary>*/
        qresync = 1 << 23,
        /**<summary>RFC 3502 MULTIAPPEND</summary>*/
        multiappend = 1 << 24,
        /**<summary>RFC 4469 CATENATE</summary>*/
        catenate = 1 << 25

        /* deimplemented pending a requirement to complete it
        threadorderedsubject = 1 << 22,
        threadreferences = 1 << 23, */
    }

    /// <summary>
    /// An immutable collection of IMAP capabilities.
    /// </summary>
    /// <remarks>
    /// The properties of this class reflect the value of <see cref="EffectiveCapabilities"/>.
    /// </remarks>
    /// <seealso cref="cIMAPClient.Capabilities"/>
    public class cIMAPCapabilities
    {
        /// <summary>
        /// The capabilities advertised by the server.
        /// </summary>
        public readonly cStrings Capabilities;

        /// <summary>
        /// The authentication mechanisms advertised by the server.
        /// </summary>
        public readonly cStrings AuthenticationMechanisms;

        /// <summary>
        /// The set of capabilities that are in use.
        /// </summary>
        /// <remarks>
        /// This value reflects the recognised elements of <see cref="Capabilities"/> less the <see cref="cIMAPClient.IgnoreCapabilities"/>.
        /// </remarks>
        public readonly fIMAPCapabilities EffectiveCapabilities;

        internal cIMAPCapabilities(cStrings pCapabilities, cStrings pAuthenticationMechanisms, fIMAPCapabilities pIgnoreCapabilities)
        {
            Capabilities = pCapabilities ?? throw new ArgumentNullException(nameof(pCapabilities));
            AuthenticationMechanisms = pAuthenticationMechanisms ?? throw new ArgumentNullException(nameof(pAuthenticationMechanisms));

            fIMAPCapabilities lCapabilities = 0;

            if (pCapabilities.Contains("LoginDisabled", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.logindisabled;
            if (pCapabilities.Contains("StartTLS", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.starttls;
            if (pCapabilities.Contains("Idle", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.idle;
            if (pCapabilities.Contains("Literal+", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.literalplus;
            if (pCapabilities.Contains("Literal-", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.literalminus;
            if (pCapabilities.Contains("Enable", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.enable;
            if (pCapabilities.Contains("UTF8=Accept", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.utf8accept;
            if (pCapabilities.Contains("UTF8=Only", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.utf8only;
            if (pCapabilities.Contains("List-Extended", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.listextended;
            if (pCapabilities.Contains("Children", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.children;
            if (pCapabilities.Contains("SASL-IR", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.sasl_ir;
            if (pCapabilities.Contains("Login-Referrals", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.loginreferrals;
            if (pCapabilities.Contains("Mailbox-Referrals", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.mailboxreferrals;
            if (pCapabilities.Contains("Id", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.id;
            if (pCapabilities.Contains("Binary", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.binary;
            if (pCapabilities.Contains("Namespace", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.namespaces;
            if (pCapabilities.Contains("List-Status", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.liststatus;
            if (pCapabilities.Contains("Special-Use", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.specialuse;
            if (pCapabilities.Contains("ESearch", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.esearch;
            if (pCapabilities.Contains("Sort", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.sort;
            if (pCapabilities.Contains("Sort=Display", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.sortdisplay;
            if (pCapabilities.Contains("ESort", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.esort;
            //if (pCapabilities.Contains("Thread=OrderedSubject", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.threadorderedsubject;
            //if (pCapabilities.Contains("Thread=References", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fCapabilities.threadreferences;
            if (pCapabilities.Contains("CondStore", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.condstore;
            if (pCapabilities.Contains("QResync", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.qresync;
            if (pCapabilities.Contains("MultiAppend", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.multiappend;
            if (pCapabilities.Contains("Catenate", StringComparer.InvariantCultureIgnoreCase)) lCapabilities |= fIMAPCapabilities.catenate;

            EffectiveCapabilities = lCapabilities & ~pIgnoreCapabilities;

            // if qresync is on then condstore must be on
            if ((EffectiveCapabilities & fIMAPCapabilities.qresync) != 0) EffectiveCapabilities |= fIMAPCapabilities.condstore;
        }

        /**<summary>Indicates whether IMAP LOGIN is in use.</summary>*/
        public bool LoginDisabled => (EffectiveCapabilities & fIMAPCapabilities.logindisabled) != 0;
        /**<summary>Indicates whether IMAP STARTTLS is in use.</summary>*/
        public bool StartTLS => (EffectiveCapabilities & fIMAPCapabilities.starttls) != 0;
        /**<summary>Indicates whether RFC 2177 IDLE is in use.</summary>*/
        public bool Idle => (EffectiveCapabilities & fIMAPCapabilities.idle) != 0;
        /**<summary>Indicates whether RFC 7888 LITERAL+ is in use.</summary>*/
        public bool LiteralPlus => (EffectiveCapabilities & fIMAPCapabilities.literalplus) != 0;
        /**<summary>Indicates whether RFC 7888 LITERAL- is in use.</summary>*/
        public bool LiteralMinus => (EffectiveCapabilities & fIMAPCapabilities.literalminus) != 0;
        /**<summary>Indicates whether RFC 5161 ENABLE is in use.</summary>*/
        public bool Enable => (EffectiveCapabilities & fIMAPCapabilities.enable) != 0;
        /**<summary>Indicates whether RFC 6855 UTF8=ACCEPT is in use.</summary>*/
        public bool UTF8Accept => (EffectiveCapabilities & fIMAPCapabilities.utf8accept) != 0;
        /**<summary>Indicates whether RFC 6855 UTF8=ONLY is in use.</summary>*/
        public bool UTF8Only => (EffectiveCapabilities & fIMAPCapabilities.utf8only) != 0;
        /**<summary>Indicates whether RFC 5258 LIST extensions is in use.</summary>*/
        public bool ListExtended => (EffectiveCapabilities & fIMAPCapabilities.listextended) != 0;
        /**<summary>Indicates whether RFC 3348 Child mailboxes is in use.</summary>*/
        public bool Children => (EffectiveCapabilities & fIMAPCapabilities.children) != 0;
        /**<summary>Indicates whether RFC 4959 SASL initial client response is in use.</summary>*/
        public bool SASL_IR => (EffectiveCapabilities & fIMAPCapabilities.sasl_ir) != 0;
        /**<summary>Indicates whether RFC 2221 Login referrals is in use.</summary>*/
        public bool LoginReferrals => (EffectiveCapabilities & fIMAPCapabilities.loginreferrals) != 0;
        /**<summary>Indicates whether RFC 2193 Mailbox referrals is in use.</summary>*/
        public bool MailboxReferrals => (EffectiveCapabilities & fIMAPCapabilities.mailboxreferrals) != 0;
        /**<summary>Indicates whether RFC 2971 Id is in use.</summary>*/
        public bool Id => (EffectiveCapabilities & fIMAPCapabilities.id) != 0;
        /**<summary>Indicates whether RFC 3516 Binary content is in use.</summary>*/
        public bool Binary => (EffectiveCapabilities & fIMAPCapabilities.binary) != 0;
        /**<summary>Indicates whether RFC 2342 Namespaces is in use.</summary>*/
        public bool Namespace => (EffectiveCapabilities & fIMAPCapabilities.namespaces) != 0;
        /**<summary>Indicates whether RFC 5819 STATUS information in LIST is in use.</summary>*/
        public bool ListStatus => (EffectiveCapabilities & fIMAPCapabilities.liststatus) != 0;
        /**<summary>Indicates whether RFC 6154 Special use is in use.</summary>*/
        public bool SpecialUse => (EffectiveCapabilities & fIMAPCapabilities.specialuse) != 0;
        /**<summary>Indicates whether RFC 4731 ESEARCH is in use.</summary>*/
        public bool ESearch => (EffectiveCapabilities & fIMAPCapabilities.esearch) != 0;
        /**<summary>Indicates whether RFC 5256 SORT is in use.</summary>*/
        public bool Sort => (EffectiveCapabilities & fIMAPCapabilities.sort) != 0;
        /**<summary>Indicates whether RFC 5256 SORT=DISPLAY is in use.</summary>*/
        public bool SortDisplay => (EffectiveCapabilities & fIMAPCapabilities.sortdisplay) != 0;
        /**<summary>Indicates whether RFC 5267 ESORT is in use.</summary>*/
        public bool ESort => (EffectiveCapabilities & fIMAPCapabilities.esort) != 0;
        //public bool ThreadOrderedSubject => (EffectiveCapabilities & fCapabilities.threadorderedsubject) != 0;
        //public bool ThreadReferences => (EffectiveCapabilities & fCapabilities.threadreferences) != 0;
        /**<summary>Indicates whether RFC 7162 CONDSTORE is in use.</summary>*/
        public bool CondStore => (EffectiveCapabilities & fIMAPCapabilities.condstore) != 0;
        /**<summary>Indicates whether RFC 7162 QRESYNC is in use.</summary>*/
        public bool QResync => (EffectiveCapabilities & fIMAPCapabilities.qresync) != 0;
        /**<summary>Indicates whether RFC 3502 MULTIAPPEND is in use.</summary>*/
        public bool MultiAppend => (EffectiveCapabilities & fIMAPCapabilities.multiappend) != 0;
        /**<summary>Indicates whether RFC 4469 CATENATE is in use.</summary>*/
        public bool Catenate => (EffectiveCapabilities & fIMAPCapabilities.catenate) != 0;

        /// <inheritdoc />
        public override string ToString() => $"{nameof(cIMAPCapabilities)}({Capabilities},{AuthenticationMechanisms},{EffectiveCapabilities})";
    }
}