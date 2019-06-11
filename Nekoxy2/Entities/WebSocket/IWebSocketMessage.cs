using Nekoxy2.Entities.Http;
using System.Collections.Generic;

namespace Nekoxy2.Entities.WebSocket
{
    /// <summary>
    /// 読み取り専用 WebSocket メッセージ
    /// </summary>
    public interface IReadOnlyWebSocketMessage : Spi.Entities.WebSocket.IReadOnlyWebSocketMessage
    {
        /// <summary>
        /// ソース SPI インスタンス
        /// </summary>
        Spi.Entities.WebSocket.IReadOnlyWebSocketMessage Source { get; }

        /// <summary>
        /// ハンドシェイク HTTP セッション
        /// </summary>
        new IReadOnlySession HandshakeSession { get; }

        /// <summary>
        /// ペイロードデータの解釈
        /// </summary>
        new WebSocketOpcode Opcode { get; }

        /// <summary>
        /// ペイロードデータ
        /// </summary>
        new IReadOnlyList<byte> PayloadData { get; }
    }

    /// <summary>
    /// WebSocket メッセージ
    /// </summary>
    public interface IWebSocketMessage : Spi.Entities.WebSocket.IWebSocketMessage
    {
        /// <summary>
        /// ソース SPI インスタンス
        /// </summary>
        Spi.Entities.WebSocket.IWebSocketMessage Source { get; }

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
