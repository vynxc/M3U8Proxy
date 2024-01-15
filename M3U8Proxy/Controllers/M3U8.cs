using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
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
    private readonly string _encryptedUrl;

    public Proxy(IConfiguration configuration)
    {
        _baseUrl = configuration["ProxyUrl"]!;
        _proxyUrl = _baseUrl + "proxy/";
        _m3U8Url = _baseUrl + "proxy/m3u8/";
        _encryptedUrl = _baseUrl + "video/";
    }


    [OutputCache(PolicyName = "m3u8")]
    [HttpHead]
    [HttpGet]
    [Route("m3u8/{url}/{headers?}/{type?}")]
    public async Task<IActionResult> GetM3U8Full(string url, string? headers = "{}",
        [FromQuery] string? forcedHeadersProxy = "{}")
    {
        return await GetM3U8(url, headers, forcedHeadersProxy);
    }


    [OutputCache(PolicyName = "m3u8")]
    [HttpHead]
    [HttpGet]
    [Route("/video/{encryptedUrl}/{headers?}/{type?}")]
    public async Task<IActionResult> GetM3U8Spoofed(string encryptedUrl, string? headers = "{}",
        [FromQuery] string? forcedHeadersProxy = "{}")
    {
        var url = Decrypt(encryptedUrl);
        return await GetM3U8(url, headers, forcedHeadersProxy, true);
    }

    public string Encrypt(string data)
    {
        return Uri.EscapeDataString(AES.Encrypt(data));
    }

    public string Decrypt(string encryptedData)
    {
        return AES.Decrypt(Uri.UnescapeDataString(encryptedData));
    }

    public async Task<IActionResult> GetM3U8(string url, string? headers = "{}",
        [FromQuery] string? forcedHeadersProxy = "{}", bool encrypted = false)
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
            var prefix = isPlaylistM3U8 ? _m3U8Url : _proxyUrl;

            if (encrypted && isPlaylistM3U8)
                prefix = _encryptedUrl;

            var finalContent =
                M3U8Parser.M3U8Parser.FixAllUrls(lines, url, prefix, suffix, encrypted, isPlaylistM3U8, _baseUrl);

            return File(Encoding.UTF8.GetBytes(finalContent), "application/vnd.apple.mpegurl",
                $"{GenerateRandomId(10)}.m3u8");
        }
        catch (Exception e)
        {
            return BadRequest(JsonConvert.SerializeObject(e));
        }
    }


    private bool IsPlaylistM3U8(string[] lines)
    {
        var isPlaylistM3U8 = false;

        for (var i = 0; i < lines.Length && i < 10; i++)
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
