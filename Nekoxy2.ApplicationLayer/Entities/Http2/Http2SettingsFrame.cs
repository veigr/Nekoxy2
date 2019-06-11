using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    /// <summary>
    /// コネクションの設定を転送するフレーム
    /// RFC7540 6.5
    /// </summary>
    internal sealed class Http2SettingsFrame : IHttp2Frame
    {
        #region Header

        /// <summary>
        /// HTTP/2 フレームヘッダー
        /// </summary>
        public Http2FrameHeader Header { get; }

        /// <summary>
        /// 受信した SETTINGS フレームを適用したことを示す
        /// </summary>
        public bool IsAck => this.HasFlag((byte)Flag.Ack);

        private enum Flag : byte
        {
            Ack = 0b00000001,
        }

        #endregion

        /// <summary>
        /// 設定
        /// </summary>
        public IReadOnlyList<(Http2SettingKey Key, uint Value)> Settings { get; }

        public Http2SettingsFrame() { }

        public Http2SettingsFrame(Http2FrameHeader header, byte[] data)
        {
            if (data.Length != header.Length)
                throw new ArgumentException("Invalid Length.");
            this.Header = header;

            var settings = new List<(Http2SettingKey, uint)>();
            var index = 0;
            while (index < data.Length)
            {
                var key = (Http2SettingKey)data.ToUInt16(index);
                index += 2;
                var value = data.ToUInt32(index);
                index += 4;
                settings.Add((key, value));
            }
            this.Settings = settings;
        }

        public Http2SettingsFrame(Http2FrameHeader header, IReadOnlyList<(Http2SettingKey Key, uint Value)> settings)
        {
            this.Header = header;
            this.Settings = settings;
        }

        public byte[] ToBytes()
        {
            var bytes = new List<byte>();
            bytes.AddRange(this.Header.ToBytes());
            foreach (var setting in this.Settings)
            {
                bytes.AddRange(BitConverter.GetBytes((ushort)setting.Key).Reverse());
                bytes.AddRange(BitConverter.GetBytes(setting.Value).Reverse());
            }
            return bytes.ToArray();
        }

        public override string ToString()
        {
            var settingStrings = string.Join("\r\n", this.Settings.Select(x => $"{x.Key}: {x.Value}"));
            settingStrings = string.IsNullOrEmpty(settingStrings) ? "" : "\r\n" + settingStrings;
            return $"{this.Header}, IsACK: {this.IsAck}, Settings: {{ {settingStrings} }}";
        }

        public static Http2SettingsFrame Create(
            int streamID,
            params (Http2SettingKey Key, uint Value)[] settings)
        {
            var header = new Http2FrameHeader(
                settings.Length * 6,
                Http2FrameType.Settings,
                0,
                streamID);
            return new Http2SettingsFrame(header, settings);
        }

        public static Http2SettingsFrame CreateAck(int streamID)
        {
            var header = new Http2FrameHeader(
                0,
                Http2FrameType.Settings,
                (byte)Flag.Ack,
                streamID);
            return new Http2SettingsFrame(header, Array.Empty<(Http2SettingKey Key, uint Value)>());
        }
    }

    /// <summary>
    /// HTTP/2 設定
    /// https://www.iana.org/assignments/http2-parameters/http2-parameters.xhtml#settings
    /// </summary>
    internal enum Http2SettingKey : ushort
    {
        Reserved = 0x0,
        /// <summary>
        /// HPACK の動的テーブルのサイズ
        /// </summary>
        HeaderTableSize = 0x1,
        /// <summary>
        /// サーバープッシュの有効/無効
        /// </summary>
        EnablePush = 0x2,
        /// <summary>
        /// 最大同時ストリーム数
        /// </summary>
        MaxConcurrentStream = 0x3,
        /// <summary>
        /// 初期ウインドウサイズ
        /// </summary>
        InitialWindowSize = 0x4,
        /// <summary>
        /// ペイロードの最大オクテットサイズ
        /// </summary>
        MaxFrameSize = 0x5,
        /// <summary>
        /// HTTP ヘッダーリストの最大サイズ
        /// </summary>
        MaxHeaderListSize = 0x6,
        /// <summary>
        /// CONNECT での :protocol ヘッダ使用の有効/無効
        /// </summary>
        EnableConnectProtocol = 0x8,
        TlsRenegPermitted = 0x10,
    }
}
