using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nekoxy2.ApplicationLayer;

namespace Nekoxy2.Default.Proxy.Tls
{
    internal static class ReadBufferedNetworkStreamExtensions
    {
        /// <summary>
        /// 現在のバッファー内容が TLS (SSL 3.0 以降)かどうか。
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static bool IsTls(this ReadBufferedNetworkStream stream)
        {
            // SSL 2.0 はサポートしない
            return 0 < stream.BufferedLength
                && stream[0] == 0x16;
        }

        /// <summary>
        /// ClientHello を待ち受け、ALPN データを取得
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="protocolNames"></param>
        /// <returns></returns>
        public static bool TryGetClientAlpn(this ReadBufferedNetworkStream stream, out IReadOnlyList<string> protocolNames)
        {
            // ちゃんと Parse してから処理すれば良いんだけど、ALPN 取りたいだけなので雑……
            protocolNames = Array.Empty<string>();
            try
            {
                var recordIndex = 0;
                var recordData = new List<byte>();
                ClientHello clientHello = null;

                while (recordData.Count < (clientHello != null ? clientHello.Length + 4 : int.MaxValue))
                {
                    stream.FillBuffer(recordIndex + 5);

                    var record = new TlsRecord
                    {
                        ContentType = stream[recordIndex++]
                    };
                    if (record.ContentType != 0x16)
                        return false;    // not Handshake Record

                    record.MajorVersion = stream[recordIndex++];
                    record.MinorVersion = stream[recordIndex++];
                    record.Length = stream.ReadBufferUInt16(recordIndex);

                    recordIndex += 2;
                    stream.FillBuffer(recordIndex + record.Length);
                    recordData.AddRange(stream.ReadBufferBytes(recordIndex, record.Length));

                    if (clientHello == null && 3 <= recordData.Count)
                    {
                        if (recordData[0] != 0x01)
                            return false;    // not Client Hello
                        clientHello = new ClientHello
                        {
                            Length = recordData.ToUInt24(1)
                        };
                    }
                }

                clientHello.MajorVersion = recordData[4];
                clientHello.MinorVersion = recordData[5];
                if (clientHello.MajorVersion != 3 || clientHello.MinorVersion < 1)
                    return false;    // less than TLS 1.0 (SSL 3.1)

                clientHello.SessionIDLength = recordData[38];
                clientHello.CipherSuitesLength = recordData.ToUInt16(39 + clientHello.SessionIDLength);
                clientHello.CompressionMethodsLength = recordData[41 + clientHello.SessionIDLength + clientHello.CipherSuitesLength];
                var extensionsStartIndex = 43 + clientHello.SessionIDLength + clientHello.CipherSuitesLength;
                if (clientHello.Length + 4 <= extensionsStartIndex - 2)
                    return false;    // no Extensions

                clientHello.ExtensionsLength = recordData.ToUInt16(extensionsStartIndex - 2);
                var extensionsIndex = extensionsStartIndex;
                while (extensionsIndex < extensionsStartIndex + clientHello.ExtensionsLength)
                {
                    var type = recordData.ToUInt16(extensionsIndex);
                    extensionsIndex += 2;
                    var length = recordData.ToUInt16(extensionsIndex);
                    extensionsIndex += 2;
                    var data = recordData.Skip(extensionsIndex).Take(length).ToArray();
                    extensionsIndex += length;

                    if (type != 16) continue;
                    var names = new List<string>();
                    var alpnIndex = 2;  // ALPN Extension Length 何のためにあるの……
                    while (alpnIndex < length)
                    {
                        var nameLength = data[alpnIndex++];
                        var nameBytes = new byte[nameLength];
                        Buffer.BlockCopy(data, alpnIndex, nameBytes, 0, nameLength);
                        names.Add(Encoding.ASCII.GetString(nameBytes));
                        alpnIndex += nameLength;
                    }
                    protocolNames = names.ToArray();
                    return true;
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        private class TlsRecord
        {
            public byte ContentType { get; set; }
            public byte MajorVersion { get; set; }
            public byte MinorVersion { get; set; }
            public ushort Length { get; set; }
            //public byte[] Data { get; set; }
        }

        private class ClientHello
        {
            #region Handshake
            public byte HandshakeType { get; set; }
            public int Length { get; set; } = int.MaxValue;
            #endregion
            #region ClientHello
            public byte MajorVersion { get; set; }
            public byte MinorVersion { get; set; }
            //public byte[] Random { get; set; }
            public byte SessionIDLength { get; set; }
            //public byte[] SessionID { get; set; }
            public ushort CipherSuitesLength { get; set; }
            //public byte[] CipherSuites { get; set; }
            public byte CompressionMethodsLength { get; set; }
            //public byte[] CompressionMethod { get; set; }
            public ushort ExtensionsLength { get; set; }
            #endregion
        }
    }
}