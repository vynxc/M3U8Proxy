using System.Text.RegularExpressions;

namespace M3U8Proxy.M3U8Parser;

public partial class M3U8Paser
{
    private readonly Regex _getParamsRegex;

    public M3U8Paser()
    {
        _getParamsRegex = GetParamsRegex();
    }

    [GeneratedRegex(@"\?.+", RegexOptions.Compiled)]
    private static partial Regex GetParamsRegex();

    public static string FixAllUrls(string[] lines, string url, string prefix, string suffix, bool addIntro,
        bool isPlaylistM3U8)
    {
        var parameters = GetParamsRegex().Match(url).Value;
        var uri = new Uri(url);
        var baseUrl = $"{uri.Scheme}://{uri.Authority}";
        var index = url.LastIndexOf('/');

        var lastIndex = 0;
        string newLine;
        if (addIntro && !isPlaylistM3U8)
        {
            for (var i = 0; i < lines.Length; i++)
                if (!lines[i].StartsWith("#"))
                {
                    lastIndex = i - 1;
                    break;
                }

            var lastLineText = lines[lastIndex];
            lines[lastIndex] = "#EXTINF:5.000000," +
                               Environment.NewLine +
                               "https://proxy.vnxservers.com/cdn/outputintro.ts" +
                               Environment.NewLine +
                               "#EXT-X-DISCONTINUITY" +
                               Environment.NewLine +
                               lastLineText;
        }


        for (var i = 0; i < lines.Length; i++)
            if (!lines[i].StartsWith("http") && !lines[i].StartsWith("#") && !string.IsNullOrWhiteSpace(lines[i]))
            {
                if (lines[i].StartsWith("/"))
                    newLine = baseUrl + lines[i] + parameters;
                else
                    newLine = url[..(index + 1)] + lines[i] + parameters;

                lines[i] = prefix + Uri.EscapeDataString(newLine) + "/" + suffix;
            }

        return string.Join(Environment.NewLine, lines);
    }
}