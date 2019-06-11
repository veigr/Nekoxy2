using Nekoxy2.Spi.Entities.WebSocket;
using System;
using System.ComponentModel;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.Entities.WebSocket
{
    /// <summary>
    /// WebSocket フレーム
    /// RFC6455 5.2
    /// </summary>
    internal sealed class WebSocketFrame
    {
        #region Headers

        /// <summary>
        /// メッセージの最終フレームであることを示す
        /// </summary>
        public bool Fin { get; internal set; }

        /// <summary>
        /// 予約済みビット1。
        /// Per-Message Compressed bit に使用 RFC7692 6
        /// 制御フレームに設定してはならない
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool Rsv1 { get; internal set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool Rsv2 { get; internal set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool Rsv3 { get; internal set; }

        /// <summary>
        /// ペイロードデータの解釈
        /// </summary>
        public WebSocketOpcode Opcode { get; internal set; }

        /// <summary>
        /// ペイロードデータがマスクされているかどうか
        /// </summary>
        public bool Mask { get; internal set; }

        /// <summary>
        /// ペイロード長の最初の 7bit
        /// </summary>
        internal sbyte PayloadLengthFirst7bits { get; set; }

        /// <summary>
        /// 解析されたペイロード長
        /// </summary>
        public long PayloadLength { get; internal set; }  // 正数のみ。しかし Array の都合上 MaxByteArrayLength を取り扱える上限とする。

        /// <summary>
        /// マスクキー
        /// </summary>
        public byte[] MaskKey { get; internal set; }

        #endregion

        /// <summary>
        /// ペイロード長の拡張部
        /// </summary>
        internal int PayloadExLength { get; set; }

        /// <summary>
        /// ペイロードデータ
        /// </summary>
        public byte[] PayloadData { get; internal set; }

        /// <summary>
        /// フレームタイプ
        /// </summary>
        public WebSocketFrameType FrameType
            => controlFrameTypes.Contains(this.Opcode) ? WebSocketFrameType.Control
            : dataFrameTypes.Contains(this.Opcode) ? WebSocketFrameType.Data
            : WebSocketFrameType.Unknown;

        public override string ToString()
        {
            return
$@"{nameof(this.Fin)}: {this.Fin}
{nameof(this.Rsv1)}: {this.Rsv1}
{nameof(this.Rsv2)}: {this.Rsv2}
{nameof(this.Rsv3)}: {this.Rsv3}
{nameof(this.Opcode)}: {this.Opcode}
{nameof(this.Mask)}: {this.Mask}
{nameof(this.PayloadLengthFirst7bits)}: {this.PayloadLengthFirst7bits}
{nameof(this.PayloadLength)}: {this.PayloadLength}
{nameof(this.MaskKey)}: {(this.Mask && this.MaskKey != null ? BitConverter.ToString(this.MaskKey) : string.Empty)}
"
;
        }

        /// <summary>
        /// 制御フレームタイプ opcode 一覧
        /// </summary>
        private static readonly WebSocketOpcode[] controlFrameTypes = new[] { WebSocketOpcode.Close, WebSocketOpcode.Ping, WebSocketOpcode.Pong };

        /// <summary>
        /// データフレームタイプ opcode 一覧
        /// </summary>
        private static readonly WebSocketOpcode[] dataFrameTypes = new[] { WebSocketOpcode.Text, WebSocketOpcode.Binary, WebSocketOpcode.Continuation };
    }
}
