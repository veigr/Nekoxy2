using System;
using System.Net;

namespace Nekoxy2.Entities.Http.Delegations
{
    internal sealed class ReadOnlyHttpStatusLine : IReadOnlyHttpStatusLine
    {
        private readonly Spi.Entities.Http.IReadOnlyHttpStatusLine source;

        public Version HttpVersion
            => this.source.HttpVersion;

        public HttpStatusCode StatusCode
            => this.source.StatusCode;

        public string ReasonPhrase
            => this.source.ReasonPhrase;

        private ReadOnlyHttpStatusLine(Spi.Entities.Http.IReadOnlyHttpStatusLine source)
            => this.source = source;

        public override string ToString()
            => this.source.ToString();

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyHttpStatusLine)
                return this.source.Equals((obj as ReadOnlyHttpStatusLine).source);
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
            => this.source.GetHashCode();

        internal static ReadOnlyHttpStatusLine Convert(Spi.Entities.Http.IReadOnlyHttpStatusLine source)
            => new ReadOnlyHttpStatusLine(source);
    }

    internal sealed class HttpStatusLine : IHttpStatusLine
    {
        private readonly Spi.Entities.Http.IHttpStatusLine source;

        public Version HttpVersion
        {
            get => this.source.HttpVersion;
            set => this.source.HttpVersion = value;
        }

        public HttpStatusCode StatusCode
        {
            get => this.source.StatusCode;
            set => this.source.StatusCode = value;
        }

        public string ReasonPhrase
        {
            get => this.source.ReasonPhrase;
            set => this.source.ReasonPhrase = value;
        }

        private HttpStatusLine(Spi.Entities.Http.IHttpStatusLine source)
            => this.source = source;

        public override string ToString()
            => this.source.ToString();

        public override bool Equals(object obj)
        {
            if (obj is HttpStatusLine)
                return this.source.Equals((obj as HttpStatusLine).source);
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
            => this.source.GetHashCode();

        internal static HttpStatusLine Convert(Spi.Entities.Http.IHttpStatusLine source)
            => new HttpStatusLine(source);
    }
}
