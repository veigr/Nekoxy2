using Nekoxy2.Spi.Entities.Http;
using Nekoxy2.Spi.Entities.WebSocket;
using System;

namespace Nekoxy2.Spi
{
    /// <summary>
    /// 読み取り専用 HTTP セッションイベントデータ
    /// </summary>
    public interface IReadOnlySessionEventArgs
    {
        IReadOnlySession Session { get; }
    }

    /// <summary>
    /// HTTP セッションイベントデータ
    /// </summary>
    public interface ISessionEventArgs
    {
        ISession Session { get; set; }
    }

    /// <summary>
    /// 読み取り専用 HTTP リクエストイベントデータ
    /// </summary>
    public interface IReadOnlyHttpRequestEventArgs
    {
        IReadOnlyHttpRequest Request { get; }
    }

    /// <summary>
    /// HTTP リクエストイベントデータ
    /// </summary>
    public interface IHttpRequestEventArgs
    {
        IHttpRequest Request { get; set; }
    }

    /// <summary>
    /// 読み取り専用 WebSocket メッセージイベントデータ
    /// </summary>
    public interface IReadOnlyWebSocketMessageEventArgs
    {
        IReadOnlyWebSocketMessage Message { get; }
    }

    /// <summary>
    /// WebSocket メッセージイベントデータ
    /// </summary>
    public interface IWebSocketMessageEventArgs
    {
        IWebSocketMessage Message { get; set; }
    }

    /// <summary>
    /// 例外イベントデータ
    /// </summary>
    public interface IExceptionEventArgs
    {
        Exception Exception { get; }
    }

    /// <summary>
    /// 読み取り専用 HTTP セッションイベントデータ
    /// </summary>
    public sealed class ReadOnlySessionEventArgs : EventArgs, IReadOnlySessionEventArgs
    {
        /// <summary>
        /// HTTP セッション
        /// </summary>
        public IReadOnlySession Session { get; }

        private ReadOnlySessionEventArgs(IReadOnlySession session)
            => this.Session = session;

        /// <summary>
        /// HTTP セッションを指定してインスタンスを作成
        /// </summary>
        /// <param name="session">HTTP セッション</param>
        /// <returns>HTTP セッションイベントデータ</returns>
        public static IReadOnlySessionEventArgs Create(IReadOnlySession session)
            => new ReadOnlySessionEventArgs(session);
    }

    /// <summary>
    /// HTTP セッションイベントデータ
    /// </summary>
    public sealed class SessionEventArgs : EventArgs, ISessionEventArgs
    {
        /// <summary>
        /// HTTP セッション
        /// </summary>
        public ISession Session { get; set; }

        private SessionEventArgs(ISession session)
            => this.Session = session;

        /// <summary>
        /// HTTP セッションを指定してインスタンスを作成
        /// </summary>
        /// <param name="session">HTTP セッション</param>
        /// <returns>HTTP セッションイベントデータ</returns>
        public static ISessionEventArgs Create(ISession session)
            => new SessionEventArgs(session);
    }

    /// <summary>
    /// 読み取り専用 HTTP リクエストイベントデータ
    /// </summary>
    public sealed class ReadOnlyHttpRequestEventArgs : EventArgs, IReadOnlyHttpRequestEventArgs
    {
        /// <summary>
        /// HTTP リクエスト
        /// </summary>
        public IReadOnlyHttpRequest Request { get; }

        private ReadOnlyHttpRequestEventArgs(IReadOnlyHttpRequest request)
            => this.Request = request;

        /// <summary>
        /// HTTP リクエストを指定してインスタンスを作成
        /// </summary>
        /// <param name="request">HTTP リクエスト</param>
        /// <returns>HTTP リクエストイベントデータ</returns>
        public static IReadOnlyHttpRequestEventArgs Create(IReadOnlyHttpRequest request)
            => new ReadOnlyHttpRequestEventArgs(request);
    }

    /// <summary>
    /// HTTP リクエストイベントデータ
    /// </summary>
    public sealed class HttpRequestEventArgs : EventArgs, IHttpRequestEventArgs
    {
        /// <summary>
        /// HTTP リクエスト
        /// </summary>
        public IHttpRequest Request { get; set; }

        private HttpRequestEventArgs(IHttpRequest request)
            => this.Request = request;

        /// <summary>
        /// HTTP リクエストを指定してインスタンスを作成
        /// </summary>
        /// <param name="request">HTTP リクエスト</param>
        /// <returns>HTTP リクエストイベントデータ</returns>
        public static IHttpRequestEventArgs Create(IHttpRequest request)
            => new HttpRequestEventArgs(request);
    }

    /// <summary>
    /// WebSocket メッセージイベントデータ
    /// </summary>
    public sealed class ReadOnlyWebSocketMessageEventArgs : EventArgs, IReadOnlyWebSocketMessageEventArgs
    {
        /// <summary>
        /// WebSocket メッセージ
        /// </summary>
        public IReadOnlyWebSocketMessage Message { get; }

        private ReadOnlyWebSocketMessageEventArgs(IReadOnlyWebSocketMessage message)
            => this.Message = message;

        /// <summary>
        /// WebSocket メッセージを指定してインスタンスを作成
        /// </summary>
        /// <param name="message">WebSocket メッセージ</param>
        /// <returns>WebSocket メッセージイベントデータ</returns>
        public static IReadOnlyWebSocketMessageEventArgs Create(IReadOnlyWebSocketMessage message)
            => new ReadOnlyWebSocketMessageEventArgs(message);
    }

    /// <summary>
    /// WebSocket メッセージイベントデータ
    /// </summary>
    public sealed class WebSocketMessageEventArgs : EventArgs, IWebSocketMessageEventArgs
    {
        /// <summary>
        /// WebSocket メッセージ
        /// </summary>
        public IWebSocketMessage Message { get; set; }

        private WebSocketMessageEventArgs(IWebSocketMessage message)
            => this.Message = message;

        /// <summary>
        /// WebSocket メッセージを指定してインスタンスを作成
        /// </summary>
        /// <param name="message">WebSocket メッセージ</param>
        /// <returns>WebSocket メッセージイベントデータ</returns>
        public static IWebSocketMessageEventArgs Create(IWebSocketMessage message)
            => new WebSocketMessageEventArgs(message);
    }

    /// <summary>
    /// 例外イベントデータ
    /// </summary>
    public sealed class ExceptionEventArgs : EventArgs, IExceptionEventArgs
    {
        /// <summary>
        /// 例外
        /// </summary>
        public Exception Exception { get; }

        private ExceptionEventArgs(Exception exception)
            => this.Exception = exception;

        /// <summary>
        /// 例外を指定してインスタンスを作成
        /// </summary>
        /// <param name="exception">例外</param>
        /// <returns>例外イベントデータ</returns>
        public static IExceptionEventArgs Create(Exception exception)
            => new ExceptionEventArgs(exception);
    }
}
