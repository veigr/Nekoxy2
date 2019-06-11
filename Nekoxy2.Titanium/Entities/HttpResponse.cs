using Nekoxy2.Spi.Entities.Http;
using System;
using System.Collections.Generic;
using System.Text;
using Titanium.Web.Proxy.Http;

namespace Nekoxy2.Titanium.Entities
{
    internal sealed class HttpResponse : IHttpResponse
    {
        public IHttpStatusLine StatusLine { get; set; }
        public IHttpHeaders Headers { get; set; }
        public byte[] Body { get; set; }
        public IHttpHeaders Trailers { get => null; set => throw new NotSupportedException(); }

        IReadOnlyHttpStatusLine IReadOnlyHttpResponse.StatusLine => this.StatusLine;
        IReadOnlyHttpHeaders IReadOnlyHttpMessage.Headers => this.Headers;
        byte[] IReadOnlyHttpMessage.Body => this.Body;
        IReadOnlyHttpHeaders IReadOnlyHttpMessage.Trailers => this.Trailers;

        public HttpResponse(Response source)
        {
            this.StatusLine = new HttpStatusLine(source);
            this.Headers = new HttpHeaders(source.Headers);
            if (source.HasBody && source.IsBodyRead)
                this.Body = source.Body;
            else
                this.Body = Array.Empty<byte>();
        }
    }
}
