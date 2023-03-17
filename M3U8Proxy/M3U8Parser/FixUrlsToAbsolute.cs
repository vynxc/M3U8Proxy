using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;


namespace M3U8Proxy.M3U8Parser;

public partial class M3U8Paser
{
    private readonly Regex _getParamsRegex;

    [GeneratedRegex(@"\?.+", RegexOptions.Compiled)]
    private static partial Regex GetParamsRegex();
    public M3U8Paser()
    {
        _getParamsRegex = GetParamsRegex();
    }
  
    public static string FixAllUrls(string[] lines, string url,string prefix,string suffix)
         {
             Stopwatch stopwatch = new();
             stopwatch.Start();
             try
             {
                 var absoluteUrl = new StringBuilder();
                 var parameters = GetParamsRegex().Match(url).Value;
                 var uri = new Uri(url);
                 var baseUrl = $"{uri.Scheme}://{uri.Authority}";
                 var index = url.LastIndexOf('/'); 
                 for (var i = 0; i < lines.Length; i++)
                     if (!lines[i].StartsWith("http") && !lines[i].StartsWith("#") && !string.IsNullOrWhiteSpace(lines[i]))
                     {
                         if (lines[i].StartsWith("/"))
                         {
                             absoluteUrl.Clear();
                             absoluteUrl.Append(baseUrl);
                             absoluteUrl.Append(lines[i]);
                             absoluteUrl.Append(parameters);
                         }
                         else
                         {
                             absoluteUrl.Clear();
                             absoluteUrl.Append(url[..(index + 1)]);
                             absoluteUrl.Append(lines[i]);
                             absoluteUrl.Append(parameters);
                         }
     
                         lines[i] = prefix + Uri.EscapeDataString(absoluteUrl.ToString()) + "/" + suffix;
                     }
     
                 return string.Join(Environment.NewLine, lines);
             }
             finally
             {
                 stopwatch.Stop();
                 Console.WriteLine($"FixAllUrls: {stopwatch.ElapsedMilliseconds} ms");
             }
         }
}