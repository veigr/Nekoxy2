using Nekoxy2.ApplicationLayer.Entities.Http;
using Nekoxy2.ApplicationLayer.Entities.WebSocket;
using Nekoxy2.Spi.Entities.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.WebSocket
{
    /// <summary>
    /// 通信データを入力し、WebSocket プロトコルとして読み取り
    /// </summary>
    internal sealed class WebSocketReader : IProtocolReader
    {
        /// <summary>
        /// ハンドシェイクに用いた <see cref="IReadOnlySession"/>
        /// </summary>
        private readonly IReadOnlySession handshakeSession;

        /// <summary>
        /// Per-Message Compression Extension を処理
        /// </summary>
        private readonly AggregatePMCE pmces;

        /// <summary>
        /// <see cref="WebSocketFrame"/> を構築
        /// </summary>
        private readonly WebSocketFrameBuilder frameBuilder;

        /// <summary>
        /// <see cref="WebSocketMessage"/> を構築
        /// </summary>
        private readonly WebSocketMessageBuilder messageBuilder;

        /// <summary>
        /// 最大キャプチャーサイズ
        /// </summary>
        private readonly int maxCaptureSize;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="handshakeSession">ハンドシェイクに用いた <see cref="IReadOnlySession"/></param>
        /// <param name="pmces">Per-Message Compression Extension リスト</param>
        /// <param name="maxCaptureSize">最大キャプチャーサイズ</param>
        private WebSocketReader(IReadOnlySession handshakeSession, IEnumerable<IPerMessageCompressionExtension> pmces, int maxCaptureSize)
        {
            this.maxCaptureSize = maxCaptureSize;
            this.handshakeSession = handshakeSession;
            this.pmces = new AggregatePMCE(pmces);
            this.messageBuilder = new WebSocketMessageBuilder(handshakeSession, this.maxCaptureSize);
            this.frameBuilder = new WebSocketFrameBuilder(this.maxCaptureSize);
        }

        /// <summary>
        /// データ受信
        /// </summary>
        /// <param name="buffer">受信バッファー</param>
        /// <param name="readSize">読み取りサイズ</param>
        public void HandleReceive(byte[] buffer, int readSize)
        {
            lock (this.pmces)
            {
                var readed = 0;
                while (readed < readSize)
                {
                    if (this.frameBuilder.TryAddData(buffer, readed, readSize, out var size, out var frame))
                    {
                        Debug.WriteLine(frame.ToString());
                        if (frame.FrameType == WebSocketFrameType.Control)
                        {
                            // 断片化されたデータフレームの合間に制御フレームが挿入されることはあり得る RFC6455 5.4
                            var message = new WebSocketMessage(this.handshakeSession, frame);
                            this.MessageReceived?.Invoke(this.pmces.Decompress(message));
                        }
                        else if (frame.FrameType == WebSocketFrameType.Data
                        && this.messageBuilder.TryCreateOrAdd(frame, out var message))
                        {
                            this.MessageReceived?.Invoke(this.pmces.Decompress(message));
                        }
                    }
                    readed += size;
                }
            }
        }

        /// <summary>
        /// <see cref="WebSocketMessage"/> を受信完了時に発生
        /// </summary>
        public event Action<WebSocketMessage> MessageReceived;

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        private void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // マネージド状態を破棄します (マネージド オブジェクト)。
                    this.frameBuilder?.Dispose();
                }

                // アンマネージド リソース (アンマネージド オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // 大きなフィールドを null に設定します。

                this.disposedValue = true;
            }
        }

        // 上の Dispose(bool disposing) にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~WebSocketReader() {
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

        /// <summary>
        /// ハンドシェイクセッションと最大キャプチャーサイズを指定してインスタンスを作成
        /// </summary>
        /// <param name="handshakeSession">ハンドシェイクセッション</param>
        /// <param name="maxCaptureSize">最大キャプチャーサイズ</param>
        /// <returns></returns>
        public static WebSocketReader Create(Session handshakeSession, int maxCaptureSize)
        {
            if (handshakeSession?.Response?.Headers?.SecWebSocketExtensions?.Exists != true)
                return new WebSocketReader(handshakeSession, default, maxCaptureSize);

            // 拡張はヘッダ値の順序に適用する RFC6455 9.1
            var pmces = new List<IPerMessageCompressionExtension>();
            foreach (var extension in handshakeSession.Response.Headers.SecWebSocketExtensions)
            {
                var name = extension.Split(new[] { ';' })[0].Trim();

                // 今の所存在する拡張は PMCE(RFC7692) の deflate のみ。
                var pmce = supportedPMCEs.FirstOrDefault(x => x.Name == name);
                if (pmce == default)
                    // サポートしていない拡張は正しく解釈できない可能性が高いので読み込みをやめる
                    throw new UnsupportedExtensionException($"{name} is not supported WebSocket Extension.");
                pmces.Add(pmce);
            }
            return new WebSocketReader(handshakeSession, pmces, maxCaptureSize);
        }

        /// <summary>
        /// テスト用ファクトリー
        /// </summary>
        /// <param name="isEnableDeflate">permessage-deflate の指定有無</param>
        /// <param name="maxCaptureSize">最大キャプチャーサイズ</param>
        /// <returns></returns>
        internal static WebSocketReader Create(bool isEnableDeflate, int maxCaptureSize)
        {
            return new WebSocketReader(null,
                isEnableDeflate ? new[] { new PerMessageDeflateExtension() } : default,
                maxCaptureSize);
        }

        /// <summary>
        /// サポートされている Per-Message Compression Extension
        /// </summary>
        private static readonly IEnumerable<IPerMessageCompressionExtension> supportedPMCEs
            = new[] { new PerMessageDeflateExtension() };
    }
}
