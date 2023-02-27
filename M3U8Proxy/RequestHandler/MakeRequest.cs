using RestSharp;

namespace M3U8Proxy.RequestHandler;

public partial class ReqHandler
{
    public IRestResponse MakeRequest(string url, Dictionary<string, string> headersDictionary)
    {
        var client = new RestClient(url) { Timeout = -1 };
        var request = new RestRequest(Method.GET);

        foreach (var header in headersDictionary) request.AddHeader(header.Key, header.Value);
        return client.Execute(request);
    }
}