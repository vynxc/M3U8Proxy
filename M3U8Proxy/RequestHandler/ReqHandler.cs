namespace M3U8Proxy.RequestHandler;

public partial class ReqHandler
{
    private static HttpContext? HttpContextAccessor => new HttpContextAccessor().HttpContext;
}