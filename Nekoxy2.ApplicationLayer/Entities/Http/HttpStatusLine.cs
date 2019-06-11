using Nekoxy2.Spi.Entities.Http;
using System;
using System.Net;
using System.Text.RegularExpressions;

namespace Nekoxy2.ApplicationLayer.Entities.Http
{
    /// <summary>
    /// HTTP ステータスライン
    /// </summary>
    internal sealed class HttpStatusLine : IReadOnlyHttpStatusLine
    {
        public Version HttpVersion { get; }

        public HttpStatusCode StatusCode { get; }

        public string ReasonPhrase { get; }

        /// <summary>
        /// ソース文字列
        /// </summary>
        public string Source { get; }

        public override string ToString()
            => $"HTTP/{this.HttpVersion} {(int)this.StatusCode} {this.ReasonPhrase}\r\n";

        public HttpStatusLine(Version httpVersion, HttpStatusCode statusCode, string reasonPhrase, string source = "")
        {
            this.Source = source;
            this.HttpVersion = httpVersion;
            this.StatusCode = statusCode;
            this.ReasonPhrase = reasonPhrase;
        }

        /// <summary>
        /// ステータスラインに合致するパターン(末尾改行あり)
        /// </summary>
        private static readonly Regex pattern = new Regex(@"HTTP/(\d)\.(\d) (\d{3})[ ]?([^\r\n]*)\r\n",
            RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// 解析(要改行)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="statusLine"></param>
        /// <returns></returns>
        public static bool TryParse(string source, out HttpStatusLine statusLine)
        {
            try
            {
                var groups = pattern.Match(source).Groups;
                var version = new Version(int.Parse(groups[1].Value), int.Parse(groups[2].Value));
                var statusCode = (HttpStatusCode)int.Parse(groups[3].Value);
                var reasonPhrase = groups[4].Value;  // reason-phrase の手前の SP は省略できないはずだが、実際には省略してくるサーバーがいる
                statusLine = new HttpStatusLine(version, statusCode, reasonPhrase, source);
                return true;
            }
            catch (Exception)
            {
                statusLine = new HttpStatusLine(null, 0, null, source);
                return false;
            }
        }

        public static HttpStatusLine Empty
            => new HttpStatusLine(null, 0, null, "");
    }
}
