using AspNetCore.Proxy;
using AspNetCore.Proxy.Options;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace M3U8Proxy.Controllers;

[EnableCors("corsPolicy")]
[ApiController]
public class Base : Controller
{
    private readonly FileStream _stream = System.IO.File.OpenRead(@"/root/Videos/outputintro.ts");
    
    [HttpHead]
    [HttpGet]
    [Route("/{**url}")]
    public Task ProxyTest(string url)
    {
        var query = Request.QueryString;
        if (query.HasValue) url += query;
        try
        {
            var options = HttpProxyOptionsBuilder.Instance
                .WithShouldAddForwardedHeaders(false)
                .WithHandleFailure(async (context, e) =>
                {
                    context.Response.StatusCode = context.Response.StatusCode;
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(e));
                })
                .WithAfterReceive((res, hrm) =>
                {
                    foreach (var header in CorsBlockedHeaders.List) hrm.Headers.Remove(header.ToLower());
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
            HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(e));
            return Task.FromResult(0);
        }
    }
}