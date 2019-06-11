using Nekoxy2.Spi.Entities.Http;
using Nekoxy2.ApplicationLayer.Entities;
using Nekoxy2.Default.Proxy;
using Nekoxy2.Default.Proxy.Tcp;
using Nekoxy2.ApplicationLayer.Entities.Http;
using Nekoxy2.Test.TestUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nekoxy2.Test.Default.Proxy
{
    public class ServerConnectionTest
    {
        [Fact]
        public async void ResponseContentLengthTest()
        {
            var response = await TestResponse("TestData/ResponseContentLength");

            response.StatusLine.HttpVersion.Is(new Version(1, 1));
            response.StatusLine.StatusCode.Is(HttpStatusCode.OK);
            response.StatusLine.ReasonPhrase.Is("OK");

            response.Headers.ContentLength.Exists.IsTrue();
            response.Headers.ContentLength.Is(47);
            response.Headers.TransferEncoding.Exists.IsFalse();
            response.Headers.IsChunked.IsFalse();
            response.Headers.GetFirstValue("Access-Control-Allow-Credentials").Is("true");
            response.Headers.GetFirstValue("Access-Control-Allow-Methods").Is("POST,GET,HEAD,OPTIONS");
            response.Headers.GetFirstValue("Access-Control-Allow-Origin").Is("http://d.hatena.ne.jp");
            response.Headers.GetFirstValue("Cache-Control").Is("no-store, no-cache");
            response.Headers.ContentType.Is("application/json");
            response.Headers.GetFirstValue("Date").Is("Thu, 20 Sep 2018 01:59:09 GMT");
            response.Headers.GetFirstValue("Expires").Is("Mon, 15 Jun 1998 00:00:00 GMT");
            response.Headers.GetFirstValue("Pragma").Is("no-cache");
            response.Headers.GetFirstValue("Server").Is("Adtech Adserver");

            response.GetBodyAsString().Is(@"{""id"":""31259257328373081"",""seatbid"":[],""nbr"":1}");
        }

        [Fact]
        public async void ResponseChunkedTest()
        {
            var response = await TestResponse("TestData/ResponseChunked");

            response.StatusLine.HttpVersion.Is(new Version(1, 1));
            response.StatusLine.StatusCode.Is(HttpStatusCode.OK);
            response.StatusLine.ReasonPhrase.Is("OK");

            response.Headers.ContentLength.Exists.IsFalse();
            response.Headers.TransferEncoding.Exists.IsTrue();
            response.Headers.IsChunked.IsTrue();
            response.Headers.GetFirstValue("Server").Is("nginx");
            response.Headers.GetFirstValue("Date").Is("Thu, 20 Sep 2018 01:59:09 GMT");
            response.Headers.ContentType.Is("application/javascript; charset=utf-8");
            response.Headers.GetFirstValue("Connection").IsNull(); // Connection は削除される
            response.Headers.GetFirstValue("Cache-Control").Is(@"private, no-cache, no-cache=""Set-Cookie"", proxy-revalidate");
            response.Headers.GetFirstValue("Pragma").Is("no-cache");
            response.Headers.GetFirstValue("Access-Control-Allow-Origin").Is("*");
            response.Headers.GetFirstValue("P3P").Is(@"CP=""ADM NOI OUR""");
            response.Headers.GetFirstValue("Content-Encoding").Is("gzip");

            response.GetBodyAsString().Is("_itm_.sa_cb({})");
        }

        [Fact]
        public async void ResponseChunkedWithTrailerTest()
        {
            var response = await TestResponse("TestData/ResponseChunkedWithTrailer");

            response.StatusLine.HttpVersion.Is(new Version(1, 1));
            response.StatusLine.StatusCode.Is(HttpStatusCode.OK);
            response.StatusLine.ReasonPhrase.Is("OK");

            response.Headers.ContentLength.Exists.IsFalse();
            response.Headers.TransferEncoding.Exists.IsTrue();
            response.Headers.IsChunked.IsTrue();
            response.Headers.GetFirstValue("Server").Is("nginx");
            response.Headers.GetFirstValue("Date").Is("Thu, 20 Sep 2018 01:59:09 GMT");
            response.Headers.ContentType.Is("application/javascript; charset=utf-8");
            response.Headers.GetFirstValue("Connection").IsNull(); // Connection は削除される
            response.Headers.GetFirstValue("Cache-Control").Is(@"private, no-cache, no-cache=""Set-Cookie"", proxy-revalidate");
            response.Headers.GetFirstValue("Pragma").Is("no-cache");
            response.Headers.GetFirstValue("Access-Control-Allow-Origin").Is("*");
            response.Headers.GetFirstValue("P3P").Is(@"CP=""ADM NOI OUR""");
            response.Headers.GetFirstValue("Content-Encoding").Is("gzip");

            response.GetBodyAsString().Is("_itm_.sa_cb({})");

            response.Trailers.GetFirstValue("Expires").Is("Wed, 21 Oct 2015 07:28:00 GMT");
        }

        [Fact]
        public async void ResponseNoBodyTest()
        {
            var response = await TestResponse("TestData/ResponseNoBody");

            response.StatusLine.HttpVersion.Is(new Version(1, 1));
            response.StatusLine.StatusCode.Is(HttpStatusCode.NotModified);
            response.StatusLine.ReasonPhrase.Is("Not Modified");

            response.Headers.ContentLength.Exists.IsFalse();
            response.Headers.TransferEncoding.Exists.IsFalse();
            response.Headers.GetFirstValue("Content-Type").Is("application/javascript");
            response.Headers.GetFirstValue("Last-Modified").Is("Wed, 03 Jun 2015 12:35:51 GMT");
            response.Headers.GetFirstValue("ETag").Is(@"""eed3683fc74523e3147bc9e4868885b6""");
            response.Headers.GetFirstValue("Expires").Is("Thu, 20 Sep 2018 01:59:09 GMT");
            response.Headers.GetFirstValue("Cache-Control").Is("max-age=0, no-cache");
            response.Headers.GetFirstValue("Pragma").Is("no-cache");
            response.Headers.GetFirstValue("Date").Is("Thu, 20 Sep 2018 01:59:09 GMT");
            response.Headers.GetFirstValue("Connection").IsNull(); // Connection は削除される
            response.Headers.GetFirstValue("P3P").Is(@"CP=""NOI PSD OTR""");

            response.Body.IsNull();
        }

        [Fact]
        public void RequestTest()
        {
            var connection = new ServerConnection(new TestTcpClient());
            connection.StartReceiving();
            connection.IsPauseBeforeReceive = false;
            var request =
@"GET http://203.104.209.71/kcs2/resources/voice/titlecall_1/005.mp3 HTTP/1.1
Host: 203.104.209.71
Connection: keep-alive
User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.79 Safari/537.36
Accept: */*
Referer: http://203.104.209.71/kcs2/index.php?api_root=/kcsapi&voice_root=/kcs/sound&osapi_root=osapi.example.com&version=4.1.1.4
Accept-Encoding: gzip, deflate
Accept-Language: en-US,en;q=0.9

";
            connection.Write(request);
            connection.client.AsTest().ReadAllStringFromOutput().Is(request);

            connection.Dispose();
        }

        [Fact]
        public void Status1xxTest()
        {
            // 1xx レスポンスの場合、最初の空行でメッセージが終了する
            var response =
@"HTTP/1.1 100 Continue

";

            var connection = new ServerConnection(new TestTcpClient());
            connection.StartReceiving();
            connection.IsPauseBeforeReceive = false;
            var tcsBody = new TaskCompletionSource<HttpResponse>();
            void handler(HttpResponse r) => tcsBody.TrySetResult(r);
            connection.ReceivedResponseBody += handler;
            connection.client.AsTest().WriteToInput(response);
            var result = tcsBody.GetResult();
            connection.ReceivedResponseBody -= handler;
            connection.Dispose();
        }

        static async Task<HttpResponse> TestResponse(string path)
        {
            var connection = new ServerConnection(new TestTcpClient());
            connection.StartReceiving();
            connection.IsPauseBeforeReceive = false;
            var tcsBody = new TaskCompletionSource<HttpResponse>();
            void handler(HttpResponse r) => tcsBody.TrySetResult(r);
            connection.ReceivedResponseBody += handler;
            connection.client.AsTest().WriteFileToInput(path);
            var result = await tcsBody.Task;
            connection.ReceivedResponseBody -= handler;
            connection.Dispose();
            return result;
        }
    }
}
