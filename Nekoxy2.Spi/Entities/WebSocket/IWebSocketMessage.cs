using Nekoxy2.Spi.Entities.Http;

namespace Nekoxy2.Spi.Entities.WebSocket
{
    /// <summary>
    /// 読み取り専用 WebSocket メッセージ
    /// </summary>
    public interface IReadOnlyWebSocketMessage
    {
        /// <summary>
        /// ハンドシェイク HTTP セッション
        /// </summary>
        IReadOnlySession HandshakeSession { get; }

        /// <summary>
        /// ペイロードデータの解釈
        /// </summary>
        WebSocketOpcode Opcode { get; }

        /// <summary>
        /// ペイロードデータ
        /// </summary>
        byte[] PayloadData { get; }
    }

    /// <summary>
    /// WebSocket メッセージ
    /// </summary>
    public interface IWebSocketMessage : IReadOnlyWebSocketMessage
    {
        /// <summary>
        /// ハンドシェイク HTTP セッション
        /// </summary>
        new IReadOnlySession HandshakeSession { get; }

        /// <summary>
        /// ペイロードデータの解釈
        /// </summary>
        new WebSocketOpcode Opcode { get; set; }

        /// <summary>
        /// ペイロードデータ
        /// </summary>
        new byte[] PayloadData { get; set; }
    }
}
