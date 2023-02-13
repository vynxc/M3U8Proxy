using System.Net;
using AspNetCore.Proxy;
using Microsoft.AspNetCore.Server.Kestrel.Https;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var MyAllowSpecificOrigins = "corsPolicy";
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLettuceEncrypt();
builder.WebHost.UseKestrel(k=>{
    
    var appServices = k.ApplicationServices;
    k.Listen(
        IPAddress.Any, 443,
        o => o.UseHttps(h =>
        {
            h.UseLettuceEncrypt(appServices);
        }));
});
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddProxies();
builder.Services.AddCors(options =>
{
    options.AddPolicy(MyAllowSpecificOrigins,
        builder =>
        {
            builder.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
                
        });
});
var app = builder.Build();
app.UseCors(MyAllowSpecificOrigins);
//app.MapReverseProxy();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();