using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using work.bacome.imapclient;

namespace testharness2
{
    public partial class frmMessage : Form
    {
        private const string kNoNodeSelected = "<no node selected>";
        private const string kLoading = "<loading>";

        private readonly string mInstanceName;
        private readonly frmSelectedMailbox mParent; // for previous/next
        private readonly cMailbox mMailbox;
        private readonly uint mMaxTextBytes;
        private cMessage mMessage;
        private bool mSubscribed = false;
        private bool mEnvelopeDisplayed = false;
        private bool mTextDisplayed = false;
        private bool mAttachmentsDisplayed = false;
        private bool mFlagsDisplayed = false;
        private bool mBodyStructureDisplayed = false;
        private bool mOtherDisplayed = false;
        private bool mSummaryDisplayed = false;
        private bool mRawDisplayed = false;
        private bool mDecodedDisplayed = false;
        private string mRawSectionData = null;

        private Task mImageLoadTask = null;
        private frmProgress mImageLoadProgress = null;
        private MemoryStream mImageStream = null;

        public frmMessage(string pInstanceName, frmSelectedMailbox pParent, cMailbox pMailbox, uint pMaxTextBytes, cMessage pMessage)
        {
            mInstanceName = pInstanceName;
            mParent = pParent;
            mMailbox = pMailbox;
            mMaxTextBytes = pMaxTextBytes;
            mMessage = pMessage;
            InitializeComponent();
        }

        private void frmMessage_Load(object sender, EventArgs e)
        {
            Text = "imapclient testharness - message - " + mInstanceName + " - " + mMessage.BaseSubject;

            pbx.Top = rtxDecoded.Top;
            pbx.Left = rtxDecoded.Left;
            pbx.Height = rtxDecoded.Height;
            pbx.Width = rtxDecoded.Width;
            pbx.Visible = false;

            if (mMailbox.IsSelectedForUpdate)
            {
                chkSeen.Enabled = true;
                chkDeleted.Enabled = true;
                chkFred.Enabled = mMailbox.MessageFlags.ContainsCreateNewPossible;
            }
            else
            {
                chkSeen.Enabled = false;
                chkDeleted.Enabled = false;
                chkFred.Enabled = false;
            }

            ZQueryAsync(true);
        }

        private string ZTooBig(uint pBytes) => $"<too big: estimated size: {pBytes}>";

        private int mQueryAsyncEntryNumber = 0;

