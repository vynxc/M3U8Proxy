using System.Net.Cache;
using Microsoft.Extensions.Caching.Memory;

namespace M3U8Proxy.RequestHandler;

public partial class ReqHandler
{
    private readonly HttpClient _webClient = new();

    private static HttpContext? HttpContextAccessor => new HttpContextAccessor().HttpContext;

    
    private static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

    public async Task<HttpResponseMessage?> MakeRequestV2(string url, Dictionary<string, string> headers)
    {
        // Check if the response is already cached
        if (Cache.TryGetValue(url, out HttpResponseMessage? cachedResponse))
        {
            return cachedResponse;
        }

        // If the response is not cached, make the network request
        var request = new HttpRequestMessage();
        request.RequestUri = new Uri(url);
        foreach (var header in headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }

        var response = await _webClient.SendAsync(request);

        // Cache the response for future use
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5)); // Cache the response for 5 minutes
        Cache.Set(url, response, cacheEntryOptions);

        return response;
    }
}