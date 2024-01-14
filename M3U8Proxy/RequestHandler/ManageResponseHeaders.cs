namespace M3U8Proxy.RequestHandler;

public partial class ReqHandler
{
    private static HashSet<string> _headersToRemove =
        new(CorsBlockedHeaders.List, StringComparer.InvariantCultureIgnoreCase);

    public static void ManageResponseHeadersV2(HttpResponseMessage response)
    {
        foreach (var header in response.Headers)
        {
            if (_headersToRemove.Contains(header.Key) || header.Key == "Transfer-Encoding") continue;
            HttpContextAccessor?.Response.Headers.Remove(header.Key);
            HttpContextAccessor?.Response.Headers.Add(header.Key, string.Join(",", header.Value));
        }
    }
}