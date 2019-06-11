using System.Text.RegularExpressions;

namespace Nekoxy2.Default.Certificate.Default
{
    internal static class CertificateExtensions
    {
        /// <summary>
        /// CN プレフィクスに合致するパターン
        /// </summary>
        private static readonly Regex CnPrefix = new Regex("^CN=", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// CN プレフィクスを追加
        /// </summary>
        /// <param name="name">コモンネーム</param>
        /// <returns>CN プレフィクスが追加されたコモンネーム</returns>
        internal static string AddCn(this string name)
            => CnPrefix.IsMatch(name) ? name : $"CN={name}";

        /// <summary>
        /// CN プレフィクスを削除
        /// </summary>
        /// <param name="name">コモンネーム</param>
        /// <returns>CN プレフィクスが削除されたコモンネーム</returns>
        internal static string RemoveCn(this string name)
            => CnPrefix.Replace(name, "");
    }
}
