namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    /// <summary>
    /// HTTP/2 フレーム
    /// </summary>
    internal interface IHttp2Frame
    {
        /// <summary>
        /// HTTP/2 フレームヘッダ
        /// </summary>
        Http2FrameHeader Header { get; }

        /// <summary>
        /// バイト配列へ変換
        /// </summary>
        /// <returns></returns>
        byte[] ToBytes();
    }
}
