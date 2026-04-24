using System;
using System.Text;

namespace NzbDrone.Core.Indexers.PyBookIrc
{
    // Shared encoding between the pybookirc indexer (producer) and download client (consumer).
    // A "release" is just an IRC command string; we encode it into a pseudo-URL so it rides
    // through Bookshelf's release pipeline like any other DownloadUrl and comes out intact.
    internal static class PyBookIrcRelease
    {
        public const string UrlScheme = "pybookirc://";

        public static string Encode(string ircCommand)
        {
            if (string.IsNullOrEmpty(ircCommand))
            {
                return null;
            }

            var bytes = Encoding.UTF8.GetBytes(ircCommand);
            var b64 = Convert.ToBase64String(bytes)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
            return UrlScheme + b64;
        }

        public static bool TryDecode(string url, out string ircCommand)
        {
            ircCommand = null;
            if (string.IsNullOrEmpty(url) || !url.StartsWith(UrlScheme, StringComparison.Ordinal))
            {
                return false;
            }

            var payload = url.Substring(UrlScheme.Length).Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            try
            {
                ircCommand = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                return !string.IsNullOrEmpty(ircCommand);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
