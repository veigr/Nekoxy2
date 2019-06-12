using Nekoxy2.ApplicationLayer.Entities.Http;
using System;
using System.Diagnostics;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http
{
    /// <summary>
    /// 通信データを入力し HTTP/1.1 レスポンスを読み取り
    /// </summary>
    internal sealed class HttpResponseReader : AbstractHttpReader
    {
        /// <summary>
        /// ステータスライン
        /// </summary>
        internal HttpStatusLine StatusLine { get; set; }

        /// <summary>
        /// 設定を指定し、インスタンスを作成
        /// </summary>
        /// <param name="config">プロキシ設定</param>
        public HttpResponseReader(bool isCaptureBody, int maxCaptureSize) : base(isCaptureBody, maxCaptureSize) { }

        /// <summary>
        /// <see cref="ReceivedResponseHeaders"/> イベントを発生させます
        /// </summary>
        protected override void InvokeReceivedHeaders()
            => this.ReceivedResponseHeaders?.Invoke(new HttpResponse(this.StatusLine, this.Headers, this.Body, this.Trailers));

        /// <summary>
        /// <see cref="ReceivedResponseBody"/> イベントを発生させます
        /// </summary>
        protected override void InvokeReceivedBody()
            => this.ReceivedResponseBody?.Invoke(new HttpResponse(this.StatusLine, this.Headers, this.Body, this.Trailers));

        /// <summary>
        /// スタートラインを解釈
        /// </summary>
        /// <param name="startLine"></param>
        protected override void ParseStartLine(string startLine)
        {
            var isParseSucceeded = HttpStatusLine.TryParse(startLine, out var statusLine);
            this.StatusLine = statusLine;
            if (!isParseSucceeded)
            {
                Debug.WriteLine($"###start###{startLine}###end###");
                throw new BadGatewayException("Invalid Status Line");
            }
        }

        /// <summary>
        /// ヘッダーを解釈
        /// </summary>
        /// <param name="headerString"></param>
        protected override void ParseHeaders(string headerString)
        {
            if (!HttpHeaders.TryParse(headerString, out var headers))
            {
                // RFC7230 3.3.3
                // Content-Length に問題がある場合は BadGateway
                Debug.WriteLine($"###start###{this.StatusLine}+++{headerString}###end###");
                throw new BadGatewayException(headers.InvalidReason);
            }
            this.Headers = headers;
        }

        /// <summary>
        /// レスポンスヘッダー受信完了時に発生
        /// </summary>
        public event Action<HttpResponse> ReceivedResponseHeaders;

        /// <summary>
        /// レスポンスボディー受信完了時に発生
        /// </summary>
        public event Action<HttpResponse> ReceivedResponseBody;
    }
}
