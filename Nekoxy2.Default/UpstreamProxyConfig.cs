using System;
using System.Collections.Generic;
using System.Text;

namespace Nekoxy2.Default
{
    /// <summary>
    /// 上流プロキシ設定
    /// </summary>
    public interface IUpstreamProxyConfig
    {
        /// <summary>
        /// 接続先からプロキシを取得。
        /// プロキシが無効な場合は <see cref="null"/> を返す。
        /// </summary>
        /// <param name="destination">接続先</param>
        /// <returns>プロキシ</returns>
        IProxyAddress GetProxy(Uri destination);
    }

    /// <summary>
    /// プロキシアドレス
    /// </summary>
    public interface IProxyAddress
    {
        /// <summary>
        /// プロキシホスト
        /// </summary>
        string Host { get; set; }

        /// <summary>
        /// プロキシポート
        /// </summary>
        ushort Port { get; set; }
    }

    /// <summary>
    /// 上流プロキシ設定
    /// </summary>
    public sealed class UpstreamProxyConfig : IUpstreamProxyConfig
    {
        // TODO 認証プロキシ対応

        /// <summary>
        /// HTTP に用いるプロキシ
        /// </summary>
        public IProxyAddress HttpProxy { get; } = default;

        /// <summary>
        /// HTTPS に用いるプロキシ
        /// </summary>
        public IProxyAddress HttpsProxy { get; } = default;

        /// <summary>
        /// 接続先からプロキシを取得。
        /// プロキシを使用しない場合は <see cref="null"/> を返す。
        /// </summary>
        /// <param name="destination">接続先</param>
        /// <returns>プロキシ</returns>
        public IProxyAddress GetProxy(Uri destination)
        {
            try
            {
                switch (destination.Scheme)
                {
                    case "http":
                        return this.HttpProxy;
                    case "https":
                        return this.HttpsProxy;
                    case "ws":
                        return this.HttpProxy;
                    case "wss":
                        return this.HttpsProxy;
                    default:
                        return null;
                }
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>
        /// 上流プロキシを使用せずに初期化
        /// </summary>
        public UpstreamProxyConfig() { }

        /// <summary>
        /// 全てのプロキシに同じホストとポートを指定して初期化
        /// </summary>
        /// <param name="host">プロキシホスト</param>
        /// <param name="port">プロキシポート</param>
        public UpstreamProxyConfig(string host, ushort port)
        {
            this.HttpProxy = new ProxyAddress(host, port);
            this.HttpsProxy = new ProxyAddress(host, port);
        }

        /// <summary>
        /// ホストとプロキシを個別に指定して初期化
        /// </summary>
        /// <param name="httpHost">HTTP のプロキシホスト</param>
        /// <param name="httpPort">HTTP のプロキシポート</param>
        /// <param name="httpsHost">HTTPS のプロキシホスト</param>
        /// <param name="httpsPort">HTTPS のプロキシポート</param>
        public UpstreamProxyConfig(string httpHost, ushort httpPort, string httpsHost, ushort httpsPort)
        {
            this.HttpProxy = new ProxyAddress(httpHost, httpPort);
            this.HttpsProxy = new ProxyAddress(httpsHost, httpsPort);
        }
    }

    /// <summary>
    /// プロキシアドレス
    /// </summary>
    public sealed class ProxyAddress : IProxyAddress
    {
        /// <summary>
        /// プロキシホスト
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// プロキシポート
        /// </summary>
        public ushort Port { get; set; }

        /// <summary>
        /// ホストとポートを指定して初期化
        /// </summary>
        /// <param name="host">プロキシホスト</param>
        /// <param name="port">プロキシポート</param>
        public ProxyAddress(string host, ushort port)
        {
            this.Host = host;
            this.Port = port;
        }
    }

    internal static partial class Extensions
    {
        public static bool IsEnabled(this IUpstreamProxyConfig config, Uri destination)
        {
            var proxy = config?.GetProxy(destination);
            if (proxy == null) return false;
            if (string.IsNullOrWhiteSpace(proxy.Host)) return false;
            if (proxy.Port < 1) return false;
            return true;
        }
    }
}
