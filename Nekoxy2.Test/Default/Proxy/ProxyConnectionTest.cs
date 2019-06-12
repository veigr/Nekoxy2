using Nekoxy2.Default;
using Nekoxy2.ApplicationLayer.Entities;
using Nekoxy2.Default.Proxy;
using Nekoxy2.Default.Proxy.Tcp;
using Nekoxy2.ApplicationLayer.Entities.Http;
using Nekoxy2.Spi.Entities.Http;
using Nekoxy2.Test.TestUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Nekoxy2.Test;
using Nekoxy2.Default.Certificate.Default;
using System.Security.Cryptography.X509Certificates;
using Nekoxy2.Spi.Entities.WebSocket;

namespace Nekoxy2.Test.Default.Proxy
{
    public class ProxyConnectionTest
    {
        static ProxyConnectionTest()
        {
            HttpHeaders.Now = () => TestConstants.Now;
        }

        [Fact]
        public void SessionContentLengthTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);
            
            var tcsSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += result => tcsSession.TrySetResult(result);

            var tcsClose = new TaskCompletionSource<ProxyConnection>();
            connection.Disposing += result => tcsClose.TrySetResult(result);

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
            clientTcp.WriteToInput(request);

            //var tcsRequestSuccess = tcsRequest.Task.Wait(1000);
            //if (!tcsRequestSuccess)
            //    Assert.False(true, "request timeout");
            var requestResult = tcsRequest.GetResult();

            connection.serverConnection.client.AsTest().WriteFileToInput("TestData/ResponseContentLength");

            //var tcsSessionSuccess = tcsSession.Task.Wait(1000);
            //if (!tcsSessionSuccess)
            //    Assert.False(true, "session timeout");
            var sessionResult = tcsSession.GetResult();

            var requestHeaders = sessionResult.Request.Headers as HttpHeaders;
            sessionResult.Request.RequestLine.HttpVersion.Is(new Version(1, 1));
            sessionResult.Request.RequestLine.Method.Is(HttpMethod.Get);
            sessionResult.Request.RequestLine.RequestTarget.Is("http://203.104.209.71/kcs2/resources/voice/titlecall_1/005.mp3");
            requestHeaders.Host.Is("203.104.209.71");
            requestHeaders.GetFirstValue("Connection").Is("keep-alive");
            requestHeaders.GetFirstValue("User-Agent").Is("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.79 Safari/537.36");
            requestHeaders.GetFirstValue("Accept").Is("*/*");
            requestHeaders.GetFirstValue("Referer").Is("http://203.104.209.71/kcs2/index.php?api_root=/kcsapi&voice_root=/kcs/sound&osapi_root=osapi.example.com&version=4.1.1.4");
            requestHeaders.GetFirstValue("Accept-Encoding").Is("gzip, deflate");
            requestHeaders.GetFirstValue("Accept-Language").Is("en-US,en;q=0.9");
            requestHeaders.HasHeader("Via").IsFalse();
            sessionResult.Request.Body.IsNull();


            var response = sessionResult.Response as HttpResponse;
            response.StatusLine.HttpVersion.Is(new Version(1, 1));
            response.StatusLine.StatusCode.Is(HttpStatusCode.OK);
            response.StatusLine.ReasonPhrase.Is("OK");
            response.Headers.ContentLength.Exists.Is(true);
            response.Headers.ContentLength.Is(47);
            response.Headers.TransferEncoding.Exists.Is(false);
            response.Headers.IsChunked.Is(false);
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



            //connection.serverConnection.client.AsTest().Close();    // サーバーから閉じる
            clientTcp.Close();  // クライアントから閉じる

            tcsClose.GetResult().Is(connection);

            connection.Dispose();
        }

        [Fact]
        public void SessionChunkedTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var tcsSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += result => tcsSession.TrySetResult(result);

            var tcsClose = new TaskCompletionSource<ProxyConnection>();
            connection.Disposing += result => tcsClose.TrySetResult(result);

            var request =
@"GET http://203.104.209.71/kcs2/resources/voice/titlecall_1/005.mp3 HTTP/1.1
Host: 203.104.209.71

