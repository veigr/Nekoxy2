using Nekoxy2.Spi.Entities.Http;

namespace Nekoxy2.Entities.Http.Delegations
{
    internal sealed class ReadOnlySession : IReadOnlySession
    {
        public Spi.Entities.Http.IReadOnlySession Source { get; }

        public IReadOnlyHttpRequest Request { get; private set; }

        public IReadOnlyHttpResponse Response { get; private set; }

        Spi.Entities.Http.IReadOnlyHttpRequest Spi.Entities.Http.IReadOnlySession.Request
            => this.Source.Request;

        Spi.Entities.Http.IReadOnlyHttpResponse Spi.Entities.Http.IReadOnlySession.Response
            => this.Source.Response;

        private ReadOnlySession(Spi.Entities.Http.IReadOnlySession source)
            => this.Source = source;

        public override string ToString()
            => this.Source.ToString();

        public override bool Equals(object obj)
        {
            if (obj is ReadOnlySession)
                return this.Source.Equals((obj as ReadOnlySession).Source);
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
            => this.Source.GetHashCode();

        internal static IReadOnlySession Convert(Spi.Entities.Http.IReadOnlySession source)
            => new ReadOnlySession(source)
            {
                Request = ReadOnlyHttpRequest.Convert(source.Request),
                Response = ReadOnlyHttpResponse.Convert(source.Response),
            };
    }

    internal sealed class Session : ISession
    {
        public Spi.Entities.Http.ISession Source { get; }

        public IReadOnlyHttpRequest Request
            => ReadOnlyHttpRequest.Convert(this.Source.Request);

        public IHttpResponse Response
        {
            get => HttpResponse.Convert(this.Source.Response);
            set => this.Source.Response = value;
        }

        Spi.Entities.Http.IReadOnlyHttpRequest Spi.Entities.Http.ISession.Request
            => this.Source.Request;

        Spi.Entities.Http.IHttpResponse Spi.Entities.Http.ISession.Response
        {
            get => this.Source.Response;
            set => this.Source.Response = value;
        }

        Spi.Entities.Http.IReadOnlyHttpRequest Spi.Entities.Http.IReadOnlySession.Request
            => (this.Source as Spi.Entities.Http.IReadOnlySession)?.Request;

        Spi.Entities.Http.IReadOnlyHttpResponse Spi.Entities.Http.IReadOnlySession.Response
            => (this.Source as Spi.Entities.Http.IReadOnlySession)?.Response;

        Spi.Entities.Http.IReadOnlySession IReadOnlySession.Source
            => this.Source;

        IReadOnlyHttpRequest IReadOnlySession.Request
            => this.Request;

        IReadOnlyHttpResponse IReadOnlySession.Response
            => this.Response;

        private Session(Spi.Entities.Http.ISession source)
            => this.Source = source;

        public override string ToString()
            => this.Source.ToString();

        public override bool Equals(object obj)
        {
            if (obj is Session)
                return this.Source.Equals((obj as Session).Source);
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
            => this.Source.GetHashCode();

        internal static ISession Convert(Spi.Entities.Http.ISession source)
            => new Session(source);
    }
}
