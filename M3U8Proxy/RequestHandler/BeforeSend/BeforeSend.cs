namespace M3U8Proxy.RequestHandler.BeforeSend;

public static class BeforeSend
{
    public static void RemoveHeaders(HttpRequestMessage hrm)
    {
        foreach (var header in CorsBlockedHeaders.List) hrm.Headers.Remove(header.ToLower());
    }

    public static void AddHeaders(Dictionary<string, string> headersDictionary, HttpRequestMessage hrm)
    {
        foreach (var header in headersDictionary)
        {
            var headerToRemove =
                hrm.Headers.FirstOrDefault(h =>
                    h.Key.Equals(header.Key, StringComparison.InvariantCultureIgnoreCase)).Key;

            if (headerToRemove != null)
                hrm.Headers.Remove(headerToRemove);

            hrm.Headers.Add(header.Key, header.Value);
        }
    }
}