using Nekoxy2.SazLoader;
using Nekoxy2.SazLoader.Entities.Http;
using Nekoxy2.Spi.Entities.Http;
using Nekoxy2.Spi.Entities.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Nekoxy2.ApplicationLayer;
using Nekoxy2.ApplicationLayer.Entities.Http;
using System.Net;

namespace Nekoxy2.Test.SazLoader
{
    public class ProxyEngineTest
    {
        [Fact(Timeout = 10000)]
        public void SazLoaderHTTPTest()
        {
            var saz = "TestData/abehiroshi.saz";
            var engine = SazLoaderEngine.Create(saz);
            var tcs = new TaskCompletionSource<bool>();
            engine.AllSessionsComlete += () => tcs.TrySetResult(true);

            var sessions = new List<(IReadOnlySession Session, DateTimeOffset Time)>();
            engine.HttpResponseSent += (_, s) => sessions.Add((s.Session, DateTimeOffset.Now));
            engine.Start();

            tcs.Task.Wait();

            foreach (var item in sessions)
            {
                Debug.WriteLine($"{item.Time.ToString("HH:mm:ss.fffffff")}: {item.Session.Request.RequestLine}");
            }

            var result = sessions.Select(x => x.Session).ToArray();
            result.Is(saz);
        }

        [Fact(Timeout = 10000)]
        public void SazLoaderWSTest()
        {
            var saz = "TestData/websocket.saz";
            var engine = SazLoaderEngine.Create(saz);
            var tcs = new TaskCompletionSource<bool>();
            engine.AllSessionsComlete += () => tcs.TrySetResult(true);

            var clientWebSockets = new List<IReadOnlyWebSocketMessage>();
            var serverWebSockets = new List<IReadOnlyWebSocketMessage>();
            engine.ClientWebSocketMessageSent += (_, x) => clientWebSockets.Add(x.Message);
            engine.ServerWebSocketMessageSent += (_, x) => serverWebSockets.Add(x.Message);
            engine.Start();

            tcs.Task.Wait();

            clientWebSockets.Count.Is(3);
            clientWebSockets[0].HandshakeSession.Request.Headers.GetFirstValue("Upgrade").Is("websocket");
            clientWebSockets[0].HandshakeSession.Response.StatusLine.StatusCode.Is(HttpStatusCode.SwitchingProtocols);
            clientWebSockets[0].HandshakeSession.Response.Headers.GetFirstValue("Upgrade").Is("websocket");
            clientWebSockets[0].Opcode.Is(WebSocketOpcode.Text);
            clientWebSockets[0].PayloadData.ToASCII().Is("Rock it with HTML5 WebSocket");
            clientWebSockets[1].PayloadData.ToASCII().Is("hogee");
            clientWebSockets[2].Opcode.Is(WebSocketOpcode.Close);

            serverWebSockets.Count.Is(3);
            serverWebSockets[0].HandshakeSession.Request.Headers.GetFirstValue("Upgrade").Is("websocket");
            serverWebSockets[0].HandshakeSession.Response.StatusLine.StatusCode.Is(HttpStatusCode.SwitchingProtocols);
            serverWebSockets[0].HandshakeSession.Response.Headers.GetFirstValue("Upgrade").Is("websocket");
            serverWebSockets[0].Opcode.Is(WebSocketOpcode.Text);
            serverWebSockets[0].PayloadData.ToASCII().Is("Rock it with HTML5 WebSocket");
            serverWebSockets[1].PayloadData.ToASCII().Is("hogee");
            serverWebSockets[2].Opcode.Is(WebSocketOpcode.Close);
        }
    }

    static partial class AssertExtensions
    {
        private static Regex pattern = new Regex(@"ClientDoneResponse=""(.+)""", RegexOptions.Compiled | RegexOptions.Multiline);

        public static void Is(this IReadOnlySession[] actualSessions, string path)
        {
            using(var zip = ZipFile.OpenRead(path))
            {
                // AfterSessionComplete は ClientDoneResponse 順に発生する
                // SAZ の Number は Request 発生順
                var expectSessions = zip.Entries
                    .Where(x => x.FullName.StartsWith("raw/"))
                    .Where(x => !string.IsNullOrEmpty(x.Name))
                    .GroupBy(x => x.Name.Split(new[] { '_' }).First(),
                    (key, elements) => new
                    {
                        Number = int.Parse(key),
                        ClientDoneResponse = DateTimeOffset.Parse(pattern.Match(elements.First(x => x.Name.EndsWith("_m.xml")).ReadAllString()).Groups[1].Value),
                        Request = elements.First(x => x.Name.EndsWith("_c.txt")).ReadAllBytes(),
                        Response = elements.First(x => x.Name.EndsWith("_s.txt")).ReadAllBytes(),
                    })
                    .OrderBy(x => x.ClientDoneResponse.Ticks)
                    .ToArray();
                for (int i = 0; i < expectSessions.Length; i++)
                {
                    var actual = actualSessions[i];
                    var expect = expectSessions[i];

                    Assert.True((actual.Request as SazHttpRequest).ToBytes().SequenceEqual(expect.Request),
$@"Assert failure at {expect.Number}.Request
Request: {actual.Request.RequestLine.ToString()}
ActualLength: {actual.Request.ToString().Length}
ExpectedLength: {expect.Request.Length}");

                    Assert.True((actual.Response as SazHttpResponse).ToBytes().SequenceEqual(expect.Response),
$@"Assert failure at {expect.Number}.Response
Request: {actual.Request.RequestLine.ToString()}
ActualLength: {actual.Response.ToString().Length}
ExpectedLength: {expect.Response.Length}");
                }
            }
        }

        public static byte[] ReadAllBytes(this ZipArchiveEntry entry)
        {
            using (var source = entry.Open())
            using (var dest = new MemoryStream())
            {
                source.CopyTo(dest);
                return dest.ToArray();
            }
        }

        public static string ReadAllString(this ZipArchiveEntry entry)
        {
            using (var source = entry.Open())
            using (var reader = new StreamReader(source))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
