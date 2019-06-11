using System.Security.Cryptography.X509Certificates;

namespace Nekoxy2.Default.Certificate.Default
{
    /// <summary>
    /// X509証明書ストア
    /// </summary>
    internal sealed class X509StoreWrapper : IX509Store
    {
        /// <summary>
        /// ソースストア
        /// </summary>
        private readonly X509Store store;

        /// <summary>
        /// 証明書リスト
        /// </summary>
        public X509Certificate2Collection Certificates
            => this.store.Certificates;

        public X509StoreWrapper(X509Store store)
            => this.store = store;

        /// <summary>
        /// 証明書を追加
        /// </summary>
        /// <param name="certificate">証明書</param>
        public void Add(X509Certificate2 certificate)
            => this.store.Add(certificate);

        /// <summary>
        /// ストアを開く
        /// </summary>
        /// <param name="flags">開き方</param>
        public void Open(OpenFlags flags)
            => this.store.Open(flags);

        /// <summary>
        /// 証明書を削除
        /// </summary>
        /// <param name="certificate">証明書</param>
        public void Remove(X509Certificate2 certificate)
            => this.store.Remove(certificate);

        public void Dispose()
            => this.store.Dispose();
    }
}
