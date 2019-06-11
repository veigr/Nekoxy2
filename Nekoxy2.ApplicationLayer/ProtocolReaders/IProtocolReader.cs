using System;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders
{
    /// <summary>
    /// 通信データを入力しプロトコルを解釈し読み取る機能を提供
    /// </summary>
    internal interface IProtocolReader : IDisposable
    {
        /// <summary>
        /// データ受信
        /// </summary>
        /// <param name="buffer">受信バッファー</param>
        /// <param name="readSize">読み取りサイズ</param>
        void HandleReceive(byte[] buffer, int readSize);
    }
}