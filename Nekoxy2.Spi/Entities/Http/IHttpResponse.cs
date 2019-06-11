namespace Nekoxy2.Spi.Entities.Http
{
    /// <summary>
    /// 読み取り専用 HTTP レスポンス
    /// </summary>
    public interface IReadOnlyHttpResponse : IReadOnlyHttpMessage
    {
        /// <summary>
        /// HTTP ステータスライン
        /// </summary>
        IReadOnlyHttpStatusLine StatusLine { get; }
    }

    /// <summary>
    /// HTTP レスポンス
    /// </summary>
    public interface IHttpResponse : IReadOnlyHttpResponse, IHttpMessage
    {
        /// <summary>
        /// HTTP ステータスライン
        /// </summary>
        new IHttpStatusLine StatusLine { get; set; }
    }
}
