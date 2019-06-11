using System;
using System.IO;

namespace Nekoxy2.ApplicationLayer.MessageBodyParsers
{
    /// <summary>
    /// Content-Length ボディー解析器
    /// </summary>
    internal sealed class ContentLengthBodyParser : IMessageBodyParser
    {
        /// <summary>
        /// ロックオブジェクト
        /// </summary>
        private readonly object lockObject = new object();

        /// <summary>
        /// ボディーバッファー
        /// </summary>
        private MemoryStream bodyStream;

        /// <summary>
        /// 現在書き込まれた長さ
        /// </summary>
        private decimal length;

        /// <summary>
        /// Content-Length
        /// </summary>
        private readonly decimal contentLength;

        /// <summary>
        /// メッセージボディー
        /// </summary>
        public byte[] Body
            => this.bodyStream?.ToArray() ?? Array.Empty<byte>();

        /// <summary>
        /// トレイラー
        /// </summary>
        public string Trailers => "";

        /// <summary>
        /// 終端に達したかどうか
        /// </summary>
        public bool IsTerminated
            => this.contentLength <= this.length;

        /// <summary>
        /// ボディをキャプチャーするかどうか
        /// </summary>
        public bool isCaptureBody;

        /// <summary>
        /// 最大キャプチャーサイズ
        /// </summary>
        private readonly int maxCaptureSize = int.MaxValue;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="isCaptureBody">ボディをキャプチャーするかどうか</param>
        /// <param name="maxCaptureSize">最大キャプチャーサイズ</param>
        /// <param name="contentLength">Content-Length</param>
        public ContentLengthBodyParser(bool isCaptureBody, int maxCaptureSize, decimal contentLength)
        {
            this.isCaptureBody = isCaptureBody;
            this.maxCaptureSize = maxCaptureSize;
            this.contentLength = contentLength;
            if (this.contentLength <= this.maxCaptureSize)
                this.bodyStream = new MemoryStream((int)this.contentLength);
        }

        /// <summary>
        /// 1 バイト書き込み
        /// </summary>
        /// <param name="b">バイトデータ</param>
        public void WriteByte(byte b)
            => this.Write(new[] { b });

        /// <summary>
        /// LF まで書き込み
        /// </summary>
        /// <param name="buffer">書き込むバイト配列</param>
        /// <returns>書き込んだバイト数</returns>
        public int Write(byte[] buffer)
        {
            lock (this.lockObject)
            {
                var remaining = this.contentLength - this.length;
                var size = remaining < buffer.Length ? (int)remaining : buffer.Length;
                this.length += size;

                if (this.isCaptureBody
                && this.length <= this.maxCaptureSize)
                {
                    this.bodyStream?.Write(buffer, 0, size);
                }
                return size;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        private void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // マネージド状態を破棄します (マネージド オブジェクト)。
                    this.bodyStream?.Dispose();
                }

                // アンマネージド リソース (アンマネージド オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // 大きなフィールドを null に設定します。
                this.bodyStream = null;

                this.disposedValue = true;
            }
        }

        // 上の Dispose(bool disposing) にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~ContentLengthBody() {
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
}
