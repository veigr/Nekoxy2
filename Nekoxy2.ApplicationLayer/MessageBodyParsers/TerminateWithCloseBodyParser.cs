using System;
using System.IO;

namespace Nekoxy2.ApplicationLayer.MessageBodyParsers
{
    /// <summary>
    /// コネクション切断により終端するボディ解析器
    /// </summary>
    internal sealed class TerminateWithCloseBodyParser : IMessageBodyParser
    {
        /// <summary>
        /// ロックオブジェクト
        /// </summary>
        private readonly object lockObject = new object();

        /// <summary>
        /// ボディーバッファー
        /// </summary>
        private MemoryStream bodyStream = new MemoryStream();

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
            => false;

        /// <summary>
        /// ボディーをキャプチャーするかどうか
        /// </summary>
        private bool isCaptureBody;

        /// <summary>
        /// 最大キャプチャーサイズ
        /// </summary>
        private readonly int maxCaptureSize = int.MaxValue;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="isCaptureBody">ボディーをキャプチャーするかどうか</param>
        /// <param name="maxCaptureSize">最大キャプチャーサイズ</param>
        public TerminateWithCloseBodyParser(bool isCaptureBody, int maxCaptureSize)
        {
            this.isCaptureBody = isCaptureBody;
            this.maxCaptureSize = maxCaptureSize;
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
                if (this.isCaptureBody)
                {
                    if (this.maxCaptureSize <= this.bodyStream.Length + buffer.Length)
                    {
                        this.isCaptureBody = false;
                        this.bodyStream.Dispose();
                        this.bodyStream = null;
                    }
                    else
                    {
                        this.bodyStream?.Write(buffer, 0, buffer.Length);
                    }
                }
                return buffer.Length;
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
