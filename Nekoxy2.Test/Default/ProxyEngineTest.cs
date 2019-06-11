using Nekoxy2.Default;
using Nekoxy2.ApplicationLayer.Entities;
using Nekoxy2.ApplicationLayer.Entities.Http;
using Nekoxy2.Default.Proxy;
using Nekoxy2.Spi;
using Nekoxy2.Spi.Entities.Http;
using Nekoxy2.Test.TestUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Nekoxy2.Test.Default
{
    public class ProxyEngineTest
    {
        [Fact]
        public void VeryQuicklyCloseTest()
        {
            var server = new TestTcpServer();
            var engine = new DefaultEngine(server)
            {
                DelayForTest = 100
            };

            var client = new TestTcpClient();
            Task.Run(() => server.AcceptTcp(client));
            client.Close();
            int count = 0;
            while (!client.IsClosed)
            {
                Thread.Sleep(100);
                count++;
                if (50 < count)
                    Assert.False(true, "timeout.");
            }
            engine.connections.Count.Is(0);

            (engine as IReadOnlyHttpProxyEngine).Stop();
        }

        [Fact]
        public void StartAndCloseTest()
        {
            var server = new TestTcpServer();
            var engine = new DefaultEngine(server);

            (engine as IReadOnlyHttpProxyEngine).Start();
            server.IsStartupCalled.IsTrue();
            server.Port.Is(8080);

            engine.connections.Count.Is(0);
            server.AcceptTcp(new TestTcpClient());
            engine.connections.Count.Is(1);
            server.AcceptTcp(new TestTcpClient());
            engine.connections.Count.Is(2);

            var testClient = new TestTcpClient();
            server.AcceptTcp(testClient);
            engine.connections.Count.Is(3);
            var tcsDispose = new TaskCompletionSource<ProxyConnection>();
            engine.connections.Last().Disposing += connection => tcsDispose.TrySetResult(connection);
            testClient.Close();
            tcsDispose.Task.Wait(5000);
            engine.connections.Count.Is(2);

            (engine as IReadOnlyHttpProxyEngine).Stop();
            server.IsShutdownCalled.IsTrue();
            engine.connections.Count.Is(0);
        }

        [Fact]
        public void ListeningConfigTest()
        {
            var server = new TestTcpServer();
            var engine = new DefaultEngine(server, new ListeningConfig(10000));

            (engine as IReadOnlyHttpProxyEngine).Start();
            server.IsStartupCalled.IsTrue();
            server.Port.Is(10000);

            engine.connections.Count.Is(0);
            server.AcceptTcp(new TestTcpClient());
            engine.connections.Count.Is(1);
            server.AcceptTcp(new TestTcpClient());
            engine.connections.Count.Is(2);

            var testClient = new TestTcpClient();
            server.AcceptTcp(testClient);
            engine.connections.Count.Is(3);
            var tcsDispose = new TaskCompletionSource<ProxyConnection>();
            engine.connections.Last().Disposing += connection => tcsDispose.TrySetResult(connection);
            testClient.Close();
            tcsDispose.Task.Wait(5000);
            engine.connections.Count.Is(2);

            (engine as IReadOnlyHttpProxyEngine).Stop();
            server.IsShutdownCalled.IsTrue();
            engine.connections.Count.Is(0);
        }

        [Fact]
        public void ChangeUpstreamProxyTest()
        {
            var server = new TestTcpServer();
            var engine = new DefaultEngine(server);
            var clientTcp1 = new TestTcpClient();
            server.AcceptTcp(clientTcp1);
            var connection1 = engine.connections.Last();
            connection1.CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port);

            engine.UpstreamProxyConfig = new UpstreamProxyConfig("first.example.com", 1);

            var tcsRequest1_1 = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection1.HttpRequestSent += r => tcsRequest1_1.TrySetResult(r);

            var request =
@"GET / HTTP/1.1
Host: hoge.example.com

";
            clientTcp1.WriteToInput(request);

            var request1_1 = tcsRequest1_1.GetResult();

            var serverTcp1 = connection1.serverConnection.client.AsTest();
            serverTcp1.Host.Is("first.example.com");
            serverTcp1.Port.Is(1);

            // UpstreamProxyConfig 変更
            engine.UpstreamProxyConfig = new UpstreamProxyConfig("second.example.com", 2);

            var tcsRequest1_2 = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection1.HttpRequestSent += r => tcsRequest1_2.TrySetResult(r);
            clientTcp1.WriteToInput(request);
            // 接続中のコネクションには反映されない
            var request1_2 = tcsRequest1_2.GetResult();
            serverTcp1.Host.Is("first.example.com");
            serverTcp1.Port.Is(1);

            // 次の接続から反映される
            var clientTcp2 = new TestTcpClient();
            server.AcceptTcp(clientTcp2);
            var connection2 = engine.connections.Last();
            connection2.CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port);

            connection2.HttpRequestSent += r => tcsRequest1_1.TrySetResult(r);

            var tcsRequest2 = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection2.HttpRequestSent += r => tcsRequest2.TrySetResult(r);
            clientTcp2.WriteToInput(request);
            var request2 = tcsRequest2.GetResult();
            var serverTcp2 = connection2.serverConnection.client.AsTest();
            serverTcp2.Host.Is("second.example.com");
            serverTcp2.Port.Is(2);

            (engine as IReadOnlyHttpProxyEngine).Stop();
        }

        [Fact]
        public void IsCaptureBodyTest()
        {
            var server = new TestTcpServer();
            var engine = new DefaultEngine(server)
            {
                IsCaptureBody = false
            };

            var clientTcp = new TestTcpClient();
            server.AcceptTcp(clientTcp);

            var connection = engine.connections.Last();
            connection.CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port);

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += r => tcsRequest.TrySetResult(r);
            var tcsSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += s => tcsSession.TrySetResult(s);

            var request =
