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
builder.WebHost.UseUrls("https://proxy.vnxservers.com:443");

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