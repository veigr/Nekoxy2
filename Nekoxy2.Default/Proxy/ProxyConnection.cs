using Nekoxy2.ApplicationLayer.Entities.Http;
using Nekoxy2.ApplicationLayer.ProtocolReaders.Http2;
using Nekoxy2.Default.Proxy.Tcp;
using Nekoxy2.Spi.Entities.Http;
using Nekoxy2.Spi.Entities.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace Nekoxy2.Default.Proxy
{
    /// <summary>
    /// プロキシコネクション。
    /// クライアント - サーバーコネクション間の協調を実施。
    /// </summary>
    internal sealed class ProxyConnection : IDisposable
    {
        /// <summary>
        /// クライアント側コネクション
        /// </summary>
        internal readonly ClientConnection clientConnection;

        /// <summary>
        /// サーバー側コネクション
        /// </summary>
        internal ServerConnection serverConnection;

        /// <summary>
        /// 接続先サーバーホスト名
        /// </summary>
        private string connectedServerHost;

        /// <summary>
        /// 接続先サーバーポート番号
        /// </summary>
        private ushort connectedServerPort;

        /// <summary>
        /// プロキシ設定
        /// </summary>
        private readonly ProxyConfig config;

        /// <summary>
        /// Request/Response の関連付けに使うキュー
        /// </summary>
        /// <remarks>
        /// RFC7230 5.6
        /// Close せずに再試行してくることはないので、Queue で大丈夫 RFC7230 6.3.1
        /// </remarks>
        private readonly ConcurrentQueue<HttpRequest> requestQueue = new ConcurrentQueue<HttpRequest>();

        /// <summary>
        /// HTTP/2 プロトコルリーダー。
        /// クライアント送信フレーム、サーバー送信フレームを読み取り、HTTP セマンティクスへマッピング。
        /// </summary>
        private readonly Http2Reader http2Reader;

        /// <summary>
        /// 待ち受け後処理ロック。
        /// クライアント・サーバーで共有。
        /// </summary>
        private readonly SemaphoreQueue receiveSharedLock = new SemaphoreQueue(1, 1);

        /// <summary>
        /// 処理ロック
        /// </summary>
        private readonly object processLock = new object();

        /// <summary>
        /// Dispose ロック
        /// </summary>
        private readonly object disposeLock = new object();

        /// <summary>
        /// サーバー側コネクション向け <see cref="ITcpClient"/> 作成関数
        /// </summary>
        internal Func<string, int, ITcpClient> CreateTcpClientForServer { get; set; }
            = (host, port) => new TcpClientWrapper(host, port);

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="acceptedClient">クライアント側コネクション向け<see cref="ITcpClient"/></param>
        /// <param name="config">プロキシ設定</param>
        public ProxyConnection(ITcpClient acceptedClient, ProxyConfig config = null)
        {
            this.DebugWriteLine($"accept client tcp");

            this.config = config ?? new ProxyConfig();

            this.clientConnection = new ClientConnection(acceptedClient, config, this.receiveSharedLock);
            this.clientConnection.ReceivedRequestHeaders += this.ProcessRequestHeaders;
            this.clientConnection.ReceivedRequestBody += this.ProcessRequestBody;
            this.clientConnection.ManualResponseSent += session => this.HttpResponseSent?.Invoke(session);
            this.clientConnection.ReceiveClosed += () =>
            {
                this.DebugWriteLine($"client receive closed.");
                this.serverConnection?.OnOtherReceiveClosed();
            };
            this.clientConnection.Closed += () =>
            {
                this.DebugWriteLine($"client connection closed.");
                this.serverConnection?.Dispose();
                this.Dispose();
            };
            this.clientConnection.FatalException += (sender, e) => this.FatalException?.Invoke(sender, e);

            this.http2Reader = new Http2Reader(config?.MaxCaptureSize ?? int.MaxValue);
            this.http2Reader.HttpRequestSent += request => this.HttpRequestSent?.Invoke(request);
            this.http2Reader.HttpResponseSent += session => this.HttpResponseSent?.Invoke(session);
            this.http2Reader.ClientWebSocketMessageSent += message => this.ClientWebSocketMessageSent?.Invoke(message);
            this.http2Reader.ServerWebSocketMessageSent += message => this.ServerWebSocketMessageSent?.Invoke(message);
            this.clientConnection.ChallengeToSsl += args =>
            {
                if (args.IsDecrypt && args.Alpn != default)
                {
                    this.clientConnection.Http2FrameReceived += this.http2Reader.HandleRequest;
                    this.serverConnection.Http2FrameReceived += this.http2Reader.HandleResponse;
                }
            };
        }

        /// <summary>
        /// 待ち受け開始
        /// </summary>
        public void StartReceiving() => this.clientConnection.StartReceiving();

        #region ProcessRequestHeaders

        /// <summary>
        /// リクエストヘッダー回送
        /// </summary>
        /// <param name="request"></param>
        private void ProcessRequestHeaders(HttpRequest request)
        {
            lock (this.processLock)
            {
                this.DebugWriteLine($"{nameof(ProcessRequestHeaders)}: {request}");

                if (!this.ResolveRouting(request))
                    return;

                if (!this.TryCreateServerConnection(request))
                    return;

                if (this.HandleConnect(request))
                    return;

                this.requestQueue.Enqueue(request);
                this.serverConnection.SendRequestHeaders(request);
                this.HttpRequestHeadersSent?.Invoke(request);

                this.serverConnection.IsPauseBeforeReceive = false;
            }
        }

        /// <summary>
        /// ルーティング解決
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private bool ResolveRouting(HttpRequest request)
        {
            // CONNECT OPTIONS 以外は absolute-form
            // CONNECT では authority-form
            // OPTIONS では asterisk-form

            if (request.RequestLine.RequestTargetForm == RequestTargetForm.AbsoluteForm
            && !this.config.UpstreamProxyConfig.IsEnabled(request.RequestTargetUri))
            {
                if (request.RequestTargetUri.PathAndQuery == "/"
                && request.RequestLine.Method == HttpMethod.Options)
                {
                    // RFC7230 5.3.4
                    request.ChangeRequestTarget("*");
                }
                else
                {
                    // RFC2616でも7230でも absolute-uri はそのまま回送することになっているし、
                    // サーバーもそれを受け入れなければならないことになっているが、
                    // 実際には送信するとおかしな挙動をするサーバーが多いため仕方なく origin-form に変換する
                    request.ChangeRequestTarget(request.RequestTargetUri.PathAndQuery);
                }
            }
            if (request.RequestLine.RequestTargetForm == RequestTargetForm.OriginForm
            || request.RequestLine.RequestTargetForm == RequestTargetForm.AsteriskForm)
            {
                if (!request.Headers.Host.Exists)
                {
                    this.clientConnection.SendResponse(request, HttpStatusCode.BadRequest, "Host Header Not Exists");
                    this.Dispose();
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// サーバー側コネクション作成
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private bool TryCreateServerConnection(HttpRequest request)
        {
            try
            {
                string host;
                ushort port;
                if (!this.config.UpstreamProxyConfig.IsEnabled(request.RequestTargetUri))
                {
                    var authority = request.Headers.Host.ParseAuthority();
                    host = authority.Host;
                    port = authority.Port ?? 0;
                    if (port == 0)
                    {
                        port = request.RequestLine.Method.Method.ToUpper() == "CONNECT"
                            ? (ushort)443
                            : (ushort)80;
                    }
                }
                else
                {
                    var proxy = this.config.UpstreamProxyConfig.GetProxy(request.RequestTargetUri);
                    host = proxy.Host;
                    port = proxy.Port;
                }

                if (this.connectedServerHost != null)
                {
                    if (this.connectedServerHost == host && this.connectedServerPort == port)
                    {
                        return true;
                    }
                    else
                    {
                        // 1:1 コネクションな実装なので、異なる接続先が要求された場合は Close して再試行を促す。
                        // パイプライン化クライアントでなければ 1:N 接続は可能だろうが、クライアントの実装によっては通信の取り違えが発生する可能性があり
                        // 予防するために切断したほうが良さそう
                        // パイプライン化対応せずにブロッキングしていれば問題なく通信できそうだが、見通しの悪い実装になりそうで……
                        this.Dispose();
                        return false;
                    }
                }

                this.connectedServerHost = host;
                this.connectedServerPort = port;
                this.serverConnection = new ServerConnection(
                    this.CreateTcpClientForServer(this.connectedServerHost,
                    this.connectedServerPort),
                    this.config,
                    this.receiveSharedLock);
            }
            catch (SocketException)
            {
                // 接続できない場合
                this.clientConnection.SendResponse(request, HttpStatusCode.BadGateway, "Could Not Connect To Server");
                this.Dispose();
                return false;
            }

            this.DebugWriteLine($"connect server connection.");

            this.serverConnection.FatalException += (sender, e) => this.FatalException?.Invoke(sender, e);
            this.serverConnection.ReceiveClosed += () =>
            {
                this.DebugWriteLine($"server recieve closed.");
                this.clientConnection.OnOtherReceiveClosed();
            };
            this.serverConnection.Closed += () =>
            {
                this.DebugWriteLine($"server connection closed.");
                this.clientConnection.Dispose();
                this.Dispose();
            };
            this.serverConnection.BadGateway += reason =>
            {
                this.DebugWriteLine($"server connection close with Bad Gateway.");
                this.clientConnection.SendResponse(request, HttpStatusCode.BadGateway, reason);
                this.clientConnection.Dispose();
                this.Dispose();
            };
            this.serverConnection.ReceivedResponseHeaders += this.ProcessResponseHeaders;
            this.serverConnection.ReceivedResponseBody += this.ProcessResponseBody;
            this.clientConnection.ReadBody += this.ClientReadBody;
            this.serverConnection.ReadBody += this.ServerReadBody;
            this.clientConnection.ChallengeToSsl += args => this.serverConnection.EnsureServerSsl(args.IsTls, args.IsDecrypt, args.Alpn);

            this.serverConnection.StartReceiving();

            return true;
        }

        /// <summary>
        /// リクエストボディー回送
        /// </summary>
        /// <param name="data"></param>
        private void ClientReadBody((byte[] buffer, int readSize) data)
            => this.serverConnection.Write(data.buffer, data.readSize);

        /// <summary>
        /// レスポンスボディー回送
        /// </summary>
        /// <param name="data"></param>
        private void ServerReadBody((byte[] buffer, int readSize) data)
            => this.clientConnection.Write(data.buffer, data.readSize);

        /// <summary>
        /// CONNECT リクエストを検証
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private bool HandleConnect(HttpRequest request)
        {
            if (request.RequestLine.Method.Method.ToUpper() != "CONNECT")
                return false;

            // 上流プロキシがいる場合は CONNECT のままでトンネルにしない
            if (this.config.UpstreamProxyConfig.IsEnabled(request.RequestTargetUri))
                return false;

            // 200 を返してトンネルモードに
            this.ChangeToTunnelMode(request.RequestTargetUri.Host);
            this.clientConnection.SendResponse(request, HttpStatusCode.OK, "Connection Established");

            if (!this.config.DecryptConfig.IsDecrypt)
                this.serverConnection.IsPauseBeforeReceive = false;

            return true;
        }

        #endregion

        /// <summary>
        /// リクエストボディー回送
        /// </summary>
        /// <param name="request"></param>
        /// <remarks>
        /// ボディーデータ自体は <see cref="ClientReadBody((byte[] buffer, int readSize))"/> で自動的に回送
        /// </remarks>
        private void ProcessRequestBody(HttpRequest request)
        {
            this.DebugWriteLine($"{nameof(ProcessRequestBody)}: {request}");
            this.requestQueue.Enqueue(request);
            this.HttpRequestSent?.Invoke(request);
        }

        /// <summary>
        /// レスポンスヘッダー回送
        /// </summary>
        /// <param name="response"></param>
        private void ProcessResponseHeaders(HttpResponse response)
        {
            this.DebugWriteLine($"{nameof(ProcessResponseHeaders)}: {response.StatusLine}{response.Headers}");

            if (200 <= (int)response.StatusLine.StatusCode)
            {
                if (!this.requestQueue.TryDequeue(out var request))
                {
                    // これ失敗したら回復不能……
                    this.Dispose();
                    return;
                }

                if (request.RequestLine.Method.Method.ToUpper() == "CONNECT"
                && (int)response.StatusLine.StatusCode <= 299)
                {
                    // 上流プロキシからの CONNECT 成功通知でトンネルモードにする
                    this.ChangeToTunnelMode(request.RequestTargetUri.Host);
                    if (this.config.DecryptConfig.IsDecrypt)
                        this.serverConnection.IsPauseBeforeReceive = true;
                }
            }

            this.clientConnection.SendResponseHeaders(response);
        }

        /// <summary>
        /// レスポンスボディー回送
        /// </summary>
        /// <param name="response"></param>
        /// <remarks>
        /// ボディーデータ自体は <see cref="ServerReadBody((byte[] buffer, int readSize))"/> で自動的に回送
        /// </remarks>
        private void ProcessResponseBody(HttpResponse response)
        {
            this.DebugWriteLine($"{nameof(ProcessResponseBody)}: {response?.Body?.Length} bytes.");

            // 1xx 応答は無視する。HTTP/1.0 には存在しないのでここで返して問題ない。
            if ((int)response.StatusLine.StatusCode < 200)
                return;

            if (!this.requestQueue.TryDequeue(out var request))
            {
                this.Dispose();
                return;
            }


            this.HttpResponseSent?.Invoke(new Session(request.GetOrigin(), response.GetOrigin()));

            if (this.clientConnection.IsTunnelMode
            || this.serverConnection.IsTunnelMode)
                return;

            if (request.Headers.IsClose
            || response.Headers.IsClose
            || request.RequestLine.HttpVersion == HttpVersion.Version10
            || response.StatusLine.HttpVersion == HttpVersion.Version10)
            {
                this.DebugWriteLine("Close connection.");
                // RFC7230 6.6 Headers.IsClose の場合はいきなり切断するのではなくクライアントから切断されるのを待つの方法もあるが、
                // ReceivedRequestBody は Response 転送完了後に発生するため、ここで切断して問題ない
                this.Dispose();
            }
        }

        /// <summary>
        /// CONNECT トンネルモードへ変更
        /// </summary>
        /// <param name="host">接続先サーバーホスト名</param>
        private void ChangeToTunnelMode(string host)
        {
            this.DebugWriteLine("change to tunnel mode.");
            this.clientConnection.ReceivedRequestHeaders -= this.ProcessRequestHeaders;
            this.clientConnection.ReceivedRequestBody -= this.ProcessRequestBody;
            this.clientConnection.ReceivedRequestBody += request =>
            {
                if (request.RequestLine.Method.Method != "CONNECT")
                    this.requestQueue.Enqueue(request);
            };
            this.clientConnection.ReceivedRequestBody += request => this.HttpRequestSent?.Invoke(request);
            this.clientConnection.ReadBody -= this.ClientReadBody;
            this.clientConnection.ReadTunnel += buffer => this.serverConnection.Write(buffer.buffer, buffer.readSize);

            this.serverConnection.ReceivedResponseHeaders -= this.ProcessResponseHeaders;
            this.serverConnection.ReceivedResponseBody -= this.ProcessResponseBody;
            this.serverConnection.ReceivedResponseBody += this.TunnelResponseBody;
            this.serverConnection.ReadBody -= this.ServerReadBody;
            this.serverConnection.ReadTunnel += buffer => this.clientConnection.Write(buffer.buffer, buffer.readSize);

            this.clientConnection.IsTunnelMode = true;
            this.serverConnection.IsTunnelMode = true;
            this.clientConnection.TunneledHost = host;
            this.serverConnection.TunneledHost = host;
        }

        /// <summary>
        /// トンネルモード時のレスポンスボディー回送
        /// </summary>
        /// <param name="response"></param>
        private void TunnelResponseBody(HttpResponse response)
        {
            var responseOrigin = response.GetOrigin();
            if (!this.requestQueue.TryDequeue(out var request))
                return;

            var session = new Session(request.GetOrigin(), responseOrigin);

            // Upgrade 応答 RFC7230 8.6
            if (responseOrigin.StatusLine.StatusCode == HttpStatusCode.SwitchingProtocols)
            {
                foreach (var upgrade in responseOrigin.Headers.Upgrade)
                {
                    // IANA Upgrade Token Registry に基づいて判断 RFC7230 8.6
                    // https://www.iana.org/assignments/http-upgrade-tokens/http-upgrade-tokens.xhtml
                    if (upgrade.ToLower() == "websocket")   // 基本大小無視 RFC6455 4
                    {
                        this.DebugWriteLine("Swicth Protocol to WebSocket");
                        try
                        {
                            this.clientConnection.ChangeToWebSocket(session);
                            this.serverConnection.ChangeToWebSocket(session);
                            this.clientConnection.WebSocketMessageReceived += m => this.ClientWebSocketMessageSent?.Invoke(m);
                            this.serverConnection.WebSocketMessageReceived += m => this.ServerWebSocketMessageSent?.Invoke(m);
                        }
                        catch (Exception e)
                        {
                            this.FatalException?.Invoke(this, e);
                        }
                        break;
                    }
                    else
                    {
                        // HTTP/2 は http でのみ Upgrade を用い、https では ALPN を利用するため、SslStream での検知が必要となる
                        // しかし SslStream の ALPN サポートは Core 2.1 以降であり、.NET Standard 2.0 では利用できない
                        // HTTP/2 クライアントの大半は https しかサポートしていないため、現時点ではクリアテキスト HTTP/2(h2c) 対応は行わない

                        // RFC2817 によると Upgrade: TLS/1.0, HTTP/1.1 とかあるらしいが、ほぼ実装されてないらしい上にトンネル内はもっと無いと思われるので無視
                        this.clientConnection.ChangeToUnknownProtocol();
                        this.serverConnection.ChangeToUnknownProtocol();
                    }
                }
            }

            this.HttpResponseSent?.Invoke(session);
        }

        /// <summary>
        /// デバッグ出力
        /// </summary>
        /// <param name="v"></param>
        private void DebugWriteLine(string v)
        {
#if DEBUG
            try
            {
                var clientEndPoint = (this.clientConnection?.client as TcpClientWrapper)?.Source?.Client?.RemoteEndPoint;
                var serverEndPoint = (this.serverConnection?.client as TcpClientWrapper)?.Source?.Client?.RemoteEndPoint;
                Debug.WriteLine($"### {clientEndPoint}<->{serverEndPoint}: {v}");
            }
            catch (ObjectDisposedException)
            {
                Debug.WriteLine($"### {v}");
            }
#endif
        }

        /// <summary>
        /// <see cref="Dispose"/> 実行時に発生
        /// </summary>
        public event Action<ProxyConnection> Disposing;

        /// <summary>
        /// HTTPリクエストヘッダーをサーバーに送信完了した際に発生
        /// </summary>
        public event Action<IReadOnlyHttpRequest> HttpRequestHeadersSent;

        /// <summary>
        /// HTTPリクエストをサーバーに送信完了した際に発生
        /// </summary>
        public event Action<IReadOnlyHttpRequest> HttpRequestSent;

        /// <summary>
        /// HTTPレスポンスをクライアントに送信完了した際に発生
        /// </summary>
        public event Action<IReadOnlySession> HttpResponseSent;

        /// <summary>
        /// クライアントが WebSocket メッセージを送信完了した際に発生
        /// </summary>
        public event Action<IReadOnlyWebSocketMessage> ClientWebSocketMessageSent;

        /// <summary>
        /// サーバーが WebSocket メッセージを送信完了した際に発生
        /// </summary>
        public event Action<IReadOnlyWebSocketMessage> ServerWebSocketMessageSent;

        /// <summary>
        /// 重大な例外がスローされた際に発生。
        /// 主に非同期の実行例外の捕捉用。
        /// </summary>
        public event EventHandler<Exception> FatalException;

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        private void Dispose(bool disposing)
        {
            lock (this.disposeLock)
            {
                if (!this.disposedValue)
                {
                    if (disposing)
                    {
                        this.DebugWriteLine($"client connection closing.");
                        this.DebugWriteLine($"server connection closing.");
                        // マネージ状態を破棄します (マネージ オブジェクト)。
                        this.clientConnection?.Dispose();
                        this.serverConnection?.Dispose();
                        this.receiveSharedLock.Dispose();
                    }
                    // アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                    // 大きなフィールドを null に設定します。

                    this.disposedValue = true;
                    this.Disposing?.Invoke(this);
                }
            }
        }

        // 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~ProxyConnection() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            this.Dispose(true);
            // 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
