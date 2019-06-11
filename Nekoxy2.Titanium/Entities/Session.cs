using Nekoxy2.Spi.Entities.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Http;

namespace Nekoxy2.Titanium.Entities
{
    internal sealed class Session : ISession
    {
        public IReadOnlyHttpRequest Request { get; }
        public IHttpResponse Response { get; set; }
        
        IReadOnlyHttpResponse IReadOnlySession.Response => this.Response;

        public Session(SessionEventArgs source)
        {
            this.Request = HttpRequest.CreateAsync(source).Result;
            this.Response = new HttpResponse(source.HttpClient.Response);
        }

        public static async Task<Session> CreateAsync(SessionEventArgs args)
        {
            var session = new Session(args);
            if (args.HttpClient.Response.HasBody && !args.HttpClient.Response.IsBodyRead)
                session.Response.Body = await args.GetResponseBody();
            return session;
        }
    }
}
