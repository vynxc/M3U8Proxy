using RestSharp;

namespace M3U8Proxy.RequestHandler;

public partial class ReqHandler
{
    private static HashSet<string> _headersToRemove =
        new(CorsBlockedHeaders.List, StringComparer.InvariantCultureIgnoreCase);
    
    public static void ManageResponseHeaders(IRestResponse response)
    {
        for (var i = response.Headers.Count - 1; i >= 0; i--)
        {
            var header = response.Headers[i];
            if (_headersToRemove.Contains(header.Name)) response.Headers.RemoveAt(i);
        }
        
        foreach (var header in response.Headers.Where(h =>
                     h.Type == ParameterType.HttpHeader && h.Name != "Transfer-Encoding"))
        {
            HttpContextAccessor?.Response.Headers.Remove(header.Name);
            HttpContextAccessor?.Response.Headers.Add(header.Name, (string)header.Value);
        }
        
    }
}