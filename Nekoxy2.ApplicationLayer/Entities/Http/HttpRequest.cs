using Nekoxy2.Spi.Entities.Http;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Nekoxy2.ApplicationLayer.Entities.Http
{
    /// <summary>
    /// HTTP リクエスト
    /// </summary>
    internal sealed class HttpRequest : IReadOnlyHttpRequest
    {
        IReadOnlyHttpRequestLine IReadOnlyHttpRequest.RequestLine => this.RequestLine;

        public HttpRequestLine RequestLine { get; private set; }

        IReadOnlyHttpHeaders IReadOnlyHttpMessage.Headers => this.Headers;

        public HttpHeaders Headers { get; }

        public byte[] Body { get; }

        IReadOnlyHttpHeaders IReadOnlyHttpMessage.Trailers => this.Trailers;

        public HttpHeaders Trailers { get; }

        /// <summary>
        /// Uri に解析されたリクエストターゲット(scheme 初期値は仮定値)
        /// </summary>
        internal Uri RequestTargetUri { get; private set; }

        /// <summary>
        /// プロトコルスキーム
        /// </summary>
        private string scheme;

        public HttpRequest(HttpRequestLine requestLine, HttpHeaders headers, byte[] body, HttpHeaders trailers, string scheme = null)
        {
            this.RequestLine = requestLine ?? HttpRequestLine.Empty;
            this.Headers = headers ?? HttpHeaders.Empty;
            this.Body = body;
            this.Trailers = trailers ?? HttpHeaders.Empty;
            this.scheme = scheme;
            if (this.RequestLine.IsValid && this.Headers.IsValid)
                this.NormalizeHost();
        }

        /// <summary>
        /// リクエストターゲットを変更
        /// </summary>
        /// <param name="value">新しいリクエストターゲット</param>
        public void ChangeRequestTarget(string value)
        {
            this.RequestLine = new HttpRequestLine(this.RequestLine.Source, this.RequestLine.Method, value, this.RequestLine.HttpVersion);
            this.NormalizeHost();
        }

        /// <summary>
        /// Scheme を変更
        /// </summary>
        /// <param name="scheme">新しい Scheme</param>
        public void ChangeScheme(string scheme)
        {
            this.scheme = scheme;
            this.NormalizeHost();
        }

        private static readonly Regex schemeReplacer = new Regex(@"^[a-z]+(://.+)", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Host ヘッダーおよびリクエストターゲットを補正
        /// </summary>
        private void NormalizeHost()
        {
            switch (this.RequestLine.RequestTargetForm)
            {
                case RequestTargetForm.OriginForm:
                    if (this.Headers.Host.Exists)
                        this.RequestTargetUri = new Uri($"{this.scheme ?? "http"}://{this.Headers.Host}{this.RequestLine.RequestTarget}");
                    else
                        this.RequestTargetUri = null;
                    break;
                case RequestTargetForm.AbsoluteForm:
                    this.RequestTargetUri = this.scheme == null
                        ? new Uri(this.RequestLine.RequestTarget)
                        : new Uri(this.scheme + schemeReplacer.Match(this.RequestLine.RequestTarget).Groups[1]);
                    this.Headers.Host.Value = this.RequestTargetUri.Authority;
                    break;
                case RequestTargetForm.AuthorityForm:
                    this.RequestTargetUri = new Uri($"{this.scheme ?? "https"}://{this.RequestLine.RequestTarget}");
                    break;
                case RequestTargetForm.AsteriskForm:
                    this.RequestTargetUri = new Uri("*", UriKind.RelativeOrAbsolute);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 操作による改変前のリクエスト
        /// </summary>
        /// <returns></returns>
        public HttpRequest GetOrigin()
            => new HttpRequest(this.RequestLine.GetOrigin(), this.Headers.GetOrigin(), this.Body, this.Trailers, this.scheme);

        /// <summary>
        /// ソース文字列
        /// </summary>
        public string Source
            => this.RequestLine.Source + this.Headers.Source + this.Body.AsString();

        /// <summary>
        /// バイト配列として取得
        /// </summary>
        /// <returns></returns>
        public byte[] HeadersAsByte()
            => Encoding.ASCII.GetBytes(this.RequestLine.ToString() + this.Headers.ToString());

        public override string ToString()
            => $"{this.RequestLine}{this.Headers}{this.GetBodyAsString()}{(this.Trailers.Any() ? this.Trailers.ToString() : "")}";
    }
}
