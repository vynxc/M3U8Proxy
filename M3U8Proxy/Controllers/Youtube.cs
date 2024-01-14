using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace M3U8Proxy.Controllers;

public class Youtube : Controller
{
    private static readonly YoutubeClient YoutubeClient = new();

    [HttpGet]
    [Route("youtube/{**id}")]
    public async Task<IActionResult> GetTrailer(string id)
    {
        var url = $"https://www.youtube.com/watch?v={id}";

        var streamManifest = await YoutubeClient.Videos.Streams.GetManifestAsync(url);
        var streamInfo = streamManifest
            .GetMuxedStreams()
            .Where(s => s.Container == Container.Mp4)
            .GetWithHighestVideoQuality();
        var stream = await YoutubeClient.Videos.Streams.GetAsync(streamInfo);

        var streamLength = (int)streamInfo.Size.Bytes;

        Response.Headers["Content-Type"] = "video/mp4";
        Response.Headers["Content-Disposition"] = $"attachment; filename=\"{id}.mp4\"";
        Response.Headers["Accept-Ranges"] = "bytes";
        Response.Headers["Content-Length"] = streamLength.ToString();

        return File(stream, "video/mp4");
    }
}