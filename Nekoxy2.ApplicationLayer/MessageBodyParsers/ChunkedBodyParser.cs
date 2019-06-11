using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Nekoxy2.ApplicationLayer.MessageBodyParsers
{
    /// <summary>
    /// チャンク化ボディー解析器
    /// </summary>
    internal sealed class ChunkedBodyParser : IMessageBodyParser
    {
        /// <summary>
        /// チャンク化解除をするかどうか
        /// </summary>
        private readonly bool dechunke = false;

        /// <summary>
        /// ボディーバッファー
        /// </summary>
        private MemoryStream body = new MemoryStream();

        /// <summary>
        /// 行バッファー
        /// </summary>
        private MemoryStream line = new MemoryStream();

        /// <summary>
        /// トレイラーバッファー
        /// </summary>
        private readonly StringBuilder trailerBuilder = new StringBuilder();

        /// <summary>
        /// 現在のチャンクサイズ
        /// </summary>
        private int chunkSize;

        /// <summary>
        /// 現在のチャンクパート
        /// </summary>
        private ChunkPart chunkPart = ChunkPart.Size;

        /// <summary>
        /// ロックオブジェクト
        /// </summary>
        private readonly object lockObject = new object();

        /// <summary>
        /// メッセージボディー
        /// </summary>
        public byte[] Body
            => this.body?.ToArray() ?? Array.Empty<byte>();

        /// <summary>
        /// トレイラー
        /// </summary>
        public string Trailers
            => this.trailerBuilder.ToString();

        /// <summary>
        /// 終端に達したかどうか
        /// </summary>
        public bool IsTerminated { get; private set; }

        /// <summary>
        /// ボディをキャプチャーするかどうか
        /// </summary>
        private bool isCaptureBody;

        /// <summary>
        /// 最大キャプチャーサイズ
        /// </summary>
        private readonly int maxCaptureSize = int.MaxValue;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="isCaptureBody">ボディをキャプチャーするかどうか</param>
        /// <param name="maxCaptureSize">最大キャプチャーサイズ</param>
        /// <param name="dechunk">チャンク化を解除するかどうか</param>
        public ChunkedBodyParser(bool isCaptureBody, int maxCaptureSize, bool dechunk = false)
        {
            this.isCaptureBody = isCaptureBody;
            this.maxCaptureSize = maxCaptureSize;
            this.dechunke = dechunk;
        }

        /// <summary>
        /// 1 バイト書き込み
        /// </summary>
        /// <param name="b">バイトデータ</param>
        public void WriteByte(byte b)
        {
            lock (this.lockObject)
                this.WriteUntilLF(new byte[] { b });
        }

        /// <summary>
        /// 書き込み
        /// </summary>
        /// <param name="buffer">書き込むバイト配列</param>
        /// <returns>書き込んだバイト数</returns>
        public int Write(byte[] buffer)
        {
            lock (this.lockObject)
            {
                // LF まで書き込む
                var position = Array.IndexOf(buffer, '\n', 0);
                var length = -1 < position ? position + 1 : buffer.Length;
                var bytes = new byte[length];
                Buffer.BlockCopy(buffer, 0, bytes, 0, length);
                this.WriteUntilLF(bytes);
                return length;
            }
        }

        /// <summary>
        /// LF まで書き込み
        /// </summary>
        /// <param name="buffer">書き込むバイト配列</param>
        private void WriteUntilLF(byte[] buffer)
        {
            if (!this.dechunke)
                this.WriteByteInternal(buffer, buffer.Length);

            this.line.Write(buffer, 0, buffer.Length);

            if (this.chunkPart == ChunkPart.Data
            && this.line.Length - 2 < this.chunkSize)
                return;

            if (buffer.Last() != '\n')
                return;

            if (!this.line.CurrentWithNewLine())
                return;

            switch (this.chunkPart)
            {
                case ChunkPart.Size:
                    this.ReadSize();
                    break;
                case ChunkPart.Data:
                    this.ReadChunk();
                    break;
                case ChunkPart.Trailer:
                    this.ReadTrailer();
                    break;
                default:
                    break;
            }
            this.line.Dispose();
            this.line = new MemoryStream();
        }

        /// <summary>
        /// チャンクサイズ読み取り
        /// </summary>
        private void ReadSize()
        {
            try
            {
                var line = Encoding.ASCII.GetString(this.line.ToArray());
                // RFC7230 4.1.1 認識できないチャンク拡張は無視しなければならない
                var size = line.Split(new[] { ";", "\r\n" }, StringSplitOptions.None)[0];
                this.chunkSize = int.Parse(size, System.Globalization.NumberStyles.HexNumber);
                if (this.chunkSize == 0)
                {
                    this.chunkPart = ChunkPart.Trailer;
                    return;
                }
            }
            catch (Exception e)
            {
                throw new InvalidChunkException("Could not read chunk-size.", e);
            }
            this.chunkPart = ChunkPart.Data;
        }

        /// <summary>
        /// チャンクデータ読み取り
        /// </summary>
        private void ReadChunk()
        {
            this.chunkPart = ChunkPart.Size;
            if (this.dechunke)
            {
                this.WriteDechunkedBody();
            }
        }

        /// <summary>
        /// チャンク化解除されたデータを書き込み
        /// </summary>
        private void WriteDechunkedBody()
        {
            var lineBytes = this.line.ToArray();
            this.WriteByteInternal(lineBytes, lineBytes.Length - 2);
        }

        /// <summary>
        /// ボディーバッファーに書き込み
        /// </summary>
        /// <param name="buffer">書き込むバイト配列</param>
        /// <param name="length">書き込む長さ</param>
        private void WriteByteInternal(byte[] buffer, int length)
        {
            if (this.isCaptureBody)
            {
                if (this.body.Length + length < this.maxCaptureSize)
                {
                    this.body.Write(buffer, 0, length);
                }
                else
                {
                    this.isCaptureBody = false;
                    this.body.Dispose();
                    this.body = null;
                }
            }
        }

        /// <summary>
        /// トレイラーを読み取り
        /// </summary>
        private void ReadTrailer()
        {
            if (!this.line.IsNewLine())
            {
                this.trailerBuilder.Append(Encoding.ASCII.GetString(this.line.ToArray()));
                return;
            }
            if (0 < this.trailerBuilder.Length)
                this.trailerBuilder.AppendLine();

            // RFC7230 4.1 chunked メッセージの末尾はゼロチャンク受信後の空行
            this.IsTerminated = true;
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
                    this.body?.Dispose();
                    this.line?.Dispose();
                }

                // アンマネージド リソース (アンマネージド オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // 大きなフィールドを null に設定します。
                this.body = null;

                this.disposedValue = true;
            }
        }

        // 上の Dispose(bool disposing) にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~ChunkedBody() {
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
        /// チャンクパート
        /// </summary>
        private enum ChunkPart
        {
            /// <summary>
            /// チャンクサイズパート
            /// </summary>
            Size,

            /// <summary>
            /// チャンクデータパート
            /// </summary>
            Data,

            /// <summary>
            /// チャンクトレイラーパート
            /// </summary>
            Trailer,
        }
    }
}
