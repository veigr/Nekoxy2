using Nekoxy2.Entities.Http;
using Nekoxy2.Entities.WebSocket;
using System;

namespace Nekoxy2
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
        ISession Session { get; }
    }

    /// <summary>
    /// HTTP リクエストイベントデータ
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
        IWebSocketMessage Message { get; }
    }

    /// <summary>
    /// 例外イベントデータ
    /// </summary>
    public interface IExceptionEventArgs
    {
        Exception Exception { get; }
    }

    internal sealed class ReadOnlySessionEventArgs : EventArgs, IReadOnlySessionEventArgs
    {
        public IReadOnlySession Session { get; }

        private ReadOnlySessionEventArgs(IReadOnlySession session)
            => this.Session = session;

        public static IReadOnlySessionEventArgs Convert(Spi.IReadOnlySessionEventArgs args)
            => new ReadOnlySessionEventArgs(Entities.Http.Delegations.ReadOnlySession.Convert(args.Session));
    }

    internal sealed class SessionEventArgs : EventArgs, ISessionEventArgs
    {
        public ISession Session { get; set; }

        private SessionEventArgs(ISession session)
            => this.Session = session;

        public static ISessionEventArgs Convert(Spi.ISessionEventArgs args)
            => new SessionEventArgs(Entities.Http.Delegations.Session.Convert(args.Session));
    }

    internal sealed class ReadOnlyHttpRequestEventArgs : EventArgs, IReadOnlyHttpRequestEventArgs
    {
        public IReadOnlyHttpRequest Request { get; set; }

        private ReadOnlyHttpRequestEventArgs(IReadOnlyHttpRequest request)
            => this.Request = request;

        public static IReadOnlyHttpRequestEventArgs Convert(Spi.IReadOnlyHttpRequestEventArgs args)
            => new ReadOnlyHttpRequestEventArgs(Entities.Http.Delegations.ReadOnlyHttpRequest.Convert(args.Request));
    }

    internal sealed class HttpRequestEventArgs : EventArgs, IHttpRequestEventArgs
    {
        public IHttpRequest Request { get; set; }

        private HttpRequestEventArgs(IHttpRequest request)
            => this.Request = request;

        public static IHttpRequestEventArgs Convert(Spi.IHttpRequestEventArgs args)
            => new HttpRequestEventArgs(Entities.Http.Delegations.HttpRequest.Convert(args.Request));
    }

    internal sealed class ReadOnlyWebSocketMessageEventArgs : EventArgs, IReadOnlyWebSocketMessageEventArgs
    {
        public IReadOnlyWebSocketMessage Message { get; }

        private ReadOnlyWebSocketMessageEventArgs(IReadOnlyWebSocketMessage message)
            => this.Message = message;

        public static IReadOnlyWebSocketMessageEventArgs Convert(Spi.IReadOnlyWebSocketMessageEventArgs args)
            => new ReadOnlyWebSocketMessageEventArgs(Entities.WebSocket.Delegations.ReadOnlyWebSocketMessage.Convert(args.Message));
    }

    internal sealed class WebSocketMessageEventArgs : EventArgs, IWebSocketMessageEventArgs
    {
        public IWebSocketMessage Message { get; set; }

        private WebSocketMessageEventArgs(IWebSocketMessage message)
            => this.Message = message;

        public static IWebSocketMessageEventArgs Convert(Spi.IWebSocketMessageEventArgs args)
            => new WebSocketMessageEventArgs(Entities.WebSocket.Delegations.WebSocketMessage.Convert(args.Message));
    }

    internal sealed class ExceptionEventArgs : EventArgs, IExceptionEventArgs
    {
        public Exception Exception { get; }

        private ExceptionEventArgs(Exception exception)
            => this.Exception = exception;

        public static IExceptionEventArgs Convert(Spi.IExceptionEventArgs args)
            => new ExceptionEventArgs(args.Exception);
    }
}
