using System;
using System.IO;
using System.Net.Sockets;

namespace Nekoxy2.Default.Proxy.Tcp
{
    /// <summary>
    /// TcpClient から必要な機能を抽出・追加
    /// </summary>
    internal interface ITcpClient : IDisposable
    {
        /// <summary>
        /// 接続しているかどうか
        /// </summary>
        bool Connected { get; }

        /// <summary>
        /// 切断状態
        /// </summary>
        CloseState CloseState { get; }

        /// <summary>
        /// 通信ストリーム
        /// </summary>
        /// <returns></returns>
        Stream GetStream();

        /// <summary>
        /// 切断
        /// </summary>
        /// <param name="how">切断対象</param>
        void Shutdown(SocketShutdown how);
    }

    /// <summary>
    /// 切断状態
    /// </summary>
    [Flags]
    internal enum CloseState
    {
        /// <summary>
        /// 受信側クローズ
        /// </summary>
        ReceiveClosed = 0b01,
        /// <summary>
        /// 送信側クローズ
        /// </summary>
        SendClosed = 0b10,
        /// <summary>
        /// 送受信クローズ
        /// </summary>
        Both = 0b11,
    }
}
