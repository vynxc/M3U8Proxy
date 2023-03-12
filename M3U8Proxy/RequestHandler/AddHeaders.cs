using System.Diagnostics;
using RestSharp;

namespace M3U8Proxy.RequestHandler;

public partial class ReqHandler
{
    public static void AddResponseHeaders(IRestResponse response)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();
        try
        {
            foreach (var header in response.Headers.Where(h =>
                         CorsBlockedHeaders.List.Contains(h.Name, StringComparer.InvariantCultureIgnoreCase)))
                response.Headers.Remove(header);

            foreach (var header in response.Headers.Where(h =>
                         h.Type == ParameterType.HttpHeader && h.Name != "Transfer-Encoding"))
            {
                HttpContextAccessor?.Response.Headers.Remove(header.Name);
                HttpContextAccessor?.Response.Headers.Add(header.Name, (string)header.Value);
            }
        }
        finally
        {
            stopwatch.Stop();
            Console.WriteLine($"AddResponseHeaders: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}