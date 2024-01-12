using System.Reflection;
using AspNetCore.Proxy;
using AspNetCore.Proxy.Options;
using M3U8Proxy.RequestHandler.AfterReceive;
using M3U8Proxy.RequestHandler.BeforeSend;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace M3U8Proxy.Controllers;

[EnableCors("corsPolicy")]
[ApiController]
public class Base : Controller
{
    private readonly Assembly _assembly;

   public Base()
    {
        _assembly = Assembly.GetExecutingAssembly();
    }
    [HttpHead]
    [HttpPost]
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
                .WithBeforeSend((res,hrm)=>
                {
                    BeforeSend.RemoveHeaders(hrm); 
                    hrm.Headers.Remove("Host");
                    
                    return Task.CompletedTask; 
                })
                .WithHandleFailure(async (context, e) =>
                {
                    context.Response.StatusCode = context.Response.StatusCode;
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(e));
                })
                .WithAfterReceive((res, hrm) =>
                {
                    AfterReceive.RemoveHeaders(hrm);
                    hrm.Headers.Remove("Cross-Origin-Resource-Policy");
                    hrm.Headers.Add("Cross-Origin-Resource-Policy","*");
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
    
    [Route("/video/intro.ts")]
    public IActionResult Intro()
    {
        var resourceName = "M3U8Proxy.Intro.segment0.ts";
        var stream = _assembly.GetManifestResourceStream(resourceName);

        if (stream != null)
        {
            return File(stream, "video/mp2t");
        }

        return NotFound();
    }
}