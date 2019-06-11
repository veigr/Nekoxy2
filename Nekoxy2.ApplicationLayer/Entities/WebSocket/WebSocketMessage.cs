using Nekoxy2.Spi.Entities.Http;
using Nekoxy2.Spi.Entities.WebSocket;
using System;

namespace Nekoxy2.ApplicationLayer.Entities.WebSocket
{
    /// <summary>
    /// WebSocket メッセージ
    /// </summary>
    internal sealed class WebSocketMessage : IReadOnlyWebSocketMessage
    {
        /// <summary>
        /// ハンドシェイク HTTP セッション
        /// </summary>
        public IReadOnlySession HandshakeSession { get; }

        /// <summary>
        /// ペイロードデータの解釈
        /// </summary>
        public WebSocketOpcode Opcode { get; }

        /// <summary>
        /// ペイロードデータ
        /// </summary>
        public byte[] PayloadData { get; }

        /// <summary>
        /// 圧縮されているかどうか
        /// </summary>
        internal bool IsCompressed { get; }

        /// <summary>
        /// 結合されたフレームデータからメッセージを作成
        /// </summary>
        /// <param name="handshakeSession"></param>
        /// <param name="opcode"></param>
        /// <param name="payloadData"></param>
        /// <param name="isCompressed"></param>
        public WebSocketMessage(IReadOnlySession handshakeSession, WebSocketOpcode opcode, byte[] payloadData, bool isCompressed)
        {
            this.HandshakeSession = handshakeSession;
            this.Opcode = opcode;
            this.PayloadData = payloadData;
            this.IsCompressed = isCompressed;
        }

        /// <summary>
        /// 単一フレームからメッセージを作成。
        /// Continuation フレームや FIN フラグが立っていないフレームからは作成できません。
        /// </summary>
        /// <param name="handshakeSession"></param>
        /// <param name="frame"></param>
        public WebSocketMessage(IReadOnlySession handshakeSession, WebSocketFrame frame)
        {
            if (!frame.Fin || frame.Opcode == WebSocketOpcode.Continuation)
                throw new ArgumentException($"WebSocketMessage can not create from not FIN or Continuation Frame.\r\n{frame}");
            this.HandshakeSession = handshakeSession;
            this.Opcode = frame.Opcode;
            this.PayloadData = frame.PayloadData;
            this.IsCompressed = frame.Rsv1;
        }
    }
}