";
            clientTcp.WriteToInput(request);

            var requestResult = tcsRequest.GetResult();

            connection.serverConnection.client.AsTest().WriteFileToInput("TestData/ResponseChunked");

            var sessionResult = tcsSession.GetResult();

            sessionResult.Request.ToString().Is(request);

            var response = sessionResult.Response as HttpResponse;
            response.GetBodyAsString().Is(@"_itm_.sa_cb({})");

            connection.serverConnection.client.AsTest().Close();    // サーバーから閉じる
            //clientTcp.Close();  // クライアントから閉じる

            tcsClose.GetResult().Is(connection);

            connection.Dispose();
        }

        [Fact]
        public void SessionNoBodyTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var tcsSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += result => tcsSession.TrySetResult(result);

            var tcsClose = new TaskCompletionSource<ProxyConnection>();
            connection.Disposing += result => tcsClose.TrySetResult(result);

            var request =
@"GET http://203.104.209.71/kcs2/resources/voice/titlecall_1/005.mp3 HTTP/1.1
Host: 203.104.209.71

";
            clientTcp.WriteToInput(request);

            var requestResult = tcsRequest.GetResult();

            connection.serverConnection.client.AsTest().WriteFileToInput("TestData/ResponseNoBody");

            var sessionResult = tcsSession.GetResult();

            sessionResult.Request.ToString().Is(request);

            var response = sessionResult.Response as HttpResponse;
            response.GetBodyAsString().Is("");

            connection.serverConnection.client.AsTest().Close();    // サーバーから閉じる
            //clientTcp.Close();  // クライアントから閉じる

            tcsClose.GetResult().Is(connection);

            connection.Dispose();
        }

        [Fact]
        public void Session302Test()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var tcsSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += result => tcsSession.TrySetResult(result);

            var tcsClose = new TaskCompletionSource<ProxyConnection>();
            connection.Disposing += result => tcsClose.TrySetResult(result);

            var request =
@"GET http://www.example.com/netgame/social/-/gadgets/=/app_id=854854/ HTTP/1.1
Host: www.example.com
Proxy-Connection: keep-alive
Upgrade-Insecure-Requests: 1
User-Agent: Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.79 Safari/537.36
Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8
Accept-Encoding: gzip, deflate
Accept-Language: en-US,en;q=0.9

";
            clientTcp.WriteToInput(request);

            var requestResult = tcsRequest.GetResult();

            connection.serverConnection.client.AsTest().WriteFileToInput("TestData/Response302");

            var sessionResult = tcsSession.GetResult();

            sessionResult.Request.ToString().Is(request);

            var response = sessionResult.Response as HttpResponse;
            response.GetBodyAsString().Is("");

            connection.serverConnection.client.AsTest().Close();    // サーバーから閉じる
            //clientTcp.Close();  // クライアントから閉じる

            tcsClose.GetResult().Is(connection);

            connection.Dispose();
        }

        // TODO Decrypt 有効時の動作も確かめたいが、ローカル内で SslStream でやり取りさせるのめんどすぎる(TestNetworkStreamをかなりちゃんと作る必要がありそう)

        [Fact]
        public void TunnelTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            var exceptions = new List<Exception>();
            connection.FatalException += (_, e) => exceptions.Add(e);

            connection.StartReceiving();

            var tcsSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += result => tcsSession.TrySetResult(result);

            var request =
@"CONNECT www.example.com:443 HTTP/1.1
Host: www.example.com:443
Proxy-Connection: keep-alive
User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.79 Safari/537.36

";
            clientTcp.WriteToInput(request);

            var session = tcsSession.GetResult();

            session.Request.ToString().Is(request);

            var expectedResponse =
@"HTTP/1.1 200 Connection Established
Date: Mon, 01 Jan 2018 01:00:00 GMT

