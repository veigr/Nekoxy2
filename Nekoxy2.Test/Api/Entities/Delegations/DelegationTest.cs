using Nekoxy2.Entities.Http.Delegations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nekoxy2.Test.Api.Entities.Delegations
{
    public class DelegationTest
    {
        [Fact]
        public void HttpRequestLineTest()
        {
            var source =
@"GET / HTTP/1.1
";
            Nekoxy2.ApplicationLayer.Entities.Http.HttpRequestLine.TryParse(source, out var spiEntity);
            var apiEntity = ReadOnlyHttpRequestLine.Convert(spiEntity);
            var apiEntity2 = ReadOnlyHttpRequestLine.Convert(spiEntity);

            apiEntity.ToString().Is(spiEntity.ToString());
            apiEntity.GetHashCode().Is(apiEntity2.GetHashCode());
            apiEntity.Equals(apiEntity2).IsTrue();

            (apiEntity is Spi.Entities.Http.IReadOnlyHttpRequestLine).IsTrue();
            (apiEntity is Nekoxy2.Entities.Http.IReadOnlyHttpRequestLine).IsTrue();
        }

        [Fact]
        public void HttpStatusLineTest()
        {
            var source =
@"HTTP/1.1 200 OK
";
            Nekoxy2.ApplicationLayer.Entities.Http.HttpStatusLine.TryParse(source, out var spiEntity);
            var apiEntity = ReadOnlyHttpStatusLine.Convert(spiEntity);
            var apiEntity2 = ReadOnlyHttpStatusLine.Convert(spiEntity);

            apiEntity.ToString().Is(spiEntity.ToString());
            apiEntity.GetHashCode().Is(apiEntity2.GetHashCode());
            apiEntity.Equals(apiEntity2).IsTrue();

            (apiEntity is Spi.Entities.Http.IReadOnlyHttpStatusLine).IsTrue();
            (apiEntity is Nekoxy2.Entities.Http.IReadOnlyHttpStatusLine).IsTrue();
        }

        [Fact]
        public void HttpHeadersTest()
        {
            var source =
@"Connection: keep-alive
User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.79 Safari/537.36
Accept: image/webp,image/apng,image/*,*/*;q=0.8
Accept-Encoding: gzip, deflate
Accept-Language: en-US,en;q=0.9

";
            Nekoxy2.ApplicationLayer.Entities.Http.HttpHeaders.TryParse(source, out var spiEntity);
            var apiEntity = ReadOnlyHttpHeaders.Convert(spiEntity);
            var apiEntity2 = ReadOnlyHttpHeaders.Convert(spiEntity);

            apiEntity.ToString().Is(spiEntity.ToString());
            apiEntity.GetHashCode().Is(apiEntity2.GetHashCode());
            apiEntity.Equals(apiEntity2).IsTrue();

            (apiEntity is Spi.Entities.Http.IReadOnlyHttpHeaders).IsTrue();
            (apiEntity is Nekoxy2.Entities.Http.IReadOnlyHttpHeaders).IsTrue();
        }

        [Fact]
        public void HttpRequestTest()
        {
            var lineSource =
@"GET / HTTP/1.1
";
            Nekoxy2.ApplicationLayer.Entities.Http.HttpRequestLine.TryParse(lineSource, out var line);
            var headersSource =
@"Host: example.com

";
            Nekoxy2.ApplicationLayer.Entities.Http.HttpHeaders.TryParse(headersSource, out var headers);
            var bodySource = "hogefuga";

            var spiEntity = new Nekoxy2.ApplicationLayer.Entities.Http.HttpRequest(line, headers, Encoding.ASCII.GetBytes(bodySource), Nekoxy2.ApplicationLayer.Entities.Http.HttpHeaders.Empty);
            var apiEntity = ReadOnlyHttpRequest.Convert(spiEntity);
            var apiEntity2 = ReadOnlyHttpRequest.Convert(spiEntity);

            apiEntity.ToString().Is(spiEntity.ToString());
            apiEntity.GetHashCode().Is(apiEntity2.GetHashCode());
            apiEntity.Equals(apiEntity2).IsTrue();

            (apiEntity is Spi.Entities.Http.IReadOnlyHttpRequest).IsTrue();
            (apiEntity is Nekoxy2.Entities.Http.IReadOnlyHttpRequest).IsTrue();
        }

        [Fact]
        public void HttpResponseTest()
        {
            var lineSource =
@"HTTP/1.1 200 OK
";
            Nekoxy2.ApplicationLayer.Entities.Http.HttpStatusLine.TryParse(lineSource, out var line);
            var headersSource =
@"Content-Length: 8

";
            Nekoxy2.ApplicationLayer.Entities.Http.HttpHeaders.TryParse(headersSource, out var headers);
            var bodySource = "hogefuga";

            var spiEntity = new Nekoxy2.ApplicationLayer.Entities.Http.HttpResponse(line, headers, Encoding.ASCII.GetBytes(bodySource), Nekoxy2.ApplicationLayer.Entities.Http.HttpHeaders.Empty);
            var apiEntity = ReadOnlyHttpResponse.Convert(spiEntity);
            var apiEntity2 = ReadOnlyHttpResponse.Convert(spiEntity);

            apiEntity.ToString().Is(spiEntity.ToString());
            apiEntity.GetHashCode().Is(apiEntity2.GetHashCode());
            apiEntity.Equals(apiEntity2).IsTrue();

            (apiEntity is Spi.Entities.Http.IReadOnlyHttpResponse).IsTrue();
            (apiEntity is Nekoxy2.Entities.Http.IReadOnlyHttpResponse).IsTrue();
        }

        [Fact]
        public void SessionTest()
        {
            var requestLineSource =
@"GET / HTTP/1.1
";
            Nekoxy2.ApplicationLayer.Entities.Http.HttpRequestLine.TryParse(requestLineSource, out var line);
            var requestHeaderSource =
@"Host: example.com

";
            Nekoxy2.ApplicationLayer.Entities.Http.HttpHeaders.TryParse(requestHeaderSource, out var requestHeaders);

            var spiRequest = new Nekoxy2.ApplicationLayer.Entities.Http.HttpRequest(line, requestHeaders, new byte[0], Nekoxy2.ApplicationLayer.Entities.Http.HttpHeaders.Empty);

            var statusLineSource =
@"HTTP/1.1 200 OK
";
            Nekoxy2.ApplicationLayer.Entities.Http.HttpStatusLine.TryParse(statusLineSource, out var statusLine);
            var responseHeaderSource =
@"Content-Length: 8

";
            Nekoxy2.ApplicationLayer.Entities.Http.HttpHeaders.TryParse(responseHeaderSource, out var responseHeaders);
            var responseBodySource = "hogefuga";

            var spiResponse = new Nekoxy2.ApplicationLayer.Entities.Http.HttpResponse(statusLine, responseHeaders, Encoding.ASCII.GetBytes(responseBodySource), Nekoxy2.ApplicationLayer.Entities.Http.HttpHeaders.Empty);

            var spiSession = new Nekoxy2.ApplicationLayer.Entities.Http.Session(spiRequest, spiResponse);
            var apiSession = ReadOnlySession.Convert(spiSession);
            var apiSession2 = ReadOnlySession.Convert(spiSession);

            apiSession.ToString().Is(spiSession.ToString());
            apiSession.GetHashCode().Is(apiSession2.GetHashCode());
            apiSession.Equals(apiSession2).IsTrue();

            (apiSession is Spi.Entities.Http.IReadOnlySession).IsTrue();
            (apiSession is Nekoxy2.Entities.Http.IReadOnlySession).IsTrue();
            apiSession.Source.Is(spiSession);
        }
    }
}
