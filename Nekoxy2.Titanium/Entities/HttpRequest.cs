using Nekoxy2.Spi.Entities.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Http;

namespace Nekoxy2.Titanium.Entities
{
    internal sealed class HttpRequest : IHttpRequest
    {
        public IHttpRequestLine RequestLine { get; set; }
        public IHttpHeaders Headers { get; set; }
        public byte[] Body { get; set; }
        public IHttpHeaders Trailers { get => null; set => throw new NotSupportedException(); }

        IReadOnlyHttpRequestLine IReadOnlyHttpRequest.RequestLine => this.RequestLine;
        IReadOnlyHttpHeaders IReadOnlyHttpMessage.Headers => this.Headers;
        byte[] IReadOnlyHttpMessage.Body => this.Body;
        IReadOnlyHttpHeaders IReadOnlyHttpMessage.Trailers => this.Trailers;

        public HttpRequest(Request source)
        {
            this.RequestLine = new HttpRequestLine(source);
            this.Headers = new HttpHeaders(source.Headers);
            if (source.HasBody && source.IsBodyRead)
                this.Body = source.Body;
            else
                this.Body = Array.Empty<byte>();
        }

        public static async Task<HttpRequest> CreateAsync(SessionEventArgs args)
        {
            var request = new HttpRequest(args.HttpClient.Request);
            if (args.HttpClient.Request.HasBody && !args.HttpClient.Request.IsBodyRead)
                request.Body = await args.GetRequestBody();
            return request;
        }
    }
}
