using System;

namespace work.bacome.imapclient
{
    public class cServer
    {
        public readonly string Host;
        public readonly int Port;
        public readonly bool SSL;

        public cServer(string pHost)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));
            Host = pHost;
            Port = 143;
            SSL = false;
        }

        public cServer(string pHost, bool pSSL)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));
            Host = pHost;
            if (pSSL) Port = 993; else Port = 143;
            SSL = pSSL;
        }

        public cServer(string pHost, int pPort, bool pSSL)
        {
            if (string.IsNullOrWhiteSpace(pHost)) throw new ArgumentOutOfRangeException(nameof(pHost));
            if (pPort < 1 || pPort > 65536) throw new ArgumentOutOfRangeException(nameof(pPort));
            Host = pHost;
            Port = pPort;
            SSL = pSSL;
        }

        public override string ToString() => $"{nameof(cServer)}({Host}:{Port},{nameof(SSL)}={SSL})";
    }
}