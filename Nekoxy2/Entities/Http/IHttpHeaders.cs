namespace Nekoxy2.Entities.Http
{
    /// <summary>
    /// 読み取り専用 HTTP ヘッダー
    /// </summary>
    public interface IReadOnlyHttpHeaders : Spi.Entities.Http.IReadOnlyHttpHeaders
    {
    }

    /// <summary>
    /// HTTP ヘッダー
    /// </summary>
    public interface IHttpHeaders : IReadOnlyHttpHeaders, Spi.Entities.Http.IHttpHeaders
    {
    }
}
