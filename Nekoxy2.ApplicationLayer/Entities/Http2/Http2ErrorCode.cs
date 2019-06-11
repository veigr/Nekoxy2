namespace Nekoxy2.ApplicationLayer.Entities.Http2
{
    internal enum Http2ErrorCode : uint
    {
        // https://www.iana.org/assignments/http2-parameters/http2-parameters.xhtml#error-code
        NoError = 0x0,
        ProtocolError = 0x1,
        InternalError = 0x2,
        FlowControlError = 0x3,
        SettingsTimeout = 0x4,
        StreamClosed = 0x5,
        FrameSizeError = 0x6,
        RefusedStream = 0x7,
        Cancel = 0x8,
        CompressionError = 0x9,
        ConnectError = 0xA,
        EnhanceYourCalm = 0xB,
        InadequateSecurity = 0xC,
        Http11Required = 0xD,
    }
}
