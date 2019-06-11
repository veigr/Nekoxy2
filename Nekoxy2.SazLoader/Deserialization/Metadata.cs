using System;
using System.ComponentModel;
using System.IO.Compression;
using System.Xml.Serialization;

namespace Nekoxy2.SazLoader.Deserialization
{
    internal static partial class SessionExtensions
    {
        private static XmlSerializer serializer = new XmlSerializer(typeof(Metadata));

        /// <summary>
        /// メタデータに変換
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static Metadata ToMetadata(this ZipArchiveEntry entry)
        {
            using (var stream = entry.Open())
            {
                return (Metadata)serializer.Deserialize(entry.Open());
            }
        }
    }

    /// <summary>
    /// SAZ メタデータ
    /// </summary>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(ElementName = "Session", Namespace = "", IsNullable = false)]
    public sealed class Metadata
    {
        /// <remarks/>
        public SessionSessionTimers SessionTimers { get; set; }

        /// <remarks/>
        public object PipeInfo { get; set; }

        /// <remarks/>
        [XmlArrayItem("SessionFlag", IsNullable = false)]
        public SessionSessionFlag[] SessionFlags { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public byte SID { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public uint BitFlags { get; set; }
    }

    /// <remarks/>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public sealed class SessionSessionTimers
    {

        /// <remarks/>
        [XmlAttribute()]
        public DateTime ClientConnected { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public DateTime ClientBeginRequest { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public DateTime GotRequestHeaders { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public DateTime ClientDoneRequest { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public byte GatewayTime { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public byte DNSTime { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public byte TCPConnectTime { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public byte HTTPSHandshakeTime { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public DateTime ServerConnected { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public DateTime FiddlerBeginRequest { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public DateTime ServerGotRequest { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public DateTime ServerBeginResponse { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public System.DateTime GotResponseHeaders { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public DateTime ServerDoneResponse { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public DateTime ClientBeginResponse { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public DateTime ClientDoneResponse { get; set; }
    }

    /// <remarks/>
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    public sealed class SessionSessionFlag
    {

        /// <remarks/>
        [XmlAttribute()]
        public string N { get; set; }

        /// <remarks/>
        [XmlAttribute()]
        public string V { get; set; }
    }
}
