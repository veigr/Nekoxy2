using Nekoxy2.ApplicationLayer.Entities.WebSocket;
using System.Collections.Generic;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.WebSocket
{
    /// <summary>
    /// 複数指定された Per-Message Compression Extension (PMCE) を <see cref="WebSocketMessage"/> に適用
    /// </summary>
    /// <remarks>
    /// RFC7692 5
    /// </remarks>
    internal sealed class AggregatePMCE
    {
        /// <summary>
        /// 指定された PMCE リスト
        /// </summary>
        private readonly IEnumerable<IPerMessageCompressionExtension> pmces;

        /// <summary>
        /// PMCE リストを指定してインスタンスを作成
        /// </summary>
        /// <param name="pmces">PMCE リスト</param>
        public AggregatePMCE(IEnumerable<IPerMessageCompressionExtension> pmces)
            => this.pmces = pmces ?? new IPerMessageCompressionExtension[0];

        /// <summary>
        /// 圧縮された <see cref="WebSocketMessage"/> を展開
        /// </summary>
        /// <param name="message">圧縮された <see cref="WebSocketMessage"/></param>
        /// <returns>展開された <see cref="WebSocketMessage"/></returns>
        public WebSocketMessage Decompress(WebSocketMessage message)
            => this.pmces.Aggregate(message,
                (prevResult, pmce) => prevResult.IsCompressed ? pmce.Decompress(prevResult) : prevResult);
    }
}