";
            session.Response.ToString().Is(expectedResponse);

            connection.serverConnection.client.AsTest().Host.Is("www.example.com");
            connection.serverConnection.client.AsTest().Port.Is(443);
            connection.serverConnection.IsTunnelMode.IsTrue();
            connection.clientConnection.IsTunnelMode.IsTrue();

            var expectedString = "hogehoge\r\n";

            clientTcp.WriteToInput(expectedString);

            // HTTP と解釈できないので例外が発生するが、データは送信されている。
            var serverTcp = connection.serverConnection.client.AsTest();
            serverTcp.GetStream().AsTest().OutputStream.Is(expectedString);

            exceptions.Count.Is(1);

            connection.Dispose();
        }

        class MyProxy : IWebProxy
        {
            private readonly string httpHost;
            private readonly ushort httpPort;
            private readonly string secureHost;
            private readonly ushort securePort;
            public MyProxy(string httpHost, ushort httpPort, string secureHost, ushort securePort)
            {
                this.httpHost = httpHost;
                this.httpPort = httpPort;
                this.secureHost = secureHost;
                this.securePort = securePort;
            }
            public ICredentials Credentials
            {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }
            public Uri GetProxy(Uri destination)
            {
                if (destination.Scheme == "http")
                    return new Uri($"http://{httpHost}:{httpPort}");
                else
                    return new Uri($"https://{secureHost}:{securePort}");
            }
            public bool IsBypassed(Uri host) => false;
        }

        [Fact]
        public void TunnelUpstreamProxyTest()
        {

            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp, new ProxyConfig
            {
                UpstreamProxyConfig = new UpstreamProxyConfig("proxy", 888, "secureproxy", 8888),
            })
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            var exceptions = new List<Exception>();
            connection.FatalException += (_, e) => exceptions.Add(e);

            connection.StartReceiving();

            var tcsSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += result => tcsSession.TrySetResult(result);

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var request =
@"CONNECT www.example.com:443 HTTP/1.1
Host: www.example.com:443
Proxy-Connection: keep-alive
User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.79 Safari/537.36

";
            var reqeustToServer =
@"CONNECT www.example.com:443 HTTP/1.1
Host: www.example.com:443
User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.79 Safari/537.36
Via: 1.1 127.0.0.1

";
            clientTcp.WriteToInput(request);

            var requestResult = tcsRequest.GetResult();

            var serverTcp = connection.serverConnection.client.AsTest();
            serverTcp.GetStream().AsTest().OutputStream.Is(reqeustToServer);

            // SslStream が生成されない限りは https とみなさないよう変更された
            connection.serverConnection.client.AsTest().Host.Is("proxy");
            connection.serverConnection.client.AsTest().Port.Is(888);

            var response =
@"HTTP/1.1 200 Connection Established
Date: Mon, 01 Jan 2018 01:00:00 GMT
StartTime: 13:31:16.236
Connection: close

";
            connection.serverConnection.client.AsTest().WriteToInput(response);

            var session = tcsSession.GetResult();

            connection.serverConnection.IsTunnelMode.IsTrue();
            connection.clientConnection.IsTunnelMode.IsTrue();

            session.Request.ToString().Is(request);
            session.Response.ToString().Is(response);

            var expectedString = "hogehoge\r\n";

            // HTTP と解釈できないので例外が発生するが、データは送信されている。
            clientTcp.WriteToInput(expectedString);
            serverTcp.GetStream().AsTest().OutputStream.Is(reqeustToServer + expectedString);

            exceptions.Count.Is(1);

            connection.Dispose();
        }

        [Fact]
        public void TunnelWebSocketTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            var exceptions = new List<Exception>();
            connection.FatalException += (_, e) => exceptions.Add(e);

            connection.StartReceiving();

            // CONNECT
            var tcsConnectSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += result => tcsConnectSession.TrySetResult(result);

            var connectRequest =
@"CONNECT www.example.com:80 HTTP/1.1
Host: www.example.com:80
Proxy-Connection: keep-alive
User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.79 Safari/537.36

";
            var connectResponse =
@"HTTP/1.1 200 Connection Established
Date: Mon, 01 Jan 2018 01:00:00 GMT

";
            clientTcp.WriteToInput(connectRequest);

            var connectSession = tcsConnectSession.GetResult();
            connectSession.Request.ToString().Is(connectRequest);
            connectSession.Response.ToString().Is(connectResponse);

            connection.serverConnection.client.AsTest().Host.Is("www.example.com");
            connection.serverConnection.client.AsTest().Port.Is(80);
            connection.serverConnection.IsTunnelMode.IsTrue();
            connection.clientConnection.IsTunnelMode.IsTrue();

            // Upgrade
            var tcsUpgradeSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += result => tcsUpgradeSession.TrySetResult(result);

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var upgradeRequest =
@"GET http://www.example.com/ HTTP/1.1
Host: www.example.com
Sec-WebSocket-Version: 13
Origin: http://www.example.com
Sec-WebSocket-Extensions: permessage-deflate
Sec-WebSocket-Key: WvYcBnHBNEpA8+rVeFI+jg==
Connection: keep-alive, Upgrade
Upgrade: websocket

";
            var upgradeResponse =
@"HTTP/1.1 101 Web Socket Protocol Handshake
Connection: Upgrade
Date: Mon, 01 Jan 2018 01:00:00 GMT
Sec-WebSocket-Accept: OYgMbbkBc+aNH7Fl5LPceT3XepE=
Upgrade: websocket

