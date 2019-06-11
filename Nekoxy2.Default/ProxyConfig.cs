using Nekoxy2.Default.Certificate;
using Nekoxy2.Default.Certificate.Default;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Nekoxy2.Default
{
    /// <summary>
    /// プロキシ設定
    /// </summary>
    internal sealed class ProxyConfig
    {
        /// <summary>
        /// バイト配列の最大サイズ
        /// </summary>
        /// <remarks>
        /// <see href="https://docs.microsoft.com/dotnet/framework/configure-apps/file-schema/runtime/gcallowverylargeobjects-element"/>
        /// </remarks>
        internal const int MaxByteArrayLength = 0x7FFFFFC7;

        /// <summary>
        /// 待ち受け設定
        /// </summary>
        public IListeningConfig ListeningConfig { get; set; } = new ListeningConfig();

        /// <summary>
        /// 上流プロキシ設定。
        /// </summary>
        public IUpstreamProxyConfig UpstreamProxyConfig { get; set; } = new UpstreamProxyConfig();

        /// <summary>
        /// HTTPS 復号化設定
        /// </summary>
        public DecryptConfig DecryptConfig { get; set; } = new DecryptConfig();

        /// <summary>
        /// キャプチャするボディやペイロード長の最大サイズ
        /// </summary>
        public int MaxCaptureSize { get; set; } = Environment.Is64BitProcess ? MaxByteArrayLength : 256 * 1024 * 1024;

        /// <summary>
        /// ボディをキャプチャするかどうか
        /// </summary>
        public bool IsCaptureBody { get; set; } = true;
    }
}
