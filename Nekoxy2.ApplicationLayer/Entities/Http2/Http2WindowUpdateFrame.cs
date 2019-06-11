using System;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    /// <summary>
    /// フロー制御を実行するためのフレーム
    /// RFC7540 6.9
    /// </summary>
    internal sealed class Http2WindowUpdateFrame : IHttp2Frame
    {
        #region Header

        /// <summary>
        /// HTTP/2 フレームヘッダー
        /// </summary>
        public Http2FrameHeader Header { get; }

        #endregion

        /// <summary>
        /// 予約済みビット
        /// </summary>
        private bool R { get; }

        /// <summary>
        /// ウィンドウサイズ増加量 (unsigned 31bit)
        /// </summary>
        public int WindowSizeIncrement { get; }

        public Http2WindowUpdateFrame() { }

        public Http2WindowUpdateFrame(Http2FrameHeader header, byte[] data)
        {
            if (data.Length != header.Length)
                throw new ArgumentException("Invalid Length.");
            this.Header = header;
            this.R = data[0].HasFlag(0b10000000);
            this.WindowSizeIncrement = data.ToUInt31(0);
        }

        public byte[] ToBytes()
        {
            return this.Header.ToBytes()
                .Concat(BitConverter.GetBytes(this.WindowSizeIncrement).Reverse())
                .ToArray();
        }

        public override string ToString()
            => $"{this.Header}, Increment: {this.WindowSizeIncrement}";
    }
}
