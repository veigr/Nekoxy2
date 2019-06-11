namespace Nekoxy2.Entities.Http
{
    /// <summary>
    /// 読み取り専用 HTTP ステータスライン
    /// </summary>
    public interface IReadOnlyHttpStatusLine : Spi.Entities.Http.IReadOnlyHttpStatusLine
    {
    }

    /// <summary>
    /// HTTP ステータスライン
    /// </summary>
    public interface IHttpStatusLine : IReadOnlyHttpStatusLine, Spi.Entities.Http.IHttpStatusLine
    {
    }
}
