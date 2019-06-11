using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    /// <summary>
    /// コネクションを終了させるフレーム
    /// RFC7540 6.8
    /// </summary>
    internal sealed class Http2GoawayFrame : IHttp2Frame
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
        /// 最終ストリーム ID (unsigned 31bit)
        /// </summary>
        public int LastStreamID { get; }

        /// <summary>
        /// エラーコード
        /// </summary>
        public Http2ErrorCode ErrorCode { get; }

        /// <summary>
        /// デバッグデータ
        /// </summary>
        public byte[] AdditionalDebugData { get; }

        public Http2GoawayFrame() { }

        public Http2GoawayFrame(Http2FrameHeader header, byte[] data)
        {
            if (data.Length != header.Length)
                throw new ArgumentException("Invalid Length.");
            this.Header = header;
            this.R = data[0].HasFlag(0b10000000);
            this.LastStreamID = data.ToUInt31(0);
            this.ErrorCode = (Http2ErrorCode)data.ToUInt32(4);
            this.AdditionalDebugData = data.Skip(8).ToArray();
        }

        public Http2GoawayFrame(Http2FrameHeader header, int lastStreamID, Http2ErrorCode errorCode, byte[] additionalDebugData)
        {
            this.Header = header;
            this.R = false;
            this.LastStreamID = lastStreamID;
            this.ErrorCode = errorCode;
            this.AdditionalDebugData = additionalDebugData;
        }

        public byte[] ToBytes()
        {
            var bytes = new List<byte>();
            bytes.AddRange(this.Header.ToBytes());
            // ignore R
            bytes.AddRange(BitConverter.GetBytes(this.LastStreamID).Reverse());
            bytes.AddRange(BitConverter.GetBytes((uint)this.ErrorCode).Reverse());
            bytes.AddRange(this.AdditionalDebugData);
            return bytes.ToArray();
        }

        public override string ToString()
            => $"{this.Header}, ErrorCode: {this.ErrorCode}, LastID: {this.LastStreamID}, DebugData: {this.AdditionalDebugData.ToUTF8()}";

        public static Http2GoawayFrame Create(
            int streamID,
            int lastStreamID,
            Http2ErrorCode errorCode,
            byte[] additionalDebugData)
        {
            var header = new Http2FrameHeader(
                4 + 4 + additionalDebugData.Length,
                Http2FrameType.Goaway,
                0,
                streamID);
            return new Http2GoawayFrame(header, lastStreamID, errorCode, additionalDebugData);
        }
    }
}
