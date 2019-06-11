using Nekoxy2.ApplicationLayer.Entities.Http;
using Nekoxy2.ApplicationLayer.MessageBodyParsers;
using Nekoxy2.ApplicationLayer.ProtocolReaders.Http;
using Nekoxy2.ApplicationLayer.ProtocolReaders.Http2;
using Nekoxy2.Default.Certificate;
using Nekoxy2.Default.Proxy.Tcp;
using Nekoxy2.Default.Proxy.Tls;
using Nekoxy2.Spi.Entities.Http;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace Nekoxy2.Default.Proxy
{
    /// <summary>
    /// クライアント側コネクション。
    /// 通信を読み取り、HTTP として解釈。
    /// </summary>
    internal sealed class ClientConnection : HttpConnection
    {
        /// <summary>
        /// HTTP リクエストリーダー
        /// </summary>
        private HttpRequestReader httpRequestReader;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="client">TCP 接続クライアント</param>
        /// <param name="config">プロキシ設定</param>
        /// <param name="receiveSharedLock">待ち受け後処理ロック</param>
        public ClientConnection(ITcpClient client, ProxyConfig config = null, SemaphoreQueue receiveSharedLock = null)
            : base(client, config, receiveSharedLock)
            => this.IsPauseBeforeReceive = false;

        /// <summary>
        /// HTTP リーダーを作成
        /// </summary>
        /// <returns></returns>
        protected override AbstractHttpReader CreateHttpReader()
        {
            this.httpRequestReader = new HttpRequestReader(this.Config.IsCaptureBody, this.Config.MaxCaptureSize);
            this.httpRequestReader.ReceivedRequestHeaders += this.OnReceivedRequestHeaders;
            this.httpRequestReader.ReceivedRequestBody += request =>
            {
                this.ResolveScheme(ref request);
                this.ReceivedRequestBody?.Invoke(request);
            };
            return this.httpRequestReader;
        }

        /// <summary>
        /// 必要に応じて SSL/TLS 接続を確立
        /// </summary>
        protected override void EnsureSsl()
        {
            var isTls = this.ReceivedStream.IsTls();
            if (isTls)
            {
                if (this.Config.DecryptConfig.IsDecrypt && this.IsDecryptTarget && this.SslStream == null)
                {
                    var sourceStream = this.ReceivedStream;
                    try
                    {
                        // Chrome は PSL (.co.jp 等) を検査しているが、最新 PSL に追従するのは現実的ではないため、ワイルドカードドメインを用いるのはやめた
                        var cert = CertificateStoreFacade.GetServerCertificate(this.TunneledHost, this.Config.DecryptConfig);

                        this.SslStream = new SslStream(this.ReceivedStream);
                        this.AddDisposableItem(this.SslStream);

                        this.ReceivedStream = new ReadBufferedNetworkStream(this.SslStream);
                        this.AddDisposableItem(this.ReceivedStream);

                        this.AuthenticateAsServer(sourceStream, cert);
                    }
                    catch (Exception e)
                    {
                        this.SslStream = null;
                        this.ReceivedStream = sourceStream;
                        if (!(e.Has<IOException>())
                        && !(e.Has<ObjectDisposedException>()))
                        {
                            // IOException はリロード時等で発生する
                            this.InvokeFatalException(this, e);
                        }
                    }
                }
                else
                {
                    // TLS & Not Decrypt
                    this.ChangeToUnknownProtocol();
                }
            }
            // 復号化対象外や失敗時等の通知
            this.InvokeChallengeToSsl(isTls, false, null);
        }

        private void AuthenticateAsServer(ReadBufferedNetworkStream sourceStream, X509Certificate2 serverCertificate)
        {
#if NETSTANDARD2_0
            this.SslStream.AuthenticateAsServer(serverCertificate, false, this.Config.DecryptConfig.EnabledSslProtocols, false);
#else
            var options = new SslServerAuthenticationOptions
            {
                ServerCertificate = serverCertificate,
                EnabledSslProtocols = this.Config.DecryptConfig.EnabledSslProtocols,
                CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
            };
            if (sourceStream.TryGetClientAlpn(out var protocolNames))
            {
                var alpn = new Alpn(protocolNames);
                this.InvokeChallengeToSsl(true, true, alpn);
                if (alpn.SelectedProtocol != default)
                    options.ApplicationProtocols = new[] { alpn.SelectedProtocol }.ToList();
            }
            this.SslStream.AuthenticateAsServerAsync(options, CancellationToken.None).Wait();
            if (this.SslStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http2)
            {
                this.ChangeToHttp2(EndPointType.Client);
            }
#endif
            this.InvokeChallengeToSsl(true, true, null);
        }

        /// <summary>
        /// 例外発生時
        /// </summary>
        protected override void OnException()
            => this.SendResponse(this.httpRequestReader.GetRequest(), HttpStatusCode.BadRequest, "Bad Request");

        /// <summary>
        /// TCP 切断時
        /// </summary>
        protected override void OnTcpClose()
        {
            try
            {
                this.httpRequestReader?.CloseTcp();
            }
            catch (IncompleteBodyException)
            {
                // RFC7230 3.4
                // Content-Length、Chunked に満たない場合は不完全なメッセージとして TCP Close する(その後 Resume されたりする)
                this.Dispose();
            }
        }

        /// <summary>
        /// TCP 通信データ読み取り時
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="readSize"></param>
        protected override void OnRead(byte[] buffer, int readSize)
        {
            try
            {
                this.httpRequestReader?.HandleReceive(buffer, readSize);
            }
            catch (BadRequestException e)
            {
                this.SendResponse(e.Request, HttpStatusCode.BadRequest, e.Message);
                this.Dispose();
            }
            catch (IncompleteBodyException)
            {
                // RFC7230 3.4
                // Content-Length、Chunked に満たない場合は不完全なメッセージとして TCP Close する(その後 Resume されたりする)
                this.Dispose();
            }
            catch (InvalidChunkException)
            {
                // RFC7230 3.4
                // チャンク復号に失敗した場合、不完全なメッセージとして TCP Close することにする。
                this.Dispose();
            }
        }

        /// <summary>
        /// プロトコルスキーム判定
        /// </summary>
        /// <param name="request">プロトコルスキーム判定を反映する Request</param>
        private void ResolveScheme(ref HttpRequest request)
        {
            if (request.Headers.HasHeader("Sec-WebSocket-Version")
            && request.Headers.HasHeader("Sec-WebSocket-Key"))
            {
                request.ChangeScheme(this.SslStream == null ? "ws" : "wss");
            }
            else
            {
                request.ChangeScheme(this.SslStream == null ? "http" : "https");
            }
        }

        #region Parse and Valiation

        /// <summary>
        /// リクエストヘッダー受信時
        /// </summary>
        /// <param name="request">受信したヘッダーを含むリクエスト</param>
        private void OnReceivedRequestHeaders(HttpRequest request)
        {
            this.ResolveScheme(ref request);

            if (!ValidateHeaders(request, out var reasonPhrase))
            { 
                this.SendResponse(request, HttpStatusCode.BadRequest, reasonPhrase);
                this.Dispose();
                return;
            }

            if (IsMaxForwards(request))
            {
                if (request.RequestLine.Method == System.Net.Http.HttpMethod.Options)
                    this.SendResponseForOptions(request);
                else if (request.RequestLine.Method == System.Net.Http.HttpMethod.Trace)
                    this.SendResponseForTrace(request);
                return;   // Invalid なわけではないんだが……
            }

            request.Headers.RemoveConnectionHeaders();

            // RFC7230 5.7.1
            // request には via ヘッダを付加しなければならない
            request.Headers.AddVia("HTTP", request.RequestLine.HttpVersion, this.Config.ListeningConfig.LocalAddress.ToString());

            this.ReceivedRequestHeaders?.Invoke(request);
        }

        /// <summary>
        /// ヘッダーを検証
        /// </summary>
        /// <param name="request">検証対象</param>
        /// <param name="reasonPhrase">返り値が false である場合その理由</param>
        /// <returns>可否</returns>
        private bool ValidateHeaders(HttpRequest request, out string reasonPhrase)
        {
            // RFC7230 3.3.3
            // Transfer-Encoding があって chunked ではないリクエストは 400 エラーを返す
            if (request.Headers.TransferEncoding.Exists
            && !request.Headers.IsChunked)
            {
                reasonPhrase = "Not Chunked Request";
                return false;
            }

            // RFC7230 5.4
            // 以下はサーバーにおいて 400 BadRequest (Proxy上でも宛先が特定できないなら400でよさそう)
            // ・1.1メッセージだがHostがない
            // ・複数のHostがある → 重複ヘッダマージ処理でエラーとしている
            if (request.RequestLine.HttpVersion == HttpVersion.Version11
            && !request.Headers.Host.Exists)
            {
                reasonPhrase = "Host Header Not Exists";
                return false;
            }

            // RFC7230 5.4
            // Hostが不正な値
            var host = request.Headers.Host.ParseAuthority().Host;
            if (Uri.CheckHostName(host) == UriHostNameType.Unknown)
            {
                reasonPhrase = "Invalid Host Header";
                return false;
            }

            // RFC7230 5.7
            // 自分宛ては処理しない
            if (request.RequestLine.RequestTarget != "*"
            // UNDONE: 本当はサーバー名や割り当てられたIPアドレス等も見なければならない
            && request.RequestTargetUri.IsLoopback
            && request.RequestTargetUri.Port == this.Config.ListeningConfig.ListeningPort)
            {
                reasonPhrase = "Loopback Request";
                return false;
            }

            reasonPhrase = null;
            return true;
        }

        /// <summary>
        /// Max-Forwards が 0 であるかどうか。
        /// 0 でない場合減算。
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <remarks>RFC7231 5.1.2</remarks>
        private static bool IsMaxForwards(HttpRequest request)
        {
            if (request.RequestLine.Method != System.Net.Http.HttpMethod.Trace
            && request.RequestLine.Method != System.Net.Http.HttpMethod.Options)
                return false;
            if (!request.Headers.MaxForwards.Exists)
                return false;
            if (request.Headers.MaxForwards.Value == 0)
                return true;

            request.Headers.MaxForwards.Value--;
            return false;
        }

        #endregion

        #region SendResponse

        /// <summary>
        /// レスポンスを送信
        /// </summary>
        /// <param name="request">レスポンス</param>
        /// <param name="statusCode">ステータスコード</param>
        /// <param name="reasonPhrase">ステータスの説明</param>
        public void SendResponse(HttpRequest request, HttpStatusCode statusCode, string reasonPhrase)
        {
            var statusLine = new HttpStatusLine(new Version(1, 1), statusCode, reasonPhrase);
            var headers = HttpHeaders.Empty;
            headers.AddDate();
            var response = new HttpResponse(statusLine, headers, Array.Empty<byte>(), HttpHeaders.Empty);
            this.SendResponseHeaders(response);
            this.ManualResponseSent?.Invoke(new Session(request.GetOrigin(), response));
        }

        /// <summary>
        /// レスポンスヘッダーを送信
        /// </summary>
        /// <param name="response">送信するヘッダーを含むレスポンス</param>
        public void SendResponseHeaders(HttpResponse response)
        {
            var headers = response.HeadersToBytes();
            this.Write(headers, headers.Length);
        }

        /// <summary>
        /// OPTIONS リクエストに対するレスポンスを送信
        /// </summary>
        /// <param name="request">OPTIONS リクエスト</param>
        private void SendResponseForOptions(HttpRequest request)
        {
            // RFC7231 4.3.7
            // OPTIONS リクエストには Body が存在し得るが、このプロキシでは処理しないので Close して受信しないようにする
            var statusLine = new HttpStatusLine(new Version(1, 1), HttpStatusCode.OK, "OK");
            var headers = HttpHeaders.Empty;
            headers.AddDate();
            headers.Allow.Value = "";
            headers.ContentLength.Value = 0;
            headers.Connection.Value = "close";
            var response = new HttpResponse(statusLine, headers, Array.Empty<byte>(), HttpHeaders.Empty);
            var responseBytes = response.ToBytes();
            this.Write(responseBytes, responseBytes.Length);
            this.ManualResponseSent?.Invoke(new Session(request.GetOrigin(), response));
            this.Dispose();
        }

        /// <summary>
        /// TRACE リクエストに対するレスポンスを送信
        /// </summary>
        /// <param name="request">TRACE リクエスト</param>
        private void SendResponseForTrace(HttpRequest request)
        {
            // RFC7231 4.3.8
            // TRACE リクエストには Body が存在しない
            var body = Encoding.ASCII.GetBytes(request.Source);

            var statusLine = new HttpStatusLine(new Version(1, 1), HttpStatusCode.OK, "OK");
            var headers = HttpHeaders.Empty;
            headers.AddDate();
            headers.ContentType.Value = "message/http";
            headers.ContentLength.Value = body.Length;
            headers.Connection.Value = "close";
            var response = new HttpResponse(statusLine, headers, body, HttpHeaders.Empty);
            var responseBytes = response.ToBytes();
            this.Write(responseBytes, responseBytes.Length);
            this.ManualResponseSent?.Invoke(new Session(request.GetOrigin(), response));
            this.Dispose();
        }

        #endregion

        private bool isCalledChallengeToSsl = false;

        private void InvokeChallengeToSsl(bool isTls, bool isDecrypt, Alpn alpn)
        {
            if (this.isCalledChallengeToSsl) return;
            this.ChallengeToSsl?.Invoke((isTls, isDecrypt, alpn));
            this.isCalledChallengeToSsl = true;
        }

        /// <summary>
        /// リクエストヘッダー受信完了時に発生
        /// </summary>
        public event Action<HttpRequest> ReceivedRequestHeaders;

        /// <summary>
        /// リクエストボディー受信完了時に発生
        /// </summary>
        public event Action<HttpRequest> ReceivedRequestBody;

        /// <summary>
        /// 手動でレスポンスを送信した際に発生
        /// </summary>
        public event Action<IReadOnlySession> ManualResponseSent;

        /// <summary>
        /// SSL/TLS 接続を試みた際に発生
        /// </summary>
        public event Action<(bool IsTls, bool IsDecrypt, Alpn Alpn)> ChallengeToSsl;
    }
}
