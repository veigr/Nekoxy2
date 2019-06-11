using System.Net;

namespace Nekoxy2.Default
{
    /// <summary>
    /// 待ち受け設定
    /// </summary>
    public interface IListeningConfig
    {
        /// <summary>
        /// 待ち受けアドレス
        /// </summary>
        IPAddress LocalAddress { get; }
        /// <summary>
        /// 待ち受けポート
        /// </summary>
        ushort ListeningPort { get; }
    }

    /// <summary>
    /// 待ち受け設定
    /// </summary>
    public sealed class ListeningConfig : IListeningConfig
    {
        /// <summary>
        /// 待ち受けアドレス
        /// </summary>
        public IPAddress LocalAddress { get; } = IPAddress.Loopback;

        /// <summary>
        /// 待ち受けポート
        /// </summary>
        public ushort ListeningPort { get; } = 8080;

        /// <summary>
        /// 127.0.0.1:8080 で待ち受け
        /// </summary>
        public ListeningConfig() { }

        /// <summary>
        /// 127.0.0.1 でポートを指定して待ち受け
        /// </summary>
        /// <param name="listeningPort">待ち受けポート</param>
        public ListeningConfig(ushort listeningPort)
            => this.ListeningPort = listeningPort;

        /// <summary>
        /// アドレスとポートを指定して待ち受け
        /// </summary>
        /// <param name="localAddress">待ち受けアドレス</param>
        /// <param name="listeningPort">待ち受けポート</param>
        public ListeningConfig(IPAddress localAddress, ushort listeningPort)
        {
            this.LocalAddress = localAddress;
            this.ListeningPort = listeningPort;
        }
    }
}
