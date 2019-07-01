using Nekoxy2.ApplicationLayer.Entities.Http;
using Nekoxy2.ApplicationLayer.Entities.Http2;
using Nekoxy2.ApplicationLayer.Entities.WebSocket;
using Nekoxy2.ApplicationLayer.ProtocolReaders;
using Nekoxy2.ApplicationLayer.ProtocolReaders.Http;
using Nekoxy2.ApplicationLayer.ProtocolReaders.Http2;
using Nekoxy2.ApplicationLayer.ProtocolReaders.WebSocket;
using Nekoxy2.Default.Proxy.Tcp;
using Nekoxy2.Default.Proxy.Tls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Nekoxy2.Default.Proxy
{
    /// <summary>
    /// TCP コネクションから通信を読み取り、HTTP として解釈する抽象クラス
    /// </summary>
    internal abstract class HttpConnection : IDisposable
    {
        /// <summary>
        /// Dispose 対象リスト
        /// </summary>
        private List<IDisposable> DisposableItems { get; } = new List<IDisposable>();

        /// <summary>
        /// TCP 接続クライアント
        /// </summary>
        internal readonly ITcpClient client;

        /// <summary>
        /// プロキシ設定
        /// </summary>
        protected ProxyConfig Config { get; }

        #region ProtocolReader

        private IProtocolReader _ProtocolReader;
        /// <summary>
        /// プロトコルリーダー
        /// </summary>
        private IProtocolReader ProtocolReader
        {
            get => this._ProtocolReader;
            set
            {
                if (this._ProtocolReader == value) return;

                this._ProtocolReader = value;
                if (value != null)
                    this.AddDisposableItem(value);
            }
        }

        #endregion

        /// <summary>
        /// 受信ストリーム
        /// </summary>
        protected ReadBufferedNetworkStream ReceivedStream { get; set; }

        /// <summary>
        /// SSL/TLS ストリーム
        /// </summary>
        protected SslStream SslStream { get; set; }

        /// <summary>
        /// CONNECT トンネルモードかどうか
        /// </summary>
        public bool IsTunnelMode { get; set; }

        /// <summary>
        /// トンネル対象ホスト
        /// </summary>
        public string TunneledHost { get; set; }

        /// <summary>
        /// 待ち受け中かどうか
        /// </summary>
        private bool isReceiving;

        #region IsPauseBeforeReceive

        private TaskCompletionSource<bool> pauseSource;
        /// <summary>
        /// 次回待ち受け前に一時中断するかどうか
        /// </summary>
        public bool IsPauseBeforeReceive
        {
            get => this.pauseSource != null;
            set
            {
                if (value)
                {
                    Interlocked.CompareExchange(ref this.pauseSource, new TaskCompletionSource<bool>(), null);
                }
                else
                {
                    while (true)
                    {
                        if (this.pauseSource == null) return;
                        var tcs = this.pauseSource;
                        if (Interlocked.CompareExchange(ref this.pauseSource, null, tcs) == tcs)
                        {
                            tcs.SetResult(true);
                            break;
                        }
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// 書き込みロック
        /// </summary>
        private readonly object writeLock = new object();

        /// <summary>
        /// 待ち受け後処理ロック
        /// </summary>
        private readonly SemaphoreQueue receiveSharedLock;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="client">TCP 接続クライアント</param>
        /// <param name="config">プロキシ設定</param>
        /// <param name="receiveSharedLock">待ち受け後処理ロック</param>
        protected HttpConnection(ITcpClient client, ProxyConfig config = null, SemaphoreQueue receiveSharedLock = null)
        {
            this.client = client;
            this.AddDisposableItem(this.client);

            this.Config = config ?? new ProxyConfig();

            if (receiveSharedLock != null)
            {
                this.receiveSharedLock = receiveSharedLock;
            }
            else
            {
                this.receiveSharedLock = receiveSharedLock ?? new SemaphoreQueue(1, 1);
                this.AddDisposableItem(this.receiveSharedLock);
            }
        }

        /// <summary>
        /// 待ち受けを開始
        /// </summary>
        public void StartReceiving()
        {
            // ハンドラ追加前に色々発生するイベントをつかめない問題があるため、Receive 開始はコンストラクタでやるべきではない。
            lock (this.client)
            {
                if (this.isReceiving) return;
                this.isReceiving = true;

                var httpReader = this.CreateHttpReader();
                httpReader.ReadBody += data => this.ReadBody?.Invoke(data);
                this.ProtocolReader = httpReader;

                this.Receive()
                    .ContinueWith(_ => this.Dispose());
            }
        }

        /// <summary>
        /// <see cref="IsPauseBeforeReceive"/> が true の場合、false になるまで待機
        /// </summary>
        /// <returns></returns>
        private Task WaitWhilePausedAsync()
        {
            return this.pauseSource != null
                ? this.pauseSource.Task
                : Task.FromResult(true);
        }

        /// <summary>
        /// 復号化対象のホストかどうか
        /// </summary>
        protected bool IsDecryptTarget
            => this.Config.DecryptConfig.HostFilterHandler?.Invoke(this.TunneledHost) ?? true;

        #region Receive

        /// <summary>
        /// データ受信を待ち受け
        /// </summary>
        /// <returns></returns>
        private async Task Receive()
        {
            try
            {
                Debug.WriteLine($"### Start receiving socket. {(this.client as TcpClientWrapper)?.Source?.Client?.RemoteEndPoint}");

                if (this.ReceivedStream == null)
                {
                    this.ReceivedStream = new ReadBufferedNetworkStream(this.client.GetStream());
                    this.AddDisposableItem(this.ReceivedStream);
                }

                var isHandleTunnel = false;

                while (true)
                {
                    await this.WaitWhilePausedAsync().ConfigureAwait(false);

                    if (!this.client.Connected)
                        break;

                    var isReceived = await this.ReceivedStream.ReceiveAsync().ConfigureAwait(false);
                    if (!isReceived)
                        break;  // 閉じられた

                    // CONNECT トンネル時のデータの処理順を守るため、順序どおり処理される共有ロックを取る
                    // WebSocket リーダーは前後が発生する可能性が高い
                    // HTTP/2 リーダーは多少の前後は吸収できるように作っている
                    await this.receiveSharedLock.WaitAsync();
                    try
                    {
                        if (this.IsTunnelMode)
                        {
                            if (!isHandleTunnel)
                            {
                                this.EnsureSsl();

                                this.ReceivedStream.ReadData += data => this.ReadTunnel(data);
                                isHandleTunnel = true;
                            }
                            var buffer = new byte[4096];
                            var startRead = DateTimeOffset.Now;
                            var size = await this.ReceivedStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                            if (size <= 0)
                                return;

                            //Debug.WriteLine($"### Read: {(this.client as TcpClientWrapper)?.Source.Client.RemoteEndPoint}\r\n" +
                            //    $"{Encoding.ASCII.GetString(buffer.Take(size).ToArray())}" +
                            //    $"\r\n### ReadEnd");

                            try
                            {
                                // CONNECT トンネル後、HTTP/2 以外は HTTP/1.1 を前提としている
                                // 異なるプロトコルが来た場合は解析に失敗する
                                this.ProtocolReader?.HandleReceive(buffer, size);
                            }
                            catch (Exception e)
                            {
                                // トンネル中の解析失敗では通信は中断させない
                                if (!e.Has<SocketException>()
                                && !e.Has<ObjectDisposedException>())
                                    this.InvokeFatalException(this, e);
                            }
                        }
                        else
                        {
                            var buffer = new byte[4096];
                            var size = await this.ReceivedStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                            if (size < 1)
                                break;  // 0 byte は切断された時っぽい
                            this.OnRead(buffer, size);
                        }
                    }
                    finally
                    {
                        this.receiveSharedLock.Release();
                    }
                }
                this.OnTcpClose();
            }
            catch (Exception e)
            {
                if (e.Has<SocketException>()
                || e.Has<ObjectDisposedException>())
                {
                    if (!this.IsTunnelMode)
                    {
                        // 切断時点では正常か異常か不明
                        this.OnTcpClose();
                    }
                }
                else
                {
                    if (!this.IsTunnelMode)
                    {
                        this.OnException();
                    }
                    this.FatalException?.Invoke(this, e);
                }
            }
            finally
            {
                this.ReceiveClosed?.Invoke();
                if (this.client.CloseState == CloseState.Both)
                    this.Dispose();
            }
        }

        /// <summary>
        /// HTTP リーダーを作成
        /// </summary>
        /// <returns></returns>
        protected abstract AbstractHttpReader CreateHttpReader();

        /// <summary>
        /// プロトコルを WebSocket へ変更
        /// </summary>
        /// <param name="handshakeSession">WebSocket ハンドシェイクセッション</param>
        public void ChangeToWebSocket(Session handshakeSession)
        {
            var reader = WebSocketReader.Create(handshakeSession, this.Config.MaxCaptureSize);
            reader.MessageReceived += message => this.WebSocketMessageReceived?.Invoke(message);
            this.ProtocolReader = reader;
        }

        /// <summary>
        /// プロトコルを HTTP/2 へ変更
        /// </summary>
        /// <param name="type">エンドポイントの種類</param>
        protected void ChangeToHttp2(EndPointType type)
        {
            var reader = new Http2FrameReader(type);
            reader.FrameReceived += frame => this.Http2FrameReceived?.Invoke(frame);
            this.ProtocolReader = reader;
        }

        /// <summary>
        /// 未知のプロトコルへ変更
        /// </summary>
        public void ChangeToUnknownProtocol()
            => this.ProtocolReader = null;

        /// <summary>
        /// 必要に応じて SSL/TLS 接続を確立
        /// </summary>
        protected abstract void EnsureSsl();

        /// <summary>
        /// TCP 切断時
        /// </summary>
        protected abstract void OnTcpClose();

        /// <summary>
        /// 例外発生時
        /// </summary>
        protected abstract void OnException();

        /// <summary>
        /// TCP 通信データ読み取り時
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="readSize"></param>
        protected abstract void OnRead(byte[] buffer, int readSize);

        #endregion

        /// <summary>
        /// データ送信
        /// </summary>
        /// <param name="data"></param>
        /// <param name="size"></param>
        public void Write(byte[] data, int size)
        {
            if (this.disposedValue) return;

            if (!this.client.Connected) return;

            if (this.ReceivedStream?.CanWrite != true) return;
            try
            {
                //if(this is ClientConnection)
                //Debug.WriteLine($"### Write: {(this.client as TcpClientWrapper)?.Source.Client.RemoteEndPoint}\r\n" +
                //    $"{Encoding.ASCII.GetString(data.Take(size).ToArray())}" +
                //    $"\r\n### WriteEnd");
                // 書き込み中に切断されないようロック
                lock (this.writeLock)
                {
                    this.ReceivedStream.Write(data, 0, size);
                }
            }
            catch (Exception e)
            {
                if (!e.Has<SocketException>()
                && !e.Has<ObjectDisposedException>())
                {
                    this.FatalException?.Invoke(this, e);
                }
                this.Dispose();
            }
        }

        /// <summary>
        /// <see cref="FatalException"/> イベントを発生させます
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void InvokeFatalException(object sender, Exception e)
            => this.FatalException?.Invoke(sender, e);

        /// <summary>
        /// 他方のプロキシ接続切断時
        /// </summary>
        public void OnOtherReceiveClosed()
        {
            this.client.Shutdown(SocketShutdown.Send);
            if (this.client.CloseState == CloseState.Both)
                this.Dispose();
        }

        /// <summary>
        /// 受信クローズ時に発生
        /// </summary>
        public event Action ReceiveClosed;

        /// <summary>
        /// TCP 切断時に発生
        /// </summary>
        public event Action Closed;

        /// <summary>
        /// CONNECT トンネルデータ読み取り時に発生
        /// </summary>
        public event Action<(byte[] buffer, int readSize)> ReadTunnel;

        /// <summary>
        /// メッセージボディーデータ読み取り時に発生
        /// </summary>
        public event Action<(byte[] buffer, int readSize)> ReadBody;

        /// <summary>
        /// WebSocket メッセージ受信完了時に発生
        /// </summary>
        public event Action<WebSocketMessage> WebSocketMessageReceived;

        /// <summary>
        /// HTTP/2 フレーム受信完了時に発生
        /// </summary>
        public event Action<IHttp2Frame> Http2FrameReceived;

        /// <summary>
        /// 重大な例外がスローされた際に発生。
        /// 主に非同期の実行例外の捕捉用。
        /// </summary>
        public event EventHandler<Exception> FatalException;

        /// <summary>
        /// Dispose 対象を追加
        /// </summary>
        /// <param name="item"></param>
        protected void AddDisposableItem(IDisposable item)
        {
            lock (this.writeLock)
                this.DisposableItems.Add(item);
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            lock (this.writeLock)
            {
                if (!this.disposedValue)
                {
                    if (disposing)
                    {
                        // マネージ状態を破棄します (マネージ オブジェクト)。
                        this.client?.Shutdown(SocketShutdown.Both);

                        foreach (var value in this.DisposableItems)
                        {
                            value?.Dispose();
                        }
                    }

                    // アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                    // 大きなフィールドを null に設定します。

                    this.disposedValue = true;

                    Task.Run(() => this.Closed?.Invoke()).ConfigureAwait(false);
                }
            }
        }

        // 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~HttpConnection() {
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

    internal static partial class ExceptionExtensions
    {
        /// <summary>
        /// 指定された型の例外を持つかどうか
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool Has<T>(this Exception e)
            where T : Exception
        {
            if (e is T)
                return true;
            else if (e.InnerException == null)
                return false;
            else
                return e.InnerException.Has<T>();
        }
    }
}
