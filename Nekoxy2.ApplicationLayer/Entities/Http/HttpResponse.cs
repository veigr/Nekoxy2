using Nekoxy2.Spi.Entities.Http;
using System.Linq;
using System.Text;

namespace Nekoxy2.ApplicationLayer.Entities.Http
{
    /// <summary>
    /// HTTP レスポンス
    /// </summary>
    internal sealed class HttpResponse : IReadOnlyHttpResponse
    {
        IReadOnlyHttpStatusLine IReadOnlyHttpResponse.StatusLine => this.StatusLine;

        public HttpStatusLine StatusLine { get; }

        IReadOnlyHttpHeaders IReadOnlyHttpMessage.Headers => this.Headers;

        public HttpHeaders Headers { get; }

        public byte[] Body { get; }

        IReadOnlyHttpHeaders IReadOnlyHttpMessage.Trailers => this.Trailers;

        public HttpHeaders Trailers { get; }

        public HttpResponse(HttpStatusLine statusLine, HttpHeaders headers, byte[] body, HttpHeaders trailers)
        {
            this.StatusLine = statusLine ?? HttpStatusLine.Empty;
            this.Headers = headers ?? HttpHeaders.Empty; ;
            this.Body = body;
            this.Trailers = trailers ?? HttpHeaders.Empty; ;
        }

        /// <summary>
        /// バイト配列として取得
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var h = this.StatusLine.ToString()
                + this.Headers.ToString();
            return Encoding.ASCII.GetBytes(h).Concat(this.Body).ToArray();
        }

        /// <summary>
        /// ヘッダーをバイト配列として取得
        /// </summary>
        /// <returns></returns>
        public byte[] HeadersToBytes()
            => Encoding.ASCII.GetBytes(this.StatusLine.ToString() + this.Headers.ToString());

        /// <summary>
        /// 操作による改変前のレスポンス
        /// </summary>
        /// <returns></returns>
        public HttpResponse GetOrigin()
            => new HttpResponse(this.StatusLine, this.Headers.GetOrigin(), this.Body, this.Trailers);

        public override string ToString()
            => $"{this.StatusLine}{this.Headers}{this.GetBodyAsString()}{(this.Trailers.Any() ? this.Trailers.ToString() : "")}";
    }
}
