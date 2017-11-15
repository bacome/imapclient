using System;
using System.Linq;
using work.bacome.apidocumentation;

namespace work.bacome.imapclient
{
    /// <summary>
    /// Represents a set of capabilities that the library understands in some way.
    /// </summary>
    /// <seealso cref="cCapabilities"/>
    /// <seealso cref="cIMAPClient.IgnoreCapabilities"/>
    /// <seealso cref="cProtocolErrorException"/>
    /// <seealso cref="cUnexpectedServerActionException"/>
    /// <seealso cref="cUnsuccessfulCompletionException"/>
    [Flags]
    public enum fCapabilities
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
        qresync = 1 << 23

        /* deimplemented pending a requirement to complete it
        threadorderedsubject = 1 << 22,
        threadreferences = 1 << 23, */
    }

    /// <summary>
    /// Contains IMAP capabilities.
    /// </summary>
    /// <remarks>
    /// The properties of this class reflect the flags set in <see cref="EffectiveCapabilities"/>.
    /// </remarks>
    /// <seealso cref="cIMAPClient.Capabilities"/>
    public class cCapabilities
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
        /// The set of server capabilities that are in use.
        /// </summary>
        /// <remarks>
        /// This value reflects the recognised elements of <see cref="Capabilities"/> less the <see cref="cIMAPClient.IgnoreCapabilities"/>.
        /// </remarks>
        public readonly fCapabilities EffectiveCapabilities;

        internal cCapabilities(cStrings pCapabilities, cStrings pAuthenticationMechanisms, fCapabilities pIgnoreCapabilities)
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

        /**<summary>Indicates if IMAP LOGIN is in use.</summary>*/
        public bool LoginDisabled => (EffectiveCapabilities & fCapabilities.logindisabled) != 0;
        /**<summary>Indicates if IMAP STARTTLS is in use.</summary>*/
        public bool StartTLS => (EffectiveCapabilities & fCapabilities.starttls) != 0;
        /**<summary>Indicates if RFC 2177 IDLE is in use.</summary>*/
        public bool Idle => (EffectiveCapabilities & fCapabilities.idle) != 0;
        /**<summary>Indicates if RFC 7888 LITERAL+ is in use.</summary>*/
        public bool LiteralPlus => (EffectiveCapabilities & fCapabilities.literalplus) != 0;
        /**<summary>Indicates if RFC 7888 LITERAL- is in use.</summary>*/
        public bool LiteralMinus => (EffectiveCapabilities & fCapabilities.literalminus) != 0;
        /**<summary>Indicates if RFC 5161 ENABLE is in use.</summary>*/
        public bool Enable => (EffectiveCapabilities & fCapabilities.enable) != 0;
        /**<summary>Indicates if RFC 6855 UTF8=ACCEPT is in use.</summary>*/
        public bool UTF8Accept => (EffectiveCapabilities & fCapabilities.utf8accept) != 0;
        /**<summary>Indicates if RFC 6855 UTF8=ONLY is in use.</summary>*/
        public bool UTF8Only => (EffectiveCapabilities & fCapabilities.utf8only) != 0;
        /**<summary>Indicates if RFC 5258 LIST extensions is in use.</summary>*/
        public bool ListExtended => (EffectiveCapabilities & fCapabilities.listextended) != 0;
        /**<summary>Indicates if RFC 3348 Child mailboxes is in use.</summary>*/
        public bool Children => (EffectiveCapabilities & fCapabilities.children) != 0;
        /**<summary>Indicates if RFC 4959 SASL initial client response is in use.</summary>*/
        public bool SASL_IR => (EffectiveCapabilities & fCapabilities.sasl_ir) != 0;
        /**<summary>Indicates if RFC 2221 Login referrals is in use.</summary>*/
        public bool LoginReferrals => (EffectiveCapabilities & fCapabilities.loginreferrals) != 0;
        /**<summary>Indicates if RFC 2193 Mailbox referrals is in use.</summary>*/
        public bool MailboxReferrals => (EffectiveCapabilities & fCapabilities.mailboxreferrals) != 0;
        /**<summary>Indicates if RFC 2971 Id is in use.</summary>*/
        public bool Id => (EffectiveCapabilities & fCapabilities.id) != 0;
        /**<summary>Indicates if RFC 3516 Binary content is in use.</summary>*/
        public bool Binary => (EffectiveCapabilities & fCapabilities.binary) != 0;
        /**<summary>Indicates if RFC 2342 Namespaces is in use.</summary>*/
        public bool Namespace => (EffectiveCapabilities & fCapabilities.namespaces) != 0;
        /**<summary>Indicates if RFC 5819 STATUS information in LIST is in use.</summary>*/
        public bool ListStatus => (EffectiveCapabilities & fCapabilities.liststatus) != 0;
        /**<summary>Indicates if RFC 6154 Special use is in use.</summary>*/
        public bool SpecialUse => (EffectiveCapabilities & fCapabilities.specialuse) != 0;
        /**<summary>Indicates if RFC 4731 ESEARCH is in use.</summary>*/
        public bool ESearch => (EffectiveCapabilities & fCapabilities.esearch) != 0;
        /**<summary>Indicates if RFC 5256 SORT is in use.</summary>*/
        public bool Sort => (EffectiveCapabilities & fCapabilities.sort) != 0;
        /**<summary>Indicates if RFC 5256 SORT=DISPLAY is in use.</summary>*/
        public bool SortDisplay => (EffectiveCapabilities & fCapabilities.sortdisplay) != 0;
        /**<summary>Indicates if RFC 5267 ESORT is in use.</summary>*/
        public bool ESort => (EffectiveCapabilities & fCapabilities.esort) != 0;
        //public bool ThreadOrderedSubject => (EffectiveCapabilities & fCapabilities.threadorderedsubject) != 0;
        //public bool ThreadReferences => (EffectiveCapabilities & fCapabilities.threadreferences) != 0;
        /**<summary>Indicates if RFC 7162 CONDSTORE is in use.</summary>*/
        public bool CondStore => (EffectiveCapabilities & fCapabilities.condstore) != 0;
        /**<summary>Indicates if RFC 7162 QRESYNC is in use.</summary>*/
        public bool QResync => (EffectiveCapabilities & fCapabilities.qresync) != 0;

        /// <inheritdoc cref="cAPIDocumentationTemplate.ToString"/>
        public override string ToString() => $"{nameof(cCapabilities)}({Capabilities},{AuthenticationMechanisms},{EffectiveCapabilities})";
    }
}