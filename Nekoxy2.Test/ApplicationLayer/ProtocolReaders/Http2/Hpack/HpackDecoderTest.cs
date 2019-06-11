using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Nekoxy2.ApplicationLayer.ProtocolReaders.Http2.Hpack;
using Nekoxy2.Test.TestUtil;

namespace Nekoxy2.Test.ApplicationLayer.ProtocolReaders.Http2.Hpack
{
    public class HpackDecoderTest
    {
        [Fact]
        public void DecodeIntegerTest()
        {
            new byte[] { 0b01010, 0b11111111 }
                .DecodeInteger(0, 0b00011111, out var length1)
                .Is(10);
            length1.Is(1);

            new byte[] { 0b11111, 0b10011010, 0b00001010, 0b11111111 }
                .DecodeInteger(0, 0b00011111, out var length2)
                .Is(1337);
            length2.Is(3);
        }

        [Fact]
        public void DecodeStringTest()
        {
            "8286 8441 0f77 7777 2e65 7861 6d70 6c65 2e63 6f6d".HexToBytes(" ")
                .DecodeString(4, out var length1)
                .Is("www.example.com");
            length1.Is(16);
        }

        [Fact]
        public void DecodeHuffmanStringTest()
        {
            "8286 8441 8cf1 e3c2 e5f2 3a6b a0ab 90f4 ff".HexToBytes(" ")
                .DecodeString(4, out var length1)
                .Is("www.example.com");
            length1.Is(13);
        }

        [Fact]
        public void LiteralHeaderFieldWithIndexingTest()
        {
            var decoder = new HpackDecoder();
            var source = "400a 6375 7374 6f6d 2d6b 6579 0d63 7573 746f 6d2d 6865 6164 6572"
                .HexToBytes(" ");

            var headers = decoder.Decode(source);

            headers.Count.Is(1);
            headers[0].Name.Is("custom-key");
            headers[0].Value.Is("custom-header");

            var dynamicTable = decoder.Table.DynamicTable.Table;
            dynamicTable.Count.Is(1);
            dynamicTable[0].Name.Is("custom-key");
            dynamicTable[0].Value.Is("custom-header");
        }

        [Fact]
        public void LiteralHeaderFieldWithoutIndexingTest()
        {
            var decoder = new HpackDecoder();
            var source = "040c 2f73 616d 706c 652f 7061 7468"
                .HexToBytes(" ");

            var headers = decoder.Decode(source);

            headers.Count.Is(1);
            headers[0].Name.Is(":path");
            headers[0].Value.Is("/sample/path");

            var dynamicTable = decoder.Table.DynamicTable.Table;
            dynamicTable.Count.Is(0);
        }

        [Fact]
        public void LiteralHeaderFieldNeverIndexedTest()
        {
            var decoder = new HpackDecoder();
            var source = "1008 7061 7373 776f 7264 0673 6563 7265 74"
                .HexToBytes(" ");

            var headers = decoder.Decode(source);

            headers.Count.Is(1);
            headers[0].Name.Is("password");
            headers[0].Value.Is("secret");

            var dynamicTable = decoder.Table.DynamicTable.Table;
            dynamicTable.Count.Is(0);
        }

        [Fact]
        public void IndexedHeaderTest()
        {
            var decoder = new HpackDecoder();
            var source = "82"
                .HexToBytes(" ");

            var headers = decoder.Decode(source);

            headers.Count.Is(1);
            headers[0].Name.Is(":method");
            headers[0].Value.Is("GET");

            var dynamicTable = decoder.Table.DynamicTable.Table;
            dynamicTable.Count.Is(0);
        }

