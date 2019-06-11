using Nekoxy2.ApplicationLayer.Entities;
using Nekoxy2.ApplicationLayer.Entities.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nekoxy2.Test.ApplicationLayer.Entities.Http
{
    public class HttpResponseTest
    {
        [Fact]
        public void CreateInstanceTest()
        {
            var statusLine = new HttpStatusLine(HttpVersion.Version11, HttpStatusCode.OK, "OK");
            var headers = HttpHeaders.Empty;
            var response = new HttpResponse(statusLine, headers, new byte[0], HttpHeaders.Empty);
            response.ToString().Is(
@"HTTP/1.1 200 OK

");
        }

        [Fact]
        public void GetOriginTest()
        {
            var statusLine = new HttpStatusLine(HttpVersion.Version11, HttpStatusCode.OK, "OK");
            var headers = HttpHeaders.Empty;
            headers.ContentLength.Value = 0;
            var response = new HttpResponse(statusLine, headers, new byte[0], HttpHeaders.Empty);
            response.ToString().Is(
@"HTTP/1.1 200 OK
Content-Length: 0

");
            response.GetOrigin().ToString().Is(
@"HTTP/1.1 200 OK

");
        }

        [Fact]
        public void HeadersToBytesTest()
        {
            var statusLine = new HttpStatusLine(HttpVersion.Version11, HttpStatusCode.OK, "OK");
            var headers = HttpHeaders.Empty;
            headers.ContentLength.Value = 0;
            var response = new HttpResponse(statusLine, headers, new byte[0], HttpHeaders.Empty);
            response.HeadersToBytes().Is(Encoding.ASCII.GetBytes(
@"HTTP/1.1 200 OK
Content-Length: 0

"));
        }

        [Fact]
        public void ToBytesTest()
        {
            var statusLine = new HttpStatusLine(HttpVersion.Version11, HttpStatusCode.OK, "OK");
            var headers = HttpHeaders.Empty;
            headers.ContentLength.Value = 8;
            var response = new HttpResponse(statusLine, headers, Encoding.ASCII.GetBytes("hogehoge"), HttpHeaders.Empty);
            response.ToBytes().Is(Encoding.ASCII.GetBytes(
@"HTTP/1.1 200 OK
Content-Length: 8

hogehoge"));
        }

        [Fact]
        public void AfterReceivedHeadersTest()
        {
            var statusLine = new HttpStatusLine(HttpVersion.Version11, HttpStatusCode.OK, "OK");
            HttpHeaders.TryParse(
@"Content-Encoding: gzip
Transfer-Encoding: gzip, chunked

", out var headers);
            var response = new HttpResponse(statusLine, headers, null, HttpHeaders.Empty);
            response.ToString().Is(
@"HTTP/1.1 200 OK
Content-Encoding: gzip
Transfer-Encoding: gzip, chunked

");
        }

        [Fact]
        public void OriginTest()
        {
            var statusLine = new HttpStatusLine(HttpVersion.Version11, HttpStatusCode.OK, "OK");
            var headers = HttpHeaders.Empty;
            headers.ContentLength.Value = 8;
            var response = new HttpResponse(statusLine, headers, Encoding.ASCII.GetBytes("hogehoge"), HttpHeaders.Empty);
            response.GetOrigin().IsNot(response.GetOrigin());
            response.ToString().Is(response.ToString());
        }
    }
}
