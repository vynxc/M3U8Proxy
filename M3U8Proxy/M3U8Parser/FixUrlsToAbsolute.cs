using System.Text;
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

    public static string FixAllUrls(string[] lines, string url, string prefix, string suffix, bool isPlaylistM3U8)
    {
        var parameters = GetParamsRegex().Match(url).Value;
        var uri = new Uri(url);
        var baseUrl = $"{uri.Scheme}://{uri.Authority}";
        var index = url.LastIndexOf('/');

        var newLineBuilder = new StringBuilder();

        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].StartsWith("http") && !lines[i].StartsWith("#") && !string.IsNullOrWhiteSpace(lines[i]))
            {
                newLineBuilder.Clear();

                if (lines[i].StartsWith("/"))
                {
                    newLineBuilder.Append(baseUrl);
                    newLineBuilder.Append(lines[i]);
                    newLineBuilder.Append(parameters);
                }
                else
                {
                    newLineBuilder.Append(url[..(index + 1)]);
                    newLineBuilder.Append(lines[i]);
                    newLineBuilder.Append(parameters);
                }

                lines[i] = prefix + Uri.EscapeDataString(newLineBuilder.ToString()) + "/" + suffix;
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

}