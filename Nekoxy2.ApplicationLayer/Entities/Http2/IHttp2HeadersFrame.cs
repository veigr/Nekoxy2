namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    /// <summary>
    /// HTTP/2 ヘッダフレーム機能
    /// </summary>
    internal interface IHttp2HeadersFrame : IHttp2Frame
    {
        /// <summary>
        /// ヘッダーブロックフラグメント
        /// </summary>
        byte[] HeaderBlockFragment { get; }

        /// <summary>
        /// このフレームの後には CONTINUATION フレームが続かないことを示す
        /// </summary>
        bool IsEndHeaders { get; }
    }
}