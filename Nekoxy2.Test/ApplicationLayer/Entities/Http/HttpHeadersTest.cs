using Nekoxy2.Spi.Entities.Http;
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
    public class HttpHeadersTest
    {
        [Fact]
        public void ParseRequestContentLengthTest()
        {
            var source =
@"Host: 203.104.209.71
Connection: keep-alive
Content-Length: 62
Accept: application/json, text/plain, */*
Origin: http://203.104.209.71
User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.79 Safari/537.36
Content-Type: application/x-www-form-urlencoded
Accept-Encoding: gzip, deflate
Accept-Language: en-US,en;q=0.9

";
            HttpHeaders.TryParse(source, out var header);
            header.Host.Is("203.104.209.71");
            header.Connection.Exists.IsTrue();
            header.Connection.Is("keep-alive");
            header.ContentLength.Exists.IsTrue();
            header.ContentLength.Is(62);
            header.TransferEncoding.Exists.IsFalse();
            header.IsChunked.IsFalse();
            header.ContentType.Exists.IsTrue();
            header.ContentType.Is("application/x-www-form-urlencoded");
            header.ToString().Is(source);
        }

        [Fact]
        public void ChangeHostTest()
        {
            var source =
@"Host: www.example.com
Accept-Encoding: gzip, deflate
Accept-Language: en-US,en;q=0.9

";
            HttpHeaders.TryParse(source, out var header);
            header.Host.Is("www.example.com");
            header.Host.Value = "hoge.com";
            header.Host.Is("hoge.com");
            header.GetOrigin().Host.Is("www.example.com");
            header.ToString().Is(
@"Host: hoge.com
Accept-Encoding: gzip, deflate
Accept-Language: en-US,en;q=0.9

");
        }

        [Fact]
        public void AddViaTest()
        {
            var source =
@"Host: www.example.com

";
            HttpHeaders.TryParse(source, out var header);
            header.HasHeader("Via").IsFalse();
            header.AddVia("HTTP", HttpVersion.Version11, "recieveHost");
            header.HasHeader("Via").IsTrue();
            header.GetFirstValue("Via").Is("1.1 recieveHost");

            header.AddVia("HTTP", HttpVersion.Version10, "recieveHost2");
            header.GetFirstValue("Via").Is("1.1 recieveHost, 1.0 recieveHost2");
        }

        [Fact]
        public void RemoveConnectionHeadersTest()
        {
            var source =
@"Host: www.example.com
Hoge: Fuga
Fuga: Piyo
Piyo: Hoge
Connection: Hoge, piyo, close

";
            HttpHeaders.TryParse(source, out var header);
            header.HasHeader("Hoge").IsTrue();
            header.HasHeader("Fuga").IsTrue();
            header.HasHeader("Piyo").IsTrue();
            header.Connection.Exists.IsTrue();
            header.Connection.ToArray()[0].Is("Hoge");
            header.Connection.ToArray()[1].Is("piyo");
            header.Connection.ToArray()[2].Is("close");
            header.IsClose.IsTrue();

            header.RemoveConnectionHeaders();

            header.HasHeader("Hoge").IsFalse();
            header.HasHeader("Fuga").IsTrue();
            header.HasHeader("Piyo").IsFalse();
            header.Connection.Exists.IsTrue();
            header.Connection.ToArray()[0].Is("close");
            header.IsClose.IsTrue();

            var source2 =
@"Host: www.example.com
Hoge: Fuga
Fuga: Piyo
Piyo: Hoge
Connection: Hoge, piyo

";
            HttpHeaders.TryParse(source2, out var header2);

            header2.RemoveConnectionHeaders();
            header2.Connection.Exists.IsFalse();
            header2.IsClose.IsFalse();
        }

        [Fact]
        public void BrokenHeadersTest()
        {
            var source =
@"hogeeee

";
            var isSucceeded = HttpHeaders.TryParse(source, out var header);
            isSucceeded.IsFalse();
            header.Source.Is(source);
            header.ToString().Is(source);
        }

        [Fact]
        public void OriginTest()
        {
            var source =
@"Host: www.example.com
Hoge: Fuga
Fuga: Piyo
Piyo: Hoge
Connection: Hoge, piyo, close

";
            HttpHeaders.TryParse(source, out var header);

            header.Source.Is(header.Source);
            header.GetOrigin().Is(header.GetOrigin());
        }
    }
}
