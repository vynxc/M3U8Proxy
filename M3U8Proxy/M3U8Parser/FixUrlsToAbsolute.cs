using System.Text.RegularExpressions;

namespace M3U8Proxy.M3U8Parser;

public partial class M3U8Parser
{
    [GeneratedRegex(@"\?.+", RegexOptions.Compiled)]
    private static partial Regex GetParamsRegex();


    public static string FixAllUrls(string[] lines, string url, string prefix, string suffix, bool encrypted,
        bool isPlaylist, string baseUrl)
    {
        var parameters = GetParamsRegex().Match(url).Value;
        var uri = new Uri(url);
        const string uriPattern = @"URI=""([^""]+)""";
        var isEncodedSegments = ContainsString(lines, "EXT-X-KEY");
        if (isEncodedSegments != -1 &&false)//disabled for now
        {
            var isAes128 = lines[isEncodedSegments].Contains("AES-128");
            if (isAes128)
            {
                //change media sequence to be +1
                var mediaSequenceIndex = ContainsString(lines, "#EXT-X-MEDIA-SEQUENCE");
                if (mediaSequenceIndex != -1)
                    lines[mediaSequenceIndex] = Regex.Replace(lines[mediaSequenceIndex],
                        @"#EXT-X-MEDIA-SEQUENCE:(\d+)",
                        m => $"#EXT-X-MEDIA-SEQUENCE:{int.Parse(m.Groups[1].Value) + 1}");

                var keyUrl = Regex.Match(lines[isEncodedSegments], @"URI=""([^""]+)""").Groups[1].Value;
                Console.WriteLine(keyUrl);
                var key = GetKey(keyUrl).Result;
                if (key != null)
                {
                    var keyString = BitConverter.ToString(key).Replace("-", "").ToLower();
                    Console.WriteLine(keyString);
                    lines = InsertIntro(lines, baseUrl, keyString);
                }
            }
        }
        else if (encrypted && !isPlaylist&&isEncodedSegments==-1)
        {
            lines = InsertIntro(lines, baseUrl);
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var isUri = lines[i].Contains("URI");
            if (!isUri && (lines[i].StartsWith("#") || string.IsNullOrWhiteSpace(lines[i]))) continue;
            var uriContent = isUri ? Regex.Match(lines[i], uriPattern).Groups[1].Value : lines[i];
            if (!Uri.TryCreate(uriContent, UriKind.RelativeOrAbsolute, out var uriExtracted)) continue;
            var newUri = !uriExtracted.IsAbsoluteUri ? new Uri(uri, uriExtracted) : uriExtracted;
            var substitutedUri = $"{prefix}{EncodeUrl(newUri + parameters, encrypted && isPlaylist)}{suffix}";
            var test = Regex.Replace(lines[i], uriPattern, m => $"URI=\"{substitutedUri}\"");
            lines[i] = isUri ? test : substitutedUri;
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string EncodeUrl(string url, bool encrypted)
    {
        return Uri.EscapeDataString(encrypted ? AES.Encrypt(url) : url);
    }

    private static string[] InsertIntro(string[] lines, string baseUrl, string? key = null)
    {
        var lastIndex = 0;
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("#")) continue;
            lastIndex = i - 1;
            break;
        }

        var keyParam = key != null ? "?key=" + key : "";
        var lastLineText = lines[lastIndex];
        var testToInsert = new[]
        {
            "#EXTINF:6.266667,",
            baseUrl + "video/intro.ts" + keyParam,
            "#EXT-X-DISCONTINUITY",
            lastLineText
        };

        lines[lastIndex] = string.Join(Environment.NewLine, testToInsert);
        return lines;
    }

    
    private static int ContainsString(string[] lines, string toFind, int maxDepth = 10)
    {
        for (var i = 0; i < lines.Length && i < maxDepth; i++)
        {
            if (lines[i].Contains(toFind))
            {
                return i;
            }
        }
        return -1;
    }

    private static async Task<byte[]?> GetKey(string keyUrl)
    {
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(keyUrl);

            if (response.IsSuccessStatusCode)
            {
                var key = await response.Content.ReadAsByteArrayAsync();

                Console.WriteLine($"Key: {BitConverter.ToString(key)}");
                // The key should be 16 bytes for AES-128
                if (key.Length == 16) return key;

                throw new Exception("Invalid key length. The key length should be 16 bytes for AES-128 encryption.");
            }

            throw new Exception($"Failed to fetch encryption key. Status code {response.StatusCode}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return null;
        }
    }
}
