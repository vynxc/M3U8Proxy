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
        const string urIpattern = @"URI=""([^""]+)""";
        for (var i = 0; i < lines.Length; i++)
        {
            var uriContent = Regex.Match(lines[i], urIpattern).Groups[1].Value;
            Console.WriteLine(uriContent);

            Uri uriExtracted;

            if (Uri.TryCreate(uriContent, UriKind.RelativeOrAbsolute, out uriExtracted))
            {
                Uri baseUri = new Uri(baseUrl);
                Uri newUri;

                if (!uriExtracted.IsAbsoluteUri)
                {
                    newUri = new Uri(baseUri, uriExtracted);
                }
                else
                {
                    newUri = uriExtracted;
                }

                string substitutedUri = $"{prefix}{Uri.EscapeDataString(newUri.ToString())}/{suffix}";

                lines[i] = Regex.Replace(lines[i], urIpattern, m => $"URI=\"{substitutedUri}\"");
            }
            if (!lines[i].StartsWith("http") && !lines[i].StartsWith("#") && !string.IsNullOrWhiteSpace(lines[i]))
            {
                newLineBuilder.Clear();

                if (lines[i].StartsWith("/") && !lines[i].StartsWith("//"))
                {
                    newLineBuilder.Append(baseUrl);
                    newLineBuilder.Append(lines[i]);
                    newLineBuilder.Append(parameters);
                }
                else if (lines[i].StartsWith("//"))
                {
                    newLineBuilder.Append("https:" + lines[i]);
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
            else if (lines[i].StartsWith("http"))
            {
                lines[i] = prefix + Uri.EscapeDataString(lines[i]) + "/" + suffix;
            }
        }

        return string.Join(Environment.NewLine, lines);
    }
}
