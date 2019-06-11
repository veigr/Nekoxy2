using Nekoxy2.Spi.Entities.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Titanium.Web.Proxy.Http;

namespace Nekoxy2.Titanium.Entities
{
    internal sealed class HttpStatusLine : IHttpStatusLine
    {
        public Version HttpVersion { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string ReasonPhrase { get; set; }

        Version IReadOnlyHttpStatusLine.HttpVersion => this.HttpVersion;
        HttpStatusCode IReadOnlyHttpStatusLine.StatusCode => this.StatusCode;
        string IReadOnlyHttpStatusLine.ReasonPhrase => this.ReasonPhrase;

        public HttpStatusLine(Response source)
        {
            this.HttpVersion = source.HttpVersion;
            this.StatusCode = (HttpStatusCode)source.StatusCode;
            this.ReasonPhrase = source.StatusDescription;
        }
    }
}
