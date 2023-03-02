using System.Text.RegularExpressions;
using System.Web;

namespace M3U8Proxy.M3U8Parser;

public partial class M3U8Paser
{
    private readonly Regex _regex = new(@"https?:\/\/[^\s""]+", RegexOptions.Compiled);

    public string ModifyContent(string content, string prefix, string headers)
    {
        var headersEncoded = HttpUtility.UrlEncode(headers);
        return _regex.Replace(content,
            match => prefix + HttpUtility.UrlEncode(match.Value) + "/" + headersEncoded);
    }
}