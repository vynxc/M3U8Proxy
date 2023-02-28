using RestSharp;

namespace M3U8Proxy.RequestHandler;

public partial class ReqHandler
{
    public void AddResponseHeaders(IRestResponse response)
    {
        foreach (var header in response.Headers.Where(h =>
                     _corsBlockedHeaders.Contains(h.Name, StringComparer.InvariantCultureIgnoreCase)))
            response.Headers.Remove(header);

        foreach (var header in response.Headers.Where(h =>
                     h.Type == ParameterType.HttpHeader && h.Name != "Transfer-Encoding"))
        {
            HttpContextAccessor?.Response.Headers.Remove(header.Name);
            HttpContextAccessor?.Response.Headers.Add(header.Name, (string)header.Value);

        }
    }
    
}