using System;
using System.Collections.Generic;
using System.Text;

namespace Nekoxy2
{
    public static class HttpProxyFactory
    {
        /// <summary>
        /// プロキシエンジンを指定して読み取り専用 HTTP プロキシを作成
        /// </summary>
        /// <param name="engine">プロキシエンジン</param>
        /// <returns>HTTP プロキシ</returns>
        public static IReadOnlyHttpProxy Create(Spi.IReadOnlyHttpProxyEngine engine)
            => new HttpProxy(engine);

        /// <summary>
        /// プロキシエンジンを指定して HTTP プロキシを作成
        /// </summary>
        /// <param name="engine">プロキシエンジン</param>
        /// <returns>HTTP プロキシ</returns>
        public static IHttpProxy Create(Spi.IHttpProxyEngine engine)
            => new HttpProxy(engine);

        /// <summary>
        /// プロキシエンジンを指定して読み取り専用 WebSocket プロキシを作成
        /// </summary>
        /// <param name="engine">プロキシエンジン</param>
        /// <returns>HTTP プロキシ</returns>
        public static IReadOnlyWebSocketProxy Create(Spi.IReadOnlyWebSocketProxyEngine engine)
            => new HttpProxy(engine);

        /// <summary>
        /// プロキシエンジンを指定して WebSocket プロキシを作成
        /// </summary>
        /// <param name="engine">プロキシエンジン</param>
        /// <returns>HTTP プロキシ</returns>
        public static IWebSocketProxy Create(Spi.IWebSocketProxyEngine engine)
            => new HttpProxy(engine);
    }
}
