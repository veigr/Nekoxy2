namespace Nekoxy2.Entities.Http
{
    /// <summary>
    /// 読み取り専用 HTTP リクエスト
    /// </summary>
    public interface IReadOnlyHttpRequest : IReadOnlyHttpMessage, Spi.Entities.Http.IReadOnlyHttpRequest
    {
        /// <summary>
        /// リクエストライン
        /// </summary>
        new IReadOnlyHttpRequestLine RequestLine { get; }
    }

    /// <summary>
    /// HTTP リクエスト
    /// </summary>
    public interface IHttpRequest : IReadOnlyHttpRequest, IHttpMessage, Spi.Entities.Http.IHttpRequest
    {
        /// <summary>
        /// リクエストライン
        /// </summary>
        new IHttpRequestLine RequestLine { get; set; }
    }
}
