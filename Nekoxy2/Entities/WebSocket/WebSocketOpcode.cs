using System.ComponentModel;

namespace Nekoxy2.Entities.WebSocket
{
    /// <summary>
    /// ペイロードデータの解釈
    /// </summary>
    public enum WebSocketOpcode
    {
        /// <summary>
        /// 継続
        /// </summary>
        Continuation = 0x0,

        /// <summary>
        /// テキスト
        /// </summary>
        Text = 0x1,

        /// <summary>
        /// バイナリ
        /// </summary>
        Binary = 0x2,
        [EditorBrowsable(EditorBrowsableState.Never)] Reserved1 = 0x3,
        [EditorBrowsable(EditorBrowsableState.Never)] Reserved2 = 0x4,
        [EditorBrowsable(EditorBrowsableState.Never)] Reserved3 = 0x5,
        [EditorBrowsable(EditorBrowsableState.Never)] Reserved4 = 0x6,
        [EditorBrowsable(EditorBrowsableState.Never)] Reserved5 = 0x7,

        /// <summary>
        /// 接続の Close
        /// </summary>
        Close = 0x8,

        /// <summary>
        /// Ping
        /// </summary>
        Ping = 0x9,

        /// <summary>
        /// Pong
        /// </summary>
        Pong = 0xA,
        [EditorBrowsable(EditorBrowsableState.Never)] Reserved6 = 0xB,
        [EditorBrowsable(EditorBrowsableState.Never)] Reserved7 = 0xC,
        [EditorBrowsable(EditorBrowsableState.Never)] Reserved8 = 0xD,
        [EditorBrowsable(EditorBrowsableState.Never)] Reserved9 = 0xE,
        [EditorBrowsable(EditorBrowsableState.Never)] Reserved10 = 0xF,
    }
}
