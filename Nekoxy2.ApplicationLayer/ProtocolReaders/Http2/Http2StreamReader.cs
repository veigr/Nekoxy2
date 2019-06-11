using Nekoxy2.ApplicationLayer.Entities.Http;
using Nekoxy2.ApplicationLayer.Entities.Http2;
using Nekoxy2.ApplicationLayer.ProtocolReaders.Http2.Hpack;
using Nekoxy2.ApplicationLayer.ProtocolReaders.WebSocket;
using Nekoxy2.Spi.Entities.WebSocket;
using System;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http2
{
    /// <summary>
    /// HTTP/2 フレームを入力し、HTTP/2 ストリームを読み取り
    /// </summary>
    internal sealed class Http2StreamReader
    {
        /// <summary>
        /// ストリーム ID
        /// </summary>
        public int Id { get; }

        private HttpRequest _PushRequest;
        /// <summary>
        /// プッシュリクエスト
        /// </summary>
        public HttpRequest PushRequest
        {
            get => this._PushRequest;
            set
            {
                if (value == null) return;
                this._PushRequest = value;
                this.isRequestReceived = true;
            }
        }

        /// <summary>
        /// リクエスト側 HTTP/2 ストリームリーダー
        /// </summary>
        private readonly Http2OneSideStreamReader requestReader;

        /// <summary>
        /// レスポンス側 HTTP/2 ストリームリーダー
        /// </summary>
        private readonly Http2OneSideStreamReader responseReader;

        /// <summary>
        /// リセットされたかどうか
        /// </summary>
        private bool isReset = false;

        /// <summary>
        /// リクエストを受信完了したかどうか
        /// </summary>
        private bool isRequestReceived = false;

        /// <summary>
        /// レスポンスを受信完了したかどうか
        /// </summary>
        private bool isResponseReceived = false;

        /// <summary>
        /// CONNECT トンネル状態かどうか
        /// </summary>
        private bool isTunnel = false;

        /// <summary>
        /// クライアント側 WebSocket リーダー
        /// </summary>
        private WebSocketReader clientWebSocketReader;

        /// <summary>
        /// サーバー側 WebSocket リーダー
        /// </summary>
        private WebSocketReader serverWebSocketReader;

        /// <summary>
        /// 最大キャプチャーサイズ
        /// </summary>
        private readonly int maxCaptureSize;

        /// <summary>
        /// ロックオブジェクト
        /// </summary>
        private readonly object lockObject = new object();

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="id">ストリーム ID</param>
        /// <param name="requestHpackDecoder">リクエスト側 HPACK デコーダー</param>
        /// <param name="responseHpackDecoder">レスポンス側 HPACK デコーダー</param>
        /// <param name="maxCaptureSize">最大キャプチャーサイズ</param>
        /// <param name="pushRequest">プッシュリクエスト</param>
        public Http2StreamReader(int id, HpackDecoder requestHpackDecoder, HpackDecoder responseHpackDecoder, int maxCaptureSize = int.MaxValue, HttpRequest pushRequest = null)
        {
            this.Id = id;
            this.maxCaptureSize = maxCaptureSize;
            this.requestReader = new Http2OneSideStreamReader(requestHpackDecoder, this.maxCaptureSize);
            this.responseReader = new Http2OneSideStreamReader(responseHpackDecoder, this.maxCaptureSize);
            this.PushRequest = pushRequest;
            this.isRequestReceived = this.PushRequest != null;

            this.responseReader.PushPromise += promise =>
            {
                var request = new HttpRequest(
                                    promise.Headers.ToRequestLine(),
                                    promise.Headers.ToHttpHeaders(),
                                    Array.Empty<byte>(),
                                    HttpHeaders.Empty);
                this.PushPromise?.Invoke((promise.StreamId, request));
            };

            this.requestReader.EndStream += () =>
            {
                this.isRequestReceived = true;
                this.HttpRequestSent?.Invoke(this, this.BuildRequest());
                this.OnEndStream();
            };
            this.responseReader.EndStream += () =>
            {
                this.isResponseReceived = true;
                this.OnEndStream();
            };

            this.requestReader.Reset += () =>
            {
                this.isReset = true;
                this.Reset?.Invoke(this);
            };
            this.responseReader.Reset += () =>
            {
                this.isReset = true;
                this.Reset?.Invoke(this);
            };
        }

        /// <summary>
        /// リクエストフレームを入力
        /// </summary>
        /// <param name="frame">入力するフレーム</param>
        public void HandleRequest(IHttp2Frame frame)
        {
            lock (this.lockObject)
            {
                if (!this.isTunnel)
                {
                    this.requestReader.HandleFrame(frame);
                }
                else if (frame is Http2DataFrame dataFrame)
                {
                    this.clientWebSocketReader?.HandleReceive(dataFrame.Data, dataFrame.Data.Length);

                    // CONNECT トンネルストリームには DATA フレーム以外は送信してはいけない RFC7540 8.3
                    // CONNECT トンネルストリームは DATA フレームの END_STREAM フラグで切断される RFC7540 8.3
                    if (dataFrame.IsEndStream)
                        this.Reset?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// レスポンスフレームを入力
        /// </summary>
        /// <param name="frame">入力するフレーム</param>
        public void HandleResponse(IHttp2Frame frame)
        {
            lock (this.lockObject)
            {
                if (!this.isTunnel)
                {
                    this.responseReader.HandleFrame(frame);
                }
                else if (frame is Http2DataFrame dataFrame)
                {
                    this.serverWebSocketReader?.HandleReceive(dataFrame.Data, dataFrame.Data.Length);

                    // CONNECT トンネルストリームには DATA フレーム以外は送信してはいけない RFC7540 8.3
                    // CONNECT トンネルストリームは DATA フレームの END_STREAM フラグで切断される RFC7540 8.3
                    if (dataFrame.IsEndStream)
                        this.Reset?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// ストリーム終端処理
        /// </summary>
        private void OnEndStream()
        {
            if (!this.isReset && this.isRequestReceived && this.isResponseReceived)
            {
                var session = this.BuildSession();

                if (session.Request?.RequestLine?.Method?.Method == "CONNECT"
                && 200 <= (int)session.Response.StatusLine?.StatusCode
                && (int)session.Response.StatusLine?.StatusCode <= 299)
                {
                    this.isTunnel = true;

                    // :protocol 疑似ヘッダが websocket の場合、WebSocket ハンドシェイク RFC8441
                    var protocol = this.requestReader.Headers?.FirstOrDefault(x => x.Name == ":protocol").Value;
                    if (protocol == "websocket")
                    {
                        this.clientWebSocketReader = WebSocketReader.Create(session, this.maxCaptureSize);
                        this.clientWebSocketReader.MessageReceived += message => this.ClientWebSocketMessageSent?.Invoke(message);
                        this.serverWebSocketReader = WebSocketReader.Create(session, this.maxCaptureSize);
                        this.serverWebSocketReader.MessageReceived += message => this.ServerWebSocketMessageSent?.Invoke(message);
                    }
                }

                this.HttpResponseSent?.Invoke(this, session);
            }
        }

        /// <summary>
        /// 読み取ったデータから HTTP/1.1 セッションへマッピングして構築
        /// </summary>
        /// <returns>HTTP/1.1 セッション</returns>
        private Session BuildSession()
            => new Session(this.PushRequest ?? this.BuildRequest(), this.BuildResponse());

        /// <summary>
        /// 読み取ったデータから HTTP/1.1 リクエストへマッピングして構築
        /// </summary>
        /// <returns>HTTP/1.1 リクエスト</returns>
        private HttpRequest BuildRequest()
        {
            return new HttpRequest(
                this.requestReader.Headers.ToRequestLine(),
                this.requestReader.Headers.ToHttpHeaders(),
                this.requestReader.Body,
                this.requestReader.Trailers.ToHttpHeaders());
        }

        /// <summary>
        /// 読み取ったデータから HTTP/1.1 レスポンスへマッピングして構築
        /// </summary>
        /// <returns>HTTP/1.1 レスポンス</returns>
        private HttpResponse BuildResponse()
        {
            return new HttpResponse(
                this.responseReader.Headers.ToStatusLine(),
                this.responseReader.Headers.ToHttpHeaders(),
                this.responseReader.Body,
                this.responseReader.Trailers.ToHttpHeaders());
        }

        /// <summary>
        /// PUSH_PROMISE 受信完了時に発生
        /// </summary>
        public event Action<(int StreamId, HttpRequest Request)> PushPromise;

        /// <summary>
        /// HTTP リクエスト送信完了時に発生
        /// </summary>
        public event Action<Http2StreamReader, HttpRequest> HttpRequestSent;

        /// <summary>
        /// HTTP レスポンス受信完了時に発生
        /// </summary>
        public event Action<Http2StreamReader, Session> HttpResponseSent;

        /// <summary>
        /// クライアント側 WebSocket メッセージ受信時に発生
        /// </summary>
        public event Action<IReadOnlyWebSocketMessage> ClientWebSocketMessageSent;

        /// <summary>
        /// サーバー側 WebSocket メッセージ受信時に発生
        /// </summary>
        public event Action<IReadOnlyWebSocketMessage> ServerWebSocketMessageSent;

        /// <summary>
        /// ストリームリセット時に発生
        /// </summary>
        public event Action<Http2StreamReader> Reset;
    }
}
