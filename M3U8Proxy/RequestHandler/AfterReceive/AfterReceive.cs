namespace M3U8Proxy.RequestHandler.AfterReceive;

public static class AfterReceive
{
    public static void RemoveHeaders(HttpResponseMessage hrm)
    {
        foreach (var header in CorsBlockedHeaders.List) hrm.Headers.Remove(header.ToLower());
    }

    public static void AddForcedHeaders(Dictionary<string, string>? forcedHeadersProxyDictionary,
        HttpResponseMessage hrm)
    {
        foreach (var header in forcedHeadersProxyDictionary)
        {
            var headerToRemove =
                hrm.Content.Headers.FirstOrDefault(h =>
                    h.Key.Equals(header.Key, StringComparison.InvariantCultureIgnoreCase)).Key;

            if (headerToRemove != null)
                hrm.Content.Headers.Remove(headerToRemove);
            hrm.Content.Headers.Add(header.Key, header.Value);
        }
    }
}