
using Nekoxy2.Spi.Entities.Http;

namespace Nekoxy2.ApplicationLayer.Entities.Http
{
    sealed partial class HttpHeaders : IReadOnlyHttpHeaders
    {
        private void ParseHeaders()
        {
            this.Host = new HttpHeaderField<string>(this.headers, "Host");
            this.ContentLength = new HttpHeaderField<decimal>(this.headers, "Content-Length");
            this.TransferEncoding = new HttpHeaderField<string>(this.headers, "Transfer-Encoding");
            this.ContentEncoding = new HttpHeaderField<string>(this.headers, "Content-Encoding");
            this.ContentType = new HttpHeaderField<string>(this.headers, "Content-Type");
            this.Connection = new HttpHeaderField<string>(this.headers, "Connection");
            this.MaxForwards = new HttpHeaderField<decimal>(this.headers, "Max-Forwards");
            this.Allow = new HttpHeaderField<string>(this.headers, "Allow");
            this.Upgrade = new HttpHeaderField<string>(this.headers, "Upgrade");
            this.SecWebSocketExtensions = new HttpHeaderField<string>(this.headers, "Sec-WebSocket-Extensions");
        }
        public HttpHeaderField<string> Host { get; private set; }
        public HttpHeaderField<decimal> ContentLength { get; private set; }
        public HttpHeaderField<string> TransferEncoding { get; private set; }
        public HttpHeaderField<string> ContentEncoding { get; private set; }
        public HttpHeaderField<string> ContentType { get; private set; }
        public HttpHeaderField<string> Connection { get; private set; }
        public HttpHeaderField<decimal> MaxForwards { get; private set; }
        public HttpHeaderField<string> Allow { get; private set; }
        public HttpHeaderField<string> Upgrade { get; private set; }
        public HttpHeaderField<string> SecWebSocketExtensions { get; private set; }
    }
}