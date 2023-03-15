using System.Diagnostics;
using AspNetCore.Proxy;

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
            policyBuilder.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var app = builder.Build();
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