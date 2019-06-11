using Nekoxy2.Spi.Entities.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Titanium.Web.Proxy.Http;

namespace Nekoxy2.Titanium.Entities
{
    internal sealed class HttpRequestLine : IHttpRequestLine
    {
        public HttpMethod Method { get; set; }
        public string RequestTarget { get; set; }
        public Version HttpVersion { get; set; }

        HttpMethod IReadOnlyHttpRequestLine.Method => this.Method;
        string IReadOnlyHttpRequestLine.RequestTarget => this.RequestTarget;
        Version IReadOnlyHttpRequestLine.HttpVersion => this.HttpVersion;

        public HttpRequestLine(Request source)
        {
            this.Method = new HttpMethod(source.Method);
            this.RequestTarget = source.OriginalUrl;
            this.HttpVersion = source.HttpVersion;
        }
    }
}
