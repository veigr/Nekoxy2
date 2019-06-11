namespace Nekoxy2.Entities.Http
{
    /// <summary>
    /// 読み取り専用 HTTP レスポンス
    /// </summary>
    public interface IReadOnlyHttpResponse : IReadOnlyHttpMessage, Spi.Entities.Http.IReadOnlyHttpResponse
    {
        /// <summary>
        /// ステータスライン
        /// </summary>
        new IReadOnlyHttpStatusLine StatusLine { get; }
    }

    /// <summary>
    /// HTTP レスポンス
    /// </summary>
    public interface IHttpResponse : IReadOnlyHttpResponse, IHttpMessage, Spi.Entities.Http.IHttpResponse
    {
        /// <summary>
        /// ステータスライン
        /// </summary>
        new IHttpStatusLine StatusLine { get; set; }
    }
}
