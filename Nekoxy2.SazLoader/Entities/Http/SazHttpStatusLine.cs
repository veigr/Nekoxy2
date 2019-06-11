using Nekoxy2.Spi.Entities.Http;
using System;
using System.Net;
using System.Text.RegularExpressions;

namespace Nekoxy2.SazLoader.Entities.Http
{
    /// <summary>
    /// ステータスライン
    /// </summary>
    internal sealed class SazHttpStatusLine : IReadOnlyHttpStatusLine
    {
        public Version HttpVersion { get; }

        public HttpStatusCode StatusCode { get; }

        public string ReasonPhrase { get; }

        public override string ToString()
            => $"HTTP/{this.HttpVersion} {(int)this.StatusCode} {this.ReasonPhrase}\r\n";

        private SazHttpStatusLine(Version httpVersion, HttpStatusCode statusCode, string reasonPhrase)
        {
            this.HttpVersion = httpVersion;
            this.StatusCode = statusCode;
            this.ReasonPhrase = reasonPhrase;
        }

        private static Regex pattern = new Regex(@"HTTP/(\d)\.(\d) (\d{3}) ([^\r\n]*)\r\n", RegexOptions.Compiled | RegexOptions.Singleline);

        public static SazHttpStatusLine Parse(string source)
        {
            var groups = pattern.Match(source).Groups;
            var version = new Version(int.Parse(groups[1].Value), int.Parse(groups[2].Value));
            var statusCode = (HttpStatusCode)int.Parse(groups[3].Value);
            var reasonPhrase = groups[4].Value;
            return new SazHttpStatusLine(version, statusCode, reasonPhrase);
        }
    }
}
