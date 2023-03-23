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
    private readonly string _baseUrl;
    private readonly List<string> _listOfKeywords = new() { "#EXT-X-STREAM-INF", "#EXT-X-I-FRAME-STREAM-INF" };

    public Proxy(IConfiguration configuration)
    {
        _baseUrl = configuration["ProxyUrl"]!;
    }

    [OutputCache(PolicyName = "m3u8")]
    [HttpHead]
    [HttpGet]
    [Route("m3u8/{url}/{headers?}/{type?}")]
    public IActionResult GetM3U8(string url, string? headers = "{}",[FromQuery]string? forcedHeadersProxy= "{}",bool addIntro = true)
    {
        Stopwatch stopwatch = new();
        var proxyUrl = _baseUrl + "proxy/";
        var m3U8Url = _baseUrl + "proxy/m3u8/";
        stopwatch.Start();
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
                return BadRequest(JsonConvert.SerializeObject(response.ErrorMessage));

            ReqHandler.RemoveBlockedHeaders(response);
            
            ReqHandler.AddResponseHeaders(response);
            
            var lines = response.Content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            var isPlaylistM3U8 = IsPlaylistM3U8(lines);
            var suffix = Uri.EscapeDataString(headers)+"?forcedHeadersProxy="+Uri.EscapeDataString(forcedHeadersProxy!);
            var finalContent = M3U8Paser.FixAllUrls(lines, url, isPlaylistM3U8 ? m3U8Url : proxyUrl, suffix,addIntro);

            return File(Encoding.UTF8.GetBytes(finalContent), "application/vnd.apple.mpegurl",
                $"{GenerateRandomId(10)}.m3u8");
        }
        catch (Exception e)
        {
            return BadRequest(JsonConvert.SerializeObject(e));
        }
        finally
        {
            stopwatch.Stop();
            Console.WriteLine($"\n GetM3U8: {stopwatch.ElapsedMilliseconds} ms");
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

            if (isPlaylistM3U8)
            {
                break;
            }
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