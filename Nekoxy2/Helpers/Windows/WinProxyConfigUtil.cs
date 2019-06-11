using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Nekoxy2.Helpers.Windows
{
    /// <summary>
    /// Windows システムプロキシ設定操作ユーティリティ
    /// </summary>
    public static class WinProxyConfigUtil
    {
        /// <summary>
        /// 現在の WinINet セッションのプロキシ設定を適用
        /// </summary>
        /// <param name="proxy">プロキシサーバー</param>
        /// <param name="proxyBypass">バイパスリスト</param>
        public static void SetProxyForCurrentSession(string proxy, string proxyBypass)
        {
            var proxyInfo = new INTERNET_PROXY_INFO
            {
                dwAccessType = INTERNET_OPEN_TYPE.INTERNET_OPEN_TYPE_PROXY,
                lpszProxy = proxy,
                lpszProxyBypass = proxyBypass,
            };
            var dwBufferLength = (uint)Marshal.SizeOf(proxyInfo);
            NativeMethods.UrlMkSetSessionOption(INTERNET_OPTION.INTERNET_OPTION_PROXY, proxyInfo, dwBufferLength, 0U);
        }

        /// <summary>
        /// システムプロキシのプロキシ設定を指定値で置換し、現在の WinINet セッションのプロキシ設定に適用
        /// </summary>
        /// <param name="listeningPort">ポート</param>
        public static void SetProxyForCurrentSession(IEStyleProxySettings overrideSettings = null, string overrideProxyBypass = null)
        {
            var winHttpConfig = WinHttpGetIEProxyConfigForCurrentUser();
            SetProxyForCurrentSession(
                winHttpConfig.ToIEStyleProxySettings().ToIEStyleString(overrideSettings),
                overrideProxyBypass ?? winHttpConfig.ProxyBypass);
        }

        /// <summary>
        /// 現在のユーザーの IE プロキシ設定を取得
        /// </summary>
        /// <returns></returns>
        public static WinHttpCurrentUserIEProxyConfig WinHttpGetIEProxyConfigForCurrentUser()
        {
            var ieProxyConfig = new WinHttpCurrentUserIEProxyConfig();
            NativeMethods.WinHttpGetIEProxyConfigForCurrentUser(ref ieProxyConfig);
            return ieProxyConfig;
        }
    }
}
