using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace M3U8Proxy.Controllers;

public class BaseController:Controller
{
    protected readonly ILogger<BaseController> _logger;

    public BaseController(ILogger<BaseController> logger)
    {
        _logger = logger;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var ipAddress = context.HttpContext.Connection.RemoteIpAddress.ToString();

        _logger.LogInformation("Request from IP address: {IpAddress}", ipAddress);

        base.OnActionExecuting(context);
    }
}