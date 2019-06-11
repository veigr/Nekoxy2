using Nekoxy2.ApplicationLayer.Entities.Http2;
using Nekoxy2.ApplicationLayer.ProtocolReaders.Http2.Hpack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http2
{
    /// <summary>
    /// HTTP/2 フレームを入力し、リクエストかレスポンス片方向の HTTP/2 ストリームを読み取り
    /// </summary>
    internal sealed class Http2OneSideStreamReader
    {
        /// <summary>
        /// ヘッダー
        /// </summary>
        public IReadOnlyList<(string Name, string Value)> Headers { get; private set; }

        /// <summary>
        /// ボディー
        /// </summary>
        public byte[] Body { get; private set; }

        /// <summary>
        /// トレイラー
        /// </summary>
        public IReadOnlyList<(string Name, string Value)> Trailers { get; private set; }

        /// <summary>
        /// HPACK デコーダー
        /// </summary>
        public HpackDecoder Decoder { get; }

        /// <summary>
        /// ロックオブジェクト
        /// </summary>
        private readonly object lockObject = new object();

        /// <summary>
        /// 受信フレームリスト
        /// </summary>
        private readonly IList<IHttp2Frame> frames = new List<IHttp2Frame>();

        /// <summary>
        /// END_STREAM フラグを受信したかどうか
        /// </summary>
        private bool isEndStream = false;

        /// <summary>
        /// 最大キャプチャーサイズ
        /// </summary>
        private readonly int maxCaptureSize;

        /// <summary>
        /// 現在キャプチャーサイズ
        /// </summary>
        private decimal currentCaptureSize;

        /// <summary>
        /// HPACK デコーダーと最大キャプチャーサイズを指定してインスタンスを作成
        /// </summary>
        /// <param name="decoder">HPACK デコーダー</param>
        /// <param name="maxCaptureSize">最大キャプチャーサイズ</param>
        public Http2OneSideStreamReader(HpackDecoder decoder, int maxCaptureSize = int.MaxValue)
        {
            this.Decoder = decoder;
            this.maxCaptureSize = maxCaptureSize;
        }

        /// <summary>
        /// フレームを受信
        /// </summary>
        /// <param name="frame">フレーム</param>
        public void HandleFrame(IHttp2Frame frame)
        {
            lock (this.lockObject)
            {
                switch (frame)
                {
                    case Http2HeadersFrame f:
                        this.frames.Add(frame);
                        this.isEndStream = f.IsEndStream;
                        if (f.IsEndHeaders)
                        {
                            this.OnEndHeaders();

                            if (f.IsEndStream)
                            {
                                this.OnEndStream();
                            }
                        }
                        break;

                    case Http2PushPromiseFrame f:
                        this.frames.Add(frame);
                        if (f.IsEndHeaders)
                        {
                            this.OnEndPushPromise(f);
                        }
                        break;

                    case Http2ContinuationFrame f:
                        this.frames.Add(frame);

                        var parent = this.frames
                            .Last(x => x is Http2HeadersFrame || x is Http2PushPromiseFrame);

                        if (f.IsEndHeaders)
                        {
                            if (parent is Http2HeadersFrame)
                            {
                                this.OnEndHeaders();
                            }
                            else if (parent is Http2PushPromiseFrame pushPromiseFrame)
                            {
                                this.OnEndPushPromise(pushPromiseFrame);
                            }
                        }
                        if (this.isEndStream && f.IsEndHeaders)
                        {
                            this.OnEndStream();
                        }
                        break;

                    case Http2DataFrame f:
                        if (this.currentCaptureSize <= this.maxCaptureSize)
                        {
                            this.frames.Add(frame);
                            this.currentCaptureSize += f.Data.Length;
                            if (this.maxCaptureSize < this.currentCaptureSize)
                            {
                                var dataFrames = this.frames.OfType<Http2DataFrame>().ToArray();
                                foreach (var dataFrame in dataFrames)
                                {
                                    this.frames.Remove(dataFrame);
                                }
                            }
                        }
                        if (f.IsEndStream)
                        {
                            this.OnEndStream();
                        }
                        break;

                    case Http2RstStreamFrame f:
                        this.Reset?.Invoke();
                        break;

                    case Http2GoawayFrame f:
                        this.Reset?.Invoke();
                        break;
                }
            }
        }
        
        /// <summary>
        /// PUSH_PROMISE 終端処理
        /// </summary>
        /// <param name="f">PUSH_PROMISE フレーム</param>
        private void OnEndPushPromise(Http2PushPromiseFrame f)
        {
            // PushPromise の動的テーブルはレスポンス側の物を用いるらしい
            this.PushPromise?.Invoke((f.PromisedStreamID, this.Decoder.Decode(this.frames.BuildHeadersBlock())));
        }

        /// <summary>
        /// HEADERS 終端処理
        /// </summary>
        private void OnEndHeaders()
        {
            var headerBlock = this.Decoder.Decode(this.frames.BuildHeadersBlock());

            // 1xx レスポンスは無視する
            if (headerBlock.GetFirstValue(":status")?.StartsWith("1") == true)
                return;

            if (this.Headers == null)
                this.Headers = headerBlock;
            else
                this.Trailers = headerBlock;
        }

        /// <summary>
        /// ストリーム終端処理
        /// </summary>
        private void OnEndStream()
        {
            if (this.currentCaptureSize <= this.maxCaptureSize)
                this.Body = this.frames.BuildBody();
            else
                this.Body = Array.Empty<byte>();

            this.EndStream?.Invoke();
        }

        /// <summary>
        /// PUSH_PROMISE 受信完了時に発生
        /// </summary>
        public event Action<(int StreamId, IReadOnlyList<(string Name, string Value)> Headers)> PushPromise;

        /// <summary>
        /// ストリーム終了時に発生
        /// </summary>
        public event Action EndStream;

        /// <summary>
        /// ストリームリセット時に発生
        /// </summary>
        public event Action Reset;
    }
}
