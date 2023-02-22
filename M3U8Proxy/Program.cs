using System.Net;
using AspNetCore.Proxy;
using Microsoft.AspNetCore.Server.Kestrel.Https;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProxies();
const string myAllowSpecificOrigins = "corsPolicy";
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddLettuceEncrypt();
builder.WebHost.ConfigureKestrel(k =>
{
    k.ListenAnyIP(80);
    k.ListenAnyIP(443, listenOptions =>
    {
        listenOptions.UseHttps();

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

app.UseRouting();
app.UseCors(myAllowSpecificOrigins);
app.UseEndpoints(endpoints =>
{
    endpoints.MapReverseProxy();
});
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthentication();
app.MapControllers();
app.Run();