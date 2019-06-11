using Nekoxy2.ApplicationLayer.Entities;
using Nekoxy2.ApplicationLayer.Entities.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nekoxy2.Test.ApplicationLayer.Entities.Http
{
    public class HttpRequestTest
    {
        [Fact]
        public void CreateInstanceTest()
        {
            const string lineSource = "GET / HTTP/1.1\r\n";
            HttpRequestLine.TryParse(lineSource, out var line);
            var headers = HttpHeaders.Empty;
            headers.Host.Value = "example.com";
            var request = new HttpRequest(line, headers, new byte[0], HttpHeaders.Empty);

            request.ToString().Is(
@"GET / HTTP/1.1
Host: example.com

");
            request.RequestTargetUri.ToString().Is("http://example.com/");
            request.RequestLine.RequestTargetForm.Is(RequestTargetForm.OriginForm);
        }

        [Fact]
        public void CreateConnectInstanceTest()
        {
            const string lineSource = "CONNECT authority.example.com:8443 HTTP/1.1\r\n";
            HttpRequestLine.TryParse(lineSource, out var line);
            var headers = HttpHeaders.Empty;
            headers.Host.Value = "authority.example.com";
            var request = new HttpRequest(line, headers, new byte[0], HttpHeaders.Empty);

            request.ToString().Is(
@"CONNECT authority.example.com:8443 HTTP/1.1
Host: authority.example.com

");
            request.RequestTargetUri.ToString().Is("https://authority.example.com:8443/");
            request.RequestLine.RequestTargetForm.Is(RequestTargetForm.AuthorityForm);
        }

        [Fact]
        public void CreateOptionsInstanceTest()
        {
            const string lineSource = "OPTIONS * HTTP/1.1\r\n";
            HttpRequestLine.TryParse(lineSource, out var line);
            var headers = HttpHeaders.Empty;
            headers.Host.Value = "example.com";
            var request = new HttpRequest(line, headers, new byte[0], HttpHeaders.Empty);

            request.ToString().Is(
@"OPTIONS * HTTP/1.1
Host: example.com

");
            request.RequestTargetUri.ToString().Is("*");
            request.RequestLine.RequestTargetForm.Is(RequestTargetForm.AsteriskForm);
        }

        [Fact]
        public void ChangeRequestTargetTest()
        {
            const string lineSource = "GET /hoge?fuga=piyo HTTP/1.1\r\n";
            HttpRequestLine.TryParse(lineSource, out var line);
            HttpHeaders.TryParse("Host: example.com\r\n", out var headers);
            var request = new HttpRequest(line, headers, new byte[0], HttpHeaders.Empty);

            request.RequestTargetUri.ToString().Is("http://example.com/hoge?fuga=piyo");
            request.RequestLine.RequestTargetForm.Is(RequestTargetForm.OriginForm);

            request.ChangeRequestTarget("http://absolute.example.com/hoge?fuga=piyo");
            request.RequestTargetUri.ToString().Is("http://absolute.example.com/hoge?fuga=piyo");
            request.RequestLine.RequestTargetForm.Is(RequestTargetForm.AbsoluteForm);
        }

        [Fact]
        public void GetOriginTest()
        {
            const string lineSource = "GET / HTTP/1.1\r\n";
            HttpRequestLine.TryParse(lineSource, out var line);
            HttpHeaders.TryParse("Host: example.com\r\n", out var headers);
            headers.Host.Value = "example2.com";
            var request = new HttpRequest(line, headers, new byte[0], HttpHeaders.Empty);

            request.GetOrigin().ToString().Is(
@"GET / HTTP/1.1
Host: example.com

");
        }

        [Fact]
        public void HeadersAsByteTest()
        {
            const string lineSource = "GET / HTTP/1.1\r\n";
            HttpRequestLine.TryParse(lineSource, out var line);
            var headers = HttpHeaders.Empty;
            headers.Host.Value = "example.com";
            var request = new HttpRequest(line, headers, new byte[0], HttpHeaders.Empty);

            request.HeadersAsByte().Is(Encoding.ASCII.GetBytes(
@"GET / HTTP/1.1
Host: example.com

"));
        }

        [Fact]
        public void OriginTest()
        {
            const string lineSource = "GET / HTTP/1.1\r\n";
            HttpRequestLine.TryParse(lineSource, out var line);
            var headers = HttpHeaders.Empty;
            headers.Host.Value = "example.com";
            var request = new HttpRequest(line, headers, new byte[0], HttpHeaders.Empty);

            // Request 系は BadRequest への対処として Source プロパティを持つ
            request.Source.Is(request.Source);
            request.GetOrigin().IsNot(request.GetOrigin());
        }
    }
}