        private async void ZQueryAsync(bool pFirst)
        {
            // defend against re-entry during awaits
            int lQueryAsyncEntryNumber = ++mQueryAsyncEntryNumber;

            if (pFirst)
            {
                mEnvelopeDisplayed = false;
                mTextDisplayed = false;
                mAttachmentsDisplayed = false;
                mFlagsDisplayed = false;
                mBodyStructureDisplayed = false;
                mOtherDisplayed = false;
            }

            lblQueryError.Text = "";

            StringBuilder lBuilder = new StringBuilder();

            try
            {
                if (ReferenceEquals(tab.SelectedTab, tpgEnvelope))
                {
                    if (!mEnvelopeDisplayed)
                    {
                        mEnvelopeDisplayed = true;
                        ZAppendEnvelope(lBuilder, mMessage.Envelope);
                        rtxEnvelope.Text = lBuilder.ToString();
                    }

                    return;
                }

                if (ReferenceEquals(tab.SelectedTab, tpgText))
                {
                    if (!mTextDisplayed)
                    {
                        mTextDisplayed = true;

                        string lText;
                        uint lBytes = mMessage.PlainTextSizeInBytes;
                        if (lBytes > mMaxTextBytes) lText = ZTooBig(lBytes);
                        else lText = await mMessage.PlainTextAsync();

                        if (lQueryAsyncEntryNumber != mQueryAsyncEntryNumber) return;
                        rtxText.Text = lText;
                    }

                    return;
                }

                if (ReferenceEquals(tab.SelectedTab, tpgAttachments))
                {
                    if (!mAttachmentsDisplayed)
                    {
                        mAttachmentsDisplayed = true;
                        // TODO
                    }

                    return;
                }

                if (ReferenceEquals(tab.SelectedTab, tpgFlags))
                {
                    if (!mFlagsDisplayed)
                    {
                        mFlagsDisplayed = true;

                        ZSubscribe(chkAutoRefresh.Checked);

                        if (mMailbox.HighestModSeq != 0) lBuilder.AppendLine("Modseq: " + mMessage.ModSeq);
                        lBuilder.AppendLine("Flags: " + mMessage.Flags);

                        rtxFlags.Text = lBuilder.ToString();

                        chkSeen.Checked = mMessage.IsSeen;
                        chkDeleted.Checked = mMessage.IsDeleted;
                        chkFred.Checked = mMessage.FlagsContain("fred");
                    }

                    return;
                }

                if (ReferenceEquals(tab.SelectedTab, tpgBodyStructure))
                {
                    if (!mBodyStructureDisplayed)
                    {
                        mBodyStructureDisplayed = true;
                        ZQueryBodyStructure();
                        ZQueryBodyStructureDetail(true);
                    }

                    return;
                }

                if (ReferenceEquals(tab.SelectedTab, tpgOther))
                {
                    if (!mOtherDisplayed)
                    {
                        mOtherDisplayed = true;

                        lBuilder.AppendLine("Received: " + mMessage.Received);
                        lBuilder.AppendLine("UID: " + mMessage.UID);
                        lBuilder.AppendLine("References: " + mMessage.References);

                        rtxOther.Text = lBuilder.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                lblQueryError.Text = $"error: {ex.ToString()}";
            }
        }

        private void ZQueryBodyStructure()
        {
            tvwBodyStructure.BeginUpdate();
            tvwBodyStructure.Nodes.Clear();
            var lRoot = tvwBodyStructure.Nodes.Add("root");
            lRoot.Tag = new cNodeTag(cSection.All, mMessage.Size);
            ZQueryBodyStructureAddSection(lRoot, "header", cSection.Header, 0);
            ZQueryBodyStructureAddPart(lRoot, mMessage.BodyStructure);
            tvwBodyStructure.ExpandAll();
            tvwBodyStructure.EndUpdate();
        }

        private void ZQueryBodyStructureAddSection(TreeNode pParent, string pText, cSection pSection, uint pBytes)
        {
            var lNode = pParent.Nodes.Add(pText);
            lNode.Tag = new cNodeTag(pSection, pBytes);
        }

        private uint ZQueryBodyStructureAddPart(TreeNode pParent, cBodyPart pBodyPart)
        {
            string lPart;

            if (pBodyPart.Section.Part == null) lPart = pBodyPart.Section.TextPart.ToString();
            else
            {
                if (pBodyPart.Section.TextPart == eSectionPart.all)
                {
                    ZQueryBodyStructureAddSection(pParent, pBodyPart.Section.Part + ".mime", new cSection(pBodyPart.Section.Part, eSectionPart.mime), 0);
                    lPart = pBodyPart.Section.Part;
                }
                else lPart = pBodyPart.Section.Part + "." + pBodyPart.Section.TextPart.ToString();
            }

            var lNode = pParent.Nodes.Add(lPart + ": " + pBodyPart.Type + "/" + pBodyPart.SubType);
            uint lBytes;

            if (pBodyPart is cMessageBodyPart lMessage)
            {
                ZQueryBodyStructureAddSection(lNode, pBodyPart.Section.Part + ".header", new cSection(pBodyPart.Section.Part, eSectionPart.header), 0);
                ZQueryBodyStructureAddPart(lNode, lMessage.BodyStructure);
                lBytes = lMessage.SizeInBytes;
            }
            else if (pBodyPart is cMultiPartBody lMultiPartPart)
            {
                lBytes = 0;
                foreach (var lBodyPart in lMultiPartPart.Parts) lBytes += ZQueryBodyStructureAddPart(lNode, lBodyPart);
            }
            else if (pBodyPart is cSinglePartBody lSinglePart) lBytes = lSinglePart.SizeInBytes;
            else lBytes = int.MaxValue; // should never happen

            lNode.Tag = new cNodeTag(pBodyPart, lBytes);

            return lBytes;
        }

        private int mQueryBodyStructureDetailEntryNumber = 0;

        private void ZQueryBodyStructureDetail(bool pFirst)
        {
            // defend against re-entry during awaits
            int lQueryBodyStructureDetailEntryNumber = ++mQueryBodyStructureDetailEntryNumber;

            if (pFirst)
            {
                mSummaryDisplayed = false;
                mRawDisplayed = false;
                mDecodedDisplayed = false;
                mRawSectionData = null;
            }

            lblQueryError.Text = "";

            if (ReferenceEquals(tabBodyStructure.SelectedTab, tpgSummary))
            {
                if (!mSummaryDisplayed)
                {
                    mSummaryDisplayed = true;
                    ZQueryBodyStructureSummary(lQueryBodyStructureDetailEntryNumber);
                }

                return;
            }

            if (ReferenceEquals(tabBodyStructure.SelectedTab, tpgRaw))
            {
                if (!mRawDisplayed)
                {
                    mRawDisplayed = true;
                    ZQueryBodyStructureRaw(lQueryBodyStructureDetailEntryNumber);
                }

                return;
            }

            if (ReferenceEquals(tabBodyStructure.SelectedTab, tpgDecoded))
            {
                if (!mDecodedDisplayed)
                {
                    mDecodedDisplayed = true;
                    ZQueryBodyStructureDecodedAsync(lQueryBodyStructureDetailEntryNumber);
                }

                return;
            }
        }

        private void ZQueryBodyStructureSummary(int pQueryBodyStructureDetailEntryNumber)
        {
            if (tvwBodyStructure.SelectedNode == null)
            {
                rtxSummary.Text = kNoNodeSelected;
                return;
            }

            var lTag = tvwBodyStructure.SelectedNode.Tag as cNodeTag;

            if (lTag.Section == cSection.All)
            {
                StringBuilder lBuilder = new StringBuilder();
                lBuilder.AppendLine("root");
                lBuilder.AppendLine("message size: " + mMessage.Size);
                rtxSummary.Text = lBuilder.ToString();
                return;
            }

            if (lTag.BodyPart != null)
            {
                StringBuilder lBuilder = new StringBuilder();

                lBuilder.AppendLine($"Section: {lTag.BodyPart.Section}");

                if (lTag.BodyPart.Disposition != null) lBuilder.AppendLine($"Disposition: {lTag.BodyPart.Disposition.Type} {lTag.BodyPart.Disposition.FileName} {lTag.BodyPart.Disposition.Size} {lTag.BodyPart.Disposition.CreationDate}");
                if (lTag.BodyPart.Languages != null) lBuilder.AppendLine($"Languages: {lTag.BodyPart.Languages}");
                if (lTag.BodyPart.Location != null) lBuilder.AppendLine($"Location: {lTag.BodyPart.Location}");

                if (lTag.BodyPart is cSinglePartBody lSingleBodyPart)
                {
                    lBuilder.AppendLine($"Content Id: {lSingleBodyPart.ContentId}");
                    lBuilder.AppendLine($"Description: {lSingleBodyPart.Description}");
                    lBuilder.AppendLine($"ContentTransferEncoding: {lSingleBodyPart.ContentTransferEncoding}");
                    lBuilder.AppendLine($"Size: {lSingleBodyPart.SizeInBytes}");

                    if (lTag.BodyPart is cTextBodyPart lTextBodyPart) lBuilder.AppendLine($"Charset: {lTextBodyPart.Charset}");
                    else if (lTag.BodyPart is cMessageBodyPart lMessageBodyPart) ZAppendEnvelope(lBuilder, lMessageBodyPart.Envelope);
                }

                rtxSummary.Text = lBuilder.ToString();
                return;
            }

            ZQueryBodyStructureRawSectionDataAsync(lTag, pQueryBodyStructureDetailEntryNumber, rtxSummary);
        }

        private void ZQueryBodyStructureRaw(int pQueryBodyStructureDetailEntryNumber)
        {
            if (tvwBodyStructure.SelectedNode == null)
            {
                rtxRaw.Text = kNoNodeSelected;
                cmdDownloadRaw.Enabled = false;
                return;
            }

            rtxRaw.Text = kLoading;
            cmdDownloadRaw.Enabled = true;

            var lTag = tvwBodyStructure.SelectedNode.Tag as cNodeTag;
            ZQueryBodyStructureRawSectionDataAsync(lTag, pQueryBodyStructureDetailEntryNumber, rtxRaw);
        }

        private async void ZQueryBodyStructureDecodedAsync(int pQueryBodyStructureDetailEntryNumber)
        {
            if (tvwBodyStructure.SelectedNode == null)
            {
                rtxDecoded.Text = kNoNodeSelected;
                rtxDecoded.Visible = true;
                pbx.Visible = false;
                cmdDownloadDecoded.Enabled = false;
                return;
            }

            rtxDecoded.Text = kLoading;
            rtxDecoded.Visible = true;
            pbx.Visible = false;
            cmdDownloadDecoded.Enabled = true;

            ZImageLoadCancel();

            var lTag = tvwBodyStructure.SelectedNode.Tag as cNodeTag;

            if (lTag.BodyPart != null)
            {
                if (lTag.BodyPart is cTextBodyPart lTextPart)
                {
                    string lText;

                    if (lTag.Bytes > mMaxTextBytes) lText = ZTooBig(lTag.Bytes);
                    else
                    {
                        try { lText = await mMessage.FetchAsync(lTag.BodyPart); }
                        catch (Exception e)
                        {
                            lText = e.ToString();
                        }

                        if (pQueryBodyStructureDetailEntryNumber != mQueryBodyStructureDetailEntryNumber) return;
                    }

                    rtxDecoded.Text = lText;
                    return;
                }

                if (lTag.BodyPart is cSinglePartBody lSinglePart && lSinglePart.TypeCode == eBodyPartTypeCode.image)
                {
                    string lError = "<something went wrong>";

                    try
                    {
                        uint lSize = await mMessage.FetchSizeInBytesAsync(lSinglePart);
                        if (pQueryBodyStructureDetailEntryNumber != mQueryBodyStructureDetailEntryNumber) return;

                        mImageLoadTask = ZImageLoad(pQueryBodyStructureDetailEntryNumber, lSinglePart, lSize);
                        await mImageLoadTask;
                        if (pQueryBodyStructureDetailEntryNumber != mQueryBodyStructureDetailEntryNumber) return;

                        pbx.Image = Image.FromStream(mImageStream);
                        pbx.Visible = true;
                        rtxDecoded.Visible = false;
                        return;
                    }
                    catch (Exception e)
                    {
                        lError = e.ToString();
                    }

                    if (pQueryBodyStructureDetailEntryNumber != mQueryBodyStructureDetailEntryNumber) return;
                    mImageLoadTask = null;

                    rtxDecoded.Text = lError;
                    return;
                }
            }

            ZQueryBodyStructureRawSectionDataAsync(lTag, pQueryBodyStructureDetailEntryNumber, rtxDecoded);
        }

        private async Task ZImageLoad(int pQueryBodyStructureDetailEntryNumber, cSinglePartBody pPart, uint pSize)
        {
            MemoryStream lStream = null;

            try
            {
                mImageLoadProgress = new frmProgress("image " + pPart.Section.Part + " [" + pSize + " bytes]", (int)pSize);
                mImageLoadProgress.Show();
                lStream = new MemoryStream();
                cBodyFetchConfiguration lConfiguration = new cBodyFetchConfiguration(mImageLoadProgress.CancellationToken, mImageLoadProgress.Increment);
                await mMessage.FetchAsync(pPart, lStream, lConfiguration);
                if (pQueryBodyStructureDetailEntryNumber != mQueryBodyStructureDetailEntryNumber) throw new Exception();
                mImageStream = lStream;
            }
            catch (Exception e)
            {
                if (mImageLoadProgress != null) mImageLoadProgress.Cancel();
                if (lStream != null) lStream.Dispose();
                if (pQueryBodyStructureDetailEntryNumber != mQueryBodyStructureDetailEntryNumber) return;
                rtxDecoded.Text = e.ToString();
                throw;
            }
            finally
            {
                if (mImageLoadProgress != null)
                {
                    mImageLoadProgress.Close();
                    mImageLoadProgress = null;
                }
            }
        }

        private void ZImageLoadCancel()
        {
            if (mImageLoadProgress != null)
            {
                try { mImageLoadProgress.Cancel(); }
                catch { }
            }

            if (mImageLoadTask != null)
            {
                try { mImageLoadTask.Wait(); }
                catch { }
            }

            if (mImageStream != null)
            {
                try { mImageStream.Dispose(); }
                catch { }
            }
        }

        private async void ZQueryBodyStructureRawSectionDataAsync(cNodeTag pTag, int pQueryBodyStructureDetailEntryNumber, RichTextBox pRTX)
        {
            if (mRawSectionData == null)
            {
                if (pTag.Bytes > mMaxTextBytes) mRawSectionData = ZTooBig(pTag.Bytes);
                else
                {
                    cSection lSection;

                    if (pTag.Section == null) lSection = pTag.BodyPart.Section;
                    else lSection = pTag.Section;

                    string lRawSectionData;

                    try { lRawSectionData = await mMessage.FetchAsync(lSection); }
                    catch (Exception e)
                    {
                        lRawSectionData = e.ToString();
                    }

                    if (pQueryBodyStructureDetailEntryNumber != mQueryBodyStructureDetailEntryNumber) return;
                    mRawSectionData = lRawSectionData;
                }
            }

            pRTX.Text = mRawSectionData;
        }

        private void frmMessage_FormClosing(object sender, FormClosingEventArgs e)
        {
            ZImageLoadCancel();
        }

        private void frmMessage_FormClosed(object sender, FormClosedEventArgs e)
        {
            ZSubscribe(false);
        }

        private void chkAutoRefresh_CheckedChanged(object sender, EventArgs e)
        {
            mFlagsDisplayed = false;
            ZQueryAsync(false);
        }

        private void ZSubscribe(bool pSubscribe)
        {
            if (mSubscribed == pSubscribe) return;

            if (pSubscribe) mMessage.PropertyChanged += ZRefresh;
            else mMessage.PropertyChanged -= ZRefresh;

            mSubscribed = pSubscribe;
        }

        private void ZRefresh(object sender, PropertyChangedEventArgs e)
        {
            mFlagsDisplayed = false;
            ZQueryAsync(false);
        }

        private void cmdPrevious_Click(object sender, EventArgs e)
        {
            var lMessage = mParent.Previous(mMessage);

            if (lMessage == null)
            {
                MessageBox.Show("no previous");
                return;
            }

            ZSubscribe(false);
            mMessage = lMessage;
            ZQueryAsync(true);
        }

        private void cmdNext_Click(object sender, EventArgs e)
        {
            var lMessage = mParent.Next(mMessage);

            if (lMessage == null)
            {
                MessageBox.Show("no next");
                return;
            }

            ZSubscribe(false);
            mMessage = lMessage;
            ZQueryAsync(true);
        }

        private void tab_Selected(object sender, TabControlEventArgs e)
        {
            ZQueryAsync(false);
        }

        private void tvwBodyStructure_AfterSelect(object sender, TreeViewEventArgs e)
        {
            ZQueryBodyStructureDetail(true);
        }

        private void ZAppendEnvelope(StringBuilder pBuilder, cEnvelope pEnvelope)
        {
            pBuilder.AppendLine("Sent: " + pEnvelope.Sent);
            pBuilder.AppendLine("Subject: " + pEnvelope.Subject);
            pBuilder.AppendLine("Base Subject: " + pEnvelope.BaseSubject);
            ZAppendAddresses(pBuilder, "From: ", pEnvelope.From);
            ZAppendAddresses(pBuilder, "Sender: ", pEnvelope.Sender);
            ZAppendAddresses(pBuilder, "Reply To: ", pEnvelope.ReplyTo);
            ZAppendAddresses(pBuilder, "To: ", pEnvelope.To);
            ZAppendAddresses(pBuilder, "CC: ", pEnvelope.CC);
            ZAppendAddresses(pBuilder, "BCC: ", pEnvelope.BCC);
            pBuilder.AppendLine("In Reply To: " + pEnvelope.InReplyTo);
            pBuilder.AppendLine("Message Id: " + pEnvelope.MessageId);
        }

        private void ZAppendAddresses(StringBuilder pBuilder, string pAddressType, cAddresses pAddresses)
        {
            if (pAddresses == null) return;

            pBuilder.Append(pAddressType);

            bool lFirst = true;

            foreach (var lAddress in pAddresses)
            {
                if (lFirst) lFirst = false;
                else pBuilder.Append(", ");

                if (lAddress.DisplayName != null) pBuilder.Append(lAddress.DisplayName);
                if (lAddress is cEmailAddress lEmailAddress) pBuilder.Append($"<{lEmailAddress.DisplayAddress}>");
            }

            pBuilder.AppendLine();
        }


        private void tabBodyStructure_Selected(object sender, TabControlEventArgs e)
        {
            ZQueryBodyStructureDetail(false);
        }














        private class cNodeTag
        {
            public readonly cBodyPart BodyPart;
            public readonly cSection Section;
            public readonly uint Bytes;

            public cNodeTag(cBodyPart pBodyPart, uint pBytes)
            {
                BodyPart = pBodyPart ?? throw new ArgumentNullException(nameof(pBodyPart));
                Section = null;
                Bytes = pBytes;
            }

            public cNodeTag(cSection pSection, uint pBytes)
            {
                BodyPart = null;
                Section = pSection ?? throw new ArgumentNullException(nameof(pSection));
                Bytes = pBytes;
            }
        }
    }
}
