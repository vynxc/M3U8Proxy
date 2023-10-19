using System.Net.Http.Headers;
using System.Web;
using AspNetCore.Proxy;
using AspNetCore.Proxy.Options;
using M3U8Proxy.RequestHandler;
using M3U8Proxy.RequestHandler.AfterReceive;
using M3U8Proxy.RequestHandler.BeforeSend;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace M3U8Proxy.Controllers;

[EnableCors("corsPolicy")]
[ApiController]
[Route("[controller]")]
public partial class Proxy : Controller
{
    private readonly ReqHandler _reqHandler = new();

    [HttpHead]
    [HttpGet]
    [Route("{url}/{headers?}/{type?}")]
    public Task GetProxy(string url, string? headers = "{}", string? forcedHeadersProxy = "{}")
    {
        try
        {
            url = HttpUtility.UrlDecode(url);
            headers = HttpUtility.UrlDecode(headers!);
            forcedHeadersProxy = HttpUtility.UrlDecode(forcedHeadersProxy!);

            var forcedHeadersProxyDictionary =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(forcedHeadersProxy);
            var headersDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(headers);

            var options = HttpProxyOptionsBuilder.Instance
                .WithShouldAddForwardedHeaders(false)
                .WithBeforeSend((_, hrm) =>
                {
                    hrm.Headers.Remove("Host");
                    hrm.Headers.Remove("Cross-Origin-Resource-Policy");
                    hrm.Headers.Add("Cross-Origin-Resource-Policy","*");
                    if (headersDictionary == null) return Task.CompletedTask;
                    BeforeSend.RemoveHeaders(hrm);
                    BeforeSend.AddHeaders(headersDictionary, hrm);
                    return Task.CompletedTask;
                })
                .WithHandleFailure(async (context, e) =>
                {
                    context.Response.StatusCode = context.Response.StatusCode;
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(e));
                })
                .WithAfterReceive((_, hrm) =>
                {
                    AfterReceive.RemoveHeaders(hrm);
                    AfterReceive.AddForcedHeaders(forcedHeadersProxyDictionary, hrm);
                    return Task.CompletedTask;
                })
                .Build();
            return this.HttpProxyAsync(url, options);
        }
        catch (Exception e)
        {
            HandleExceptionResponse(e);
            return Task.FromResult(0);
        }
    }

    private readonly HttpClientHandler _handler = new()
    {
        AllowAutoRedirect = false
    };

    [Route("grabRedirect/{url}/{headers?}")]
    public async Task<IActionResult> Demo(string url, string? headers = "{}")
    {
        string? redirectedUrl = null;
        try
        {
            url = HttpUtility.UrlDecode(url);
            headers = HttpUtility.UrlDecode(headers!);
            var headersDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(headers);

            var client = new HttpClient(_handler);
            client.Timeout = TimeSpan.FromMinutes(15);
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            if (headersDictionary != null)
                foreach (var keyValuePair in headersDictionary)
                    request.Headers.Add(keyValuePair.Key, keyValuePair.Value);
            var response = await client.SendAsync(request);

            if (response.Headers.Location != null)
                redirectedUrl = response.Headers.Location.AbsoluteUri;

            return Ok(new
            {
                url = redirectedUrl!=null? _baseUrl+redirectedUrl : null,
            });
        }
        catch (Exception e)
        {
            return BadRequest(JsonConvert.SerializeObject(e));
        }
    }

    private void HandleExceptionResponse(Exception e)
    {
        HttpContext.Response.StatusCode = 400;
        HttpContext.Response.ContentType = "application/json";
        HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(e));
    }
}