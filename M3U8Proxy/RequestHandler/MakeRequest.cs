using System.Diagnostics;
using RestSharp;

namespace M3U8Proxy.RequestHandler;

public partial class ReqHandler
{
    private readonly RestClient _client = new();

    public IRestResponse MakeRequest(string url, Dictionary<string, string> headersDictionary)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();
        try
        {
            _client.BaseUrl = new Uri(url);
            var request = new RestRequest(Method.GET);

            foreach (var header in headersDictionary) request.AddHeader(header.Key, header.Value);
            return _client.Execute(request);
        }
        finally
        {
            stopwatch.Stop();
            Console.WriteLine($"\n MakeRequest: {stopwatch.ElapsedMilliseconds} ms");
        }
       
    }
}