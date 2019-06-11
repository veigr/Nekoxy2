using System;
using System.Net.Http;

namespace Nekoxy2.Entities.Http.Delegations
{
    internal sealed class ReadOnlyHttpRequestLine : IReadOnlyHttpRequestLine
    {
        private readonly Spi.Entities.Http.IReadOnlyHttpRequestLine source;

        public HttpMethod Method => this.source.Method;

        public string RequestTarget => this.source.RequestTarget;

        public Version HttpVersion => this.source.HttpVersion;

        private ReadOnlyHttpRequestLine(Spi.Entities.Http.IReadOnlyHttpRequestLine source)
            => this.source = source;

        public override string ToString()
            => this.source.ToString();

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyHttpRequestLine)
                return this.source.Equals((obj as ReadOnlyHttpRequestLine).source);
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
            => this.source.GetHashCode();

        internal static ReadOnlyHttpRequestLine Convert(Spi.Entities.Http.IReadOnlyHttpRequestLine source)
            => new ReadOnlyHttpRequestLine(source);
    }

    internal sealed class HttpRequestLine : IHttpRequestLine
    {
        private readonly Spi.Entities.Http.IHttpRequestLine source;

        public HttpMethod Method
        {
            get => this.source.Method;
            set => this.source.Method = value;
        }

        public string RequestTarget
        {
            get => this.source.RequestTarget;
            set => this.source.RequestTarget = value;
        }

        public Version HttpVersion
        {
            get => this.source.HttpVersion;
            set => this.source.HttpVersion = value;
        }

        private HttpRequestLine(Spi.Entities.Http.IHttpRequestLine source)
            => this.source = source;

        public override string ToString()
            => this.source.ToString();

        public override bool Equals(object obj)
        {
            if (obj is HttpRequestLine)
                return this.source.Equals((obj as HttpRequestLine).source);
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
            => this.source.GetHashCode();

        internal static HttpRequestLine Convert(Spi.Entities.Http.IHttpRequestLine source)
            => new HttpRequestLine(source);
    }
}
