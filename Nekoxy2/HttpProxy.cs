using System;

namespace Nekoxy2
{
    /// <summary>
    /// HTTP プロキシ
    /// </summary>
    public sealed class HttpProxy : IWebSocketProxy
    {
        /// <summary>
        /// プロキシエンジン
        /// </summary>
        private readonly Spi.IReadOnlyHttpProxyEngine proxyEngine;

        private HttpProxy(Spi.IReadOnlyHttpProxyEngine engine)
        {
            this.proxyEngine = engine;
            engine.HttpRequestSent += (sender, args) => this.HttpRequestSent?.Invoke(sender, ReadOnlyHttpRequestEventArgs.Convert(args));
            engine.HttpResponseSent += (sender, args) => this.HttpResponseSent?.Invoke(sender, ReadOnlySessionEventArgs.Convert(args));
            engine.FatalException += (sender, args) => this.FatalException?.Invoke(sender, ExceptionEventArgs.Convert(args));

            if (engine is Spi.IHttpProxyEngine writableEngine)
            {
                writableEngine.HttpRequestReceived += (sender, args) => this.HttpRequestReceived?.Invoke(sender, HttpRequestEventArgs.Convert(args));
                writableEngine.HttpResponseReceived += (sender, args) => this.HttpResponseReceived?.Invoke(sender, SessionEventArgs.Convert(args));
            }
            if (engine is Spi.IReadOnlyWebSocketProxyEngine rowsEngine)
            {
                rowsEngine.ClientWebSocketMessageSent += (sender, args) => this.ClientWebSocketMessageSent?.Invoke(sender, ReadOnlyWebSocketMessageEventArgs.Convert(args));
                rowsEngine.ServerWebSocketMessageSent += (sender, args) => this.ServerWebSocketMessageSent?.Invoke(sender, ReadOnlyWebSocketMessageEventArgs.Convert(args));
            }
            if(engine is Spi.IWebSocketProxyEngine wsEngine)
            {
                wsEngine.ClientWebSocketMessageReceived += (sender, args) => this.ClientWebSocketMessageReceived?.Invoke(sender, WebSocketMessageEventArgs.Convert(args));
                wsEngine.ServerWebSocketMessageReceived += (sender, args) => this.ServerWebSocketMessageReceived?.Invoke(sender, WebSocketMessageEventArgs.Convert(args));
            }
        }
        /// <summary>
        /// プロキシの待ち受けを開始
        /// </summary>
        public void Start()
            => this.proxyEngine.Start();

        /// <summary>
        /// プロキシの待ち受けを終了
        /// </summary>
        public void Stop()
            => this.proxyEngine.Stop();

        /// <summary>
        /// HTTP リクエストをクライアントから受信完了した際に発生
        /// </summary>
        public event EventHandler<IHttpRequestEventArgs> HttpRequestReceived;

        /// <summary>
        /// HTTP リクエストをサーバーへ送信完了した際に発生
        /// </summary>
        public event EventHandler<IReadOnlyHttpRequestEventArgs> HttpRequestSent;

        /// <summary>
        /// HTTP レスポンスをサーバーから受信完了した際に発生
        /// </summary>
        public event EventHandler<ISessionEventArgs> HttpResponseReceived;

        /// <summary>
        /// HTTPレスポンスをクライアントへ送信完了した際に発生
        /// </summary>
        public event EventHandler<IReadOnlySessionEventArgs> HttpResponseSent;

        /// <summary>
        /// クライアントからの WebSocket メッセージを受信完了した際に発生
        /// </summary>
        public event EventHandler<IWebSocketMessageEventArgs> ClientWebSocketMessageReceived;

        /// <summary>
        /// クライアントが WebSocket メッセージを送信完了した際に発生
        /// </summary>
        public event EventHandler<IReadOnlyWebSocketMessageEventArgs> ClientWebSocketMessageSent;

        /// <summary>
        /// サーバーからの WebSocket メッセージを受信完了した際に発生
        /// </summary>
        public event EventHandler<IWebSocketMessageEventArgs> ServerWebSocketMessageReceived;

        /// <summary>
        /// サーバーが WebSocket メッセージを送信完了した際に発生
        /// </summary>
        public event EventHandler<IReadOnlyWebSocketMessageEventArgs> ServerWebSocketMessageSent;

        /// <summary>
        /// 重大な例外がスローされた際に発生。
        /// 主に非同期の実行例外の捕捉用。
        /// </summary>
        public event EventHandler<IExceptionEventArgs> FatalException;

        /// <summary>
        /// プロキシエンジンを指定して読み取り専用 HTTP プロキシを作成
        /// </summary>
        /// <param name="engine">プロキシエンジン</param>
        /// <returns>HTTP プロキシ</returns>
        public static IReadOnlyHttpProxy Create(Spi.IReadOnlyHttpProxyEngine engine)
            => new HttpProxy(engine);

        /// <summary>
        /// プロキシエンジンを指定して HTTP プロキシを作成
        /// </summary>
        /// <param name="engine">プロキシエンジン</param>
        /// <returns>HTTP プロキシ</returns>
        public static IHttpProxy Create(Spi.IHttpProxyEngine engine)
            => new HttpProxy(engine);

        /// <summary>
        /// プロキシエンジンを指定して読み取り専用 WebSocket プロキシを作成
        /// </summary>
        /// <param name="engine">プロキシエンジン</param>
        /// <returns>HTTP プロキシ</returns>
        public static IReadOnlyWebSocketProxy Create(Spi.IReadOnlyWebSocketProxyEngine engine)
            => new HttpProxy(engine);

        /// <summary>
        /// プロキシエンジンを指定して WebSocket プロキシを作成
        /// </summary>
        /// <param name="engine">プロキシエンジン</param>
        /// <returns>HTTP プロキシ</returns>
        public static IWebSocketProxy Create(Spi.IWebSocketProxyEngine engine)
            => new HttpProxy(engine);
    }
}
