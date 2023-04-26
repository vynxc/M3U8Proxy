using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace M3U8Proxy.Controllers;


public class Youtube: Controller
{
    private static readonly YoutubeClient YoutubeClient = new ();

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
                    
        Response.Headers["Content-Type"] = "video/mp4";

        Response.Headers["Content-Disposition"] = $"attachment; filename=\"{id}.mp4\"";

        return File(stream, "video/mp4");
    }

}