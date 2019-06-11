using Nekoxy2.ApplicationLayer.Entities.WebSocket;
using Nekoxy2.Spi.Entities.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.WebSocket
{
    /// <summary>
    /// <see cref="WebSocketMessage"/> を構築
    /// </summary>
    internal sealed class WebSocketMessageBuilder
    {
        /// <summary>
        /// ハンドシェイクセッション
        /// </summary>
        private readonly IReadOnlySession handshakeSession;

        /// <summary>
        /// メッセージを構成するフレームのバッファー
        /// </summary>
        private readonly Queue<WebSocketFrame> frameBuffer = new Queue<WebSocketFrame>();

        /// <summary>
        /// 最大キャプチャーサイズ
        /// </summary>
        private readonly int maxCaptureSize;

        /// <summary>
        /// ハンドシェイクセッションと最大キャプチャーサイズを指定してインスタンスを作成
        /// </summary>
        /// <param name="handshakeSession">ハンドシェイクセッション</param>
        /// <param name="maxCaptureSize">最大キャプチャーサイズ</param>
        public WebSocketMessageBuilder(IReadOnlySession handshakeSession, int maxCaptureSize)
        {
            this.handshakeSession = handshakeSession;
            this.maxCaptureSize = maxCaptureSize;
        }

        /// <summary>
        /// フレームを追加し、メッセージ構築を試行
        /// </summary>
        /// <param name="frame">フレーム</param>
        /// <param name="message">メッセージ</param>
        /// <returns>構築されたかどうか</returns>
        public bool TryCreateOrAdd(WebSocketFrame frame, out WebSocketMessage message)
        {
            // 拡張データが有る場合の断片化の扱いが難しそうだが、現状では拡張データの使用例はないらしい。
            // PMCEs(RFC7692)では使用していない＆対応していない拡張がある場合は読み込み中断するため、拡張データは考慮しない。
            lock (this.frameBuffer)
            {
                this.frameBuffer.Enqueue(frame);
                if (!frame.Fin)
                {
                    message = default;
                    return false;
                }

                var firstFrame = this.frameBuffer.First();   // dequeue 前にとっとく

                var size = this.frameBuffer.Sum(x => x.PayloadLength);

                byte[] data;
                if (size <= this.maxCaptureSize)
                {
                    data = new byte[size];
                    var index = 0;
                    while (0 < this.frameBuffer.Count)
                    {
                        var f = this.frameBuffer.Dequeue();
                        Buffer.BlockCopy(f.PayloadData, 0, data, index, f.PayloadData.Length);
                        index += f.PayloadData.Length;
                    }
                }
                else
                {
                    data = Array.Empty<byte>();
                }

                message = new WebSocketMessage(this.handshakeSession, firstFrame.Opcode, data, firstFrame.Rsv1);

                this.frameBuffer.Clear();
                return true;
            }
        }
    }
}
