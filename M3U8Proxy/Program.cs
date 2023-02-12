using AspNetCore.Proxy;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var MyAllowSpecificOrigins = "corsPolicy";
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();