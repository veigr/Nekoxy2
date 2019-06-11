using System.Collections.Generic;
using System.Linq;
using System.Net.Security;

namespace Nekoxy2.Default.Proxy.Tls
{
    /// <summary>
    /// ALPN データ
    /// </summary>
    internal sealed class Alpn
    {
#if !NETSTANDARD2_0
        /// <summary>
        /// クライアントがリストアップしたプロトコルリスト
        /// </summary>
        public List<SslApplicationProtocol> ListOfProtocols { get; }

        /// <summary>
        /// サーバーが選択したプロトコル
        /// </summary>
        public SslApplicationProtocol SelectedProtocol { get; set; }

        public Alpn(IReadOnlyList<string> listOfProtocols)
        {
            this.ListOfProtocols = listOfProtocols
                .Select(x => new SslApplicationProtocol(x))
                .ToList();
        }
#endif
    }
}
