using System;
using System.Collections.Generic;
using work.bacome.imapclient.support;

namespace work.bacome.imapclient
{
    public class cHeaderFieldComment : cHeaderFieldValuePart
    {
        private List<cHeaderFieldValuePart> mParts = new List<cHeaderFieldValuePart>();

        public cHeaderFieldComment() { }

        internal override void GetBytes(cHeaderFieldBytes pBytes)
        {
            pBytes.AddToken(cASCII.LPAREN);
            foreach (var lPart in mParts) lPart.GetBytes(pBytes);
            pBytes.AddToken(cASCII.RPAREN);
        }

        public void Add(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            mParts.Add(new cHeaderFieldCommentPart(pText));
        }

        public void Add(cHeaderFieldComment pComment)
        {
            mParts.Add(pComment);
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderFieldComment));
            foreach (var lPart in mParts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }

    public class cHeaderFieldPhrase : cHeaderFieldValuePart
    {
        private List<cHeaderFieldValuePart> mParts = new List<cHeaderFieldValuePart>();

        public cHeaderFieldPhrase() { }

        internal override void GetBytes(cHeaderFieldBytes pBytes)
        {
            foreach (var lPart in mParts) lPart.GetBytes(pBytes);
        }

        public void Add(string pText)
        {
            if (pText == null) throw new ArgumentNullException(nameof(pText));
            mParts.Add(new cHeaderFieldPhrasePart(pText));
        }

        public void Add(cHeaderFieldComment pComment)
        {
            mParts.Add(pComment);
        }

        public void AddComment(string pText)
        {
            cHeaderFieldComment lComment = new cHeaderFieldComment();
            lComment.Add(pText);
            mParts.Add(lComment);
        }

        public override string ToString()
        {
            cListBuilder lBuilder = new cListBuilder(nameof(cHeaderFieldPhrase));
            foreach (var lPart in mParts) lBuilder.Append(lPart);
            return lBuilder.ToString();
        }
    }
}
