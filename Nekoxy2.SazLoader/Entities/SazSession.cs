using Nekoxy2.SazLoader.Deserialization;
using Nekoxy2.SazLoader.Entities.Http;
using Nekoxy2.SazLoader.Entities.WebSocket;
using Nekoxy2.Spi.Entities.Http;
using System.Collections.Generic;

namespace Nekoxy2.SazLoader.Entities
{
    /// <summary>
    /// SAZ セッション
    /// </summary>
    internal sealed class SazSession : IReadOnlySession
    {
        /// <summary>
        /// セッション番号
        /// </summary>
        public int Number { get; }

        /// <summary>
        /// メタデータ
        /// </summary>
        public Metadata Metadata { get; }

        /// <summary>
        /// リクエストバイナリ
        /// </summary>
        public IReadOnlyList<byte> RequestBytes { get; }

        /// <summary>
        /// レスポンスバイナリ
        /// </summary>
        public IReadOnlyList<byte> ResponseBytes { get; }

        /// <summary>
        /// リクエスト
        /// </summary>
        public IReadOnlyHttpRequest Request { get; }

        /// <summary>
        /// レスポンス
        /// </summary>
        public IReadOnlyHttpResponse Response { get; }

        /// <summary>
        /// ハンドシェイクセッションに紐付く WebSocket フレームリスト
        /// </summary>
        public IReadOnlyList<SazWebSocketFrame> WebSocketFrames { get; }

        public SazSession() { }

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="number">セッション番号</param>
        /// <param name="metadata">*_m.xml ファイルから変換したメタデータ</param>
        /// <param name="requestBytes">*_c.txt ファイルデータ</param>
        /// <param name="responseBytes">*_s.txt ファイルデータ</param>
        /// <param name="webSocketBytes">*_w.txt ファイルデータ</param>
        public SazSession(int number, Metadata metadata, byte[] requestBytes, byte[] responseBytes, byte[] webSocketBytes)
        {
            this.Number = number;
            this.Metadata = metadata;
            this.RequestBytes = requestBytes;
            this.ResponseBytes = responseBytes;
            this.Request = SazHttpRequest.Parse(requestBytes);
            this.Response = SazHttpResponse.Parse(responseBytes);
            this.WebSocketFrames = SazWebSocketFormatParser.Parse(webSocketBytes);
        }
    }
}
