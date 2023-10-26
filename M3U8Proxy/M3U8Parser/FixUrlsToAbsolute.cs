using System.Text;
using System.Text.RegularExpressions;

namespace M3U8Proxy.M3U8Parser;

public partial class M3U8Paser
{

    [GeneratedRegex(@"\?.+", RegexOptions.Compiled)]
    private static partial Regex GetParamsRegex();

    public static string FixAllUrls(string[] lines, string url, string prefix, string suffix)
    {
        var parameters = GetParamsRegex().Match(url).Value;
        var uri = new Uri(url);
        var baseUrl = $"{uri.Scheme}://{uri.Authority}";
        const string uriPattern =  @"URI=""([^""]+)""";
        for (var i = 0; i < lines.Length; i++)
        {
            var isUri = lines[i].Contains("URI");
            if (!isUri && (lines[i].StartsWith("#") || string.IsNullOrWhiteSpace(lines[i]))) continue;
            var uriContent = isUri?Regex.Match(lines[i], uriPattern).Groups[1].Value:lines[i];
            if (!Uri.TryCreate(uriContent, UriKind.RelativeOrAbsolute, out var uriExtracted)) continue;
            var baseUri = new Uri(baseUrl);
            var newUri = !uriExtracted.IsAbsoluteUri ? new Uri(baseUri, uriExtracted) : uriExtracted;
            var substitutedUri = $"{prefix}{Uri.EscapeDataString(newUri+parameters)}{suffix}";
            var test = Regex.Replace(lines[i], uriPattern, m => $"URI=\"{substitutedUri}\"");
            lines[i] = isUri?test:substitutedUri;
        }

        return string.Join(Environment.NewLine, lines);
    }
}