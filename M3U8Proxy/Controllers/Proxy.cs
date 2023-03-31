using AspNetCore.Proxy;
using AspNetCore.Proxy.Options;
using M3U8Proxy.RequestHandler;
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
            url = Uri.UnescapeDataString(url);
            headers = Uri.UnescapeDataString(headers!);
            forcedHeadersProxy = Uri.UnescapeDataString(forcedHeadersProxy!);

            var forcedHeadersProxyDictionary =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(forcedHeadersProxy);
            var headersDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(headers);

            var options = HttpProxyOptionsBuilder.Instance
                .WithShouldAddForwardedHeaders(false)
                .WithBeforeSend((_, hrm) =>
                {
                    BeforeSendRemoveCors(hrm);

                    if (headersDictionary == null) return Task.CompletedTask;

                    BeforeSendAddHeaders(headersDictionary, hrm);

                    return Task.CompletedTask;
                })
                .WithHandleFailure(async (context, e) =>
                {
                    context.Response.StatusCode = context.Response.StatusCode;
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(e));
                })
                .WithAfterReceive((_, hrm) =>
                {
                    AfterReceiveRemoveCors(hrm);
                    AfterReceiveAddForcedHeaders(forcedHeadersProxyDictionary, hrm);
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

    private static void AfterReceiveAddForcedHeaders(Dictionary<string, string>? forcedHeadersProxyDictionary,
        HttpResponseMessage hrm)
    {
        foreach (var header in forcedHeadersProxyDictionary)
        {
            var headerToRemove =
                hrm.Content.Headers.FirstOrDefault(h =>
                    h.Key.Equals(header.Key, StringComparison.InvariantCultureIgnoreCase)).Key;

            if (headerToRemove != null)
                hrm.Content.Headers.Remove(headerToRemove);
            hrm.Content.Headers.Add(header.Key, header.Value);
        }
    }

    private void HandleExceptionResponse(Exception e)
    {
        HttpContext.Response.StatusCode = 400;
        HttpContext.Response.ContentType = "application/json";
        HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(e));
    }

    private static void AfterReceiveRemoveCors(HttpResponseMessage hrm)
    {
        foreach (var header in CorsBlockedHeaders.List) hrm.Headers.Remove(header.ToLower());
    }

    private static void BeforeSendAddHeaders(Dictionary<string, string> headersDictionary, HttpRequestMessage hrm)
    {
        foreach (var header in headersDictionary)
        {
            var headerToRemove =
                hrm.Headers.FirstOrDefault(h =>
                    h.Key.Equals(header.Key, StringComparison.InvariantCultureIgnoreCase)).Key;

            if (headerToRemove != null)
                hrm.Headers.Remove(headerToRemove);

            hrm.Headers.Add(header.Key, header.Value);
        }
    }

    private static void BeforeSendRemoveCors(HttpRequestMessage hrm)
    {
        foreach (var header in CorsBlockedHeaders.List)
        {
            var headerToRemove =
                hrm.Headers.FirstOrDefault(h =>
                    h.Key.Equals(header, StringComparison.InvariantCultureIgnoreCase)).Key;

            if (headerToRemove != null) hrm.Headers.Remove(headerToRemove);
        }
    }
}