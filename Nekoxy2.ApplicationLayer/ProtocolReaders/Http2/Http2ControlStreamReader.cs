using Nekoxy2.ApplicationLayer.Entities.Http2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.ProtocolReaders.Http2
{
    /// <summary>
    /// HTTP/2 制御用ストリーム (ID 0) 読み取り
    /// </summary>
    internal sealed class Http2ControlStreamReader
    {
        /// <summary>
        /// ロックオブジェクト
        /// </summary>
        private readonly object lockObject = new object();

        /// <summary>
        /// 受信フレームリスト
        /// </summary>
        public IList<IHttp2Frame> Frames { get; } = new List<IHttp2Frame>();

        /// <summary>
        /// リクエスト・レスポンスの対となる他方の制御用ストリームリーダー
        /// </summary>
        public Http2ControlStreamReader Partner { get; set; }

        /// <summary>
        /// フレームを入力
        /// </summary>
        /// <param name="frame">入力するフレーム</param>
        public void HandleFrame(IHttp2Frame frame)
        {
            lock (this.lockObject)
            {
                if (frame.Header.StreamID != 0)
                    return;

                this.Frames.Add(frame);

                if (frame is Http2SettingsFrame settingsFrame
                && settingsFrame.IsAck)
                {
                    IReadOnlyList<(Http2SettingKey Key, uint Value)> settings = default;
                    // ACK が前後しても大丈夫なようにする
                    if (settingsFrame.IsAck)
                    {
                        settings = this.Partner.Frames
                            .OfType<Http2SettingsFrame>()
                            .Where(x => !x.IsAck)
                            .LastOrDefault()
                            ?.Settings;
                    }
                    else if (this.Frames.OfType<Http2SettingsFrame>().Count(x => !x.IsAck)
                        == this.Frames.OfType<Http2SettingsFrame>().Count(x => x.IsAck))
                    {
                        settings = settingsFrame.Settings;
                    }
                    if (settings?.Any(x => x.Key == Http2SettingKey.HeaderTableSize) ?? false)
                    {
                        var size = settings.First(x => x.Key == Http2SettingKey.HeaderTableSize).Value;
                        this.UpdateDynamicTableSize?.Invoke(size);
                    }
                }
            }
        }

        /// <summary>
        /// 動的テーブルサイズ更新
        /// </summary>
        public event Action<uint> UpdateDynamicTableSize;
    }
}
