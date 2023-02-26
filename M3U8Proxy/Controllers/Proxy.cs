using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using AspNetCore.Proxy;
using AspNetCore.Proxy.Options;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;

namespace M3U8Proxy.Controllers;

[EnableCors("corsPolicy")]
[ApiController]
[Route("[controller]")]
public class Proxy : Controller
{
    [HttpGet("hello")]
    public async Task<string> Hello()
    {
        return "Hello World!";
    }
    
    [HttpGet]
    [Route("base/{*.*url}")] 
    public Task ProxyTest([FromRoute]string url)
    {  
        try
        {
           
            var options = HttpProxyOptionsBuilder.Instance
                .WithShouldAddForwardedHeaders(false)
                .WithBeforeSend((c, hrm) =>
                {
                    foreach (var header in _corsBlockedHeaders)
                    {
                        var headerToRemove =
                            hrm.Headers.FirstOrDefault(h =>
                                h.Key.Equals(header, StringComparison.InvariantCultureIgnoreCase)).Key;

                        if (headerToRemove != null) hrm.Headers.Remove(headerToRemove);
                    }
                    return Task.CompletedTask;
                })
                .WithHandleFailure(async (context, e) =>
                {
                    context.Response.StatusCode = context.Response.StatusCode;
                    await context.Response.WriteAsync(e.Message);
                })
                .WithAfterReceive((c, hrm) =>
                {
                    foreach (var header in _corsBlockedHeaders) hrm.Headers.Remove(header.ToLower());

                    return Task.CompletedTask;
                })
                .Build();
            return this.HttpProxyAsync(url, options);
        }
        catch (Exception e)
        {
            //handle errors
            HttpContext.Response.StatusCode = 400;
            HttpContext.Response.ContentType = "application/json";
            HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(e.Message));
            return Task.FromResult(0);
        }
    }
    
    
    [HttpGet("{url}/{headers?}/{type?}")]
    public Task GetProxy(string url, string? headers = "{}")
    {  
        try
        {
            url = HttpUtility.UrlDecode(url);
            headers = HttpUtility.UrlDecode(headers);

            var headersDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(headers);

            var options = HttpProxyOptionsBuilder.Instance
                .WithShouldAddForwardedHeaders(false)
                .WithBeforeSend((c, hrm) =>
                {
                    foreach (var header in _corsBlockedHeaders)
                    {
                        var headerToRemove =
                            hrm.Headers.FirstOrDefault(h =>
                                h.Key.Equals(header, StringComparison.InvariantCultureIgnoreCase)).Key;

                        if (headerToRemove != null) hrm.Headers.Remove(headerToRemove);
                    }
                    if (headersDictionary != null)
                        foreach (var header in headersDictionary)
                        {
                            var headerToRemove =
                                hrm.Headers.FirstOrDefault(h =>
                                    h.Key.Equals(header.Key, StringComparison.InvariantCultureIgnoreCase)).Key;
                            if (headerToRemove != null) hrm.Headers.Remove(headerToRemove);
                            hrm.Headers.Add(header.Key, header.Value);
                        }

                    return Task.CompletedTask;
                })
                .WithHandleFailure(async (context, e) =>
                {
                    context.Response.StatusCode = context.Response.StatusCode;
                    await context.Response.WriteAsync(e.Message);
                })
                .WithAfterReceive((c, hrm) =>
                {
                    foreach (var header in _corsBlockedHeaders) hrm.Headers.Remove(header.ToLower());

                    return Task.CompletedTask;
                })
                .Build();
            return this.HttpProxyAsync(url, options);
        }
        catch (Exception e)
        {
            HttpContext.Response.StatusCode = 400;
            HttpContext.Response.ContentType = "application/json";
            HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(e.Message));
            return Task.FromResult(0);
        }
    }

    [HttpGet("m3u8/{url?}/{headers?}/{type?}")]
    public async Task<IActionResult> GetM3U8(string? url, string? headers = "{}")
    {
        var isPlaylistM3U8 = false;
        var listOfKeywords = new List<string> { "#EXT-X-STREAM-INF", "#EXT-X-I-FRAME-STREAM-INF" };
        var baseUrl = "https://proxy.vnxservers.com/";
        var proxyUrl = baseUrl + "proxy/";
        var m3U8Url = baseUrl + "proxy/m3u8/";

        try
        {
            
            url = HttpUtility.UrlDecode(url);

            headers = HttpUtility.UrlDecode(headers);

            if (string.IsNullOrEmpty(url))
                return BadRequest("URL missing or malformed.");

            var headersDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(headers);

            var response = MakeRequest(url, headersDictionary!);
            
            HttpContext.Response.StatusCode = (int) response.StatusCode;
          
            if(response.StatusCode != HttpStatusCode.OK)
                return BadRequest(response.Content);
            
            RemoveBlockedHeaders(response);
            AddResponseHeaders(response);

            var content = await FixUrlsInM3U8File(response, url);
            
            if (listOfKeywords.Any(keyword => content.Contains(keyword)))
                isPlaylistM3U8 = true;
            
            var modifiedContent = ModifyContent(content, isPlaylistM3U8 ? m3U8Url : proxyUrl, headers);

            return File(Encoding.UTF8.GetBytes(modifiedContent), "application/vnd.apple.mpegurl",
                $"{url.Substring(0, 15)}.m3u8");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    #region PrivateMethods
    private async Task<string> FixUrlsInM3U8File(IRestResponse response, string url)
    {
        string absoluteUrl = "";
        
        var lines = response.Content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        for (var i = 0; i < lines.Length; i++)
            if (!lines[i].StartsWith("http") && !lines[i].StartsWith("#") && !string.IsNullOrWhiteSpace(lines[i]))
            {
                
                if (lines[i].StartsWith("/"))
                {
                    string parameters = Regex.Match(url, @"\?.+").Value;
                    var Uri = new Uri(url);
                    
                    string baseUrl = string.Format("{0}://{1}", Uri.Scheme, Uri.Authority);
                    absoluteUrl = baseUrl + lines[i] + parameters;

                }
                else
                {
                    int index = url.LastIndexOf('/');
                    string parameters = Regex.Match(url, @"\?.+").Value;
                    absoluteUrl = url.Substring(0, index + 1) + lines[i] +parameters;
                }
                lines[i] = absoluteUrl;
            }
        return string.Join(Environment.NewLine, lines);
    }
    private void RemoveBlockedHeaders(IRestResponse response)
    {
        foreach (var header in _corsBlockedHeaders)
        {
            var headerToRemove =
                response.Headers.FirstOrDefault(h =>
                    h.Name.Equals(header, StringComparison.InvariantCultureIgnoreCase));
            if (headerToRemove != null) response.Headers.Remove(headerToRemove);
        }
    }

    private string ModifyContent(string content, string prefix, string headers)
    {
        var headersEncoded = HttpUtility.UrlEncode(headers);
        
        return Regex.Replace(content, @"https?:\/\/[^\s""]+",
            match => { return prefix + HttpUtility.UrlEncode(match.Value) + "/" + headersEncoded; });
    }
    private IRestResponse MakeRequest(string url, Dictionary<string, string> headersDictionary)
    {
        var client = new RestClient(url) { Timeout = -1 };
        var request = new RestRequest(Method.GET);

        foreach (var header in headersDictionary) request.AddHeader(header.Key, header.Value);
        return client.Execute(request);
    }
    private void AddResponseHeaders(IRestResponse response)
    {
        foreach (var header in response.Headers.Where(h =>
                     _corsBlockedHeaders.Contains(h.Name, StringComparer.InvariantCultureIgnoreCase)))
            response.Headers.Remove(header);
        
        foreach (var header in response.Headers.Where(h =>
                     h.Type == ParameterType.HttpHeader && h.Name != "Transfer-Encoding"))
            HttpContext.Response.Headers.Add(header.Name, (string)header.Value);
    }

    private readonly string[] _corsBlockedHeaders =
    {
        "Access-Control-Allow-Origin",
        "Access-Control-Allow-Methods",
        "Access-Control-Allow-Headers",
        "Access-Control-Max-Age",
        "Access-Control-Allow-Credentials",
        "Access-Control-Expose-Headers",
        "Access-Control-Request-Method",
        "Access-Control-Request-Headers",
        "Origin",
        "Vary",
        "Referer",
        "Server",
        "x-cache",
        "via",
        "x-amz-cf-pop",
        "x-amz-cf-id"
    };
    #endregion
}