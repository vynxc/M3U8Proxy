using AspNetCore.Proxy;
using Microsoft.Net.Http.Headers;
const string myAllowSpecificOrigins = "corsPolicy";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddResponseCaching();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProxies();

if (!builder.Environment.IsDevelopment())
{
    //builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
    //builder.Services.AddLettuceEncrypt();
    builder.WebHost.ConfigureKestrel(k =>
    {
        k.ListenAnyIP(5001);
        //k.ListenAnyIP(443, listenOptions => { listenOptions.UseHttps(); });
    });
    
}

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

//app.UseHsts();
//app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(myAllowSpecificOrigins);
app.UseResponseCaching();
app.UseEndpoints(endpoints =>
{
    endpoints.MapGet("/hello", async context =>
    {
        await context.Response.WriteAsync("Hello World!");
    });
    if(!builder.Environment.IsDevelopment()) endpoints.MapReverseProxy();
});
app.UseAuthentication();
app.MapControllers();
app.Run();