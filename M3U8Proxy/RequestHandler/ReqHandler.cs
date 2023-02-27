namespace M3U8Proxy.RequestHandler;

public partial class ReqHandler
{
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


    private static HttpContext? HttpContextAccessor => new HttpContextAccessor().HttpContext;
}