using Nekoxy2.Default.Proxy;
using Nekoxy2.Default.Proxy.Tcp;
using Nekoxy2.Spi;
using Nekoxy2.Spi.Entities.Http;
using Nekoxy2.Spi.Entities.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Nekoxy2.Default
{
    /// <summary>
    /// Nekoxy2 既定のプロキシエンジン。
    /// クライアント - サーバー 1:1 コネクションの読み取り専用プロキシとして動作。
    /// 設定とコネクションリスト、イベントをコントロール。
    /// </summary>
    /// <remarks>
    /// <see cref="IReadOnlyHttpProxyEngine"/> のメンバーは API を通じて操作するため、このクラスでは明示的実装を行い非公開。
    /// </remarks>
    public sealed class DefaultEngine : IReadOnlyWebSocketProxyEngine
    {
        #region Configurations

        private readonly ProxyConfig config = new ProxyConfig();

        /// <summary>
        /// 上流プロキシ設定。
        /// </summary>
        public IUpstreamProxyConfig UpstreamProxyConfig
        {
            get => this.config.UpstreamProxyConfig;
            set => this.config.UpstreamProxyConfig = value;
        }

        /// <summary>
        /// 待ち受け設定
        /// </summary>
        public IListeningConfig ListeningConfig => this.config.ListeningConfig;

        /// <summary>
        /// HTTPS 復号化設定
        /// </summary>
        public IDecryptConfig DecryptConfig => this.config.DecryptConfig;

        /// <summary>
        /// キャプチャするボディやペイロード長の最大サイズ
        /// </summary>
        public int MaxCaptureSize
        {
            get => this.config.MaxCaptureSize;
            set => this.config.MaxCaptureSize = value;
        }

        /// <summary>
        /// ボディをキャプチャするかどうか
        /// </summary>
        public bool IsCaptureBody
        {
            get => this.config.IsCaptureBody;
            set => this.config.IsCaptureBody = value;
        }

        #endregion

        /// <summary>
        /// 待ち受けサーバー
        /// </summary>
        private readonly ITcpServer server;

        /// <summary>
        /// プロキシコネクションリスト
        /// </summary>
        internal readonly IList<ProxyConnection> connections = new List<ProxyConnection>();

        /// <summary>
        /// イベントを発生させる <see cref="SynchronizationContext"/>
        /// </summary>
        private readonly SynchronizationContext context;

        /// <summary>
        /// テスト向けのディレイ
        /// </summary>
        internal int DelayForTest = 0;

        internal DefaultEngine()
            : this(new TcpServer()) { }

        internal DefaultEngine(IListeningConfig listeningConfig)
            : this(new TcpServer(), listeningConfig) { }

        internal DefaultEngine(ITcpServer server, IListeningConfig listeningConfig = null)
        {
            if (listeningConfig != null)
                this.config.ListeningConfig = listeningConfig;

            this.config.DecryptConfig.ServerCertificateCreated += (sender, cert)
                => this.InvokeEvent(() => this.ServerCertificateCreated?.Invoke(sender, CertificateCreatedEventArgs.Create(cert)));
            
            this.server = server;
            this.server.FatalException += (sender, e)
                => this.InvokeEvent(() => this.FatalException?.Invoke(sender, ExceptionEventArgs.Create(e)));

            this.context = SynchronizationContext.Current;

            this.server.AcceptTcpClient += (acceptedClient) =>
            {
                var connection = new ProxyConnection(acceptedClient, this.config);
                // ハンドラ追加前に色々発生するイベントをつかめない問題があるため、Receive 開始はコンストラクタでやるべきではない。
                Task.Delay(this.DelayForTest).Wait();
                connection.Disposing += this.Connection_Disposing;
                lock (this.connections)
                {
                    connection.FatalException += this.Connection_FatalException;
                    connection.HttpRequestSent += this.Connection_HttpRequestSent;
                    connection.HttpResponseSent += this.Connection_HttpResponseSent;
                    connection.ClientWebSocketMessageSent += this.Connection_ClientWebSocketMessageSent;
                    connection.ServerWebSocketMessageSent += this.Connection_ServerWebSocketMessageSent;
                    this.connections.Add(connection);
                    var count = this.connections.Count;
                    this.InvokeEvent(() => this.ConnectionAdded?.Invoke(this, ConnectionCountChangedEventArgs.Create(count)));
                }
                connection.StartReceiving();
            };
        }

        #region  Connection Event Handlers

        private void Connection_Disposing(ProxyConnection connection)
        {
            connection.Disposing -= this.Connection_Disposing;
            lock (this.connections)
            {
                connection.FatalException -= this.Connection_FatalException;
                connection.HttpRequestSent -= this.Connection_HttpRequestSent;
                connection.HttpResponseSent -= this.Connection_HttpResponseSent;
                connection.ClientWebSocketMessageSent -= this.Connection_ClientWebSocketMessageSent;
                connection.ServerWebSocketMessageSent -= this.Connection_ServerWebSocketMessageSent;
                this.connections.Remove(connection);
                var count = this.connections.Count;
                this.InvokeEvent(() => this.ConnectionRemoved?.Invoke(this, ConnectionCountChangedEventArgs.Create(count)));
            }
        }

        private void Connection_HttpRequestSent(IReadOnlyHttpRequest request)
            => this.InvokeEvent(() => this.HttpRequestSent?.Invoke(this, ReadOnlyHttpRequestEventArgs.Create(request)));

        private void Connection_HttpResponseSent(IReadOnlySession session)
            => this.InvokeEvent(() => this.HttpResponseSent?.Invoke(this, ReadOnlySessionEventArgs.Create(session)));

        private void Connection_ClientWebSocketMessageSent(IReadOnlyWebSocketMessage message)
            => this.InvokeEvent(() => this.ClientWebSocketMessageSent?.Invoke(this, ReadOnlyWebSocketMessageEventArgs.Create(message)));

        private void Connection_ServerWebSocketMessageSent(IReadOnlyWebSocketMessage message)
            => this.InvokeEvent(() => this.ServerWebSocketMessageSent(this, ReadOnlyWebSocketMessageEventArgs.Create(message)));

        private void Connection_FatalException(object sender, Exception e)
            => this.InvokeEvent(() => this.FatalException?.Invoke(sender, ExceptionEventArgs.Create(e)));

        #endregion

        /// <summary>
        /// 待ち受け開始。
        /// API を通じてのみ操作するため、明示的実装。
        /// </summary>
        void IReadOnlyHttpProxyEngine.Start() => this.server.Startup(this.ListeningConfig.LocalAddress ?? IPAddress.Loopback, this.ListeningConfig.ListeningPort);

        /// <summary>
        /// 待ち受け終了。
        /// API を通じてのみ操作するため、明示的実装。
        /// </summary>
        void IReadOnlyHttpProxyEngine.Stop()
        {
            this.server.Shutdown();
            this.ClearConnections();
        }

        private void ClearConnections()
        {
            lock (this.connections)
            {
                foreach (var connection in this.connections.ToArray())
                {
                    // Disponse すると Connection_Disposing により connections から Remove される。
                    connection.Dispose();
                }
                this.connections.Clear();
            }
        }

        /// <summary>
        /// 適切なスレッドでイベントを発生させる
        /// </summary>
        /// <param name="invokeEventAction"></param>
        private void InvokeEvent(Action invokeEventAction)
        {
            // インスタンス作った時に SynchronizationContext があればそれでイベントを発生させる
            void invoke(object _)
            {
                try { invokeEventAction(); }
                catch (Exception e)
                {
                    /* 外部イベントハンドラ内の例外は無視する */
                    Debug.WriteLine(e);
                }
            }
            // イベントハンドラによって動作がブロックされないよう非同期で発生させる
            if (this.context != null)
                this.context.Post(invoke, null);
            else
                Task.Run(() => invoke(null)).ConfigureAwait(false);
        }

        #region Events

        private event EventHandler<IReadOnlyHttpRequestEventArgs> HttpRequestSent;
        event EventHandler<IReadOnlyHttpRequestEventArgs> IReadOnlyHttpProxyEngine.HttpRequestSent
        {
            add => this.HttpRequestSent += value;
            remove => this.HttpRequestSent -= value;
        }

        private event EventHandler<IReadOnlySessionEventArgs> HttpResponseSent;
        event EventHandler<IReadOnlySessionEventArgs> IReadOnlyHttpProxyEngine.HttpResponseSent
        {
            add => this.HttpResponseSent += value;
            remove => this.HttpResponseSent -= value;
        }

        private event EventHandler<IReadOnlyWebSocketMessageEventArgs> ClientWebSocketMessageSent;
        event EventHandler<IReadOnlyWebSocketMessageEventArgs> IReadOnlyWebSocketProxyEngine.ClientWebSocketMessageSent
        {
            add => this.ClientWebSocketMessageSent += value;
            remove => this.ClientWebSocketMessageSent -= value;
        }

        private event EventHandler<IReadOnlyWebSocketMessageEventArgs> ServerWebSocketMessageSent;
        event EventHandler<IReadOnlyWebSocketMessageEventArgs> IReadOnlyWebSocketProxyEngine.ServerWebSocketMessageSent
        {
            add => this.ServerWebSocketMessageSent += value;
            remove => this.ServerWebSocketMessageSent -= value;
        }

        private event EventHandler<IExceptionEventArgs> FatalException;
        event EventHandler<IExceptionEventArgs> IReadOnlyHttpProxyEngine.FatalException
        {
            add => this.FatalException += value;
            remove => this.FatalException -= value;
        }

        /// <summary>
        /// 接続が追加された際に接続数を通知
        /// </summary>
        public event EventHandler<IConnectionCountChangedEventArgs> ConnectionAdded;

        /// <summary>
        /// 接続が削除された際に接続数を通知
        /// </summary>
        public event EventHandler<IConnectionCountChangedEventArgs> ConnectionRemoved;

        /// <summary>
        /// サーバー証明書が作成された際に発生
        /// </summary>
        public event EventHandler<ICertificateCreatedEventArgs> ServerCertificateCreated;

        #endregion

        /// <summary>
        /// 待ち受け設定を指定し、インスタンスを作成
        /// </summary>
        /// <param name="config">待ち受け設定</param>
        /// <returns>既定のプロキシエンジン</returns>
        public static DefaultEngine Create(IListeningConfig config = null)
            => new DefaultEngine(config);
    }
}
