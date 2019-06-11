using System;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    /// <summary>
    /// 連続するヘッダーフラグメントの続きに使用するフレーム
    /// RFC7540 6.10
    /// </summary>
    internal sealed class Http2ContinuationFrame : IHttp2HeadersFrame
    {
        #region Header

        /// <summary>
        /// HTTP/2 フレームヘッダー
        /// </summary>
        public Http2FrameHeader Header { get; }

        /// <summary>
        /// このフレームの後には CONTINUATION フレームが続かないことを示す
        /// </summary>
        public bool IsEndHeaders => this.HasFlag((byte)Flag.EndHeaders);

        private enum Flag : byte
        {
            EndHeaders = 0b00000100,
        }

        #endregion

        /// <summary>
        /// ヘッダーブロックフラグメント
        /// </summary>
        public byte[] HeaderBlockFragment { get; }

        public Http2ContinuationFrame() { }

        public Http2ContinuationFrame(Http2FrameHeader header, byte[] data)
        {
            if (data.Length != header.Length)
                throw new ArgumentException("Invalid Length.");
            this.Header = header;
            this.HeaderBlockFragment = data;
        }

        public byte[] ToBytes()
        {
            return this.Header.ToBytes()
                .Concat(this.HeaderBlockFragment)
                .ToArray();
        }

        public override string ToString()
            => $"{this.Header}, IsEndHeaders: {this.IsEndHeaders}";

        public static Http2ContinuationFrame Create(
            int streamID,
            bool isEndHeaders,
            byte[] headerBlockFragment)
        {
            var flags = isEndHeaders ? (byte)Flag.EndHeaders : (byte)0;
            var header = new Http2FrameHeader(
                headerBlockFragment.Length,
                Http2FrameType.Continuation,
                flags,
                streamID);
            return new Http2ContinuationFrame(header, headerBlockFragment);
        }
    }
}
