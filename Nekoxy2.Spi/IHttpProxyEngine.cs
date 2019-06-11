using System;

namespace Nekoxy2.Spi
{
    /// <summary>
    /// 読み取り専用 HTTP プロキシエンジン
    /// </summary>
    public interface IReadOnlyHttpProxyEngine
    {
        /// <summary>
        /// HTTP リクエストをサーバーに送信完了した際に発生
        /// </summary>
        event EventHandler<IReadOnlyHttpRequestEventArgs> HttpRequestSent;

        /// <summary>
        /// HTTPレスポンスをクライアントに送信完了した際に発生
        /// </summary>
        event EventHandler<IReadOnlySessionEventArgs> HttpResponseSent;

        /// <summary>
        /// 重大な例外がスローされた際に発生。
        /// 主に非同期の実行例外の捕捉用。
        /// </summary>
        event EventHandler<IExceptionEventArgs> FatalException;

        /// <summary>
        /// プロキシの待ち受けを開始
        /// </summary>
        void Start();

        /// <summary>
        /// プロキシの待ち受けを終了
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// HTTP プロキシエンジン
    /// </summary>
    public interface IHttpProxyEngine : IReadOnlyHttpProxyEngine
    {
        /// <summary>
        /// HTTP リクエストをクライアントから受信完了した際に発生
        /// </summary>
        event EventHandler<IHttpRequestEventArgs> HttpRequestReceived;

        /// <summary>
        /// HTTP レスポンスをサーバーから受信完了した際に発生
        /// </summary>
        event EventHandler<ISessionEventArgs> HttpResponseReceived;
    }

    /// <summary>
    /// 読み取り専用 WebSocket プロキシエンジン
    /// </summary>
    public interface IReadOnlyWebSocketProxyEngine : IReadOnlyHttpProxyEngine
    {
        /// <summary>
        /// クライアントからの WebSocket メッセージを送信完了した際に発生
        /// </summary>
        event EventHandler<IReadOnlyWebSocketMessageEventArgs> ClientWebSocketMessageSent;

        /// <summary>
        /// サーバーからの WebSocket メッセージを送信完了した際に発生
        /// </summary>
        event EventHandler<IReadOnlyWebSocketMessageEventArgs> ServerWebSocketMessageSent;
    }

    /// <summary>
    /// WebSocket プロキシエンジン
    /// </summary>
    public interface IWebSocketProxyEngine : IReadOnlyWebSocketProxyEngine, IHttpProxyEngine
    {
        /// <summary>
        /// クライアントからの WebSocket メッセージを受信完了した際に発生
        /// </summary>
        event EventHandler<IWebSocketMessageEventArgs> ClientWebSocketMessageReceived;

        /// <summary>
        /// サーバーからの WebSocket メッセージを受信完了した際に発生
        /// </summary>
        event EventHandler<IWebSocketMessageEventArgs> ServerWebSocketMessageReceived;
    }
}
