using System.IO;
using System.Net.Sockets;

namespace Nekoxy2.Default.Proxy.Tcp
{
    /// <summary>
    /// <see cref="TcpClient"/> を用いて実装した <see cref="ITcpClient"/>
    /// </summary>
    internal sealed class TcpClientWrapper : ITcpClient
    {
        /// <summary>
        /// 基となる <see cref="TcpClient"/>
        /// </summary>
        public TcpClient Source { get; }

        /// <summary>
        /// 接続しているかどうか
        /// </summary>
        public bool Connected
            => this.Source.Connected;

        /// <summary>
        /// 切断状態
        /// </summary>
        public CloseState CloseState { get; private set; }

        public TcpClientWrapper(TcpClient source)
        {
            this.Source = source;
            this.Source.NoDelay = true;
        }

        public TcpClientWrapper(string host, int port)
            : this(new TcpClient(host, port)) { }

        /// <summary>
        /// 通信ストリーム
        /// </summary>
        /// <returns></returns>
        public Stream GetStream()
            => this.Source.GetStream();

        /// <summary>
        /// 切断
        /// </summary>
        /// <param name="how">切断対象</param>
        public void Shutdown(SocketShutdown how)
        {
            try
            {
                this.Source.Client.Shutdown(how);
                // half close になったら60秒タイムアウトを設定する
                this.Source.ReceiveTimeout = 60 * 1000;
                this.Source.SendTimeout = 60 * 1000;
                switch (how)
                {
                    case SocketShutdown.Receive:
                        this.CloseState |= CloseState.ReceiveClosed;
                        break;
                    case SocketShutdown.Send:
                        this.CloseState |= CloseState.SendClosed;
                        break;
                    case SocketShutdown.Both:
                        this.CloseState |= CloseState.Both;
                        break;
                }
            }
            catch
            {
                this.CloseState = CloseState.Both;
                this.Dispose();
            }
        }

        public void Dispose()
            => this.Source.Dispose();
    }
}
