namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    internal enum Http2FrameType : byte
    {
        // https://www.iana.org/assignments/http2-parameters/http2-parameters.xhtml#frame-type
        Data = 0x0,         // for Stream
        Headers = 0x1,      // for Stream
        Priority = 0x2,     // for Stream
        RstStream = 0x3,    // for Stream
        Settings = 0x4,     // for Connection
        PushPromise = 0x5,  // for Stream
        Ping = 0x6,         // for Stream
        Goaway = 0x7,       // for Connection
        WindowUpdate = 0x8, // for Stream and Connection
        Continuation = 0x9, // for Stream
        Altsvc = 0xA,       // for Stream and Connection
        Origin = 0xC,       // for Connection
    }
}
