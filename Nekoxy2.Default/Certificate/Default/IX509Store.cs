using System;
using System.Security.Cryptography.X509Certificates;

namespace Nekoxy2.Default.Certificate.Default
{
    /// <summary>
    /// X509証明書ストア
    /// </summary>
    internal interface IX509Store : IDisposable
    {
        /// <summary>
        /// ストアを開く
        /// </summary>
        /// <param name="flags">開き方</param>
        void Open(OpenFlags flags);

        /// <summary>
        /// 証明書を追加
        /// </summary>
        /// <param name="certificate">証明書</param>
        void Add(X509Certificate2 certificate);

        /// <summary>
        /// 証明書を削除
        /// </summary>
        /// <param name="certificate">証明書</param>
        void Remove(X509Certificate2 certificate);

        /// <summary>
        /// 証明書リスト
        /// </summary>
        X509Certificate2Collection Certificates { get; }
    }
}
