namespace Nekoxy2.ApplicationLayer.Entities.WebSocket
{
    /// <summary>
    /// WebSocket フレームタイプ
    /// </summary>
    internal enum WebSocketFrameType
    {
        /// <summary>
        /// 未知のフレーム
        /// </summary>
        Unknown,

        /// <summary>
        /// 制御フレーム
        /// </summary>
        Control,

        /// <summary>
        /// データフレーム
        /// </summary>
        Data,
    }
}