@"GET / HTTP/1.1
Host: hoge.example.com

";
            clientTcp.WriteToInput(request);

            tcsRequest.Task.Wait(5000);

            connection.serverConnection.client.AsTest().WriteFileToInput("TestData/ResponseChunked");

            var session = tcsSession.GetResult();
            (session.Request as HttpRequest).Source.Is(request);
            session.Response.ToString().Is(@"HTTP/1.1 200 OK
Server: nginx
Date: Thu, 20 Sep 2018 01:59:09 GMT
Content-Type: application/javascript; charset=utf-8
Transfer-Encoding: chunked
Connection: keep-alive
Cache-Control: private, no-cache, no-cache=""Set-Cookie"", proxy-revalidate
Pragma: no-cache
Access-Control-Allow-Origin: *
P3P: CP=""ADM NOI OUR""
Content-Encoding: gzip

");
            session.Response.Body.Length.Is(0);

            (engine as IReadOnlyHttpProxyEngine).Stop();
        }

        [Fact]
        public void HandlerExceptionTest()
        {
            var server = new TestTcpServer();
            var engine = new DefaultEngine(server);

            var clientTcp = new TestTcpClient();
            server.AcceptTcp(clientTcp);

            var connection = engine.connections.Last();
            connection.CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port);

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += r => tcsRequest.TrySetResult(r);

            var tcsSession = new TaskCompletionSource<IReadOnlySession>();
            (engine as IReadOnlyHttpProxyEngine).HttpResponseSent += (_, s) => tcsSession.TrySetResult(s.Session);
            (engine as IReadOnlyHttpProxyEngine).HttpResponseSent += (_, s) => throw new Exception();

            var request =
@"GET / HTTP/1.1
Host: hoge.example.com

";
            clientTcp.WriteToInput(request);

            tcsRequest.Task.Wait(5000);

            connection.serverConnection.client.AsTest().WriteFileToInput("TestData/ResponseChunked");

            var session = tcsSession.GetResult();

            engine.connections.Count.Is(1);

            (engine as IReadOnlyHttpProxyEngine).Stop();
        }
    }
}