        [Fact]
        public void RequestWithoutHuffmanCodingTest()
        {
            var decoder = new HpackDecoder();

            var source1 = "8286 8441 0f77 7777 2e65 7861 6d70 6c65 2e63 6f6d"
                .HexToBytes(" ");
            var headers1 = decoder.Decode(source1);

            headers1.Count.Is(4);
            headers1[0].Is((":method", "GET"));
            headers1[1].Is((":scheme", "http"));
            headers1[2].Is((":path", "/"));
            headers1[3].Is((":authority", "www.example.com"));

            var dynamicTable = decoder.Table.DynamicTable.Table;
            dynamicTable.Count.Is(1);
            dynamicTable[0].Is((":authority", "www.example.com"));


            var source2 = "8286 84be 5808 6e6f 2d63 6163 6865"
                .HexToBytes(" ");
            var headers2 = decoder.Decode(source2);
            headers2.Count.Is(5);
            headers2[0].Is((":method", "GET"));
            headers2[1].Is((":scheme", "http"));
            headers2[2].Is((":path", "/"));
            headers2[3].Is((":authority", "www.example.com"));
            headers2[4].Is(("cache-control", "no-cache"));

            dynamicTable.Count.Is(2);
            dynamicTable[0].Is(("cache-control", "no-cache"));
            dynamicTable[1].Is((":authority", "www.example.com"));


            var source3 = "8287 85bf 400a 6375 7374 6f6d 2d6b 6579 0c63 7573 746f 6d2d 7661 6c75 65"
                .HexToBytes(" ");
            var headers3 = decoder.Decode(source3);
            headers3.Count.Is(5);
            headers3[0].Is((":method", "GET"));
            headers3[1].Is((":scheme", "https"));
            headers3[2].Is((":path", "/index.html"));
            headers3[3].Is((":authority", "www.example.com"));
            headers3[4].Is(("custom-key", "custom-value"));

            dynamicTable.Count.Is(3);
            dynamicTable[0].Is(("custom-key", "custom-value"));
            dynamicTable[1].Is(("cache-control", "no-cache"));
            dynamicTable[2].Is((":authority", "www.example.com"));
        }

        [Fact]
        public void ResponseWithHuffmanCodingTest()
        {
            var decoder = new HpackDecoder();
            decoder.UpdateDynamicTableSize(256);

            var source1 = (
                "4882 6402 5885 aec3 771a 4b61 96d0 7abe" +
                "9410 54d4 44a8 2005 9504 0b81 66e0 82a6" +
                "2d1b ff6e 919d 29ad 1718 63c7 8f0b 97c8" +
                "e9ae 82ae 43d3"
                ).HexToBytes(" ");
            var headers1 = decoder.Decode(source1);

            headers1.Count.Is(4);
            headers1[0].Is((":status", "302"));
            headers1[1].Is(("cache-control", "private"));
            headers1[2].Is(("date", "Mon, 21 Oct 2013 20:13:21 GMT"));
            headers1[3].Is(("location", "https://www.example.com"));

            var dynamicTable = decoder.Table.DynamicTable.Table;
            dynamicTable.Count.Is(4);
            dynamicTable[0].Is(("location", "https://www.example.com"));
            dynamicTable[1].Is(("date", "Mon, 21 Oct 2013 20:13:21 GMT"));
            dynamicTable[2].Is(("cache-control", "private"));
            dynamicTable[3].Is((":status", "302"));


            var source2 = (
                "4883 640e ffc1 c0bf"
                ).HexToBytes(" ");
            var headers2 = decoder.Decode(source2);

            headers2.Count.Is(4);
            headers2[0].Is((":status", "307"));
            headers2[1].Is(("cache-control", "private"));
            headers2[2].Is(("date", "Mon, 21 Oct 2013 20:13:21 GMT"));
            headers2[3].Is(("location", "https://www.example.com"));

            dynamicTable.Count.Is(4);
            dynamicTable[0].Is((":status", "307"));
            dynamicTable[1].Is(("location", "https://www.example.com"));
            dynamicTable[2].Is(("date", "Mon, 21 Oct 2013 20:13:21 GMT"));
            dynamicTable[3].Is(("cache-control", "private"));


            var source3 = (
                "88c1 6196 d07a be94 1054 d444 a820 0595" +
                "040b 8166 e084 a62d 1bff c05a 839b d9ab" +
                "77ad 94e7 821d d7f2 e6c7 b335 dfdf cd5b" +
                "3960 d5af 2708 7f36 72c1 ab27 0fb5 291f" +
                "9587 3160 65c0 03ed 4ee5 b106 3d50 07"
                ).HexToBytes(" ");
            var headers3 = decoder.Decode(source3);

            headers3.Count.Is(6);
            headers3[0].Is((":status", "200"));
            headers3[1].Is(("cache-control", "private"));
            headers3[2].Is(("date", "Mon, 21 Oct 2013 20:13:22 GMT"));
            headers3[3].Is(("location", "https://www.example.com"));
            headers3[4].Is(("content-encoding", "gzip"));
            headers3[5].Is(("set-cookie", "foo=ASDJKHQKBZXOQWEOPIUAXQWEOIU; max-age=3600; version=1"));

            dynamicTable.Count.Is(3);
            dynamicTable[0].Is(("set-cookie", "foo=ASDJKHQKBZXOQWEOPIUAXQWEOIU; max-age=3600; version=1"));
            dynamicTable[1].Is(("content-encoding", "gzip"));
            dynamicTable[2].Is(("date", "Mon, 21 Oct 2013 20:13:22 GMT"));
        }

