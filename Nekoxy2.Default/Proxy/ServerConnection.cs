using Nekoxy2.ApplicationLayer.Entities.Http;
using Nekoxy2.ApplicationLayer.MessageBodyParsers;
using Nekoxy2.ApplicationLayer.ProtocolReaders.Http;
using Nekoxy2.ApplicationLayer.ProtocolReaders.Http2;
using Nekoxy2.Default.Proxy.Tcp;
using Nekoxy2.Default.Proxy.Tls;
using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Nekoxy2.Default.Proxy
{
    /// <summary>
    /// サーバー側コネクション。
    /// 通信を読み取り、HTTP として解釈。
    /// </summary>
    internal sealed class ServerConnection : HttpConnection
    {
        /// <summary>
        /// HTTP レスポンスリーダー
        /// </summary>
        private HttpResponseReader httpResponseReader;

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="client">TCP 接続クライアント</param>
        /// <param name="config">プロキシ設定</param>
        /// <param name="receiveSharedLock">待ち受け後処理ロック</param>
        public ServerConnection(ITcpClient client, ProxyConfig config = null, SemaphoreQueue receiveSharedLock = null)
            : base(client, config, receiveSharedLock)
            => this.IsPauseBeforeReceive = true;

        /// <summary>
        /// HTTP リーダーを作成
        /// </summary>
        /// <returns></returns>
        protected override AbstractHttpReader CreateHttpReader()
        {
            this.httpResponseReader = new HttpResponseReader(this.Config.IsCaptureBody, this.Config.MaxCaptureSize);
            this.httpResponseReader.ReceivedResponseHeaders += this.OnReceivedResponseHeaders;
            this.httpResponseReader.ReceivedResponseBody += request => this.ReceivedResponseBody?.Invoke(request);
            return this.httpResponseReader;
        }

        /// <summary>
        /// 必要に応じて SSL/TLS 接続を確立
        /// </summary>
        protected override void EnsureSsl() { }

        /// <summary>
        /// 必要に応じてサーバー側 SSL/TLS 接続を確立
        /// </summary>
        public void EnsureServerSsl(bool isTls, bool canDecrypt, Alpn alpn)
        {
            if (isTls)
            {
                if (this.Config.DecryptConfig.IsDecrypt && canDecrypt && this.SslStream == null && this.IsDecryptTarget)
                {
                    if (this.ReceivedStream == null)
                        this.ReceivedStream = new ReadBufferedNetworkStream(this.client.GetStream());
                    var sourceStream = this.ReceivedStream;
                    try
                    {
                        this.SslStream = new SslStream(this.ReceivedStream);
                        this.AddDisposableItem(this.SslStream);

                        this.ReceivedStream = new ReadBufferedNetworkStream(this.SslStream);
                        this.AddDisposableItem(this.ReceivedStream);

                        this.AuthenticateAsClient(alpn);
                    }
                    catch (Exception e)
                    {
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
            this.IsPauseBeforeReceive = false;
        }

        private void AuthenticateAsClient(Alpn alpn)
        {
            if (alpn == null)
            {
                // Receive 中は失敗する
                this.SslStream.AuthenticateAsClient(this.TunneledHost, null, this.Config.DecryptConfig.EnabledSslProtocols, false);
            }
#if !NETSTANDARD2_0
            else
            {
                var options = new SslClientAuthenticationOptions
                {
                    ApplicationProtocols = alpn.ListOfProtocols,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    EnabledSslProtocols = this.Config.DecryptConfig.EnabledSslProtocols,
                    TargetHost = this.TunneledHost,
                };
                this.SslStream.AuthenticateAsClientAsync(options, CancellationToken.None).Wait();
                alpn.SelectedProtocol = this.SslStream.NegotiatedApplicationProtocol;
                if (this.SslStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http2)
                {
                    this.ChangeToHttp2(EndPointType.Server);
                }
            }
#endif
        }

        /// <summary>
        /// 例外発生時
        /// </summary>
        protected override void OnException()
            => this.BadGateway?.Invoke("Bad Gateway");

        /// <summary>
        /// TCP 切断時
        /// </summary>
        protected override void OnTcpClose()
        {
            try
            {
                this.httpResponseReader?.CloseTcp();
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
                this.httpResponseReader?.HandleReceive(buffer, readSize);
            }
            catch (BadGatewayException e)
            {
                this.BadGateway?.Invoke(e.Message);
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
        /// レスポンスヘッダー受信時
        /// </summary>
        /// <param name="response">受信したヘッダーを含むレスポンス</param>
        private void OnReceivedResponseHeaders(HttpResponse response)
        {
            response.Headers.RemoveConnectionHeaders();
            this.ReceivedResponseHeaders?.Invoke(response);
        }

        /// <summary>
        /// リクエストヘッダー送信
        /// </summary>
        /// <param name="request">送信するヘッダーを含むリクエスト</param>
        public void SendRequestHeaders(HttpRequest request)
        {
            var headers = request.HeadersAsByte();
            this.Write(headers, headers.Length);
        }

        /// <summary>
        /// レスポンスヘッダー受信完了時に発生
        /// </summary>
        public event Action<HttpResponse> ReceivedResponseHeaders;

        /// <summary>
        /// レスポンスボディー受信完了時に発生
        /// </summary>
        public event Action<HttpResponse> ReceivedResponseBody;

        /// <summary>
        /// BadBateway となるレスポンスを受信時に発生
        /// </summary>
        public event Action<string> BadGateway;
    }
}