";
            clientTcp.WriteToInput(upgradeRequest);
            tcsRequest.Task.Wait(5000);
            connection.serverConnection.client.AsTest().WriteToInput(upgradeResponse);

            var upgradeSession = tcsUpgradeSession.GetResult();

            // WebSocket
            var tcsClientWS = new TaskCompletionSource<IReadOnlyWebSocketMessage>();
            var tcsServerWS = new TaskCompletionSource<IReadOnlyWebSocketMessage>();
            connection.ClientWebSocketMessageSent += result => tcsClientWS.TrySetResult(result);
            connection.ServerWebSocketMessageSent += result => tcsServerWS.TrySetResult(result);

            var clientFrame = new byte[]
            {
                0b10000010, // FIN, Binary
                0b01111110, // noMask, 2byteExPart
                0b00000000, // BigEndian
                0b01111110, // 126bytes
            }
            .Concat(Enumerable.Repeat((byte)'c', 126))
            .ToArray();
            var serverFrame = new byte[]
            {
                0b10000010, // FIN, Binary
                0b01111110, // noMask, 2byteExPart
                0b00000000, // BigEndian
                0b01111110, // 126bytes
            }
            .Concat(Enumerable.Repeat((byte)'s', 126))
            .ToArray();

            clientTcp.WriteToInput(clientFrame);
            connection.serverConnection.client.AsTest().WriteToInput(serverFrame);

            var clientWsMessage = tcsClientWS.GetResult();
            var serverWsMessage = tcsServerWS.GetResult();

            clientWsMessage.Opcode.Is(WebSocketOpcode.Binary);
            clientWsMessage.PayloadData.Is(Enumerable.Repeat((byte)'c', 126).ToArray());

            serverWsMessage.Opcode.Is(WebSocketOpcode.Binary);
            serverWsMessage.PayloadData.Is(Enumerable.Repeat((byte)'s', 126).ToArray());

            var serverTcp = connection.serverConnection.client.AsTest();
            serverTcp.GetStream().AsTest().OutputStream.IsEndWith(clientFrame);
            clientTcp.GetStream().AsTest().OutputStream.IsEndWith(serverFrame);

            exceptions.Count.Is(0);

            connection.Dispose();
        }

        [Fact]
        public void TunnelWebSocketWithUpstreamProxyTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp, new ProxyConfig
            {
                UpstreamProxyConfig = new UpstreamProxyConfig("proxy", 888, "secureproxy", 8888),
            })
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            var exceptions = new List<Exception>();
            connection.FatalException += (_, e) => exceptions.Add(e);

            connection.StartReceiving();

            // CONNECT
            var tcsConnectSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += result => tcsConnectSession.TrySetResult(result);

            var tcsConnectRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsConnectRequest.TrySetResult(result);

            var connectRequest =
@"CONNECT www.example.com:80 HTTP/1.1
Host: www.example.com:80
Proxy-Connection: keep-alive
User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/67.0.3396.79 Safari/537.36

";
            var connectResponse =
@"HTTP/1.1 200 Connection Established
Date: Mon, 01 Jan 2018 01:00:00 GMT

";
            clientTcp.WriteToInput(connectRequest);
            tcsConnectRequest.Task.Wait(5000);
            connection.serverConnection.client.AsTest().WriteToInput(connectResponse);

            var connectSession = tcsConnectSession.GetResult();
            connectSession.Request.ToString().Is(connectRequest);
            connectSession.Response.ToString().Is(connectResponse);

            // SslStream が生成されない限りは https とみなさないよう変更された
            connection.serverConnection.client.AsTest().Host.Is("proxy");
            connection.serverConnection.client.AsTest().Port.Is(888);
            connection.serverConnection.IsTunnelMode.IsTrue();
            connection.clientConnection.IsTunnelMode.IsTrue();

            // Upgrade
            var tcsUpgradeSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += result => tcsUpgradeSession.TrySetResult(result);

            var tcsUpgradeRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsUpgradeRequest.TrySetResult(result);

            var upgradeRequest =
@"GET http://www.example.com/ HTTP/1.1
Host: www.example.com
Sec-WebSocket-Version: 13
Origin: http://www.example.com
Sec-WebSocket-Extensions: permessage-deflate
Sec-WebSocket-Key: WvYcBnHBNEpA8+rVeFI+jg==
Connection: keep-alive, Upgrade
Upgrade: websocket

";
            var upgradeResponse =
