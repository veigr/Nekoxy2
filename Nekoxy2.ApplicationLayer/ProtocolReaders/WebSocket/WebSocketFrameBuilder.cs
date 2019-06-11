using Nekoxy2.ApplicationLayer.Entities.WebSocket;
using Nekoxy2.Spi.Entities.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.WebSocket
{
    /// <summary>
    /// <see cref="WebSocketFrame"/> を構築
    /// </summary>
    internal sealed class WebSocketFrameBuilder : IDisposable
    {
        /// <summary>
        /// ロックオブジェクト
        /// </summary>
        private readonly object lockObject = new object();

        /// <summary>
        /// ヘッダーバッファー
        /// </summary>
        private List<byte> headerBuffer = new List<byte>(14);

        /// <summary>
        /// ペイロードバッファー
        /// </summary>
        private MemoryStream payloadBuffer;

        /// <summary>
        /// 読み込んだペイロードサイズ
        /// </summary>
        private decimal payloadCounter;

        /// <summary>
        /// 構築したフレーム
        /// </summary>
        internal WebSocketFrame Frame { get; private set; }

        /// <summary>
        /// 最大キャプチャーサイズ
        /// </summary>
        private readonly int maxCaptureSize;

        /// <summary>
        /// 構築完了したかどうか
        /// </summary>
        private bool IsCapture => this.Frame?.PayloadLength < this.maxCaptureSize;

        /// <summary>
        /// 最大キャプチャーサイズを指定してインスタンスを作成
        /// </summary>
        /// <param name="maxCaptureSize">最大キャプチャーサイズ</param>
        public WebSocketFrameBuilder(int maxCaptureSize)
        {
            this.maxCaptureSize = maxCaptureSize;
            this.Frame = new WebSocketFrame();
        }

        /// <summary>
        /// データを入力し、<see cref="WebSocketFrame"/> の構築を試行
        /// </summary>
        /// <param name="buffer">入力データ</param>
        /// <param name="endIndex">入力長</param>
        /// <param name="readSize">実際に入力された長さ</param>
        /// <param name="frame">構築された <see cref="WebSocketFrame"/></param>
        /// <returns>構築が完了したかどうか</returns>
        public bool TryAddData(byte[] buffer, int startIndex, int endIndex, out int readSize, out WebSocketFrame frame)
        {
            lock (this.lockObject)
            {
                for (var i = startIndex; i < endIndex;)
                {
                    if (this.payloadBuffer == null)
                    {
                        if (this.TryAddHeaderData(buffer[i++]))
                        {
                            if (this.Frame.PayloadLength == 0)
                            {
                                frame = this.Frame;
                                readSize = i - startIndex;
                                this.Clear();
                                return true;
                            }

                            this.payloadBuffer = new MemoryStream((int)this.Frame.PayloadLength);
                        }
                    }
                    else
                    {

                        var remaining = this.Frame.PayloadLength - this.payloadCounter;
                        var size = remaining < (endIndex - i) ? (int)remaining : (endIndex - i);
                        if (this.IsCapture)
                        {
                            this.payloadBuffer.Write(buffer, i, size);
                        }
                        this.payloadCounter += size;
                        i += size;
                        if (this.Frame.PayloadLength <= this.payloadCounter)
                        {
                            try
                            {
                                this.CreatePayloadData();
                                frame = this.Frame;
                                readSize = i - startIndex;
                                return true;
                            }
                            finally
                            {
                                this.Clear();
                            }
                        }
                    }
                }
                readSize = endIndex - startIndex;
                frame = default;
                return false;
            }
        }

        /// <summary>
        /// ペイロードデータを構築
        /// </summary>
        private void CreatePayloadData()
        {
            this.Frame.PayloadData = this.payloadBuffer?.ToArray() ?? Array.Empty<byte>();
            this.payloadBuffer?.Dispose();
            this.payloadBuffer = null;
            this.payloadCounter = 0;
            if (this.Frame.Mask)
            {
                for (var i = 0; i < this.Frame.PayloadData.Length; i++)
                {
                    this.Frame.PayloadData[i] ^= this.Frame.MaskKey[i % 4];
                }
            }
        }

        /// <summary>
        /// ヘッダーデータを入力し、完了を確認
        /// </summary>
        /// <param name="value">ヘッダーデータ</param>
        /// <returns>ヘッダーの構築が完了したかどうか</returns>
        private bool TryAddHeaderData(byte value)
        {
            // header bytes: 1st, 2nd, ext(2byte/8byte), key(4byte)
            this.headerBuffer.Add(value);

            if (this.headerBuffer.Count == 1)
            {
                this.Frame.Fin = (this.headerBuffer[0] & 0b10000000) == 0b10000000;
                this.Frame.Rsv1 = (this.headerBuffer[0] & 0b01000000) == 0b01000000;
                this.Frame.Rsv2 = (this.headerBuffer[0] & 0b00100000) == 0b00100000;
                this.Frame.Rsv3 = (this.headerBuffer[0] & 0b00010000) == 0b00010000;
                this.Frame.Opcode = (WebSocketOpcode)(this.headerBuffer[0] & 0b00001111);
            }
            else if (this.headerBuffer.Count == 2)
            {
                this.Frame.Mask = (this.headerBuffer[1] & 0b10000000) == 0b10000000;
                this.Frame.PayloadLengthFirst7bits = (sbyte)(this.headerBuffer[1] & 0b01111111);

                this.Frame.PayloadExLength
                    = this.Frame.PayloadLengthFirst7bits == 126 ? 2
                    : this.Frame.PayloadLengthFirst7bits == 127 ? 8
                    : 0;

                if (this.Frame.PayloadLengthFirst7bits < 126)
                {
                    this.Frame.PayloadLength = this.Frame.PayloadLengthFirst7bits;
                    if (this.Frame.PayloadLength == 0)
                        this.Frame.PayloadData = new byte[0];

                    if (!this.Frame.Mask)
                    {
                        return true;
                    }
                }
            }
            else if (this.headerBuffer.Count == 2 + this.Frame.PayloadExLength)
            {
                if (this.Frame.PayloadExLength == 2)
                    this.Frame.PayloadLength = this.headerBuffer.ToUInt16(2);
                else if (this.Frame.PayloadExLength == 8)
                    this.Frame.PayloadLength = this.headerBuffer.ToInt64(2);

                if (!this.Frame.Mask)
                {
                    return true;
                }
            }
            else if (this.Frame.Mask
                && this.headerBuffer.Count == 6 + this.Frame.PayloadExLength)
            {
                this.Frame.MaskKey = this.headerBuffer.Skip(2 + this.Frame.PayloadExLength).ToArray();
                return true;
            }
            return false;
        }

        /// <summary>
        /// バッファーを初期化
        /// </summary>
        private void Clear()
        {
            this.headerBuffer = new List<byte>(14);
            this.payloadBuffer?.Dispose();
            this.payloadBuffer = null;
            this.payloadCounter = 0;
            this.Frame = new WebSocketFrame();
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
                    this.payloadBuffer?.Dispose();
                }

                // アンマネージド リソース (アンマネージド オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // 大きなフィールドを null に設定します。
                this.payloadBuffer = null;

                this.disposedValue = true;
            }
        }

        // 上の Dispose(bool disposing) にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~WebSocketFrameBuilder() {
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
