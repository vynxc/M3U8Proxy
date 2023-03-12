using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace M3U8Proxy.M3U8Parser;

public partial class M3U8Paser
{
    private readonly Regex _fixUrlRegex;


    public M3U8Paser()
    {
        _fixUrlRegex = FixUrlRegex();
        _getParamsRegex = GetParamsRegex();
    }

    [GeneratedRegexAttribute(@"https?:\/\/[^\s""]+")]
    private static partial Regex FixUrlRegex();

   
    public string ModifyContent(string content, string prefix, string headers)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        try
        {
            var headersEncoded = Uri.EscapeDataString(headers);
            var sb = new StringBuilder(content.Length);
            var lastIndex = 0;
            foreach (Match match in FixUrlRegex().Matches(content))
            {
                sb.Append(content, lastIndex, match.Index - lastIndex);
                sb.Append(prefix);
                sb.Append(Uri.EscapeDataString(match.Value));
                sb.Append("/");
                sb.Append(headersEncoded);
                lastIndex = match.Index + match.Length;
            }

            sb.Append(content, lastIndex, content.Length - lastIndex);
            return sb.ToString();
        }
        finally
        {
            stopwatch.Stop();
            Console.WriteLine($"ModifyContent: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}