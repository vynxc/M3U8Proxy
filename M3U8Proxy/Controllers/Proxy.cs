using AspNetCore.Proxy;
using AspNetCore.Proxy.Options;
using M3U8Proxy.M3U8Parser;
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
    private readonly M3U8Paser _paser = new();
    private readonly ReqHandler _reqHandler = new();

    [HttpGet("{url}/{headers?}/{type?}")]
    public Task GetProxy(string url, string? headers = "{}")
    {
        try
        {
            url = Uri.UnescapeDataString(url);
            headers = Uri.UnescapeDataString(headers!);

            var headersDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(headers);

            var options = HttpProxyOptionsBuilder.Instance
                .WithShouldAddForwardedHeaders(false)
                .WithBeforeSend((_, hrm) =>
                {
                    foreach (var header in CorsBlockedHeaders.List)
                    {
                        var headerToRemove =
                            hrm.Headers.FirstOrDefault(h =>
                                h.Key.Equals(header, StringComparison.InvariantCultureIgnoreCase)).Key;

                        if (headerToRemove != null) hrm.Headers.Remove(headerToRemove);
                    }

                    if (headersDictionary == null) return Task.CompletedTask;

                    foreach (var header in headersDictionary)
                    {
                        var headerToRemove =
                            hrm.Headers.FirstOrDefault(h =>
                                h.Key.Equals(header.Key, StringComparison.InvariantCultureIgnoreCase)).Key;

                        if (headerToRemove != null)
                            hrm.Headers.Remove(headerToRemove);

                        hrm.Headers.Add(header.Key, header.Value);
                    }


                    return Task.CompletedTask;
                })
                .WithHandleFailure(async (context, e) =>
                {
                    context.Response.StatusCode = context.Response.StatusCode;
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(e));
                })
                .WithAfterReceive((_, hrm) =>
                {
                    foreach (var header in CorsBlockedHeaders.List) hrm.Headers.Remove(header.ToLower());

                    return Task.CompletedTask;
                })
                .Build();
            return this.HttpProxyAsync(url, options);
        }
        catch (Exception e)
        {
            HttpContext.Response.StatusCode = 400;
            HttpContext.Response.ContentType = "application/json";
            HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(e));
            return Task.FromResult(0);
        }
    }
}
