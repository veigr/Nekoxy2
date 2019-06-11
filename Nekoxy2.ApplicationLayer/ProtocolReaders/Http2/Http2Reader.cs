using Nekoxy2.ApplicationLayer.Entities.Http;
using Nekoxy2.ApplicationLayer.Entities.Http2;
using Nekoxy2.ApplicationLayer.ProtocolReaders.Http2.Hpack;
using Nekoxy2.Spi.Entities.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http2
{
    /// <summary>
    /// HTTP/2 フレームを入力し、HTTP/2 を読み取り
    /// </summary>
    /// <remarks>
    /// プロキシの処理上、リクエストとレスポンスのフレーム順序は多少前後する可能性があるため、前後しても大丈夫なよう作成。
    /// HPACK の仕様上、リクエスト・レスポンス各方向内では順序は保たれなければならない。
    /// </remarks>
    internal sealed class Http2Reader
    {
        /// <summary>
        /// HTTP/2 ストリームリスト
        /// </summary>
        private readonly ConcurrentDictionary<int, Http2StreamReader> streams
            = new ConcurrentDictionary<int, Http2StreamReader>();

        /// <summary>
        /// PUSH_PROMISE による予約リスト
        /// </summary>
        private readonly ConcurrentDictionary<int, HttpRequest> pushPromises
            = new ConcurrentDictionary<int, HttpRequest>();

        /// <summary>
        /// リクエスト側 HPACK デコーダー
        /// </summary>
        private readonly HpackDecoder requestHpackDecoder = new HpackDecoder();

        /// <summary>
        /// レスポンス側 HPACK デコーダー
        /// </summary>
        private readonly HpackDecoder responseHpackDecoder = new HpackDecoder();

        /// <summary>
        /// リクエスト側制御ストリームリーダー
        /// </summary>
        private readonly Http2ControlStreamReader requestControlStreamReader;

        /// <summary>
        /// レスポンス側制御ストリームリーダー
        /// </summary>
        private readonly Http2ControlStreamReader responseControlStreamReader;

        /// <summary>
        /// 最大キャプチャーサイズ
        /// </summary>
        private readonly int maxCaptureSize;

        /// <summary>
        /// 最大キャプチャーサイズを指定してインスタンスを作成
        /// </summary>
        /// <param name="maxCaptureSize"></param>
        public Http2Reader(int maxCaptureSize)
        {
            this.maxCaptureSize = maxCaptureSize;

            this.requestControlStreamReader = new Http2ControlStreamReader();
            this.responseControlStreamReader = new Http2ControlStreamReader();

            this.requestControlStreamReader.Partner = this.responseControlStreamReader;
            this.responseControlStreamReader.Partner = this.requestControlStreamReader;

            this.requestControlStreamReader.UpdateDynamicTableSize += this.UpdateDynamicTableSize;
            this.responseControlStreamReader.UpdateDynamicTableSize += this.UpdateDynamicTableSize;
        }

        /// <summary>
        /// 動的テーブルサイズ更新
        /// </summary>
        /// <param name="size">新しいサイズ</param>
        private void UpdateDynamicTableSize(uint size)
        {
            this.requestHpackDecoder.UpdateDynamicTableSize(size);
            this.responseHpackDecoder.UpdateDynamicTableSize(size);
        }

        /// <summary>
        /// リクエストフレームを入力
        /// </summary>
        /// <param name="frame">リクエストフレーム</param>
        public void HandleRequest(IHttp2Frame frame)
        {
            lock (this.streams)
            {
                Debug.WriteLine($"Request : {frame.ToString()}");

                if (frame.Header.StreamID == 0)
                {
                    this.requestControlStreamReader.HandleFrame(frame);
                }
                else
                {
                    if (frame is Http2RstStreamFrame rstFrame
                    && (rstFrame.ErrorCode == Http2ErrorCode.Cancel || rstFrame.ErrorCode == Http2ErrorCode.RefusedStream))
                    {
                        // PushPromise で予約した ID に対するキャンセル RFC7540 8.2.2
                        if (this.pushPromises.TryRemove(frame.Header.StreamID, out _))
                            return;
                    }
                    if (!this.streams.ContainsKey(frame.Header.StreamID))
                    {
                        this.AddStreamReader(frame);
                    }
                    this.streams[frame.Header.StreamID].HandleRequest(frame);
                }
            }
        }

        /// <summary>
        /// レスポンスフレームを入力
        /// </summary>
        /// <param name="frame">レスポンスフレーム</param>
        public void HandleResponse(IHttp2Frame frame)
        {
            lock (this.streams)
            {
                Debug.WriteLine($"Response: {frame.ToString()}");

                if (frame.Header.StreamID == 0)
                {
                    this.responseControlStreamReader.HandleFrame(frame);
                }
                else
                {
                    if (!this.streams.ContainsKey(frame.Header.StreamID))
                    {
                        this.AddStreamReader(frame);
                    }
                    this.streams[frame.Header.StreamID].HandleResponse(frame);
                }
            }
        }

        /// <summary>
        /// ストリームリストに新規の <see cref="Http2StreamReader"/> を追加
        /// </summary>
        /// <param name="frame">フレーム</param>
        private void AddStreamReader(IHttp2Frame frame)
        {
            Http2StreamReader reader;
            if (this.pushPromises.TryRemove(frame.Header.StreamID, out var request))
            {
                this.HttpRequestSent?.Invoke(request);
                // PUSH_PROMISE による予約済みストリーム
                reader = new Http2StreamReader(frame.Header.StreamID, this.requestHpackDecoder, this.responseHpackDecoder, this.maxCaptureSize, request);
            }
            else
            {
                // 通常のストリーム
                reader = new Http2StreamReader(frame.Header.StreamID, this.requestHpackDecoder, this.responseHpackDecoder, this.maxCaptureSize);
            }
            reader.PushPromise += this.OnPushPromise;
            reader.HttpRequestSent += this.OnHttpRequestSent;
            reader.HttpResponseSent += this.OnHttpResponseSent;
            reader.ClientWebSocketMessageSent += this.OnClientWebSocketMessageSent;
            reader.ServerWebSocketMessageSent += this.OnServerWebSocketMessageSent;
            reader.Reset += this.OnResetStream;
            this.streams.TryAdd(frame.Header.StreamID, reader);
        }

        /// <summary>
        /// ストリームリストから <see cref="Http2StreamReader"/> を削除
        /// </summary>
        /// <param name="reader">削除する <see cref="Http2StreamReader"/></param>
        private void RemoveStreamReader(Http2StreamReader reader)
        {
            reader.PushPromise -= this.OnPushPromise;
            reader.HttpRequestSent -= this.OnHttpRequestSent;
            reader.HttpResponseSent -= this.OnHttpResponseSent;
            reader.ClientWebSocketMessageSent -= this.OnClientWebSocketMessageSent;
            reader.ServerWebSocketMessageSent -= this.OnServerWebSocketMessageSent;
            reader.Reset -= this.OnResetStream;
            this.streams.TryRemove(reader.Id, out _);
        }

        /// <summary>
        /// <see cref="Http2StreamReader.PushPromise"/> ハンドラー
        /// </summary>
        /// <param name="promise">予約 ID とプッシュリクエスト</param>
        private void OnPushPromise((int StreamId, HttpRequest Request) promise)
        {
            if (this.streams.ContainsKey(promise.StreamId))
            {
                this.streams[promise.StreamId].PushRequest = promise.Request;
            }
            else
            {
                this.pushPromises.TryAdd(promise.StreamId, promise.Request);
            }
        }

        /// <summary>
        /// <see cref="Http2StreamReader.HttpRequestSent"/> ハンドラー
        /// </summary>
        /// <param name="reader">発生元</param>
        /// <param name="request">HTTP/1.1 リクエスト</param>
        private void OnHttpRequestSent(Http2StreamReader reader, HttpRequest request)
            => this.HttpRequestSent?.Invoke(request);

        /// <summary>
        /// <see cref="Http2StreamReader.HttpResponseSent"/> ハンドラー
        /// </summary>
        /// <param name="reader">発生元</param>
        /// <param name="session">HTTP/1.1 セッション</param>
        private void OnHttpResponseSent(Http2StreamReader reader, Session session)
        {
            this.RemoveStreamReader(reader);
            this.HttpResponseSent?.Invoke(session);
        }

        /// <summary>
        /// <see cref="Http2StreamReader.ClientWebSocketMessageSent"/> ハンドラー
        /// </summary>
        /// <param name="message">WebSocket メッセージ</param>
        private void OnClientWebSocketMessageSent(IReadOnlyWebSocketMessage message)
            => this.ClientWebSocketMessageSent?.Invoke(message);

        /// <summary>
        /// <see cref="Http2StreamReader.ServerWebSocketMessageSent"/> ハンドラー
        /// </summary>
        /// <param name="message">WebSocket メッセージ</param>
        private void OnServerWebSocketMessageSent(IReadOnlyWebSocketMessage message)
            => this.ServerWebSocketMessageSent?.Invoke(message);

        /// <summary>
        /// <see cref="Http2StreamReader.Reset"/> ハンドラー
        /// </summary>
        /// <param name="reader">発生元</param>
        private void OnResetStream(Http2StreamReader reader)
            => this.RemoveStreamReader(reader);

        /// <summary>
        /// リクエスト送信完了時に発生
        /// </summary>
        public event Action<HttpRequest> HttpRequestSent;

        /// <summary>
        /// レスポンス受信完了時に発生
        /// </summary>
        public event Action<Session> HttpResponseSent;

        /// <summary>
        /// クライアント WebSocket メッセージ受信時に発生
        /// </summary>
        public event Action<IReadOnlyWebSocketMessage> ClientWebSocketMessageSent;

        /// <summary>
        /// サーバー WebSocket メッセージ受信完了時に発生
        /// </summary>
        public event Action<IReadOnlyWebSocketMessage> ServerWebSocketMessageSent;
    }
}
