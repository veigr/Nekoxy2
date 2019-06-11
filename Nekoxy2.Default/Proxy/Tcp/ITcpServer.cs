using System;
using System.Net;

namespace Nekoxy2.Default.Proxy.Tcp
{
    /// <summary>
    /// TCP サーバー
    /// </summary>
    internal interface ITcpServer
    {
        /// <summary>
        /// 待ち受け開始
        /// </summary>
        /// <param name="localAddress">待ち受けアドレス</param>
        /// <param name="port">待ち受けポート</param>
        void Startup(IPAddress localAddress, ushort port);

        /// <summary>
        /// 待ち受け終了
        /// </summary>
        void Shutdown();

        /// <summary>
        /// クライアントが接続された際に発生
        /// </summary>
        event Action<ITcpClient> AcceptTcpClient;

        /// <summary>
        /// 重大な例外がスローされた際に発生。
        /// 主に非同期の実行例外の捕捉用。
        /// </summary>
        event EventHandler<Exception> FatalException;
    }
}