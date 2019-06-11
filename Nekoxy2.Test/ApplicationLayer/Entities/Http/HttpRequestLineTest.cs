using Nekoxy2.ApplicationLayer.Entities;
using Nekoxy2.ApplicationLayer.Entities.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nekoxy2.Test.ApplicationLayer.Entities.Http
{
    public class HttpRequestLineTest
    {
        [Fact]
        public void OriginFormTest()
        {
            const string source = "GET /where?q=now HTTP/1.1\r\n";
            HttpRequestLine.TryParse(source, out var line);
            line.Method.Is(HttpMethod.Get);
            line.HttpVersion.Is(HttpVersion.Version11);
            line.RequestTargetForm.Is(RequestTargetForm.OriginForm);
            line.RequestTarget.Is("/where?q=now");
            line.ToString().Is(source);
        }

        [Fact]
        public void AbsoluteFormTest()
        {
            const string source = "GET http://www.example.org/pub/WWW/TheProject.html HTTP/1.1\r\n";
            HttpRequestLine.TryParse(source, out var line);
            line.Method.Is(HttpMethod.Get);
            line.HttpVersion.Is(HttpVersion.Version11);
            line.RequestTargetForm.Is(RequestTargetForm.AbsoluteForm);
            line.RequestTarget.Is("http://www.example.org/pub/WWW/TheProject.html");
            line.ToString().Is(source);
        }

        [Fact]
        public void AuthorityForm()
        {
            const string source = "CONNECT www.example.com:80 HTTP/1.1\r\n";
            HttpRequestLine.TryParse(source, out var line);
            line.Method.Is(new HttpMethod("CONNECT"));
            line.HttpVersion.Is(HttpVersion.Version11);
            line.RequestTargetForm.Is(RequestTargetForm.AuthorityForm);
            line.RequestTarget.Is("www.example.com:80");
            line.ToString().Is(source);
        }

        [Fact]
        public void AsteriskFormTest()
        {
            const string source = "OPTIONS * HTTP/1.1\r\n";
            HttpRequestLine.TryParse(source, out var line);
            line.Method.Is(HttpMethod.Options);
            line.HttpVersion.Is(HttpVersion.Version11);
            line.RequestTargetForm.Is(RequestTargetForm.AsteriskForm);
            line.RequestTarget.Is("*");
            line.ToString().Is(source);
        }

        [Fact]
        public void BrokenRequestLineTest()
        {
            const string source = "hogeeeee\r\n";
            var isSucceeded = HttpRequestLine.TryParse(source, out var line);
            isSucceeded.IsFalse();
            line.Source.Is(source);
            line.ToString().Is(source);
        }

        [Fact]
        public void OriginTest()
        {
            const string source = "CONNECT www.example.com:80 HTTP/1.1\r\n";
            HttpRequestLine.TryParse(source, out var line);
            line.Source.Is(line.Source);
            line.GetOrigin().IsNot(line.GetOrigin());
        }
    }
}
