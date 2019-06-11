using Nekoxy2.Spi.Entities.Http;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Nekoxy2.ApplicationLayer.Entities.Http
{
    /// <summary>
    /// HTTP リクエストライン
    /// </summary>
    internal sealed class HttpRequestLine : IReadOnlyHttpRequestLine
    {
        public HttpMethod Method { get; }

        public string RequestTarget { get; }

        public Version HttpVersion { get; }

        /// <summary>
        /// リクエストターゲットの形式
        /// </summary>
        public RequestTargetForm RequestTargetForm { get; } = RequestTargetForm.Unknown;

        /// <summary>
        /// ソース文字列
        /// </summary>
        public string Source { get; }

        internal bool IsValid { get; } = true;

        public HttpRequestLine(string source, HttpMethod method, string requestTarget, Version httpVersion)
        {
            this.Source = source;
            try
            {
                this.Method = method;
                this.RequestTarget = requestTarget;
                this.HttpVersion = httpVersion;

                if (method.Method == "CONNECT")
                    this.RequestTargetForm = RequestTargetForm.AuthorityForm;
                else if (absoluteUriPattern.IsMatch(requestTarget))
                    this.RequestTargetForm = RequestTargetForm.AbsoluteForm;
                else if (requestTarget.StartsWith("/"))
                    this.RequestTargetForm = RequestTargetForm.OriginForm;
                else if (method == HttpMethod.Options && requestTarget == "*")
                    this.RequestTargetForm = RequestTargetForm.AsteriskForm;
                else
                    this.RequestTargetForm = RequestTargetForm.Unknown;
            }
            catch (Exception)
            {
                this.IsValid = false;
            }
        }

        /// <summary>
        /// 操作による改変前のリクエストライン
        /// </summary>
        /// <returns></returns>
        public HttpRequestLine GetOrigin()
        {
            TryParse(this.Source, out var requestLine);
            return requestLine;
        }

        public override string ToString()
        {
            if (this.IsValid)
                return $"{this.Method} {this.RequestTarget} HTTP/{this.HttpVersion}\r\n";
            else
                return this.Source;
        }

        /// <summary>
        /// Absolute-Form に適合するパターン
        /// </summary>
        private static readonly Regex absoluteUriPattern = new Regex(@"^\w+://.+",
            RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// リクエストラインに適合するパターン(末尾改行あり)
        /// </summary>
        private static readonly Regex requestLinePattern = new Regex(@"([A-Z]+) ([^ ]+) HTTP/(\d).(\d)\r\n",
            RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// 解析(要改行)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="requestLine"></param>
        /// <returns></returns>
        public static bool TryParse(string source, out HttpRequestLine requestLine)
        {
            try
            {
                var groups = requestLinePattern.Match(source).Groups;
                var method = new HttpMethod(groups[1].Value);
                var target = groups[2].Value;
                var version = new Version(int.Parse(groups[3].Value), int.Parse(groups[4].Value));
                requestLine = new HttpRequestLine(source, method, target, version);
                return requestLine.IsValid;
            }
            catch (Exception)
            {
                requestLine = new HttpRequestLine(source, null, null, null);
                return false;
            }
        }

        public static HttpRequestLine Empty
            => new HttpRequestLine("", null, null, null);
    }
}
