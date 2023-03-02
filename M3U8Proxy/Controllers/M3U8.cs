using System.Net;
using System.Text;
using M3U8Proxy.M3U8Parser;
using M3U8Proxy.RequestHandler;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace M3U8Proxy.Controllers;

public partial class Proxy
{
    [ResponseCache(Duration = 30, Location = ResponseCacheLocation.Any)]
    [HttpGet("m3u8/{url}/{headers?}/{type?}")]
    public IActionResult GetM3U8(string url, string? headers = "{}")
    {
        var listOfKeywords = new List<string> { "#EXT-X-STREAM-INF", "#EXT-X-I-FRAME-STREAM-INF" };
        var baseUrl = "https://proxy.vnxservers.com/";
        var proxyUrl = baseUrl + "proxy/";
        var m3U8Url = baseUrl + "proxy/m3u8/";

        try
        {
            url = Uri.UnescapeDataString(url);

            headers = Uri.UnescapeDataString(headers!);

            if (string.IsNullOrEmpty(url))
                return BadRequest("URL missing or malformed.");

            var headersDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(headers);
            var response = _reqHandler.MakeRequest(url, headersDictionary!);

            HttpContext.Response.StatusCode = (int)response.StatusCode;

            if (response.StatusCode != HttpStatusCode.OK)
                return BadRequest(JsonConvert.SerializeObject(response));

            ReqHandler.RemoveBlockedHeaders(response);
            ReqHandler.AddResponseHeaders(response);

            var content = M3U8Paser.FixUrls(response, url);

            var isPlaylistM3U8 = content.IndexOf(listOfKeywords[0], StringComparison.OrdinalIgnoreCase) >= 0
                                 || content.IndexOf(listOfKeywords[1], StringComparison.OrdinalIgnoreCase) >= 0;

            var modifiedContent = _paser.ModifyContent(content, isPlaylistM3U8 ? m3U8Url : proxyUrl, headers);

            return File(Encoding.UTF8.GetBytes(modifiedContent), "application/vnd.apple.mpegurl",
                $"{url.Substring(0, 15)}.m3u8");
        }
        catch (Exception e)
        {
            return BadRequest(JsonConvert.SerializeObject(e));
        }
    }
}