@"HTTP/1.1 101 Web Socket Protocol Handshake
Connection: Upgrade
Date: Mon, 01 Jan 2018 01:00:00 GMT
Sec-WebSocket-Accept: OYgMbbkBc+aNH7Fl5LPceT3XepE=
Upgrade: websocket

";
            clientTcp.WriteToInput(upgradeRequest);
            tcsUpgradeRequest.Task.Wait(5000);
            connection.serverConnection.client.AsTest().WriteToInput(upgradeResponse);

            var upgradeSession = tcsUpgradeSession.GetResult();
            (upgradeSession.Request as HttpRequest)?.RequestTargetUri.Scheme.Is("ws");

            // WebSocket
            var tcsClientWS = new TaskCompletionSource<IReadOnlyWebSocketMessage>();
            var tcsServerWS = new TaskCompletionSource<IReadOnlyWebSocketMessage>();
            connection.ClientWebSocketMessageSent += result => tcsClientWS.TrySetResult(result);
            connection.ServerWebSocketMessageSent += result => tcsServerWS.TrySetResult(result);

            var clientFrame = new byte[]
            {
                0b10000010, // FIN, Binary
                0b01111110, // noMask, 2byteExPart
                0b00000000, // BigEndian
                0b01111110, // 126bytes
            }
            .Concat(Enumerable.Repeat((byte)'c', 126))
            .ToArray();
            var serverFrame = new byte[]
            {
                0b10000010, // FIN, Binary
                0b01111110, // noMask, 2byteExPart
                0b00000000, // BigEndian
                0b01111110, // 126bytes
            }
            .Concat(Enumerable.Repeat((byte)'s', 126))
            .ToArray();

            clientTcp.WriteToInput(clientFrame);
            connection.serverConnection.client.AsTest().WriteToInput(serverFrame);

            var clientWsMessage = tcsClientWS.GetResult();
            var serverWsMessage = tcsServerWS.GetResult();

            clientWsMessage.Opcode.Is(WebSocketOpcode.Binary);
            clientWsMessage.PayloadData.Is(Enumerable.Repeat((byte)'c', 126).ToArray());

            serverWsMessage.Opcode.Is(WebSocketOpcode.Binary);
            serverWsMessage.PayloadData.Is(Enumerable.Repeat((byte)'s', 126).ToArray());

            var serverTcp = connection.serverConnection.client.AsTest();
            serverTcp.GetStream().AsTest().OutputStream.IsEndWith(clientFrame);
            clientTcp.GetStream().AsTest().OutputStream.IsEndWith(serverFrame);

            exceptions.Count.Is(0);

            connection.Dispose();
        }

        [Fact]
        public void UpstreamProxyConfig()
        {
            var expectedHost = "proxyhost";
            ushort expectedPort = 65000;
            var expectedSecureHost = "secureproxyhost";
            ushort expectedSecurePort = 65443;
            var config = new ProxyConfig
            {
                UpstreamProxyConfig = new UpstreamProxyConfig(expectedHost, expectedPort, expectedSecureHost, expectedSecurePort),
            };

            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp, config)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var request =
@"GET / HTTP/1.1
Host: www.example.com

";
            clientTcp.WriteToInput(request);

            tcsRequest.Task.Wait();
            connection.serverConnection.client.AsTest().Host.Is(expectedHost);
            connection.serverConnection.client.AsTest().Port.Is(expectedPort);

            connection.Dispose();


            // SslStream が生成されない限りは https とみなさないよう変更された
            /*
            var secureClientTcp = new TestTcpClient();
            var secureConnection = new ProxyConnection(secureClientTcp, config)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            secureConnection.StartReceiving();

            var tcsSecureRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            secureConnection.HttpRequestSent += result => tcsSecureRequest.TrySetResult(result);

            var secureRequest =
@"CONNECT www.example.com:443 HTTP/1.1
Host: www.example.com

";
            secureClientTcp.WriteToInput(secureRequest);

            tcsSecureRequest.Task.Wait();
            secureConnection.serverConnection.client.AsTest().Host.Is(expectedSecureHost);
            secureConnection.serverConnection.client.AsTest().Port.Is(expectedSecurePort);

            secureConnection.Dispose();
            */
        }

        [Fact]
        public void CanNotAccessTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new CannotAccessTcpClient()
            };
            connection.StartReceiving();

            var tcsDispose = new TaskCompletionSource<ProxyConnection>();
            connection.Disposing += result => tcsDispose.TrySetResult(result);

            var request =