        [Fact]
        public void DynamicTableSizeUpdateTest()
        {
            var source =
                "3f e1 5f 88 00 83 90 69 2f 96 df 69 7e 94 0b 2a 69 3f 75 04 00 bc a0 41 71 b6 ae 36 f2 98 b4 6f 64 02 2d 31 58 8d ae c3 77 1a 4b f4 a5 23 f2 b0 e6 2c 00 5f 92 49 7c a5 89 d3 4d 1f 6a 12 71 d8 82 a6 0e 1b f0 ac f7 40 85 4d 83 35 05 b3 a0 fc 5b 11 cf 35 05 5b 16 1c 0b 5e b6 cb 0b 52 56 da 5e d6 95 09 5a f1 d0 95 b0 d8 7a 56 c5 cb 77 78 8c a4 7e 56 1c c5 81 90 b6 cb 80 00 3f 5a 02 62 72 76 03 67 77 73 40 8c f2 b7 94 21 6a ec 3a 4a 44 98 f5 7f 8a 0f da 94 9e 42 c1 1d 07 27 5f 40 8b f2 b4 b6 0e 92 ac 7a d2 63 d4 8f 89 dd 0e 8c 1a b6 e4 c5 93 4f 00 87 41 52 b1 0e 7e a6 2f c2 0e b8 b2 c3 b6 01 00 2f 2c 10 ac 16 56 08 3e d4 2f 9a cd 61 51 06 f9 ed fa 50 2c ad 7c a4 58 40 0b ca 04 17 1b 6a e3 6f 29 8b 46 ff b5 2b 1a 67 81 8f b5 24 3d 23 35 50 2f 31 cf 35 05 5c 87 5f a5 7f 40 85 1d 09 59 1d c9 a1 ed 69 89 07 f3 71 a6 99 fe 7e d4 a4 70 09 b7 c4 00 03 ed 4e f0 7f 2d 35 f4 d3 3f 4c bf f4 cb 7f cf"
                .HexToBytes(" ");
            var decoder = new HpackDecoder();
            var headers = decoder.Decode(source);

            decoder.Table.DynamicTable.Size.Is((uint)12288);
            headers.Count.Is(13);
            headers[0].Is((":status", "200"));
            headers[1].Is(("date", "Tue, 13 Nov 2018 10:54:58 GMT"));
            headers[2].Is(("expires", "-1"));
            headers[3].Is(("cache-control", "private, max-age=0"));
            headers[4].Is(("content-type", "text/html; charset=UTF-8"));
            headers[5].Is(("trailer", "X-Google-GFE-Current-Request-Cost-From-GWS"));
            headers[6].Is(("strict-transport-security", "max-age=31536000"));
            headers[7].Is(("content-encoding", "br"));
            headers[8].Is(("server", "gws"));
            headers[9].Is(("x-xss-protection", "1; mode=block"));
            headers[10].Is(("x-frame-options", "SAMEORIGIN"));
            headers[11].Is(("set-cookie", "1P_JAR=2018-11-13-10; expires=Thu, 13-Dec-2018 10:54:58 GMT; path=/; domain=.google.co.jp"));
            headers[12].Is(("alt-svc", "quic=\":443\"; ma=2592000; v=\"44,43,39,35\""));
        }

        [Fact]
        public void ReferenceRemovedEntryTest()
        {
            var decoder = new HpackDecoder();

            var source1 = "8286 8441 0f77 7777 2e65 7861 6d70 6c65 2e63 6f6d"
                .HexToBytes(" ");
            var headers1 = decoder.Decode(source1);

            headers1.Count.Is(4);
            headers1[0].Is((":method", "GET"));
            headers1[1].Is((":scheme", "http"));
            headers1[2].Is((":path", "/"));
            headers1[3].Is((":authority", "www.example.com"));

            var dynamicTable = decoder.Table.DynamicTable.Table;
            dynamicTable.Count.Is(1);
            dynamicTable[0].Is((":authority", "www.example.com"));


            var source2 = "3f1a 7e10 77 77 77 2e 65 78 61 6d 70 6c 65 32 2e 63 6f 6d" // テーブルサイズ57, www.example2.com
                .HexToBytes(" ");
            var headers2 = decoder.Decode(source2);
            headers2.Count.Is(1);
            headers2[0].Is((":authority", "www.example2.com"));

            dynamicTable.Count.Is(0);
        }
    }
}
