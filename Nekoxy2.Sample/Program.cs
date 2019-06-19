using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nekoxy2;
using Nekoxy2.Default;
using Nekoxy2.Default.Certificate;
using Nekoxy2.Entities.Http;
using Nekoxy2.Entities.Http.Extensions;
using Nekoxy2.Entities.WebSocket;
using Nekoxy2.Titanium;

namespace Nekoxy2.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("### Application Started. ###");
            StartDefault();
            //StartTitanium();
        }

        private static void StartDefault()
        {
            if (CertificateUtil.FindRootCertificate() == default)
            {
                CertificateUtil.InstallNewRootCertificate();
            }
            //CertificateUtil.UninstallRootCertificates();
            //CertificateUtil.UninstallAllServerCertificatesByIssuer();
            //var ca = CertificateUtil.FindRootCertificate();
            //CertificateUtil.UninstallFromRootStore(ca);

            var engine = DefaultEngine.Create(new ListeningConfig(8080));

            engine.DecryptConfig.IsDecrypt = true;
            //engine.DecryptConfig.HostFilterHandler = host
            //    => host.EndsWith("cat-ears.net");
            // engine.DecryptConfig.RootCertificateResolver = store => store.FindRootCertificate("Titanium Root Certificate Authority");
            engine.DecryptConfig.CacheLocations
                = new[] {   // 定義順が読み取り優先順
                    CertificateCacheLocation.Memory,
                    CertificateCacheLocation.Custom,
                    CertificateCacheLocation.Store,
                };
            engine.DecryptConfig.ServerCertificateCacheResolver = host =>
            {
                Console.WriteLine($"### ServerCertificateCacheResolver: {host}");
                return null;
                //var path = host.Replace("*", "$x$") + ".pfx";
                //if (!File.Exists(path)) return null;
                //var bytes = File.ReadAllBytes(path);
                //return new X509Certificate2(bytes);
            };
            engine.ServerCertificateCreated += (_, c) =>
            {
                Console.WriteLine($"### ServerCertificateCreated: {c.Certificate.Subject}");
                //var path = c.Certificate.Subject.Replace("CN=", "").Replace("*", "$x$") + ".pfx";
                //if (!File.Exists(path))
                //    File.WriteAllBytes(path, c.Certificate.RawData);
            };

            engine.UpstreamProxyConfig = null;
            engine.ConnectionAdded += (_, c) => Console.WriteLine($"### Connection Added. Connections Count: {c.Count}");
            engine.ConnectionRemoved += (_, c) => Console.WriteLine($"### Connection Removed. Connections Count: {c.Count}");


            var proxy = HttpProxyFactory.Create(engine);

            proxy.FatalException += (_, e) => Console.WriteLine(e.Exception);
            proxy.HttpResponseSent += (_, s) =>
            {
                if (IsOutputHeaders(s.Session))
                {
                    Console.WriteLine($"{s.Session.GetHost()}: {s.Session.Request}\r\n" +
                        $"{s.Session.Response.StatusLine }{s.Session.Response.Headers}");
                }
                else if (s.Session.Request.RequestLine.Method.Method != "CONNECT")
                {
                    Console.WriteLine($"{s.Session.GetHost()}: {s.Session.Request.RequestLine}" +
                        $"{s.Session.Response.StatusLine}");
                }
            };
            proxy.ClientWebSocketMessageSent += (_, m) =>
            {
                var host = m.Message.HandshakeSession.GetHost();
                if (m.Message.Opcode == WebSocketOpcode.Text)
                {
                    Console.WriteLine(
$@"ClientWebSocketMessage: {host}
Text: {Encoding.UTF8.GetString(m.Message.PayloadData.ToArray())}");
                }
                else
                {
                    //                    Console.WriteLine(
                    //$@"ClientWebSocketMessage: {host}
                    //{m.Message.Opcode}: {m.Message.PayloadData.Count} bytes.");
                }
            };
            proxy.ServerWebSocketMessageSent += (_, m) =>
            {
                var host = m.Message.HandshakeSession.GetHost();
                if (m.Message.Opcode == WebSocketOpcode.Text)
                {
                    Console.WriteLine(
$@"ServerWebSocketMessage: {host}
Text: {Encoding.UTF8.GetString(m.Message.PayloadData.ToArray())}");
                }
                else
                {
                    //                    Console.WriteLine(
                    //$@"ServerWebSocketMessage: {host}
                    //{m.Message.Opcode}: {m.Message.PayloadData.Count} bytes.");
                }
            };

            try
            {
                proxy.Start();
                while (true) Task.Delay(1000).Wait();
            }
            finally
            {
                proxy.Stop();
            }
        }

        private static bool IsOutputHeaders(IReadOnlySession s)
        {
            var statusCode = (int)s.Response.StatusLine.StatusCode;
            return 400 <= statusCode
                && statusCode != 404;
        }


        private static void StartTitanium()
        {
            var engine = new TitaniumEngine();
            var proxy = HttpProxyFactory.Create(engine);

            proxy.HttpRequestReceived += (_, args) =>
            {
                Console.WriteLine($"HttpRequestReceived: {args.Request.GetHost()} {args.Request.RequestLine.Method} {args.Request.RequestLine.RequestTarget}");
                args.Request.Headers.SetValue("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:63.0) Gecko/20100101 Firefox/63.0");
            };

            proxy.HttpResponseReceived += (_, args) =>
            {
                Console.WriteLine($"HttpResponseReceived: {args.Session.Response.StatusLine.StatusCode}");
                args.Session.Response.Headers.SetValue("X-Hoge", "hogeee");
            };

            proxy.HttpResponseSent += (_, args)
                => Console.WriteLine($"HttpResponseSent: {args.Session.Response.StatusLine.StatusCode}, Body: {args.Session.Response.Body?.Count.ToString() ?? "?"} bytes");

            //proxy.FatalException += (_, args)
            //    => Console.WriteLine(args.Exception);

            try
            {
                proxy.Start();
                while (true) Task.Delay(1000).Wait();
            }
            finally
            {
                proxy.Stop();
            }
        }
    }
}
