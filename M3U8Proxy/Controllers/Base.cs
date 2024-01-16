using System.Reflection;
using System.Security.Cryptography;
using AspNetCore.Proxy;
using AspNetCore.Proxy.Options;
using M3U8Proxy.RequestHandler.AfterReceive;
using M3U8Proxy.RequestHandler.BeforeSend;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace M3U8Proxy.Controllers;

[EnableCors("corsPolicy")]
[ApiController]
public class Base : Controller
{
    private readonly Assembly _assembly;

    public Base()
    {
        _assembly = Assembly.GetExecutingAssembly();
    }

    [HttpHead]
    [HttpPost]
    [HttpGet]
    [Route("/{**url}")]
    public Task ProxyTest(string url)
    {
        var query = Request.QueryString;
        if (query.HasValue) url += query;
        try
        {
            var options = HttpProxyOptionsBuilder.Instance
                .WithShouldAddForwardedHeaders(false)
                .WithBeforeSend((res, hrm) =>
                {
                    BeforeSend.RemoveHeaders(hrm);
                    hrm.Headers.Remove("Host");

                    return Task.CompletedTask;
                })
                .WithHandleFailure(async (context, e) =>
                {
                    context.Response.StatusCode = context.Response.StatusCode;
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(e));
                })
                .WithAfterReceive((res, hrm) =>
                {
                    AfterReceive.RemoveHeaders(hrm);
                    hrm.Headers.Remove("Cross-Origin-Resource-Policy");
                    hrm.Headers.Add("Cross-Origin-Resource-Policy", "*");
                    return Task.CompletedTask;
                })
                .Build();
            return this.HttpProxyAsync(url, options);
        }
        catch (Exception e)
        {
            //handle errors
            HttpContext.Response.StatusCode = 400;
            HttpContext.Response.ContentType = "application/json";
            HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(e));
            return Task.FromResult(0);
        }
    }

    [Route("/video/intro.ts")]
    public IActionResult Intro(string? key)
    {
        if (key != null) return File(IntroEncrypt(FromHexString(key)), "video/mp2t");
        Console.WriteLine("key is null");
        var resourceName = "M3U8Proxy.Intro.intro.ts";
        var stream = _assembly.GetManifestResourceStream(resourceName);

        if (stream != null) return File(stream, "video/mp2t");

        return NotFound();
    }


    private MemoryStream IntroEncrypt(byte[] key)
    {
        var resourceName = "M3U8Proxy.Intro.intro.ts";
        var stream = _assembly.GetManifestResourceStream(resourceName);

        var encryptedStream = new MemoryStream();
        EncryptStream(stream, encryptedStream, key, 0); // Sequence number is 0.
        encryptedStream.Position = 0; // Reset position.
        return encryptedStream;
    }

    private byte[] FromHexString(string hexString)
    {
        var numberChars = hexString.Length;
        var bytes = new byte[numberChars / 2];
        for (var i = 0; i < numberChars; i += 2) bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
        return bytes;
    }

    private void EncryptStream(Stream inputStream, Stream outputStream, byte[] Key, long sequenceNumber)
    {
        // Check arguments. 
        if (inputStream == null)
            throw new ArgumentNullException(nameof(inputStream));
        if (outputStream == null)
            throw new ArgumentNullException(nameof(outputStream));
        if (Key == null || Key.Length != 16)
            throw new ArgumentException("Key should be 16 bytes", nameof(Key));

        // Create an AES service provider. 
        var aesAlg = Aes.Create();

        aesAlg.Key = Key;
        aesAlg.Mode = CipherMode.CBC;
        aesAlg.Padding = PaddingMode.PKCS7;

        // Use the sequence number as IV.
        var IV = new byte[16];
        var sequenceNumberBytes = BitConverter.GetBytes(sequenceNumber);
        Array.Copy(sequenceNumberBytes, IV, sequenceNumberBytes.Length);

        aesAlg.IV = IV;

        // Create a decrytor to perform the stream transform.
        var encryptor = aesAlg.CreateEncryptor();
        var csEncrypt = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);

        // Copy from input stream to output stream via crypto stream. (The data will be encrypted.)
        inputStream.CopyTo(csEncrypt);
    }
}