@"GET / HTTP/1.1
Host: www.example.com

";
            clientTcp.WriteToInput(request);

            tcsDispose.GetResult().Is(connection);

            connection.Dispose();
        }

        [Fact]
        public void CloseByServerTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsDispose = new TaskCompletionSource<ProxyConnection>();
            connection.Disposing += result => tcsDispose.TrySetResult(result);

            var tcsClosedClient = new TaskCompletionSource<bool>();
            connection.clientConnection.Closed += () => tcsClosedClient.TrySetResult(true);
            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var request =
@"GET / HTTP/1.1
Host: www.example.com

";
            clientTcp.WriteToInput(request);

            tcsRequest.Task.Wait();

            var tcsClosedServer = new TaskCompletionSource<bool>();
            connection.serverConnection.Closed += () => tcsClosedServer.TrySetResult(true);

            connection.serverConnection.Dispose();

            tcsClosedServer.GetResult().IsTrue();
            tcsClosedClient.GetResult().IsTrue();
            tcsDispose.GetResult().Is(connection);

            connection.Dispose();
        }

        [Fact]
        public void CloseByClientTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsDispose = new TaskCompletionSource<ProxyConnection>();
            connection.Disposing += result => tcsDispose.TrySetResult(result);

            var tcsClosedClient = new TaskCompletionSource<bool>();
            connection.clientConnection.Closed += () => tcsClosedClient.TrySetResult(true);
            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var request =
@"GET / HTTP/1.1
Host: www.example.com

";
            clientTcp.WriteToInput(request);

            tcsRequest.Task.Wait();

            var tcsClosedServer = new TaskCompletionSource<bool>();
            connection.serverConnection.Closed += () => tcsClosedServer.TrySetResult(true);

            connection.clientConnection.Dispose();

            tcsClosedClient.GetResult().IsTrue();
            tcsClosedServer.GetResult().IsTrue();
            tcsDispose.GetResult().Is(connection);

            connection.Dispose();
        }

        [Fact]
        public void SessionGadgetsTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var tcsSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += result => tcsSession.TrySetResult(result);

            var tcsClose = new TaskCompletionSource<ProxyConnection>();
            connection.Disposing += result => tcsClose.TrySetResult(result);

            clientTcp.WriteFileToInput("TestData/gadgets_Request");

            var requestResult = tcsRequest.GetResult();

            connection.serverConnection.client.AsTest().WriteFileToInput("TestData/gadgets_Response");

            var sessionResult = tcsSession.GetResult();

            var response = sessionResult.Response as HttpResponse;
            response.GetBodyAsString().Is(File.ReadAllText("TestData/gadgets_ResponseBody"));

            //connection.serverConnection.client.AsTest().Close();    // サーバーから閉じる
            clientTcp.Close();  // クライアントから閉じる

            tcsClose.GetResult().Is(connection);

            connection.Dispose();
        }

        [Fact]
        public void Session190Test()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var tcsSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += result => tcsSession.TrySetResult(result);

            var tcsClose = new TaskCompletionSource<ProxyConnection>();
            connection.Disposing += result => tcsClose.TrySetResult(result);

            clientTcp.WriteFileToInput("TestData/190_Request");

            var requestResult = tcsRequest.GetResult();

            connection.serverConnection.client.AsTest().WriteFileToInput("TestData/190_Response");

            var sessionResult = tcsSession.GetResult();

            var response = sessionResult.Response as HttpResponse;

            //connection.serverConnection.client.AsTest().Close();    // サーバーから閉じる
            clientTcp.Close();  // クライアントから閉じる

            tcsClose.GetResult().Is(connection);

            connection.Dispose();
        }

        [Fact]
        public void SessionMainjsTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var tcsSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += result => tcsSession.TrySetResult(result);

            var tcsClose = new TaskCompletionSource<ProxyConnection>();
            connection.Disposing += result => tcsClose.TrySetResult(result);

            clientTcp.WriteFileToInput("TestData/mainjs_Request");

            var requestResult = tcsRequest.GetResult();

            connection.serverConnection.client.AsTest().WriteFileToInput("TestData/mainjs_Response");

            var sessionResult = tcsSession.GetResult();

            var response = sessionResult.Response as HttpResponse;
            response.GetBodyAsString().Is(File.ReadAllText("TestData/mainjs_ResponseBody"));

            //connection.serverConnection.client.AsTest().Close();    // サーバーから閉じる
            clientTcp.Close();  // クライアントから閉じる

            tcsClose.GetResult().Is(connection);

            connection.Dispose();
        }

        [Fact]
        public void ChangeRequestTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var request =
