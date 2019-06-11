using Nekoxy2.Default;
using Nekoxy2.ApplicationLayer.Entities.Http;
using Nekoxy2.ApplicationLayer.ProtocolReaders.Http;
using Nekoxy2.Test.TestUtil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nekoxy2.Test.ApplicationLayer.ProtocolReaders.Http
{
    public class HttpResponseReaderTest
    {
        [Fact]
        public void TooLongBodyTest()
        {
            //var maxCaptureSize = ProxyConfig.MaxByteArrayLength;
            var maxCaptureSize = 10_000_000;
            var config = new ProxyConfig
            {
                MaxCaptureSize = maxCaptureSize
            };


            var contentLengthResponseHeader = Encoding.ASCII.GetBytes(
$@"HTTP/1.1 200 OK
Date: Thu, 20 Sep 2018 01:59:09 GMT
Content-Length: {maxCaptureSize + 1}

");
            using (var reader = new HttpResponseReader(config.IsCaptureBody, config.MaxCaptureSize))
            {
                var tcs = new TaskCompletionSource<HttpResponse>();
                reader.ReceivedResponseBody += r => tcs.TrySetResult(r);
                reader.HandleReceive(contentLengthResponseHeader, contentLengthResponseHeader.Length);
                for (var i = 0m; i < maxCaptureSize + 1;)
                {
                    Debug.WriteLine(i);
                    var length = 200000000m;
                    if (maxCaptureSize < length + i)
                    {
                        length = maxCaptureSize + 1 - i;
                    }
                    var bytes = Enumerable.Repeat((byte)0, (int)length).ToArray();
                    reader.HandleReceive(bytes, bytes.Length);
                    i += bytes.Length;
                }
                var response = tcs.GetResult();
                response.Body.Length.Is(0);
            }



            var chunkedBodyResonseHeader = Encoding.ASCII.GetBytes(
$@"HTTP/1.1 200 OK
Date: Thu, 20 Sep 2018 01:59:09 GMT
Transfer-Encoding: chunked

");
            using (var reader = new HttpResponseReader(config.IsCaptureBody, config.MaxCaptureSize))
            {
                var tcs = new TaskCompletionSource<HttpResponse>();
                reader.ReceivedResponseBody += r => tcs.TrySetResult(r);
                reader.HandleReceive(chunkedBodyResonseHeader, chunkedBodyResonseHeader.Length);

                var lastChunk = Encoding.ASCII.GetBytes("\r\n0\r\n\r\n");
                var maxSize = maxCaptureSize + 1 - Convert.ToString(maxCaptureSize, 16).Length - lastChunk.Length;
                var maxSizeString = Convert.ToString(maxSize, 16);
                var chunkSize = Encoding.ASCII.GetBytes(maxSizeString + "\r\n");

                reader.HandleReceive(chunkSize, chunkSize.Length);

                for (var i = 0m; i < maxSize;)
                {
                    Debug.WriteLine(i);
                    var length = 200000000m;
                    if (maxCaptureSize < length + i)
                    {
                        length = maxSize - i;
                    }
                    var bytes = Enumerable.Repeat((byte)0, (int)length).ToArray();
                    reader.HandleReceive(bytes, bytes.Length);
                    i += bytes.Length;
                }
                reader.HandleReceive(lastChunk, lastChunk.Length);
                var response = tcs.GetResult();
                response.Body.Length.Is(0);
            }



            var terminateWithCloseResponseHeader = Encoding.ASCII.GetBytes(
$@"HTTP/1.1 200 OK
Date: Thu, 20 Sep 2018 01:59:09 GMT
Transfer-Encoding: hoge

");
            using (var reader = new HttpResponseReader(config.IsCaptureBody, config.MaxCaptureSize))
            {
                var tcs = new TaskCompletionSource<HttpResponse>();
                reader.ReceivedResponseBody += r => tcs.TrySetResult(r);
                reader.HandleReceive(terminateWithCloseResponseHeader, terminateWithCloseResponseHeader.Length);
                for (var i = 0m; i < maxCaptureSize + 1;)
                {
                    Debug.WriteLine(i);
                    var length = 200000000m;
                    if (maxCaptureSize < length + i)
                    {
                        length = maxCaptureSize + 1 - i;
                    }
                    var bytes = Enumerable.Repeat((byte)0, (int)length).ToArray();
                    reader.HandleReceive(bytes, bytes.Length);
                    i += bytes.Length;
                }
                reader.CloseTcp();
                var response = tcs.GetResult();
                response.Body.Length.Is(0);
            }

        }

        [Fact]
        public void StartWithLFFragmentTest()
        {
            var heades = Encoding.ASCII.GetBytes(@"HTTP/1.1 200
Content-Type: application/json;charset=UTF-8
Date: Mon, 03 Dec 2018 02:54:48 GMT
transfer-encoding: chunked

");
            using (var reader = new HttpResponseReader(true, 1024 * 1024 * 1024))
            {
                var tcs = new TaskCompletionSource<HttpResponse>();
                reader.ReceivedResponseBody += r => tcs.TrySetResult(r);

                reader.HandleReceive(heades, heades.Length);
                reader.HandleReceive("2\r\n");
                reader.HandleReceive("{}\r");
                reader.HandleReceive("\n0\r\n\r\n");

                var response = tcs.GetResult();
                response.GetBodyAsString().Is("{}");
            }
        }

        [Fact]
        public void HandleResponseTest()
        {
            using (var reader = new HttpResponseReader(true, int.MaxValue))
            {
                var counter = 0;
                var tcs = new TaskCompletionSource<HttpResponse>();
                reader.ReceivedResponseBody += r =>
                {
                    counter++;
                    if(1 < counter)
                        tcs.TrySetResult(r);
                };

                reader.HandleReceive(
@"HTTP/1.1 100 Continue
Date: Mon, 01 Jan 2018 01:00:00 GMT

");

                reader.HandleReceive(
@"HTTP/1.1 200 OK
Date: Mon, 01 Jan 2018 01:00:00 GMT

");

                var response = tcs.GetResult();
                response.StatusLine.StatusCode.Is(System.Net.HttpStatusCode.OK);
            }
        }
    }

    static partial class Extensions
    {
        public static void HandleReceive(this HttpResponseReader reader, string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            reader.HandleReceive(bytes, bytes.Length);
        }
    }
}
