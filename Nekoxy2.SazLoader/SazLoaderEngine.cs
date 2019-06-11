using Nekoxy2.ApplicationLayer.ProtocolReaders.WebSocket;
using Nekoxy2.SazLoader.Deserialization;
using Nekoxy2.SazLoader.Entities;
using Nekoxy2.SazLoader.Entities.WebSocket;
using Nekoxy2.Spi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nekoxy2.SazLoader
{
    /// <summary>
    /// SAZ ファイルを読み込み、通信を再現
    /// </summary>
    public sealed class SazLoaderEngine : IReadOnlyWebSocketProxyEngine
    {
        /// <summary>
        /// 実行キャンセル用
        /// </summary>
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        /// <summary>
        /// SAZ セッションリスト
        /// </summary>
        private readonly IEnumerable<SazSession> sessions;

        /// <summary>
        /// SAZ セッションリストを指定してインスタンスを作成
        /// </summary>
        /// <param name="sessions">SAZ セッションリスト</param>
        private SazLoaderEngine(IEnumerable<SazSession> sessions)
            => this.sessions = sessions;

        /// <summary>
        /// 通信の再現を開始
        /// </summary>
        public void Start()
        {
            Task.Run(() =>
            {
                var events = this.sessions
                    .SelectMany(x => this.ToInvoker(x))
                    .OrderBy(x => x.Time);
                DateTimeOffset previousTime = default;
                foreach (var e in events)
                {
                    if (previousTime == default)
                        previousTime = e.Time;
                    var span = e.Time - previousTime;
                    previousTime = e.Time;
                    Debug.WriteLine($"Wait {span}");
                    Task.Delay(span).Wait(this.cts.Token);
                    try
                    {
                        e.Invoker.Invoke();
                    }
                    catch (Exception ex)
                    {
                        this.FatalException?.Invoke(this, ExceptionEventArgs.Create(ex));
                    }
                }
                this.AllSessionsComlete?.Invoke();
            }, this.cts.Token);
        }

        /// <summary>
        /// SAZ セッションからイベントリストを作成
        /// </summary>
        /// <param name="session">SAZ セッション</param>
        /// <returns>イベントリスト</returns>
        private IList<(DateTimeOffset Time, Action Invoker)> ToInvoker(SazSession session)
        {
            var isWebSocket = session.WebSocketFrames != null;
            var timers = session.Metadata.SessionTimers;
            var list = new List<(DateTimeOffset Time, Action Invoker)>
            {
                (timers.ClientDoneRequest,
                () => this.HttpRequestSent?.Invoke(this, ReadOnlyHttpRequestEventArgs.Create(session.Request))),

                (isWebSocket ? new DateTimeOffset(timers.GotResponseHeaders) : new DateTimeOffset(timers.ClientDoneResponse),
                () => this.HttpResponseSent?.Invoke(this, ReadOnlySessionEventArgs.Create(session)))
            };

            if (isWebSocket)
            {
                var maxCaptureSize = Environment.Is64BitProcess ? 0x7FFFFFC7 : 256 * 1024 * 1024;
                var clientBuilder = new WebSocketMessageBuilder(session, maxCaptureSize);
                var serverBuilder = new WebSocketMessageBuilder(session, maxCaptureSize);
                foreach (var frame in session.WebSocketFrames)
                {
                    Action invoker = default;
                    if (frame.Direction == Direction.Request)
                    {
                        if (clientBuilder.TryCreateOrAdd(frame.Frame, out var message))
                            invoker = () => this.ClientWebSocketMessageSent?.Invoke(this, ReadOnlyWebSocketMessageEventArgs.Create(message));
                    }
                    else
                    {
                        if (serverBuilder.TryCreateOrAdd(frame.Frame, out var message))
                            invoker = () => this.ServerWebSocketMessageSent?.Invoke(this, ReadOnlyWebSocketMessageEventArgs.Create(message));
                    }
                    if (invoker != default)
                    {
                        list.Add((frame.DoneSend, invoker));
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 通信の再現を中止
        /// </summary>
        public void Stop()
            => this.cts.Cancel();

        /// <summary>
        /// HTTPリクエストをサーバーへ送信完了した際に発生
        /// </summary>
        public event EventHandler<IReadOnlyHttpRequestEventArgs> HttpRequestSent;

        /// <summary>
        /// HTTPレスポンスをクライアントへ送信完了した際に発生
        /// </summary>
        public event EventHandler<IReadOnlySessionEventArgs> HttpResponseSent;

        /// <summary>
        /// クライアントが WebSocket メッセージを送信完了した際に発生
        /// </summary>
        public event EventHandler<IReadOnlyWebSocketMessageEventArgs> ClientWebSocketMessageSent;

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
        /// すべてのセッションの再現が完了した際に発生
        /// </summary>
        public event Action AllSessionsComlete;

        /// <summary>
        /// SAZ ファイルパスを指定し、インスタンスを作成
        /// </summary>
        /// <param name="path">SAZ ファイルパス</param>
        /// <returns><see cref="SazLoaderEngine"/> インスタンス</returns>
        public static SazLoaderEngine Create(string path)
            => new SazLoaderEngine(SazFile.Load(path));
    }
}
