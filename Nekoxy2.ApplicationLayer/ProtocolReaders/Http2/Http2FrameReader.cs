using Nekoxy2.ApplicationLayer.Entities.Http2;
using System;
using System.IO;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http2
{
    /// <summary>
    /// 通信データを入力し、HTTP/2 フレームとして読み取り
    /// </summary>
    internal sealed class Http2FrameReader : IProtocolReader
    {
        /// <summary>
        /// ロックオブジェクト
        /// </summary>
        private readonly object lockObject = new object();

        /// <summary>
        /// 読み取りパート
        /// </summary>
        private Part part = Part.Header;

        /// <summary>
        /// ヘッダーバッファー
        /// </summary>
        private MemoryStream headerStream = new MemoryStream();

        /// <summary>
        /// 現在読み取り中のフレームのヘッダー
        /// </summary>
        private Http2FrameHeader currentHeader;

        /// <summary>
        /// ペイロードバッファー
        /// </summary>
        private MemoryStream payloadStream = new MemoryStream();

        /// <summary>
        /// スキップするべき Client Connection Preface の長さ
        /// </summary>
        private int skipPrefaceCount;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="type">エンドポイントの種類</param>
        public Http2FrameReader(EndPointType type)
        {
            if (type == EndPointType.Client)
            {
                // RFC7540 3.5
                // Skip Client Connection Preface "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"
                this.skipPrefaceCount = 24;
            }
        }

        /// <summary>
        /// データ受信
        /// </summary>
        /// <param name="buffer">受信バッファー</param>
        /// <param name="readSize">読み取りサイズ</param>
        public void HandleReceive(byte[] buffer, int readSize)
        {
            lock (this.lockObject)
            {
                for (var i = 0; i < readSize;)
                {
                    if (0 < this.skipPrefaceCount)
                    {
                        this.skipPrefaceCount--;
                        i++;
                        continue;
                    }
                    else if (this.part == Part.Header)
                    {
                        if (readSize - i + this.headerStream.Length < Http2FrameHeader.HeaderSize)
                        {
                            this.headerStream.Write(buffer, i, readSize - i);
                            break;
                        }
                        else
                        {
                            var size = Http2FrameHeader.HeaderSize - (int)this.headerStream.Length;
                            this.headerStream.Write(buffer, i, size);
                            i += size;

                            this.currentHeader = new Http2FrameHeader(this.headerStream.ToArray());
                            this.headerStream.Dispose();
                            this.headerStream = new MemoryStream();
                            if (this.currentHeader.Length == 0)
                            {
                                this.InvokeFrameReceived();
                            }
                            else
                            {
                                this.part = Part.Payload;
                            }
                        }
                    }
                    else if (this.part == Part.Payload)
                    {
                        if (readSize - i + this.payloadStream.Length < this.currentHeader.Length)
                        {
                            this.payloadStream.Write(buffer, i, readSize - i);
                            break;
                        }
                        else
                        {
                            var size = this.currentHeader.Length - (int)this.payloadStream.Length;
                            this.payloadStream.Write(buffer, i, size);
                            i += size;

                            this.InvokeFrameReceived();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// <see cref="FrameReceived"/> イベントを発生させます
        /// </summary>
        private void InvokeFrameReceived()
        {
            try
            {
                var frame = this.currentHeader.CreateFrame(this.payloadStream.ToArray());

                if (frame != null)
                    this.FrameReceived?.Invoke(frame);
            }
            finally
            {
                this.payloadStream?.Dispose();
                this.payloadStream = new MemoryStream();
                this.currentHeader = null;
                this.part = Part.Header;
            }
        }

        /// <summary>
        /// フレーム受信完了時に発生
        /// </summary>
        public event Action<IHttp2Frame> FrameReceived;

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        private void Dispose(bool disposing)
        {
            lock (this.lockObject)
            {
                if (!this.disposedValue)
                {
                    if (disposing)
                    {
                        // マネージド状態を破棄します (マネージド オブジェクト)。
                        this.headerStream?.Dispose();
                        this.payloadStream?.Dispose();
                    }

                    // アンマネージド リソース (アンマネージド オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                    // 大きなフィールドを null に設定します。

                    this.headerStream = null;
                    this.payloadStream = null;

                    this.disposedValue = true;
                }
            }
        }

        // 上の Dispose(bool disposing) にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~Http2FrameReader() {
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
        /// 読み取りパート
        /// </summary>
        private enum Part
        {
            /// <summary>
            /// ヘッダー
            /// </summary>
            Header,
            /// <summary>
            /// ペイロード
            /// </summary>
            Payload,
        }
    }

    /// <summary>
    /// エンドポイントの種類
    /// </summary>
    internal enum EndPointType
    {
        /// <summary>
        /// クライアントサイド
        /// </summary>
        Client,
        /// <summary>
        /// サーバーサイド
        /// </summary>
        Server,
    }
}
