using Nekoxy2.Spi;
using Nekoxy2.Spi.Entities.Http;
using Nekoxy2.Titanium.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.Exceptions;
using Titanium.Web.Proxy.Models;

namespace Nekoxy2.Titanium
{
    public sealed class TitaniumEngine : IWebSocketProxyEngine
    {
        private ProxyServer proxy = new ProxyServer();

        public TitaniumEngine()
        {
            // TODO 各種設定 interface
            proxy.UpStreamHttpProxy = new ExternalProxy { HostName = "localhost", Port = 8888 };
            proxy.UpStreamHttpsProxy = new ExternalProxy { HostName = "localhost", Port = 8888 };

            proxy.BeforeRequest += this.Proxy_BeforeRequest;
            proxy.BeforeResponse += this.Proxy_BeforeResponse;
            proxy.AfterResponse += this.Proxy_AfterResponse;
            proxy.ExceptionFunc = e
                => this.FatalException?.Invoke(this, ExceptionEventArgs.Create(e));
        }

        private async Task Proxy_BeforeRequest(object sender, global::Titanium.Web.Proxy.EventArguments.SessionEventArgs e)
        {
            var args = HttpRequestEventArgs.Create(await HttpRequest.CreateAsync(e));
            this.HttpRequestReceived?.Invoke(sender, args);
            e.SetValues(args.Request);
        }

        private async Task Proxy_BeforeResponse(object sender, global::Titanium.Web.Proxy.EventArguments.SessionEventArgs e)
        {
            var args = SessionEventArgs.Create(await Session.CreateAsync(e));
            this.HttpResponseReceived?.Invoke(sender, args);
            e.SetValues(args.Session.Response);
        }

        private async Task Proxy_AfterResponse(object sender, global::Titanium.Web.Proxy.EventArguments.SessionEventArgs e)
        {
            var args = ReadOnlySessionEventArgs.Create(await Session.CreateAsync(e));
            this.HttpResponseSent?.Invoke(sender, args);
        }

        public void Start()
        {
            var endpoint = new ExplicitProxyEndPoint(IPAddress.Loopback, 8080);
            proxy.AddEndPoint(endpoint);

            proxy.Start();
        }

        public void Stop()
        {
            foreach (var endpoint in proxy.ProxyEndPoints.ToArray())
            {
                proxy.RemoveEndPoint(endpoint);
            }
            proxy.Stop();
        }

        public event EventHandler<IReadOnlyHttpRequestEventArgs> HttpRequestSent;
        public event EventHandler<IHttpRequestEventArgs> HttpRequestReceived;
        public event EventHandler<ISessionEventArgs> HttpResponseReceived;
        public event EventHandler<IReadOnlySessionEventArgs> HttpResponseSent;

        public event EventHandler<IWebSocketMessageEventArgs> ClientWebSocketMessageReceived;
        public event EventHandler<IReadOnlyWebSocketMessageEventArgs> ClientWebSocketMessageSent;
        public event EventHandler<IWebSocketMessageEventArgs> ServerWebSocketMessageReceived;
        public event EventHandler<IReadOnlyWebSocketMessageEventArgs> ServerWebSocketMessageSent;

        public event EventHandler<IExceptionEventArgs> FatalException;
    }

    static partial class Extensions
    {
        public static void SetValues(this global::Titanium.Web.Proxy.EventArguments.SessionEventArgs e, IHttpRequest request)
        {
            e.HttpClient.Request.Method = request.RequestLine.Method.Method;
            e.HttpClient.Request.OriginalUrl = request.RequestLine.RequestTarget;   //TODO 要検討
            e.HttpClient.Request.HttpVersion = request.RequestLine.HttpVersion;

            e.HttpClient.Request.Headers.Clear();
            e.HttpClient.Request.Headers.AddHeaders(request.Headers.Select(x => x.ToHeader()));

            e.SetRequestBody(request.Body);
        }

        public static void SetValues(this global::Titanium.Web.Proxy.EventArguments.SessionEventArgs e, IHttpResponse response)
        {
            e.HttpClient.Response.HttpVersion = response.StatusLine.HttpVersion;
            e.HttpClient.Response.StatusCode = (int)response.StatusLine.StatusCode;
            e.HttpClient.Response.StatusDescription = response.StatusLine.ReasonPhrase;

            e.HttpClient.Response.Headers.Clear();
            e.HttpClient.Response.Headers.AddHeaders(response.Headers.Select(x => x.ToHeader()));

            e.SetResponseBody(response.Body);
        }
    }
}
