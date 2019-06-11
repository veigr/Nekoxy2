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

        // require over TLS1.0(0x0301)
        // 0        1byte   ContentType 22 (0x16/Handshake)
        // 1        1byte   MajorVersion
        // 2        1byte   MinorVersion
        // 3-4      2bytes  length(uint16)
        // -- ここまでTLSレコードヘッダ。以下は分割される可能性あり。
        // 0        1byte   HandshakeType (0x01/clienthello)
        // 1-3      3bytes  length(uint24)  // length が取れるか、length 分データを受信するまではレコード読み取り継続したい
        // -- ここまでHandshakeヘッダ。
        // 4        1byte   MajorVersion
        // 5        1byte   MinorVersion    <- こっちで判定。TLS1.3以降は拡張でネゴるので1.2(3.3)固定となる。3.1以上なら問題ない。
        // 6-37     4+28bytes   Random
        // 38       1byte   SessionIDLength
        //          nbytes  SessionID
        //          2bytes  CipherSuitesLength
        //          nbytes  CipherSuites
        //          1byte   CompressionMethodsLength
        //          nbyte   CompressionMethod
        //          2bytes  ExtensionsLength

        // Extension
        //  2bytes Type (ALPN=16(0x0e))
        //  2bytes Length
        //  nbytes Data

        // ALPN Extension
        //  2bytes TotalLength
        //  ALPNType[]  <- この配列が欲しい

        // ALPN Protocol Name
        //  1byte  Length
        //  nbyte  NextProtocol
    }
}