@"GET http://www.example.com/netgame/social/-/gadgets/=/app_id=854854/?hoge=fuga HTTP/1.0
Host: www.example.com

";
            var expectedRequest =
@"GET /netgame/social/-/gadgets/=/app_id=854854/?hoge=fuga HTTP/1.0
Host: www.example.com
Via: 1.0 127.0.0.1

";
            clientTcp.WriteToInput(request);

            var requestResult = tcsRequest.GetResult();

            connection.serverConnection.client.AsTest().GetStream().AsTest().OutputStream.Is(expectedRequest);

            connection.Dispose();
        }

        [Fact]
        public void ContainsColonUriTest()
        {
            //http://b.hatena.ne.jp/entry/image/http://www.kyoji-kuzunoha.com/2012/05/get-domain.html
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var request =
@"GET http://b.hatena.ne.jp/entry/image/http://www.kyoji-kuzunoha.com/2012/05/get-domain.html HTTP/1.1
Host: b.hatena.ne.jp

";
            var expectedRequest =
@"GET /entry/image/http://www.kyoji-kuzunoha.com/2012/05/get-domain.html HTTP/1.1
Host: b.hatena.ne.jp
Via: 1.1 127.0.0.1

";
            clientTcp.WriteToInput(request);

            var requestResult = tcsRequest.GetResult();

            connection.serverConnection.client.AsTest().GetStream().AsTest().OutputStream.Is(expectedRequest);

            connection.Dispose();
        }

        [Fact]
        public void MaxForwards1_OptionsTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var request =
@"OPTIONS * HTTP/1.1
Host: www.example.com
Max-Forwards: 1

";
            var expectedRequest =
@"OPTIONS * HTTP/1.1
Host: www.example.com
Max-Forwards: 0
Via: 1.1 127.0.0.1

";
            clientTcp.WriteToInput(request);

            var requestResult = tcsRequest.GetResult();

            connection.serverConnection.client.AsTest().GetStream().AsTest().OutputStream.Is(expectedRequest);

            connection.Dispose();
        }

        [Fact]
        public void MaxForwards0_OptionsTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsClosed = new TaskCompletionSource<bool>();
            connection.clientConnection.Closed += () => tcsClosed.TrySetResult(true);

            var request =
@"OPTIONS * HTTP/1.1
Host: www.example.com
Max-Forwards: 0

";
            var expectedResponse =
@"HTTP/1.1 200 OK
Date: Mon, 01 Jan 2018 01:00:00 GMT
Allow: 
Content-Length: 0
Connection: close

";
            clientTcp.WriteToInput(request);

            tcsClosed.Task.Wait(5000);

            var actualResponse = connection.clientConnection.client.AsTest().LastOutputString;
            actualResponse.Is(expectedResponse);

            connection.Dispose();
        }

        [Fact]
        public void MaxForwards1_TraceTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var request =
@"TRACE http://www.example.com/ HTTP/1.1
Host: www.example.com
Max-Forwards: 1

";
            var expectedRequest =
@"TRACE / HTTP/1.1
Host: www.example.com
Max-Forwards: 0
Via: 1.1 127.0.0.1

";
            clientTcp.WriteToInput(request);

            var requestResult = tcsRequest.GetResult();

            connection.serverConnection.client.AsTest().GetStream().AsTest().OutputStream.Is(expectedRequest);

            connection.Dispose();
        }

        [Fact]
        public void MaxForwards0_TraceTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsClosed = new TaskCompletionSource<bool>();
            connection.clientConnection.Closed += () => tcsClosed.TrySetResult(true);

            var request =
@"TRACE http://www.example.com/ HTTP/1.1
Host: www.example.com
Max-Forwards: 0

";
            var expectedResponse =
$@"HTTP/1.1 200 OK
Date: Mon, 01 Jan 2018 01:00:00 GMT
Content-Type: message/http
Content-Length: {request.Length}
Connection: close

TRACE http://www.example.com/ HTTP/1.1
Host: www.example.com
Max-Forwards: 0

