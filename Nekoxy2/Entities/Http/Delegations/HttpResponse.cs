using System.Collections.Generic;

namespace Nekoxy2.Entities.Http.Delegations
{
    internal sealed class ReadOnlyHttpResponse : IReadOnlyHttpResponse
    {
        private readonly Spi.Entities.Http.IReadOnlyHttpResponse source;

        public IReadOnlyHttpStatusLine StatusLine { get; private set; }

        public IReadOnlyHttpHeaders Headers { get; private set; }

        public IReadOnlyList<byte> Body
            => this.source.Body;

        public IReadOnlyHttpHeaders Trailers { get; private set; }

        Spi.Entities.Http.IReadOnlyHttpStatusLine Spi.Entities.Http.IReadOnlyHttpResponse.StatusLine
            => this.source.StatusLine;

        Spi.Entities.Http.IReadOnlyHttpHeaders Spi.Entities.Http.IReadOnlyHttpMessage.Headers
            => this.source.Headers;

        byte[] Spi.Entities.Http.IReadOnlyHttpMessage.Body
            => this.source.Body;

        Spi.Entities.Http.IReadOnlyHttpHeaders Spi.Entities.Http.IReadOnlyHttpMessage.Trailers
            => this.source.Trailers;

        private ReadOnlyHttpResponse(Spi.Entities.Http.IReadOnlyHttpResponse source)
            => this.source = source;

        public override string ToString()
            => this.source.ToString();

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlyHttpResponse)
                return this.source.Equals((obj as ReadOnlyHttpResponse).source);
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
            => this.source.GetHashCode();

        internal static IReadOnlyHttpResponse Convert(Spi.Entities.Http.IReadOnlyHttpResponse source)
            => new ReadOnlyHttpResponse(source)
            {
                StatusLine = ReadOnlyHttpStatusLine.Convert(source.StatusLine),
                Headers = ReadOnlyHttpHeaders.Convert(source.Headers),
                Trailers = ReadOnlyHttpHeaders.Convert(source.Trailers),
            };
    }

    internal sealed class HttpResponse : IHttpResponse
    {
        private readonly Spi.Entities.Http.IHttpResponse source;

        public IHttpStatusLine StatusLine
        {
            get => HttpStatusLine.Convert(this.source.StatusLine);
            set => this.source.StatusLine = value;
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

        Spi.Entities.Http.IHttpStatusLine Spi.Entities.Http.IHttpResponse.StatusLine
        {
            get => this.source.StatusLine;
            set => this.source.StatusLine = value;
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

        Spi.Entities.Http.IReadOnlyHttpStatusLine Spi.Entities.Http.IReadOnlyHttpResponse.StatusLine
            => this.source.StatusLine;

        Spi.Entities.Http.IReadOnlyHttpHeaders Spi.Entities.Http.IReadOnlyHttpMessage.Headers
            => this.source.Headers;

        Spi.Entities.Http.IReadOnlyHttpHeaders Spi.Entities.Http.IReadOnlyHttpMessage.Trailers
            => this.source.Headers;

        IReadOnlyHttpStatusLine IReadOnlyHttpResponse.StatusLine
            => this.StatusLine;

        IReadOnlyHttpHeaders IReadOnlyHttpMessage.Headers
            => this.Headers;

        IReadOnlyList<byte> IReadOnlyHttpMessage.Body
            => this.Body;

        IReadOnlyHttpHeaders IReadOnlyHttpMessage.Trailers
            => this.Trailers;

        private HttpResponse(Spi.Entities.Http.IHttpResponse source)
            => this.source = source;

        public override string ToString()
            => this.source.ToString();

        public override bool Equals(object obj)
        {
            if (obj is HttpResponse)
                return this.source.Equals((obj as HttpResponse).source);
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
            => this.source.GetHashCode();

        internal static IHttpResponse Convert(Spi.Entities.Http.IHttpResponse source)
            => new HttpResponse(source);
    }
}
