using Nekoxy2.ApplicationLayer.Entities.Http;
using Nekoxy2.ApplicationLayer.MessageBodyParsers;
using Nekoxy2.Spi.Entities.Http;
using System;
using System.Linq;
using System.Text;

namespace Nekoxy2.SazLoader.Entities.Http
{
    /// <summary>
    /// リクエスト
    /// </summary>
    internal sealed class SazHttpRequest : IReadOnlyHttpRequest
    {
        IReadOnlyHttpRequestLine IReadOnlyHttpRequest.RequestLine => this.RequestLine;

        public SazHttpRequestLine RequestLine { get; private set; }

        IReadOnlyHttpHeaders IReadOnlyHttpMessage.Headers => this.Headers;

        public SazHttpHeaders Headers { get; }

        public byte[] Body { get; }

        IReadOnlyHttpHeaders IReadOnlyHttpMessage.Trailers => this.Trailers;

        public SazHttpHeaders Trailers { get; }

        private SazHttpRequest(SazHttpRequestLine requestLine, SazHttpHeaders headers, byte[] body, SazHttpHeaders trailers)
        {
            this.RequestLine = requestLine;
            this.Headers = headers;
            this.Body = body;
            this.Trailers = trailers;
        }

        public override string ToString()
            => $"{this.RequestLine}{this.Headers}{this.GetBodyAsString()}{(this.Trailers.Any() ? this.Trailers.ToString() : "")}";

        public byte[] ToBytes()
            => Encoding.ASCII.GetBytes(this.RequestLine.ToString())
                .Concat(Encoding.ASCII.GetBytes(this.Headers.ToString()))
                .Concat(this.Body)
                .ToArray();

        public static SazHttpRequest Parse(byte[] source)
        {
            var strings = Encoding.ASCII.GetString(source);
            var lines = strings.Split(new[] { "\r\n" }, StringSplitOptions.None);

            var requestLine = SazHttpRequestLine.Parse(lines[0] + "\r\n");

            var headersSource = string.Join("\r\n", lines.Skip(1).TakeWhile(x => !string.IsNullOrEmpty(x))) + "\r\n\r\n";
            var headers = SazHttpHeaders.Parse(headersSource);

            var bodySkipCount = strings.Split(new[] { "\r\n\r\n" }, StringSplitOptions.None).First().Length + 4;
            var body = source.Skip(bodySkipCount).ToArray();
            if (headers.HasHeader("Transfer-Encoding") && headers.GetValues("Transfer-Encoding").Contains("chunked"))
            {
                var parser = new ChunkedBodyParser(true, int.MaxValue, false);
                foreach (var b in body)
                {
                    parser.WriteByte(b);
                }
                return new SazHttpRequest(requestLine, headers, parser.Body, SazHttpHeaders.Parse(parser.Trailers));
            }
            return new SazHttpRequest(requestLine, headers, body, SazHttpHeaders.Parse(""));
        }
    }
}
