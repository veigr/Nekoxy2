using System;
using System.Net.Http;

namespace Nekoxy2.Entities.Http
{
    /// <summary>
    /// 読み取り専用 HTTP リクエストライン
    /// </summary>
    public interface IReadOnlyHttpRequestLine : Spi.Entities.Http.IReadOnlyHttpRequestLine
    {
    }

    /// <summary>
    /// HTTP リクエストライン
    /// </summary>
    public interface IHttpRequestLine : IReadOnlyHttpRequestLine, Spi.Entities.Http.IHttpRequestLine
    {
    }
}
