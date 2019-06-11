using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Nekoxy2.Spi.Entities.Http;
using Nekoxy2.ApplicationLayer.Entities;
using Nekoxy2.Default.Proxy;
using Nekoxy2.ApplicationLayer.Entities.Http;
using Xunit;
using System.Linq;
using Nekoxy2.Test.TestUtil;

namespace Nekoxy2.Test.Default.Proxy
{
    public class ClientConnectionTest
    {
        [Fact]
        public void RequestTest()
        {
            var tcp = new TestTcpClient();
            var connection = new ClientConnection(tcp);
            connection.StartReceiving();
            var tcsHeader = new TaskCompletionSource<HttpRequest>();
            connection.ReceivedRequestHeaders += result => tcsHeader.TrySetResult(result);
            var tcsBody = new TaskCompletionSource<HttpRequest>();
            connection.ReceivedRequestBody += result => tcsBody.TrySetResult(result);

            tcp.WriteToInput(
@"GET http://203.104.209.71/kcs2/resources/voice/titlecall_1/005.mp3 HTTP/1.1
Host: 203.104.209.71
Connection: keep-alive
User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.79 Safari/537.36
Accept: */*
Referer: http://203.104.209.71/kcs2/index.php?api_root=/kcsapi&voice_root=/kcs/sound&osapi_root=osapi.example.com&version=4.1.1.4
Accept-Encoding: gzip, deflate
Accept-Language: en-US,en;q=0.9

");

            var header = tcsHeader.GetResult();
            var body = tcsBody.GetResult();

            header.RequestLine.HttpVersion.Is(new Version(1, 1));
            header.RequestLine.Method.Is(HttpMethod.Get);
            header.RequestLine.RequestTarget.Is("http://203.104.209.71/kcs2/resources/voice/titlecall_1/005.mp3");

            header.Headers.Host.Is("203.104.209.71");
            header.Headers.GetFirstValue("Connection").IsNull();
            header.Headers.GetOrigin().GetFirstValue("Connection").Is("keep-alive");
            header.Headers.GetFirstValue("User-Agent").Is("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.79 Safari/537.36");
            header.Headers.GetFirstValue("Accept").Is("*/*");
            header.Headers.GetFirstValue("Referer").Is("http://203.104.209.71/kcs2/index.php?api_root=/kcsapi&voice_root=/kcs/sound&osapi_root=osapi.example.com&version=4.1.1.4");
            header.Headers.GetFirstValue("Accept-Encoding").Is("gzip, deflate");
            header.Headers.GetFirstValue("Accept-Language").Is("en-US,en;q=0.9");

            body.Body.IsNull();

            connection.Dispose();
        }

        [Fact]
        public void RequestBodyTest()
        {
            var tcp = new TestTcpClient();
            var connection = new ClientConnection(tcp);
            connection.StartReceiving();
            var tcsHeader = new TaskCompletionSource<HttpRequest>();
            connection.ReceivedRequestHeaders += result => tcsHeader.TrySetResult(result);
            var tcsBody = new TaskCompletionSource<HttpRequest>();
            connection.ReceivedRequestBody += result => tcsBody.TrySetResult(result);

            tcp.WriteToInput(
@"POST http://203.104.209.71/kcsapi/api_get_member/sortie_conditions HTTP/1.1
Host: 203.104.209.71
Connection: keep-alive
Content-Length: 62
Accept: application/json, text/plain, */*
Origin: http://203.104.209.71
User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.79 Safari/537.36
Content-Type: application/x-www-form-urlencoded
Referer: http://203.104.209.71/kcs2/index.php?api_root=/kcsapi&voice_root=/kcs/sound&osapi_root=osapi.example.com&version=4.1.1.4
Accept-Encoding: gzip, deflate
Accept-Language: en-US,en;q=0.9

api_token=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx&api_verno=1");

            var header = tcsHeader.GetResult();
            var body = tcsBody.GetResult();

            header.RequestLine.HttpVersion.Is(new Version(1, 1));
            header.RequestLine.Method.Is(HttpMethod.Post);
            header.RequestLine.RequestTarget.Is("http://203.104.209.71/kcsapi/api_get_member/sortie_conditions");

            header.Headers.Host.Is("203.104.209.71");
            header.Headers.GetFirstValue("Connection").IsNull();
            header.Headers.GetOrigin().GetFirstValue("Connection").Is("keep-alive");
            header.Headers.ContentLength.Exists.IsTrue();
            header.Headers.ContentLength.Is(62);
            header.Headers.GetFirstValue("Accept").Is("application/json, text/plain, */*");
            header.Headers.GetFirstValue("User-Agent").Is("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.79 Safari/537.36");
            header.Headers.ContentLength.Exists.IsTrue();
            header.Headers.ContentType.Is("application/x-www-form-urlencoded");
            header.Headers.GetFirstValue("Referer").Is("http://203.104.209.71/kcs2/index.php?api_root=/kcsapi&voice_root=/kcs/sound&osapi_root=osapi.example.com&version=4.1.1.4");
            header.Headers.GetFirstValue("Accept-Encoding").Is("gzip, deflate");
            header.Headers.GetFirstValue("Accept-Language").Is("en-US,en;q=0.9");

            body.GetBodyAsString().Is("api_token=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx&api_verno=1");

            connection.Dispose();
        }

        [Fact]
        public void ResponseContentLengthTest()
        {
            this.ResponseTest("TestData/ResponseContentLength");
        }

        [Fact]
        public void ResponseChunkedTest()
        {
            this.ResponseTest("TestData/ResponseChunked");
        }

        [Fact]
        public void ResponseChunkedWithTrailerTest()
        {
            this.ResponseTest("TestData/ResponseChunkedWithTrailer");
        }

        [Fact]
        public void ResponseNoBodyTest()
        {
            this.ResponseTest("TestData/ResponseNoBody");
        }

        private void ResponseTest(string path)
        {
            var tcp = new TestTcpClient();
            var connection = new ClientConnection(tcp);
            connection.StartReceiving();
            connection.WriteFile(path);
            var response = tcp.ReadAllBytesFromOutput();
            response.Is(path);
            connection.Dispose();
        }
    }
}
