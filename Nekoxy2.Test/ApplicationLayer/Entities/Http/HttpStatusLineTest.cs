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
    public class HttpStatusLineTest
    {
        [Fact]
        public void ParseTest()
        {
            const string source = "HTTP/1.1 200 Connection Established\r\n";
            HttpStatusLine.TryParse(source, out var line);
            line.HttpVersion.Is(HttpVersion.Version11);
            line.StatusCode.Is(HttpStatusCode.OK);
            line.ReasonPhrase.Is("Connection Established");
            line.ToString().Is(source);
        }

        [Fact]
        public void NoReasonPhraseTest()
        {
            const string source = "HTTP/1.1 200 \r\n";
            HttpStatusLine.TryParse(source, out var line);
            line.HttpVersion.Is(HttpVersion.Version11);
            line.StatusCode.Is(HttpStatusCode.OK);
            line.ReasonPhrase.Is("");
            line.ToString().Is(source);
        }

        [Fact]
        public void OriginTest()
        {
            const string source = "HTTP/1.1 200 Connection Established\r\n";
            HttpStatusLine.TryParse(source, out var line);
            line.Source.Is(line.Source);
        }
    }
}
