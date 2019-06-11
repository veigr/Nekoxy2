using Nekoxy2.ApplicationLayer.Entities.WebSocket;
using System.IO;
using System.IO.Compression;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.WebSocket
{
    /// <summary>
    /// permessage-deflate により圧縮された <see cref="WebSocketMessage"/> を展開
    /// </summary>
    /// <remarks>
    /// RFC7692 7
    /// </remarks>
    internal sealed class PerMessageDeflateExtension : IPerMessageCompressionExtension
    {
        /// <summary>
        /// 拡張名
        /// </summary>
        public string Name => "permessage-deflate";

        /// <summary>
        /// 圧縮された <see cref="WebSocketMessage"/> を展開
        /// </summary>
        /// <param name="message">圧縮された <see cref="WebSocketMessage"/></param>
        /// <returns>展開された <see cref="WebSocketMessage"/></returns>
        public WebSocketMessage Decompress(WebSocketMessage message)
        {
            using (var dest = new MemoryStream())
            using (var source = new MemoryStream(message.PayloadData))
            {
                using (var deflate = new DeflateStream(source, CompressionMode.Decompress))
                {
                    deflate.CopyTo(dest);
                }
                return new WebSocketMessage(message.HandshakeSession, message.Opcode, dest.ToArray(), false);
            }
        }
    }
}
