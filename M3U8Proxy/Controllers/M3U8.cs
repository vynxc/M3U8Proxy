using System.Diagnostics;
using System.Net;
using System.Text;
using M3U8Proxy.M3U8Parser;
using M3U8Proxy.RequestHandler;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Newtonsoft.Json;

namespace M3U8Proxy.Controllers;

public partial class Proxy
{
    private readonly List<string> _listOfKeywords = new() { "#EXT-X-STREAM-INF", "#EXT-X-I-FRAME-STREAM-INF" };
    private readonly string _proxyUrl;
    private readonly string _m3U8Url;

    public Proxy(IConfiguration configuration)
    {
        var baseUrl = configuration["ProxyUrl"]!;
        _proxyUrl = baseUrl + "proxy/";
        _m3U8Url = baseUrl + "proxy/m3u8/";
    }

    //[OutputCache(PolicyName = "m3u8")]
    [HttpHead]
    [HttpGet]
    [Route("m3u8/{url}/{headers?}/{type?}")]
    public async Task<IActionResult> GetM3U8(string url, string? headers = "{}", [FromQuery] string? forcedHeadersProxy = "{}")
    {
        var stopwatchv1 = new Stopwatch();
        stopwatchv1.Start();
        try
        {
            url = Uri.UnescapeDataString(url);

            headers = Uri.UnescapeDataString(headers!);

            if (string.IsNullOrEmpty(url))
                return BadRequest("URL Is Null Or Empty.");

            var headersDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(headers);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await _reqHandler.MakeRequestV2(url, headersDictionary!);
            stopwatch.Stop();
            Console.WriteLine(stopwatch.Elapsed.Milliseconds);
            if (response is not { StatusCode: HttpStatusCode.OK })
                return BadRequest(JsonConvert.SerializeObject("""{"message":"Error while fetching the m3u8 file"}"""));
            HttpContext.Response.StatusCode = (int)response.StatusCode;

            ReqHandler.ManageResponseHeadersV2(response);

            var content = await response.Content.ReadAsStringAsync();

            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var isPlaylistM3U8 = IsPlaylistM3U8(lines);

            var suffix = Uri.EscapeDataString(headers) + "?forcedHeadersProxy=" +
                         Uri.EscapeDataString(forcedHeadersProxy!);

            var finalContent = M3U8Paser.FixAllUrls(lines, url, isPlaylistM3U8 ? _m3U8Url : _proxyUrl, suffix,
                isPlaylistM3U8);

            return File(Encoding.UTF8.GetBytes(finalContent), "application/vnd.apple.mpegurl",
                $"{GenerateRandomId(10)}.m3u8");
        }
        catch (Exception e)
        {
            return BadRequest(JsonConvert.SerializeObject(e));
        }
        finally
        {
            stopwatchv1.Stop();
            Console.WriteLine(stopwatchv1.Elapsed.Milliseconds);
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