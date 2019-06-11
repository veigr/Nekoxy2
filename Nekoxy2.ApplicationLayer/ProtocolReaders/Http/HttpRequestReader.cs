using Nekoxy2.ApplicationLayer.Entities.Http;
using System;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http
{
    /// <summary>
    /// 通信データを入力し HTTP/1.1 リクエストを読み取り
    /// </summary>
    internal sealed class HttpRequestReader : AbstractHttpReader
    {
        /// <summary>
        /// リクエストライン
        /// </summary>
        internal HttpRequestLine RequestLine { get; private set; }

        /// <summary>
        /// 設定を指定し、インスタンスを作成
        /// </summary>
        /// <param name="config">プロキシ設定</param>
        public HttpRequestReader(bool isCaptureBody, int maxCaptureSize) : base(isCaptureBody, maxCaptureSize) { }

        /// <summary>
        /// <see cref="ReceivedRequestHeaders"/> イベントを発生させます
        /// </summary>
        protected override void InvokeReceivedHeaders()
            => this.ReceivedRequestHeaders?.Invoke(this.GetRequest());

        /// <summary>
        /// <see cref="ReceivedRequestBody"/> イベントを発生させます
        /// </summary>
        protected override void InvokeReceivedBody()
            => this.ReceivedRequestBody?.Invoke(this.GetRequest());

        /// <summary>
        /// リクエストを取得
        /// </summary>
        /// <returns></returns>
        internal HttpRequest GetRequest()
            => new HttpRequest(this.RequestLine, this.Headers, this.Body, this.Trailers);

        /// <summary>
        /// スタートラインを解釈
        /// </summary>
        /// <param name="startLine"></param>
        protected override void ParseStartLine(string startLine)
        {
            var isParseSucceeded = HttpRequestLine.TryParse(startLine, out var requestLine);
            this.RequestLine = requestLine;
            if (!isParseSucceeded)
            {
                Console.WriteLine($"###start###{startLine}###end###");
                throw new BadRequestException("Invalid Request Line", this.GetRequest());
            }
        }

        /// <summary>
        /// ヘッダーを解釈
        /// </summary>
        /// <param name="headerString"></param>
        protected override void ParseHeaders(string headerString)
        {
            var isParseSucceeded = HttpHeaders.TryParse(headerString, out var headers);
            this.Headers = headers;
            if (!isParseSucceeded)
            {
                // RFC7230 3.3.3
                // Content-Length に問題がある場合は BadRequest
                throw new BadRequestException(headers.InvalidReason, this.GetRequest());
            }
        }

        /// <summary>
        /// リクエストヘッダー受信完了時に発生
        /// </summary>
        public event Action<HttpRequest> ReceivedRequestHeaders;

        /// <summary>
        /// リクエストボディー受信完了時に発生
        /// </summary>
        public event Action<HttpRequest> ReceivedRequestBody;
    }
}
