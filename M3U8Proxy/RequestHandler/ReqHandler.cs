using System.Net.Cache;
using Microsoft.Extensions.Caching.Memory;

namespace M3U8Proxy.RequestHandler;

public partial class ReqHandler
{
    private readonly HttpClient _webClient = new();

    private static HttpContext? HttpContextAccessor => new HttpContextAccessor().HttpContext;


    public async Task<HttpResponseMessage?> MakeRequestV2(string url, Dictionary<string, string> headers)
    {
        var request = new HttpRequestMessage();
        request.RequestUri = new Uri(url);
        foreach (var header in headers) request.Headers.Add(header.Key, header.Value);

        var response = await _webClient.SendAsync(request);

        return response;
    }
}