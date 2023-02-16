using System.Net;
using AspNetCore.Proxy;
using Microsoft.AspNetCore.Server.Kestrel.Https;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProxies();
const string myAllowSpecificOrigins = "corsPolicy";
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLettuceEncrypt();

builder.WebHost.UseUrls("http://proxy.vnxservers.com:80", "https://proxy.vnxservers.com:5000");
builder.WebHost.ConfigureKestrel(kestre =>
{
    kestre.ListenAnyIP(80);
    kestre.ListenAnyIP(443, listenOptions =>
    {
        listenOptions.UseHttps(h =>
        {
            h.UseLettuceEncrypt(kestre.ApplicationServices);
        });
    });
});

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

app.UseCors(myAllowSpecificOrigins);
app.UseSwagger();

app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();