";
            clientTcp.WriteToInput(request);

            tcsClosed.Task.Wait(5000);

            var actualResponse = connection.clientConnection.client.AsTest().LastOutputString;
            actualResponse.Is(expectedResponse);

            connection.Dispose();
        }

        [Fact]
        public void BadRequestLineTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += result => tcsSession.TrySetResult(result);

            clientTcp.WriteToInput("hogeee\r\n");

            var session = tcsSession.GetResult();
            session.Response.StatusLine.StatusCode.Is(HttpStatusCode.BadRequest);
            (session.Request as HttpRequest).Source.Is("hogeee\r\n");

            connection.Dispose();
        }

        [Fact]
        public void BadRequestHeaderTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += result => tcsSession.TrySetResult(result);

            var request =
@"TRACE http://www.example.com/ HTTP/1.1
hogeeee

";
            clientTcp.WriteToInput(request);

            var session = tcsSession.GetResult();
            session.Response.StatusLine.StatusCode.Is(HttpStatusCode.BadRequest);
            session.Request.ToString().Is(request);

            connection.Dispose();
        }

        [Fact]
        public void BadGatewayTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var tcsSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += result => tcsSession.TrySetResult(result);

            var request =
@"GET http://www.example.com/ HTTP/1.1
Host: www.example.com

";
            clientTcp.WriteToInput(request);

            var actualRequest = tcsRequest.GetResult();

            var response =
$@"hogeeee

";
            connection.serverConnection.client.AsTest().WriteToInput(response);

            var session = tcsSession.GetResult();
            session.Response.StatusLine.StatusCode.Is(HttpStatusCode.BadGateway);

            connection.Dispose();
        }

        [Fact]
        public void DifferentHostErrorTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var tcsClosed = new TaskCompletionSource<bool>();
            connection.clientConnection.Closed += () => tcsClosed.TrySetResult(true);

            var request1 =
@"GET http://www.example.com/ HTTP/1.1
Host: www.example.com

";
            clientTcp.WriteToInput(request1);

            var request1result = tcsRequest.GetResult();

            var request2 =
@"GET http://www2.example.com/ HTTP/1.1
Host: www2.example.com

";
            clientTcp.WriteToInput(request2);

            var isClosed = tcsClosed.GetResult();
            isClosed.IsTrue();

            connection.Dispose();
        }

        [Fact]
        public void InformationResponseTest()
        {
            var clientTcp = new TestTcpClient();
            var connection = new ProxyConnection(clientTcp)
            {
                CreateTcpClientForServer = (host, port) => new TestTcpClient(host, port)
            };
            connection.StartReceiving();

            var tcsRequestHeaders = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestHeadersSent += result => tcsRequestHeaders.TrySetResult(result);

            var tcsRequest = new TaskCompletionSource<IReadOnlyHttpRequest>();
            connection.HttpRequestSent += result => tcsRequest.TrySetResult(result);

            var responseSentCount = 0;
            var tcsSession = new TaskCompletionSource<IReadOnlySession>();
            connection.HttpResponseSent += result =>
            {
                responseSentCount++;
                tcsSession.TrySetResult(result);
            };

            var tcsClose = new TaskCompletionSource<ProxyConnection>();
            connection.Disposing += result => tcsClose.TrySetResult(result);


            var requestHeaders =
@"POST http://203.104.209.71/kcsapi/api_get_member/sortie_conditions HTTP/1.1
Host: 203.104.209.71
Content-Length: 62
Content-Type: application/x-www-form-urlencoded
Expect: 100-continue

";
            clientTcp.WriteToInput(requestHeaders);

            var requestHeadersResult = tcsRequestHeaders.GetResult();

            var response1 =
@"HTTP/1.1 100 Continue
Date: Mon, 01 Jan 2018 01:00:00 GMT

";

            const string requestBody = @"api_token=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx&api_verno=1";
            clientTcp.WriteToInput(requestBody);

            var requestResult = tcsRequest.GetResult();

            connection.serverConnection.client.AsTest().WriteToInput(response1);

            Thread.Sleep(100);

            var response2 =
@"HTTP/1.1 200 OK
Date: Mon, 01 Jan 2018 01:00:00 GMT

";
            connection.serverConnection.client.AsTest().WriteToInput(response2);

            var sessionResult = tcsSession.GetResult();

            sessionResult.Request.ToString().Is(requestHeaders + requestBody);

            var response = sessionResult.Response as HttpResponse;
            response.StatusLine.StatusCode.Is(HttpStatusCode.OK);
            responseSentCount.Is(1);

            connection.serverConnection.client.AsTest().Close();    // サーバーから閉じる
            //clientTcp.Close();  // クライアントから閉じる

            tcsClose.GetResult().Is(connection);

            connection.Dispose();
        }
    }
}
