using System.Text;
using System.Text.RegularExpressions;
using RestSharp;

namespace M3U8Proxy.M3U8Parser;

public partial class M3U8Paser
{
    public string FixUrlsInM3U8File1(IRestResponse response, string url)
    {
        var absoluteUrl = "";

        var lines = response.Content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        for (var i = 0; i < lines.Length; i++)
            if (!lines[i].StartsWith("http") && !lines[i].StartsWith("#") && !string.IsNullOrWhiteSpace(lines[i]))
            {
                if (lines[i].StartsWith("/"))
                {
                    var parameters = Regex.Match(url, @"\?.+").Value;
                    var Uri = new Uri(url);

                    var baseUrl = string.Format("{0}://{1}", Uri.Scheme, Uri.Authority);
                    absoluteUrl = baseUrl + lines[i] + parameters;
                }
                else
                {
                    var index = url.LastIndexOf('/');
                    var parameters = Regex.Match(url, @"\?.+").Value;
                    absoluteUrl = url.Substring(0, index + 1) + lines[i] + parameters;
                }

                lines[i] = absoluteUrl;
            }

        return string.Join(Environment.NewLine, lines);
    }

    public string FixUrlsInM3U8File(IRestResponse response, string url)
    {
        var absoluteUrl = new StringBuilder();
        var lines = response.Content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var parameters = Regex.Match(url, @"\?.+").Value;
        var Uri = new Uri(url);
        var baseUrl = string.Format("{0}://{1}", Uri.Scheme, Uri.Authority);
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
                    absoluteUrl.Append(url.Substring(0, index + 1));
                    absoluteUrl.Append(lines[i]);
                    absoluteUrl.Append(parameters);
                }

                lines[i] = absoluteUrl.ToString();
            }

        return string.Join(Environment.NewLine, lines);
    }
}