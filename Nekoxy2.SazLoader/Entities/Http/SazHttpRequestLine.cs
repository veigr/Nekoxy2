using Nekoxy2.Spi.Entities.Http;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Nekoxy2.SazLoader.Entities.Http
{
    /// <summary>
    /// リクエストライン
    /// </summary>
    internal sealed class SazHttpRequestLine : IReadOnlyHttpRequestLine
    {
        public HttpMethod Method { get; }

        public string RequestTarget { get; }

        public Version HttpVersion { get; }

        private SazHttpRequestLine(HttpMethod method, string requestTarget, Version httpVersion)
        {
            this.Method = method;
            this.RequestTarget = requestTarget;
            this.HttpVersion = httpVersion;
        }

        public override string ToString() => $"{this.Method} {this.RequestTarget} HTTP/{this.HttpVersion}\r\n";

        private static readonly Regex requestLinePattern = new Regex(@"([A-Z]+) ([^ ]+) HTTP/(\d).(\d)\r\n", RegexOptions.Compiled | RegexOptions.Singleline);

        public static SazHttpRequestLine Parse(string source)
        {
            var groups = requestLinePattern.Match(source).Groups;
            var method = new HttpMethod(groups[1].Value);
            var target = groups[2].Value;
            var version = new Version(int.Parse(groups[3].Value), int.Parse(groups[4].Value));
            return new SazHttpRequestLine(method, target, version);
        }
    }
}
