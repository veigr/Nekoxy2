using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Nekoxy2.Default.Proxy.Tcp
{
    /// <summary>
    /// <see cref="TcpListener"/> を用いて実装した <see cref="ITcpServer"/>
    /// </summary>
    internal sealed class TcpServer : ITcpServer
    {
        /// <summary>
        /// 基となる <see cref="TcpListener"/>
        /// </summary>
        private TcpListener listener;

        /// <summary>
        /// クライアント接続時に <see cref="ITcpClient"/> を生成する関数
        /// </summary>
        internal Func<TcpClient, ITcpClient> CreateTcpClientForClient { get; set; }
            = client => new TcpClientWrapper(client);

        /// <summary>
        /// 待ち受け開始
        /// </summary>
        /// <param name="localAddress">待ち受けアドレス</param>
        /// <param name="port">待ち受けポート</param>
        public void Startup(IPAddress localAddress, ushort port)
        {
            this.listener = new TcpListener(localAddress, port);
            this.listener.Start();
            new Func<bool>(() =>
            {
                this.AcceptClient();
                return this.listener?.Server?.Connected == true;
            })
            .RunAutoRestartTask();
        }

        private void AcceptClient()
        {
            Debug.WriteLine($"### Start listening. {this.listener?.Server.LocalEndPoint}");
            try
            {
                while (true)
                {
                    var client = this.listener.AcceptTcpClient();
                    this.AcceptTcpClient?.Invoke(this.CreateTcpClientForClient(client));
                }
            }
            catch (Exception e)
            {
                if (!e.Has<SocketException>()
                && !e.Has<ObjectDisposedException>())
                {
                    this.FatalException?.Invoke(this, e);
                }
            }
            finally
            {
                this.Shutdown();
            }
        }

        /// <summary>
        /// 待ち受け終了
        /// </summary>
        public void Shutdown()
        {
            this.listener.Server.Close();
            this.listener.Stop();
            this.listener = null;
        }

        /// <summary>
        /// クライアントが接続された際に発生
        /// </summary>
        public event Action<ITcpClient> AcceptTcpClient;

        /// <summary>
        /// 重大な例外がスローされた際に発生。
        /// 主に非同期の実行例外の捕捉用。
        /// </summary>
        public event EventHandler<Exception> FatalException;
    }
}
