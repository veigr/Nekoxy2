using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Nekoxy2.Helpers.Windows
{
    public sealed class IEStyleProxySettings
    {
        public string HttpHost { get; set; }

        public ushort? HttpPort { get; set; }

        public string HttpsHost { get; set; }

        public ushort? HttpsPort { get; set; }

        public string FtpHost { get; set; }

        public ushort? FtpPort { get; set; }

        public string SocksHost { get; set; }

        public ushort? SocksPort { get; set; }

        public bool? IsUseHttpProxyForAllProtocols { get; set; }

        private static Regex pattern = new Regex("(?<scheme>http|https|ftp|socks)=(?<host>[^:]*)(:(?<port>\\d+))?",
            RegexOptions.Singleline | RegexOptions.Compiled);

        public static IEStyleProxySettings Parse(WinHttpCurrentUserIEProxyConfig config)
            => Parse(config.Proxy);

        public static IEStyleProxySettings Parse(string ieStyleSettings)
        {
            var value = new IEStyleProxySettings();

            if (string.IsNullOrWhiteSpace(ieStyleSettings))
                return value;

            if (!ieStyleSettings.Contains("="))
            {
                // すべてのプロトコルに～
                var setting = ieStyleSettings.Split(':');
                value = new IEStyleProxySettings()
                {
                    IsUseHttpProxyForAllProtocols = true,
                    HttpHost = setting[0],
                };
                if (1 < setting.Length && ushort.TryParse(setting[1], out var httpPort))
                    value.HttpPort = httpPort;
                return value;
            }

            var settings = ieStyleSettings.Split(';');
            foreach (var setting in settings)
            {
                var groups = pattern.Match(setting).Groups;
                if (groups.Count < 1) continue;
                switch (groups["scheme"].Value)
                {
                    case "http":
                        value.HttpHost = groups["host"].Value;
                        if (ushort.TryParse(groups["port"].Value, out var httpPort))
                            value.HttpPort = httpPort;
                        break;
                    case "https":
                        value.HttpsHost = groups["host"].Value;
                        if (ushort.TryParse(groups["port"].Value, out var httpsPort))
                            value.HttpsPort = httpsPort;
                        break;
                    case "ftp":
                        value.FtpHost = groups["host"].Value;
                        if (ushort.TryParse(groups["port"].Value, out var ftpPort))
                            value.FtpPort = ftpPort;
                        break;
                    case "socks":
                        value.SocksHost = groups["host"].Value;
                        if (ushort.TryParse(groups["port"].Value, out var socksPort))
                            value.SocksPort = socksPort;
                        break;
                    default:
                        break;
                }
            }
            return value;
        }
    }

    public static partial class IEStyleProxySettingsExtensions
    {
        public static IEStyleProxySettings ToIEStyleProxySettings(this WinHttpCurrentUserIEProxyConfig config)
            => IEStyleProxySettings.Parse(config);

        public static string ToIEStyleString(this IEStyleProxySettings baseSettings, IEStyleProxySettings overrideSettings = null)
        {
            var settings = baseSettings.Override(overrideSettings);
            var values = new List<string>();
            values.TryAddIEStyleSetting("http", settings.HttpHost, settings.HttpPort);

            if (settings.IsUseHttpProxyForAllProtocols == true)
            {
                values.TryAddIEStyleSetting("https", settings.HttpHost, settings.HttpPort);
                values.TryAddIEStyleSetting("ftp", settings.HttpHost, settings.HttpPort);
            }
            else
            {
                values.TryAddIEStyleSetting("https", settings.HttpsHost, settings.HttpsPort);
                values.TryAddIEStyleSetting("ftp", settings.FtpHost, settings.FtpPort);
                values.TryAddIEStyleSetting("socks", settings.SocksHost, settings.SocksPort);
            }
            return string.Join(";", values);
        }

        private static IEStyleProxySettings Override(this IEStyleProxySettings baseSettings, IEStyleProxySettings overrideSettings)
        {
            if (overrideSettings == default)
                return baseSettings;
            return new IEStyleProxySettings
            {
                HttpHost = overrideSettings.HttpHost ?? baseSettings.HttpHost,
                HttpPort = overrideSettings.HttpPort ?? baseSettings.HttpPort,
                HttpsHost = overrideSettings.HttpsHost ?? baseSettings.HttpsHost,
                HttpsPort = overrideSettings.HttpsPort ?? baseSettings.HttpsPort,
                FtpHost = overrideSettings.FtpHost ?? baseSettings.FtpHost,
                FtpPort = overrideSettings.FtpPort ?? baseSettings.FtpPort,
                SocksHost = overrideSettings.SocksHost ?? baseSettings.SocksHost,
                SocksPort = overrideSettings.SocksPort ?? baseSettings.SocksPort,
                IsUseHttpProxyForAllProtocols = overrideSettings.IsUseHttpProxyForAllProtocols ?? baseSettings.IsUseHttpProxyForAllProtocols,
            };
        }

        private static void TryAddIEStyleSetting(this IList<string> list, string scheme, string host, ushort? port)
        {
            if (string.IsNullOrWhiteSpace(host))
                return;
            list.Add(scheme.ToIEStyle(host, port));
        }

        private static string ToIEStyle(this string scheme, string host, ushort? port)
            => port.IsEmpty() ? $"{scheme}={host}" : $"{scheme}={host}:{port}";

        private static bool IsEmpty(this ushort? value)
            => value == default || value == 0;
    }
}
