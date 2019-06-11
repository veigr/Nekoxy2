using Nekoxy2.ApplicationLayer.ProtocolReaders.WebSocket;
using System;

namespace Nekoxy2.SazLoader.Entities.WebSocket
{
    /// <summary>
    /// SAZ WebSocket フレーム
    /// </summary>
    internal sealed class SazWebSocketFrame
    {
        /// <summary>
        /// リクエスト・レスポンス方向
        /// </summary>
        public Direction Direction { get; }

        /// <summary>
        /// フレーム ID
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// 受信完了日時
        /// </summary>
        public DateTimeOffset DoneRead { get; }

        /// <summary>
        /// 送信開始日時
        /// </summary>
        public DateTimeOffset BeginSend { get; }

        /// <summary>
        /// 送信完了日時
        /// </summary>
        public DateTimeOffset DoneSend { get; }

        /// <summary>
        /// フレームデータ
        /// </summary>
        public ApplicationLayer.Entities.WebSocket.WebSocketFrame Frame { get; }

        /// <summary>
        /// コンストラクター
        /// </summary>
        /// <param name="direction">リクエスト・レスポンス方向</param>
        /// <param name="id">フレーム ID</param>
        /// <param name="doneRead">受信完了日時</param>
        /// <param name="beginSend">送信開始日時</param>
        /// <param name="doneSend">送信完了日時</param>
        /// <param name="frameBytes">フレームデータ</param>
        public SazWebSocketFrame(
            Direction direction,
            int id,
            DateTimeOffset doneRead,
            DateTimeOffset beginSend,
            DateTimeOffset doneSend,
            byte[] frameBytes)
        {
            this.Direction = direction;
            this.ID = id;
            this.DoneRead = doneRead;
            this.BeginSend = beginSend;
            this.DoneSend = doneSend;
            if (new WebSocketFrameBuilder(0x7FFFFFC7)
                .TryAddData(frameBytes, 0, frameBytes.Length, out _, out var frame))
            {
                this.Frame = frame;
            }
        }
    }

    /// <summary>
    /// リクエスト・レスポンス方向
    /// </summary>
    internal enum Direction
    {
        Request,
        Response,
    }
}
