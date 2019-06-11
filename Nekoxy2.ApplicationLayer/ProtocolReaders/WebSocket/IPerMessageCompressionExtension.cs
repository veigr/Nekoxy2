using Nekoxy2.ApplicationLayer.Entities.WebSocket;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.WebSocket
{
    /// <summary>
    /// Per-Message Compress Extension により圧縮された <see cref="WebSocketMessage"/> を展開
    /// </summary>
    /// <remarks>
    /// RFC7692
    /// </remarks>
    internal interface IPerMessageCompressionExtension
    {
        /// <summary>
        /// 拡張名
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 圧縮された <see cref="WebSocketMessage"/> を展開
        /// </summary>
        /// <param name="message">圧縮された <see cref="WebSocketMessage"/></param>
        /// <returns>展開された <see cref="WebSocketMessage"/></returns>
        WebSocketMessage Decompress(WebSocketMessage message);
    }
}
