using AspNetCore.Proxy;

const string myAllowSpecificOrigins = "corsPolicy";

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProxies();

if (!builder.Environment.IsDevelopment())
    builder.WebHost.ConfigureKestrel(k => { k.ListenAnyIP(8080); });

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
    endpoints.MapGet("/hello", async context => { await context.Response.WriteAsync("Hello World!"); });
});
app.UseAuthentication();
app.MapControllers();
app.Run();