using System.Collections.Generic;

namespace Nekoxy2.Entities.Http.Delegations
{
    internal sealed class ReadOnlyHttpRequest : IReadOnlyHttpRequest
    {
        private readonly Spi.Entities.Http.IReadOnlyHttpRequest source;

        public IReadOnlyHttpRequestLine RequestLine { get; private set; }

        public IReadOnlyHttpHeaders Headers { get; private set; }

        public IReadOnlyList<byte> Body
            => this.source.Body;

        public IReadOnlyHttpHeaders Trailers { get; private set; }

        Spi.Entities.Http.IReadOnlyHttpRequestLine Spi.Entities.Http.IReadOnlyHttpRequest.RequestLine
            => this.source.RequestLine;

        Spi.Entities.Http.IReadOnlyHttpHeaders Spi.Entities.Http.IReadOnlyHttpMessage.Headers


            => this.source.Headers;

        byte[] Spi.Entities.Http.IReadOnlyHttpMessage.Body => this.source.Body;

        Spi.Entities.Http.IReadOnlyHttpHeaders Spi.Entities.Http.IReadOnlyHttpMessage.Trailers => this.source.Trailers;

        private ReadOnlyHttpRequest(Spi.Entities.Http.IReadOnlyHttpRequest source)
            => this.source = source;

        public override string ToString()
            => this.source.ToString();

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyHttpRequest)
                return this.source.Equals((obj as ReadOnlyHttpRequest).source);
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
            => this.source.GetHashCode();

        internal static IReadOnlyHttpRequest Convert(Spi.Entities.Http.IReadOnlyHttpRequest source)
            => new ReadOnlyHttpRequest(source)
            {
                RequestLine = ReadOnlyHttpRequestLine.Convert(source.RequestLine),
                Headers = ReadOnlyHttpHeaders.Convert(source.Headers),
                Trailers = ReadOnlyHttpHeaders.Convert(source.Trailers),
            };
    }

    internal sealed class HttpRequest : IHttpRequest
    {
        private readonly Spi.Entities.Http.IHttpRequest source;

        public IHttpRequestLine RequestLine
        {
            get => HttpRequestLine.Convert(this.source.RequestLine);
            set => this.source.RequestLine = value;
        }

        public IHttpHeaders Headers
        {
            get => HttpHeaders.Convert(this.source.Headers);
            set => this.source.Headers = value;
        }

        public byte[] Body
        {
            get => this.source.Body;
            set => this.source.Body = value;
        }

        public IHttpHeaders Trailers
        {
            get => HttpHeaders.Convert(this.source.Trailers);
            set => this.source.Trailers = value;
        }

        Spi.Entities.Http.IHttpRequestLine Spi.Entities.Http.IHttpRequest.RequestLine
        {
            get => this.source.RequestLine;
            set => this.source.RequestLine = value;
        }

        Spi.Entities.Http.IHttpHeaders Spi.Entities.Http.IHttpMessage.Headers
        {
            get => this.source.Headers;
            set => this.source.Headers = value;
        }

        byte[] Spi.Entities.Http.IHttpMessage.Body
        {
            get => this.source.Body;
            set => this.source.Body = value;
        }

        Spi.Entities.Http.IHttpHeaders Spi.Entities.Http.IHttpMessage.Trailers
        {
            get => this.source.Trailers;
            set => this.source.Trailers = value;
        }

        Spi.Entities.Http.IReadOnlyHttpRequestLine Spi.Entities.Http.IReadOnlyHttpRequest.RequestLine
            => this.source.RequestLine;

        Spi.Entities.Http.IReadOnlyHttpHeaders Spi.Entities.Http.IReadOnlyHttpMessage.Headers
            => this.source.Headers;

        Spi.Entities.Http.IReadOnlyHttpHeaders Spi.Entities.Http.IReadOnlyHttpMessage.Trailers
            => this.source.Trailers;

        IReadOnlyHttpRequestLine IReadOnlyHttpRequest.RequestLine
            => this.RequestLine;

        IReadOnlyHttpHeaders IReadOnlyHttpMessage.Headers
            => this.Headers;

        IReadOnlyList<byte> IReadOnlyHttpMessage.Body
            => this.Body;

        IReadOnlyHttpHeaders IReadOnlyHttpMessage.Trailers
            => this.Trailers;

        private HttpRequest(Spi.Entities.Http.IHttpRequest source)
            => this.source = source;

        public override string ToString()
            => this.source.ToString();

        public override bool Equals(object obj)
        {
            if (obj is HttpRequest)
                return this.source.Equals((obj as HttpRequest).source);
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
            => this.source.GetHashCode();

        internal static IHttpRequest Convert(Spi.Entities.Http.IHttpRequest source)
            => new HttpRequest(source);
    }
}
