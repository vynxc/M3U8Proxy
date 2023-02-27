using RestSharp;

namespace M3U8Proxy.RequestHandler;

public partial class ReqHandler
{
    public void RemoveBlockedHeaders(IRestResponse response)
    {
        foreach (var header in _corsBlockedHeaders)
        {
            var headerToRemove =
                response.Headers.FirstOrDefault(h =>
                    h.Name.Equals(header, StringComparison.InvariantCultureIgnoreCase));
            if (headerToRemove != null) response.Headers.Remove(headerToRemove);
        }
    }
}