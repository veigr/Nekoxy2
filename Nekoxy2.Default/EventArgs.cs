using System;
using System.Security.Cryptography.X509Certificates;

namespace Nekoxy2.Default
{
    /// <summary>
    /// コネクション数変化時イベントデータ
    /// </summary>
    public interface IConnectionCountChangedEventArgs
    {
        /// <summary>
        /// コネクション数
        /// </summary>
        int Count { get; }
    }

    /// <summary>
    /// コネクション数変化時イベントデータ
    /// </summary>
    public sealed class ConnectionCountChangedEventArgs : EventArgs, IConnectionCountChangedEventArgs
    {
        /// <summary>
        /// コネクション数
        /// </summary>
        public int Count { get; }

        private ConnectionCountChangedEventArgs(int count)
            => this.Count = count;

        /// <summary>
        /// コネクション数変化時イベントデータ作成
        /// </summary>
        /// <param name="count">コネクション数</param>
        /// <returns>コネクション数変化時イベントデータ</returns>
        public static IConnectionCountChangedEventArgs Create(int count)
            => new ConnectionCountChangedEventArgs(count);
    }

    /// <summary>
    /// 証明書作成時イベントデータ
    /// </summary>
    public interface ICertificateCreatedEventArgs
    {
        /// <summary>
        /// 証明書
        /// </summary>
        X509Certificate2 Certificate { get; }
    }

    /// <summary>
    /// 証明書作成時イベントデータ
    /// </summary>
    public sealed class CertificateCreatedEventArgs : EventArgs, ICertificateCreatedEventArgs
    {
        /// <summary>
        /// 証明書
        /// </summary>
        public X509Certificate2 Certificate { get; }

        private CertificateCreatedEventArgs(X509Certificate2 certificate)
            => this.Certificate = certificate;

        /// <summary>
        /// 証明書作成時イベントデータ作成
        /// </summary>
        /// <param name="certificate">証明書</param>
        /// <returns>証明書作成時イベントデータ</returns>
        public static ICertificateCreatedEventArgs Create(X509Certificate2 certificate)
            => new CertificateCreatedEventArgs(certificate);
    }
}
