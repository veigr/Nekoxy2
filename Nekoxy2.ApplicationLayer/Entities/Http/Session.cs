using Nekoxy2.Spi.Entities.Http;

namespace Nekoxy2.ApplicationLayer.Entities.Http
{
    /// <summary>
    /// HTTP リクエスト・レスポンスペア
    /// </summary>
    internal sealed class Session : IReadOnlySession
    {
        IReadOnlyHttpRequest IReadOnlySession.Request => this.Request;

        public HttpRequest Request { get; }

        IReadOnlyHttpResponse IReadOnlySession.Response => this.Response;

        public HttpResponse Response { get; }

        internal Session(HttpRequest request, HttpResponse response)
        {
            this.Request = request;
            this.Response = response;
        }

        public override string ToString()
            => $"{this.Request}\r\n{this.Response}";
    }
}
