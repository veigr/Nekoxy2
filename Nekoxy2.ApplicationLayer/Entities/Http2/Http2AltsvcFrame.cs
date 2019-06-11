using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    /// <summary>
    /// クライアントに代替サービスを広告するフレーム
    /// RFC7838 4
    /// </summary>
    internal sealed class Http2AltsvcFrame : IHttp2Frame
    {
        #region Header

        /// <summary>
        /// HTTP/2 フレームヘッダー
        /// </summary>
        public Http2FrameHeader Header { get; }

        #endregion

        /// <summary>
        /// Origin の長さ
        /// </summary>
        public ushort OriginLen { get; }

        /// <summary>
        /// 代替サービス適用対象
        /// </summary>
        public byte[] Origin { get; }

        /// <summary>
        /// 利用可能な代替サービスを示す値。
        /// Alt-Svc ヘッダー相当値。
        /// </summary>
        public byte[] AltSvcFieldValue { get; }

        public Http2AltsvcFrame() { }

        public Http2AltsvcFrame(Http2FrameHeader header, byte[] data)
        {
            if (data.Length != header.Length)
                throw new ArgumentException("Invalid Length.");
            this.Header = header;
            this.OriginLen = data.ToUInt16(0);
            this.Origin = data.Skip(2).Take(this.OriginLen).ToArray();
            this.AltSvcFieldValue = data.Skip(2 + this.OriginLen).ToArray();
        }

        public Http2AltsvcFrame(Http2FrameHeader header, byte[] origin, byte[] altSvcFieldValue)
        {
            this.Header = header;
            this.OriginLen = (ushort)origin.Length;
            this.Origin = origin;
            this.AltSvcFieldValue = altSvcFieldValue;
        }

        public byte[] ToBytes()
        {
            var bytes = new List<byte>();
            bytes.AddRange(this.Header.ToBytes());
            bytes.AddRange(BitConverter.GetBytes(this.OriginLen).Reverse());
            bytes.AddRange(this.Origin);
            bytes.AddRange(this.AltSvcFieldValue);
            return bytes.ToArray();
        }

        public override string ToString()
            => $"{this.Header}, Origin: {this.Origin.ToASCII()}, AltSvc: {this.AltSvcFieldValue.ToASCII()}";
    }
}
