using System.Diagnostics;
using System.Net;
using System.Text;
using System.Web;
using AngleSharp;
using M3U8Proxy.M3U8Parser;
using M3U8Proxy.RequestHandler;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Newtonsoft.Json;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace M3U8Proxy.Controllers;

public partial class Proxy
{
    private readonly List<string> _listOfKeywords = new() { "#EXT-X-STREAM-INF", "#EXT-X-I-FRAME-STREAM-INF" };
    private readonly string _proxyUrl;
    private readonly string _baseUrl;
    private readonly string _m3U8Url;

    public Proxy(IConfiguration configuration)
    {
        _baseUrl = configuration["ProxyUrl"]!;
        _proxyUrl = _baseUrl + "proxy/";
        _m3U8Url = _baseUrl + "proxy/m3u8/";
    }

    [OutputCache(PolicyName = "m3u8")]
    [HttpHead]
    [HttpGet]
    [Route("m3u8/{url}/{headers?}/{type?}")]
    public async Task<IActionResult> GetM3U8(string url, string? headers = "{}",
        [FromQuery] string? forcedHeadersProxy = "{}",[FromQuery] bool? file=true)
    {
        
        try
        {
            url = HttpUtility.UrlDecode(url);

            headers = HttpUtility.UrlDecode(headers!);

            if (string.IsNullOrEmpty(url))
                return BadRequest("URL Is Null Or Empty.");

            var headersDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(headers);
            var response = await _reqHandler.MakeRequestV2(url, headersDictionary!);
            if (response is not { StatusCode: HttpStatusCode.OK })
                return BadRequest(JsonConvert.SerializeObject("""{"message":"Error while fetching the m3u8 file"}"""));
            HttpContext.Response.StatusCode = (int)response.StatusCode;
            ReqHandler.ManageResponseHeadersV2(response);
            var content = await response.Content.ReadAsStringAsync();
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var isPlaylistM3U8 = IsPlaylistM3U8(lines);
            var forcedHeadersString = forcedHeadersProxy == "{}"
                ? ""
                : "?forcedHeadersProxy=" + Uri.EscapeDataString(forcedHeadersProxy!);
            var headersString = headers == "{}" ? "" : Uri.EscapeDataString(headers!);
            var suffix = headersString + forcedHeadersString;
            if (suffix != "") suffix = "/" + suffix;
            var finalContent = M3U8Paser.FixAllUrls(lines, url, isPlaylistM3U8 ? _m3U8Url : _proxyUrl, suffix);
            if(file == true)
            {
                return File(Encoding.UTF8.GetBytes(finalContent), "application/vnd.apple.mpegurl",
                    $"{GenerateRandomId(10)}.m3u8");
            }
            
            return Content(finalContent,"application/vnd.apple.mpegurl");
        }
        catch (Exception e)
        {
            return BadRequest(JsonConvert.SerializeObject(e));
        }

    }

    private bool IsPlaylistM3U8(string[] lines)
    {
        var isPlaylistM3U8 = false;

        for (var i = 0; i < lines.Length || i < 10; i++)
        {
            for (var j = 0; j < _listOfKeywords.Count; j++)
            {
                if (lines[i].IndexOf(_listOfKeywords[j], StringComparison.OrdinalIgnoreCase) < 0) continue;
                isPlaylistM3U8 = true;
                break;
            }

            if (isPlaylistM3U8) break;
        }

        return isPlaylistM3U8;
    }

    public static string GenerateRandomId(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
