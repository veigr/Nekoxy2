using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    /// <summary>
    /// ストリーム優先度を指定するフレーム
    /// RFC7540 6.3
    /// </summary>
    internal sealed class Http2PriorityFrame : IHttp2Frame
    {
        #region Header

        /// <summary>
        /// HTTP/2 フレームヘッダー
        /// </summary>
        public Http2FrameHeader Header { get; }

        #endregion

        /// <summary>
        /// ストリームの依存関係の排他を示す
        /// </summary>
        public bool E { get; }

        /// <summary>
        /// 依存先ストリーム ID (unsigned 31bit)
        /// </summary>
        public int StreamDependencyID { get; }

        /// <summary>
        /// ストリーム優先度の重み
        /// </summary>
        public byte Weight { get; }

        public Http2PriorityFrame() { }

        public Http2PriorityFrame(Http2FrameHeader header, byte[] data)
        {
            if (data.Length != header.Length)
                throw new ArgumentException("Invalid Length.");
            this.Header = header;
            this.E = data[0].HasFlag(0b10000000);
            this.StreamDependencyID = data.ToUInt31(0);
            this.Weight = data[4];
        }

        public Http2PriorityFrame(Http2FrameHeader header, bool e, int streamDependencyID, byte weight)
        {
            this.Header = header;
            this.E = e;
            this.StreamDependencyID = streamDependencyID;
            this.Weight = weight;
        }

        public byte[] ToBytes()
        {
            var bytes = new List<byte>();
            bytes.AddRange(this.Header.ToBytes());
            var e = this.E ? 0b10000000 : 0;
            var streamDependencyID = BitConverter.GetBytes(this.StreamDependencyID).Reverse().ToArray();
            bytes.Add((byte)(e & streamDependencyID[0]));
            bytes.Add(streamDependencyID[1]);
            bytes.Add(streamDependencyID[2]);
            bytes.Add(streamDependencyID[3]);
            bytes.Add(this.Weight);
            return bytes.ToArray();
        }

        public override string ToString()
            => $"{this.Header}, E: {this.E}, Dependency: {this.StreamDependencyID}, Weight: {this.Weight}";
    }
}
