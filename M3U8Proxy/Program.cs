using AspNetCore.Proxy;
using M3U8Proxy.Middleware;
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
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(myAllowSpecificOrigins,
        policyBuilder =>
        {
            Console.WriteLine(allowedOrigins);
            if (allowedOrigins != null)
                policyBuilder.WithOrigins(allowedOrigins);
            else
                policyBuilder.AllowAnyOrigin();
        });
});

var app = builder.Build();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseRouting();
app.UseCors(myAllowSpecificOrigins);
app.UseOutputCache();
app.MapGet("/hello", async context => { await context.Response.WriteAsync("Hello, Bitches!"); });
app.UseAuthentication();
app.MapControllers();
app.Run();