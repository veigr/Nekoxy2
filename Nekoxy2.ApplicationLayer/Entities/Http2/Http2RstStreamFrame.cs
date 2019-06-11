using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    /// <summary>
    /// ストリームの即時終了を示すフレーム
    /// RFC7540 6.4
    /// </summary>
    internal sealed class Http2RstStreamFrame : IHttp2Frame
    {
        #region Header

        /// <summary>
        /// HTTP/2 フレームヘッダー
        /// </summary>
        public Http2FrameHeader Header { get; }

        #endregion

        /// <summary>
        /// エラーコード
        /// </summary>
        public Http2ErrorCode ErrorCode { get; }

        public Http2RstStreamFrame() { }

        public Http2RstStreamFrame(Http2FrameHeader header, byte[] data)
        {
            if (data.Length != header.Length)
                throw new ArgumentException("Invalid Length.");
            this.Header = header;
            this.ErrorCode = (Http2ErrorCode)data.ToUInt32(0);
        }

        public Http2RstStreamFrame(Http2FrameHeader header, Http2ErrorCode errorCode)
        {
            this.Header = header;
            this.ErrorCode = errorCode;
        }

        public byte[] ToBytes()
        {
            var bytes = new List<byte>();
            bytes.AddRange(this.Header.ToBytes());
            bytes.AddRange(BitConverter.GetBytes((uint)this.ErrorCode).Reverse());
            return bytes.ToArray();
        }

        public override string ToString()
            => $"{this.Header}, ErrorCode: {this.ErrorCode}";

        public static Http2RstStreamFrame Create(
            int streamID,
            Http2ErrorCode errorCode)
        {
            var header = new Http2FrameHeader(
                4,
                Http2FrameType.RstStream,
                0,
                streamID);
            return new Http2RstStreamFrame(header, errorCode);
        }
    }
}
