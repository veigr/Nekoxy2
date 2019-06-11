using Nekoxy2.Default;
using Nekoxy2.Test.TestUtil;
using Nekoxy2.Entities.Http;
using Nekoxy2.Entities.Http.Delegations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nekoxy2.Test.Api
{
    public class HttpProxyTest
    {
        static HttpProxyTest()
        {
            Nekoxy2.ApplicationLayer.Entities.Http.HttpHeaders.Now = () => TestConstants.Now;
        }

        [Fact]
        public void AfterSessionCompleteTest()
        {
            var server = new TestTcpServer();
            var engine = new DefaultEngine(server);

            var proxy = HttpProxy.Create(engine);
            var tcsComplete = new TaskCompletionSource<IReadOnlySession>();
            proxy.HttpResponseSent += (_, s) => tcsComplete.TrySetResult(s.Session);

            var clientTcp = new TestTcpClient();
            server.AcceptTcp(clientTcp);
            var connection = engine.connections.Last();
            connection.CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port);

            var request =
@"CONNECT www.example.com:443 HTTP/1.1
Host: www.example.com:443
Proxy-Connection: keep-alive
User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.79 Safari/537.36

";
            clientTcp.WriteToInput(request);

            var expectedResponse =
@"HTTP/1.1 200 Connection Established
Date: Mon, 01 Jan 2018 01:00:00 GMT

";

            var session = tcsComplete.GetResult();
            session.Request.ToString().Is(request);
            session.Response.ToString().Is(expectedResponse);
        }
    }
}
