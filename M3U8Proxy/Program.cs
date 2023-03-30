using System.Diagnostics;
using AspNetCore.Proxy;
using Microsoft.AspNetCore.HttpOverrides;


const string myAllowSpecificOrigins = "corsPolicy";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("m3u8", builder =>
    {
        builder.Cache();
        builder.Expire(TimeSpan.FromSeconds(5));
    });

});
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProxies();

if (!builder.Environment.IsDevelopment())
    builder.WebHost.ConfigureKestrel(k => { k.ListenAnyIP(5001); });

builder.Services.AddCors(options =>
{
    options.AddPolicy(myAllowSpecificOrigins,
        policyBuilder =>
        {
            policyBuilder.WithOrigins("https://unime.vercel.app","https://streamable.moe","https://anistreme.live","https://hlsplayer.net/");
        });
});

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                       ForwardedHeaders.XForwardedProto
});  
app.UseRouting();
app.UseCors(myAllowSpecificOrigins);
app.UseOutputCache();

app.MapGet("/hello", async context =>
{
    await context.Response.WriteAsync("Hello, Bitches!");
});
app.UseAuthentication();
app.MapControllers();
app.Run();