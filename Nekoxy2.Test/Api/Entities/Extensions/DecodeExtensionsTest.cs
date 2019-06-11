using Nekoxy2.ApplicationLayer.Entities;
using Nekoxy2.Default.Proxy;
using Nekoxy2.Test.TestUtil;
using Nekoxy2.ApplicationLayer.Entities.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nekoxy2.Test.Api.Entities.Extensions
{
    public class DecodeExtensionsTest
    {
        [Fact]
        public async void GetBodyAsStringTest()
        {
            var response = await TestResponse("TestData/ResponseChunked");
            response.GetBodyAsString()
                .Is("_itm_.sa_cb({})");
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
